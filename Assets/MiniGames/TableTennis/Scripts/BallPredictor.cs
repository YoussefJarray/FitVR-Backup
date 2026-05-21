using UnityEngine;

public class BallPredictor : MonoBehaviour
{
    [Header("Trajectory Visualizer")]
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private int          steps    = 30;
    [SerializeField] private float        timeStep = 0.05f;
    [SerializeField] private bool         showLine = true;

    [Header("Table")]
    [SerializeField] private float tableY = 0.76f;

    private Rigidbody rb;

    private void Awake() => rb = GetComponent<Rigidbody>();

    private void Update()
    {
        if (!showLine || lineRenderer == null) return;
        DrawTrajectory();
    }

    private void DrawTrajectory()
    {
        Vector3 pos = transform.position;
        Vector3 vel = rb.linearVelocity;

        lineRenderer.positionCount = steps;

        for (int i = 0; i < steps; i++)
        {
            float t = i * timeStep;
            Vector3 point = new Vector3(
                pos.x + vel.x * t,
                pos.y + vel.y * t + 0.5f * Physics.gravity.y * t * t,
                pos.z + vel.z * t);

            lineRenderer.SetPosition(i, point);

            if (point.y <= tableY)
            {
                lineRenderer.positionCount = i + 1;
                break;
            }
        }
    }

    /// <summary>
    /// Predicts the XZ landing position when the ball reaches targetY.
    /// Returns false if the ball is moving away from targetY or cannot reach it.
    /// </summary>
    public bool TryPredictLanding(float targetY, out Vector3 landingPosition)
    {
        landingPosition = Vector3.zero;

        Vector3 pos = transform.position;
        Vector3 vel = rb.linearVelocity;

        // Early-out: ball is moving away from targetY
        if (Mathf.Sign(vel.y) == Mathf.Sign(pos.y - targetY))
        {
            landingPosition = pos;
            return false;
        }

        // Solve: targetY = pos.y + vel.y*t + 0.5*g*t^2
        float g   = Physics.gravity.y;
        float a   = 0.5f * g;
        float b   = vel.y;
        float c   = -(targetY - pos.y);

        float discriminant = b * b - 4f * a * c;
        if (discriminant < 0f) return false;

        float sqrtDisc = Mathf.Sqrt(discriminant);
        float t1 = (-b + sqrtDisc) / (2f * a);
        float t2 = (-b - sqrtDisc) / (2f * a);

        // Pick the smallest positive time (soonest future landing)
        float t = -1f;
        if      (t1 > 0f && t2 > 0f) t = Mathf.Min(t1, t2);
        else if (t1 > 0f)             t = t1;
        else if (t2 > 0f)             t = t2;

        if (t < 0f) return false;

        landingPosition = new Vector3(
            pos.x + vel.x * t,
            targetY,
            pos.z + vel.z * t);

        return true;
    }

    public Vector3 Velocity => rb.linearVelocity;
}