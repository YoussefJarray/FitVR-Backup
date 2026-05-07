using UnityEngine;

public class TableTennisAI : MonoBehaviour
{
    private Rigidbody myRb;
    private GameObject currentBall;
    
    [Header("Settings")]
    public float paddleSpeed = 8f;
    public float hitPower = 12f;
    public float reactionDelay = 0.1f;

    void Start() => myRb = GetComponent<Rigidbody>();

    void FixedUpdate()
    {
        if (currentBall == null) currentBall = GameObject.FindWithTag("Ball");
        if (currentBall == null) return;

        Rigidbody ballRb = currentBall.GetComponent<Rigidbody>();
        bool isMovingTowardMe = ballRb.linearVelocity.z > 0;

        // 1. AI Serving Logic
        if (!TableTennisGameManager.Instance.isPlayerTurnToServe && TableTennisGameManager.Instance.lastTouch == TableTennisGameManager.LastHit.None)
        {
            PerformServe(ballRb);
        }
        // 2. Defensive/Rally Logic
        else if (isMovingTowardMe)
        {
            // Predict intercept point at AI's current Z
            float time = Mathf.Abs((transform.position.z - currentBall.transform.position.z) / ballRb.linearVelocity.z);
            Vector3 predictedPos = currentBall.transform.position + (ballRb.linearVelocity * time);
            
            // Constrain within table width
            float limit = TableTennisGameManager.Instance.cpuSideCollider.bounds.extents.x;
            predictedPos.x = Mathf.Clamp(predictedPos.x, -limit, limit);
            predictedPos.y = Mathf.Clamp(predictedPos.y, 1.0f, 1.5f); // Stay within reach

            // Only "strike" if it's hit the table or is very close
            if (TableTennisGameManager.Instance.isServicePhase || TableTennisGameManager.Instance.lastTouch == TableTennisGameManager.LastHit.Player)
            {
                MovePaddle(new Vector3(predictedPos.x, predictedPos.y, transform.position.z));
            }
        }
        else
        {
            // Return to center idle
            MovePaddle(new Vector3(0, 1.2f, transform.position.z));
        }
    }

    void MovePaddle(Vector3 target)
    {
        myRb.MovePosition(Vector3.MoveTowards(myRb.position, target, paddleSpeed * Time.fixedDeltaTime));
    }

    void PerformServe(Rigidbody ballRb)
    {
        // Serve: Hit own side first
        Vector3 target = TableTennisGameManager.Instance.cpuSideCollider.bounds.center;
        HitBall(ballRb, target, hitPower * 0.7f);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ball"))
        {
            // Reset bounce flag because AI just hit it
            TableTennisGameManager.Instance.ballHasBouncedOnValidSide = false;
            TableTennisGameManager.Instance.lastTouch = TableTennisGameManager.LastHit.CPU;

            // Aim for player side
            Bounds b = TableTennisGameManager.Instance.playerSideCollider.bounds;
            Vector3 target = new Vector3(Random.Range(b.min.x, b.max.x), b.center.y, b.center.z);
            HitBall(collision.rigidbody, target, hitPower);
        }
    }

    void HitBall(Rigidbody rb, Vector3 target, float power)
    {
        Vector3 dir = (target - transform.position).normalized;
        dir.y += 0.15f; // Add arc
        rb.linearVelocity = Vector3.zero;
        rb.AddForce(dir * power, ForceMode.Impulse);
    }
}