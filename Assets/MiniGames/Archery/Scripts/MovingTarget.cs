using System;
using System.Collections;
using UnityEngine;

public interface IHittable
{
    void GetHit();
}

/// <summary>
/// Controls target movement (Back-and-Forth or Sequential Waypoints),
/// proximity-based scoring, VFX on hit, and self-despawning.
/// </summary>
public class MovingTarget : MonoBehaviour, IHittable
{
    // ─────────────────────────────────────────────────────────────
    //  Events
    // ─────────────────────────────────────────────────────────────
    public static event Action<int> OnTargetHit;

    // ─────────────────────────────────────────────────────────────
    //  Enums
    // ─────────────────────────────────────────────────────────────
    public enum MovementMode { BackAndForthAxis, SequentialWaypoints }
    public enum EasingMode   { Linear, SmoothStep, EaseIn, EaseOut }

    // ─────────────────────────────────────────────────────────────
    //  Inspector fields
    // ─────────────────────────────────────────────────────────────

    [Header("─── Movement Mode ───────────────────────")]
    [SerializeField] private MovementMode movementMode = MovementMode.BackAndForthAxis;
    [SerializeField] private EasingMode   easingMode   = EasingMode.SmoothStep;

    [Header("─── Back-And-Forth (Axis) Settings ──────")]
    [Tooltip("Axis along which the target oscillates.")]
    [SerializeField] private bool moveX = true;
    [SerializeField] private bool moveY = false;
    [SerializeField] private bool moveZ = false;
    [Tooltip("Distance from origin to each endpoint (total swing = 2 × travelDistance).")]
    [SerializeField] private float travelDistance = 2f;

    [Header("─── Sequential Waypoint Settings ─────────")]
    [SerializeField] private Vector3[] waypoints;
    [Tooltip("When true, waypoints are offsets from the spawn position.")]
    [SerializeField] private bool relativeToStart = true;
    [Tooltip("When true, the target reverses direction instead of looping back to index 0.")]
    [SerializeField] private bool pingPongWaypoints = false;

    [Header("─── Target Properties ────────────────────")]
    [SerializeField] private float speed          = 1f;
    [SerializeField] private float arriveThreshold = 0.05f;
    [SerializeField] private int   health         = 1;

    [Header("─── Scoring (Proximity-Based) ────────────")]
    [Tooltip("Radius of the outermost scoring ring.")]
    [SerializeField] private float targetRadius = 0.5f;
    [SerializeField] private int   maxScore     = 100;
    [SerializeField] private int   minScore     = 10;

    [Header("─── VFX ─────────────────────────────────")]
    [Tooltip("Particle system prefab spawned at the hit point.")]
    [SerializeField] private GameObject hitVFXPrefab;
    [Tooltip("Seconds after the arrow hits before the VFX plays.")]
    [SerializeField] private float vfxDelay = 2f;
    [Tooltip("How long the VFX lives before being destroyed (set to match your particle Duration).")]
    [SerializeField] private float vfxLifetime = 3f;

    [Header("─── Audio ───────────────────────────────")]
    [SerializeField] private AudioSource audioSource;

    // ─────────────────────────────────────────────────────────────
    //  Private state
    // ─────────────────────────────────────────────────────────────
    private Rigidbody rb;
    private bool      stopped;

    // Back-and-forth
    private Vector3 originPosition;
    private Vector3 pointA;
    private Vector3 pointB;
    private bool    movingToB = true;

    // Sequential
    private int  currentWaypointIndex;
    private int  waypointDirection = 1; // +1 forward, -1 reverse (ping-pong)

    // Smoothing
    private Vector3 currentTarget;

