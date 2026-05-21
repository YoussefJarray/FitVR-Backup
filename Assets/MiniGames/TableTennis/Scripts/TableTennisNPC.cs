using System.Collections;
using UnityEngine;

public class TableTennisNPC : MonoBehaviour
{
    // ── Difficulty ────────────────────────────────────────────────
    public enum Difficulty { Easy, Medium, Hard }

    [Header("Difficulty")]
    [SerializeField] private Difficulty difficulty = Difficulty.Medium;

    private float MissChance => difficulty switch
    {
        Difficulty.Easy   => 0.45f,
        Difficulty.Medium => 0.15f,
        Difficulty.Hard   => 0f,
        _                 => 0.15f
    };

    private float AimError => difficulty switch
    {
        Difficulty.Easy   => 0.25f,
        Difficulty.Medium => 0.08f,
        Difficulty.Hard   => 0f,
        _                 => 0.08f
    };

    private float ReactionTime => difficulty switch
    {
        Difficulty.Easy   => 0.6f,
        Difficulty.Medium => 0.3f,
        Difficulty.Hard   => 0.05f,
        _                 => 0.3f
    };

    // ── References ────────────────────────────────────────────────
    [Header("References")]
    [SerializeField] private BallPredictor ball;
    [SerializeField] private Transform     readyPosition;

    [Header("Table Bounds")]
    [SerializeField] private float tableY          = 0.76f;
    [SerializeField] private float npcTableMinZ    = 0.05f;
    [SerializeField] private float npcTableMaxZ    = 1.37f;
    [SerializeField] private float playerTableMinZ = -1.37f;
    [SerializeField] private float playerTableMaxZ = -0.05f;
    [SerializeField] private float tableHalfWidth  = 0.7625f;

    [Header("Movement")]
    [SerializeField] private float moveSpeed     = 4f;
    [SerializeField] private float swingDuration = 0.12f;

    [Header("Hit")]
    [SerializeField] private float hitForce   = 6f;
    [SerializeField] private float hitUpAngle = 15f;

    // ── FSM ───────────────────────────────────────────────────────
    private enum State { Idle, Predict, Move, Hit, Return, WaitingAfterServe }
    private State currentState = State.Idle;

    private Vector3 targetPosition;
    private bool    willMiss;
    private bool    isSwinging;

    // ── Unity ─────────────────────────────────────────────────────
    private void Update()
    {
        switch (currentState)
        {
            case State.Idle:   UpdateIdle();   break;
            case State.Move:   UpdateMove();   break;
            case State.Return: UpdateReturn(); break;
        }
    }

    // ── State updates ─────────────────────────────────────────────
    private void UpdateIdle()
    {
        if (ball == null) return;
        if (IsBallOnNPCSide() && ball.Velocity.z > 0)
            TransitionTo(State.Predict);
    }

    private void UpdateMove()
    {
        transform.position = Vector3.MoveTowards(
            transform.position, targetPosition, moveSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, targetPosition) < 0.02f)
            TransitionTo(State.Hit);
    }

    private void UpdateReturn()
    {
        transform.position = Vector3.MoveTowards(
            transform.position, readyPosition.position, moveSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, readyPosition.position) < 0.02f)
            TransitionTo(State.Idle);
    }

    // ── Transitions ───────────────────────────────────────────────
    private void TransitionTo(State next)
    {
        currentState = next;
        if (next == State.Predict) StartCoroutine(PredictRoutine());
        if (next == State.Hit)     StartCoroutine(HitRoutine());
    }

    // ── Predict ───────────────────────────────────────────────────
    private IEnumerator PredictRoutine()
    {
        yield return new WaitForSeconds(ReactionTime);

        willMiss = Random.value < MissChance;

        if (ball.TryPredictLanding(tableY, out Vector3 landing))
        {
            landing.x = Mathf.Clamp(landing.x, -tableHalfWidth, tableHalfWidth);
            landing.z = Mathf.Clamp(landing.z, npcTableMinZ, npcTableMaxZ);

            landing.x += Random.Range(-AimError, AimError);
            landing.z += Random.Range(-AimError, AimError);

            if (willMiss)
                landing += new Vector3(
                    Random.Range(-0.5f, 0.5f), 0f, Random.Range(-0.4f, 0.4f));

            targetPosition = new Vector3(landing.x, tableY + 0.15f, landing.z);
        }
        else
        {
            targetPosition = readyPosition.position;
        }

        TransitionTo(State.Move);
    }

    // ── Hit ───────────────────────────────────────────────────────
    private IEnumerator HitRoutine()
    {
        isSwinging = true;

        if (!willMiss)
        {
            Vector3 swingTarget = ball.transform.position;
            Vector3 startPos    = transform.position;
            float   elapsed     = 0f;

            while (elapsed < swingDuration)
            {
                elapsed           += Time.deltaTime;
                float t            = Mathf.Clamp01(elapsed / swingDuration);
                transform.position = Vector3.Lerp(startPos, swingTarget, t);
                yield return null;
            }

            Rigidbody ballRb = ball.GetComponent<Rigidbody>();
            if (ballRb != null)
            {
                Vector3 aimSpot = new Vector3(
                    Random.Range(-tableHalfWidth, tableHalfWidth),
                    tableY,
                    Random.Range(playerTableMinZ, playerTableMaxZ));

                Vector3 hitDir = (aimSpot - ball.transform.position).normalized;
                hitDir.y += Mathf.Tan(hitUpAngle * Mathf.Deg2Rad);
                hitDir    = hitDir.normalized;

                ballRb.linearVelocity = hitDir * hitForce;

                TableTennisGameManager.Instance?.RegisterHit(TableTennisGameManager.LastHit.CPU);
            }
        }

        isSwinging = false;
        yield return new WaitForSeconds(0.2f);
        TransitionTo(State.Return);
    }

    // ── Helpers ───────────────────────────────────────────────────
    private bool IsBallOnNPCSide()
    {
        return ball != null && ball.transform.position.z >= npcTableMinZ;
    }

    // ── Public API ────────────────────────────────────────────────

    /// <summary>Called by NetRelay when ball crosses to NPC side mid-rally.</summary>
    public void OnBallCrossedToNPCSide()
    {
        if (currentState == State.Idle)
            TransitionTo(State.Predict);
    }

    /// <summary>
    /// Called by the game manager right after the NPC launches its serve.
    /// Puts the NPC in a short wait so it doesn't try to intercept its own serve.
    /// </summary>
    public void OnNPCServed()
    {
        StopAllCoroutines();
        currentState = State.WaitingAfterServe;
        StartCoroutine(PostServeWait());
    }

    private IEnumerator PostServeWait()
    {
        // Wait long enough for the ball to leave the NPC side and bounce once
        yield return new WaitForSeconds(1.5f);
        TransitionTo(State.Idle);
    }

    public void SetDifficulty(Difficulty d) => difficulty = d;
}