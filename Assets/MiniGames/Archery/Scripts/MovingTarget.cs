using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IHittable
{
    void GetHit();
}

public class MovingTarget : MonoBehaviour, IHittable
{
    private Rigidbody rb;
    private bool stopped = false;
    private Vector3 nextPosition;
    private Vector3 originPosition;
    private Vector3 pointA;
    private Vector3 pointB;
    private bool movingToB = true;
    private int currentWaypointIndex = 0;

    public static event Action<int> OnTargetHit;

    public enum MovementMode { BackAndForthAxis, SequentialPositions }

    [Header("--- MOVEMENT MODE ---")]
    [SerializeField] private MovementMode movementMode = MovementMode.BackAndForthAxis;

    [Header("--- LOOK AT PLAYER ---")]
    [SerializeField] private bool lookAtPlayer = true;
    [SerializeField] private Transform playerTransform;

    [Header("--- AXIS SETTINGS (Origin is Middle) ---")]
    [SerializeField] private bool moveX = true;
    [SerializeField] private bool moveY = false;
    [SerializeField] private bool moveZ = false;
    [SerializeField] private float travelDistance = 2f;

    [Header("--- SEQUENTIAL SETTINGS ---")]
    [SerializeField] private Vector3[] waypoints;
    [SerializeField] private bool relativeToStart = true;

    [Header("--- SCORING (Proximity Based) ---")]
    [Tooltip("The total radius of the target rings.")]
    [SerializeField] private float targetRadius = 0.5f; 
    [SerializeField] private int maxScore = 100;
    [SerializeField] private int minScore = 10;

    [Header("--- TARGET PROPERTIES ---")]
    [SerializeField] private float speed = 1f;
    [SerializeField] private float arriveThreshold = 0.05f;
    [SerializeField] private int health = 1;
    [SerializeField] private AudioSource audioSource;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        originPosition = transform.position;
        SetupTargetLogic();
    }

    private void SetupTargetLogic()
    {
        if (movementMode == MovementMode.BackAndForthAxis)
        {
            Vector3 direction = Vector3.zero;
            if (moveX) direction.x = 1;
            if (moveY) direction.y = 1;
            if (moveZ) direction.z = 1;

            pointA = originPosition - (direction.normalized * travelDistance);
            pointB = originPosition + (direction.normalized * travelDistance);
            nextPosition = pointB;
        }
        else if (waypoints != null && waypoints.Length > 0)
        {
            nextPosition = GetWaypointPosition(0);
        }
    }

    private Vector3 GetWaypointPosition(int index)
    {
        return relativeToStart ? originPosition + waypoints[index] : waypoints[index];
    }

    public void SetSpeed(float newSpeed) => speed = newSpeed;
    public void SetPlayerTransform(Transform player) => playerTransform = player;

    public void StartLifetime(float duration)
    {
        StartCoroutine(LifetimeRoutine(duration));
    }

    private IEnumerator LifetimeRoutine(float duration)
    {
        yield return new WaitForSeconds(duration);
        if (!stopped) Destroy(gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Arrow") && !stopped)
        {
            stopped = true; // Lock immediately to prevent multiple hits

            ContactPoint contact = collision.contacts[0];
            float hitDistance = Vector3.Distance(contact.point, transform.position);
            
            float scorePercent = Mathf.Clamp01(1 - (hitDistance / targetRadius));
            int calculatedScore = Mathf.RoundToInt(Mathf.Lerp(minScore, maxScore, scorePercent));

            if (audioSource != null) audioSource.Play();
            
            OnTargetHit?.Invoke(calculatedScore);
            
            // Delay the physics release slightly so the collision registers properly
            Invoke(nameof(ProcessHitFall), 0.02f);
        }
    }

    private void ProcessHitFall()
    {
        health--;
        if (health <= 0)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            StartCoroutine(DespawnAfterDelay(3f));
        }
        else
        {
            // If it has more health, let it keep moving
            stopped = false;
        }
    }

    public void GetHit() { /* Manual trigger if needed */ }

    private IEnumerator DespawnAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (gameObject != null) Destroy(gameObject);
    }

    private void FixedUpdate()
    {
        if (!stopped)
        {
            float distance = Vector3.Distance(transform.position, nextPosition);
            if (distance < arriveThreshold) CycleTarget();

            Vector3 moveStep = Vector3.MoveTowards(transform.position, nextPosition, speed * Time.fixedDeltaTime);
            rb.MovePosition(moveStep);

            if (lookAtPlayer && playerTransform != null)
                transform.LookAt(playerTransform);
        }
    }

    private void CycleTarget()
    {
        if (movementMode == MovementMode.BackAndForthAxis)
        {
            movingToB = !movingToB;
            nextPosition = movingToB ? pointB : pointA;
        }
        else if (waypoints != null && waypoints.Length > 0)
        {
            currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
            nextPosition = GetWaypointPosition(currentWaypointIndex);
        }
    }
}