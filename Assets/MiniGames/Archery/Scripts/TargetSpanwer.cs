using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns targets inside a BoxCollider zone using weighted random selection.
/// Enable/disable this component to start/stop the spawn loop.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class TargetSpawner : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────
    //  Data types
    // ─────────────────────────────────────────────────────────────
    [System.Serializable]
    public struct TargetSettings
    {
        [Tooltip("Label shown in the Inspector for organisation only.")]
        public string label;

        public GameObject prefab;

        [Range(0f, 100f)]
        [Tooltip("Relative spawn weight. Higher = spawns more often.")]
        public float spawnWeight;

        [Tooltip("Lifetime in seconds. 0 = use global default.")]
        public float customLifetime;

        [Tooltip("Movement speed override. 0 = use prefab's default.")]
        public float customSpeed;
    }

    // ─────────────────────────────────────────────────────────────
    //  Inspector
    // ─────────────────────────────────────────────────────────────

    [Header("─── Target Pool ──────────────────────────")]
    [SerializeField] private List<TargetSettings> targetPool = new();

    [Header("─── Spawn Settings ───────────────────────")]
    [SerializeField] private float spawnInterval = 2f;
    [SerializeField] private int maxActiveTargets = 5;
    [SerializeField] private float defaultLifetime = 10f;

    [Header("─── Facing ──────────────────────────────")]
    [Tooltip("Targets will rotate to face this on spawn. Leave blank to auto-use Camera.main.")]
    [SerializeField] private Transform playerCamera;

    // ─────────────────────────────────────────────────────────────
    //  Private state
    // ─────────────────────────────────────────────────────────────
    private BoxCollider spawnZone;
    private List<GameObject> activeTargets = new();
    private Coroutine spawnRoutine;
    private bool ready = false;  // must call Activate() before spawning starts

    // ─────────────────────────────────────────────────────────────
    //  Unity lifecycle
    // ─────────────────────────────────────────────────────────────
    private void Awake()
    {
        spawnZone = GetComponent<BoxCollider>();
        spawnZone.isTrigger = true;

        if (playerCamera == null && Camera.main != null)
            playerCamera = Camera.main.transform;
    }

    private void OnEnable()
    {
        // Do nothing if Activate() hasn't been called yet.
        // This prevents a stray spawn during the first frame before the game manager
        // has a chance to disable the spawner in its Start().
        if (!ready) return;

        if (targetPool.Count == 0)
        {
            Debug.LogError($"[TargetSpawner] Target pool is empty on '{gameObject.name}'. " +
                           "Add at least one entry before enabling.");
            return;
        }
        spawnRoutine = StartCoroutine(SpawnLoop());
    }

    /// <summary>
    /// Call this once (from ArcheryGameManager.StartGame) to allow spawning.
    /// After this, enabling/disabling the component starts/stops the loop as normal.
    /// </summary>
    public void Activate()
    {
        ready = true;
        enabled = true; // triggers OnEnable which starts the SpawnLoop
    }

    /// <summary>Deactivate and reset — call on restart.</summary>
    public void Deactivate()
    {
        ready = false;
        enabled = false;
        if (spawnRoutine != null) { StopCoroutine(spawnRoutine); spawnRoutine = null; }
        ClearAllTargets();
    }

    private void OnDisable()
    {
        if (spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);
            spawnRoutine = null;
        }
    }

    // ─────────────────────────────────────────────────────────────
    //  Spawn loop
    // ─────────────────────────────────────────────────────────────
    private IEnumerator SpawnLoop()
    {
        // Wait before the very first spawn. This prevents a target slipping through
        // during the one-frame window between OnEnable and ArcheryGameManager.Start
        // setting spawner.enabled = false.
        yield return new WaitForSeconds(spawnInterval);

        while (true)
        {
            activeTargets.RemoveAll(t => t == null);

            if (activeTargets.Count < maxActiveTargets)
                SpawnTarget();

            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private void SpawnTarget()
    {
        if (!TryGetWeightedTarget(out TargetSettings settings)) return;
        if (settings.prefab == null)
        {
            Debug.LogWarning($"[TargetSpawner] Selected entry '{settings.label}' has no prefab assigned.");
            return;
        }

        Vector3 spawnPoint = GetRandomPointInBounds(spawnZone.bounds);
        Quaternion spawnRot = GetFacingRotation(spawnPoint);
        GameObject instance = Instantiate(settings.prefab, spawnPoint, spawnRot);
        activeTargets.Add(instance);

        // Configure the MovingTarget component if present
        if (instance.TryGetComponent(out MovingTarget target))
        {
            float lifetime = settings.customLifetime > 0f ? settings.customLifetime : defaultLifetime;
            target.StartLifetime(lifetime);

            if (settings.customSpeed > 0f)
                target.SetSpeed(settings.customSpeed);
        }
    }

    // ─────────────────────────────────────────────────────────────
    //  Facing
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns a rotation so the spawned target faces the player.
    /// Falls back to identity if no camera is available.
    /// </summary>
    private Quaternion GetFacingRotation(Vector3 spawnPoint)
    {
        // Late-resolve in case XR rig wasn't ready in Awake
        if (playerCamera == null && Camera.main != null)
            playerCamera = Camera.main.transform;

        if (playerCamera == null) return Quaternion.identity;

        // Flatten both positions to the same Y before calculating direction.
        // This prevents the target tilting up/down due to the XR camera being at head height.
        Vector3 cameraFlat = new Vector3(playerCamera.position.x, spawnPoint.y, playerCamera.position.z);
        Vector3 dir = cameraFlat - spawnPoint;

        if (dir.sqrMagnitude < 0.001f) return Quaternion.identity;

        return Quaternion.LookRotation(dir);
    }

    // ─────────────────────────────────────────────────────────────
    //  Weighted random selection
    // ─────────────────────────────────────────────────────────────
    private bool TryGetWeightedTarget(out TargetSettings result)
    {
        float totalWeight = 0f;
        foreach (var t in targetPool) totalWeight += Mathf.Max(0f, t.spawnWeight);

        if (totalWeight <= 0f)
        {
            Debug.LogWarning("[TargetSpawner] All spawn weights are 0. Using first entry.");
            result = targetPool[0];
            return true;
        }

        float roll = Random.Range(0f, totalWeight);
        float running = 0f;

        foreach (var t in targetPool)
        {
            running += Mathf.Max(0f, t.spawnWeight);
            if (roll <= running)
            {
                result = t;
                return true;
            }
        }

        result = targetPool[0];
        return true;
    }

    // ─────────────────────────────────────────────────────────────
    //  Utility
    // ─────────────────────────────────────────────────────────────
    private static Vector3 GetRandomPointInBounds(Bounds b)
    {
        return new Vector3(
            Random.Range(b.min.x, b.max.x),
            Random.Range(b.min.y, b.max.y),
            Random.Range(b.min.z, b.max.z));
    }

    /// <summary>Destroys all currently tracked targets immediately.</summary>
    public void ClearAllTargets()
    {
        foreach (var t in activeTargets)
            if (t != null) Destroy(t);
        activeTargets.Clear();
    }

    // ─────────────────────────────────────────────────────────────
    //  Editor Gizmos
    // ─────────────────────────────────────────────────────────────
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        BoxCollider box = GetComponent<BoxCollider>();
        if (box == null) return;

        // Draw a filled, semi-transparent box for the spawn zone
        Matrix4x4 oldMatrix = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.TRS(
            transform.TransformPoint(box.center),
            transform.rotation,
            transform.lossyScale);

        Gizmos.color = new Color(0.2f, 0.9f, 0.2f, 0.08f);
        Gizmos.DrawCube(Vector3.zero, box.size);

        Gizmos.color = new Color(0.2f, 0.9f, 0.2f, 0.6f);
        Gizmos.DrawWireCube(Vector3.zero, box.size);

        Gizmos.matrix = oldMatrix;
    }
#endif
}