    // ─────────────────────────────────────────────────────────────
    //  Unity lifecycle
    // ─────────────────────────────────────────────────────────────
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity  = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        // Auto-fetch AudioSource if not assigned in Inspector
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        originPosition = transform.position;
        SetupMovement();
    }

    // ─────────────────────────────────────────────────────────────
    //  Setup
    // ─────────────────────────────────────────────────────────────
    private void SetupMovement()
    {
        if (movementMode == MovementMode.BackAndForthAxis)
        {
            Vector3 dir = Vector3.zero;
            if (moveX) dir.x = 1f;
            if (moveY) dir.y = 1f;
            if (moveZ) dir.z = 1f;

            if (dir == Vector3.zero) dir = Vector3.right; // Fallback: prevent zero-vector

            dir = dir.normalized;
            pointA = originPosition - dir * travelDistance;
            pointB = originPosition + dir * travelDistance;
            currentTarget = pointB;
        }
        else
        {
            if (waypoints != null && waypoints.Length > 0)
            {
                currentTarget = ResolveWaypoint(0);
            }
        }
    }

    private Vector3 ResolveWaypoint(int index)
    {
        return relativeToStart
            ? originPosition + waypoints[index]
            : waypoints[index];
    }

    // ─────────────────────────────────────────────────────────────
    //  Public API (called by TargetSpawner)
    // ─────────────────────────────────────────────────────────────
    public void SetSpeed(float newSpeed) => speed = Mathf.Max(0f, newSpeed);

    public void StartLifetime(float duration)
    {
        if (duration > 0f)
            StartCoroutine(LifetimeRoutine(duration));
    }

    // ─────────────────────────────────────────────────────────────
    //  Lifetime
    // ─────────────────────────────────────────────────────────────
    private IEnumerator LifetimeRoutine(float duration)
    {
        yield return new WaitForSeconds(duration);
        if (!stopped) DestroySelf();
    }

    // ─────────────────────────────────────────────────────────────
    //  Collision / Hit
    // ─────────────────────────────────────────────────────────────
    private void OnCollisionEnter(Collision collision)
    {
        if (!collision.gameObject.CompareTag("Arrow") || stopped) return;

        stopped = true;

        // --- Proximity score ---
        ContactPoint contact  = collision.contacts[0];
        float        hitDist  = Vector3.Distance(contact.point, transform.position);
        float        percent  = Mathf.Clamp01(1f - (hitDist / targetRadius));
        int          score    = Mathf.RoundToInt(Mathf.Lerp(minScore, maxScore, percent));

        if (audioSource != null) audioSource.Play();
        OnTargetHit?.Invoke(score);

        // --- VFX (delayed) ---
        if (hitVFXPrefab != null)
            StartCoroutine(SpawnVFXDelayed(contact.point, contact.normal));

        // --- Physics release (slight delay so arrow embeds properly) ---
        Invoke(nameof(ProcessHit), 0.02f);
    }

    private void ProcessHit()
    {
        health--;
        if (health <= 0)
        {
            rb.isKinematic = false;
            rb.useGravity  = true;
            StartCoroutine(DespawnAfterDelay(3f));
        }
        else
        {
            stopped = false; // Still alive — resume movement
        }
    }

    // IHittable manual trigger (e.g., debug or game events)
    public void GetHit()
    {
        if (!stopped)
        {
            stopped = true;
            OnTargetHit?.Invoke(minScore);
            Invoke(nameof(ProcessHit), 0.02f);
        }
    }

    // ─────────────────────────────────────────────────────────────
    //  VFX
    // ─────────────────────────────────────────────────────────────
    private IEnumerator SpawnVFXDelayed(Vector3 position, Vector3 normal)
    {
        yield return new WaitForSeconds(vfxDelay);
        GameObject vfx = Instantiate(hitVFXPrefab, position, Quaternion.LookRotation(normal));
        Destroy(vfx, vfxLifetime);
    }

    // ─────────────────────────────────────────────────────────────
    //  Movement (FixedUpdate)
    // ─────────────────────────────────────────────────────────────
    private void FixedUpdate()
    {
        if (stopped) return;

        float dist = Vector3.Distance(transform.position, currentTarget);
        if (dist < arriveThreshold)
        {
            CycleTarget();
            return; // Skip move this frame to avoid overshoot
        }

        float t        = Mathf.Clamp01(1f - (dist / GetMaxDistance()));
        float eased    = ApplyEasing(t, easingMode);
        float thisStep = Mathf.Lerp(speed * 0.5f, speed, eased) * Time.fixedDeltaTime;

        Vector3 newPos = Vector3.MoveTowards(transform.position, currentTarget, thisStep);
        rb.MovePosition(newPos);
    }

    private void CycleTarget()
    {
        if (movementMode == MovementMode.BackAndForthAxis)
        {
            movingToB     = !movingToB;
            currentTarget = movingToB ? pointB : pointA;
        }
        else if (waypoints != null && waypoints.Length > 0)
        {
            if (pingPongWaypoints)
            {
                currentWaypointIndex += waypointDirection;
                if (currentWaypointIndex >= waypoints.Length - 1 ||
                    currentWaypointIndex <= 0)
                {
                    waypointDirection = -waypointDirection;
                    currentWaypointIndex = Mathf.Clamp(currentWaypointIndex, 0, waypoints.Length - 1);
                }
            }
            else
            {
                currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
            }
            currentTarget = ResolveWaypoint(currentWaypointIndex);
        }
    }

    // Returns the full span length for the current mode (used for easing normalisation)
    private float GetMaxDistance()
    {
        if (movementMode == MovementMode.BackAndForthAxis)
            return travelDistance * 2f;

        return (waypoints != null && waypoints.Length > 1)
            ? Vector3.Distance(ResolveWaypoint(0), ResolveWaypoint(1))
            : 1f;
    }

    // ─────────────────────────────────────────────────────────────
    //  Easing
    // ─────────────────────────────────────────────────────────────
    private static float ApplyEasing(float t, EasingMode mode)
    {
        return mode switch
        {
            EasingMode.SmoothStep => t * t * (3f - 2f * t),
            EasingMode.EaseIn     => t * t,
            EasingMode.EaseOut    => 1f - (1f - t) * (1f - t),
            _                     => t,  // Linear
        };
    }

    // ─────────────────────────────────────────────────────────────
    //  Despawn helpers
    // ─────────────────────────────────────────────────────────────
    private IEnumerator DespawnAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        DestroySelf();
    }

    private void DestroySelf()
    {
        if (gameObject != null) Destroy(gameObject);
    }
}