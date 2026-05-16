using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class TargetSpawner : MonoBehaviour
{
    [System.Serializable]
    public struct TargetSettings
    {
        public string label; // For organization in Inspector
        public GameObject prefab;
        [Range(0, 100)] public float spawnChance; // Weighting for this specific target
        public float customLifetime;
        public float customSpeed;
    }

    [Header("Target Variety")]
    [SerializeField] private List<TargetSettings> targetPool = new List<TargetSettings>();

    [Header("Spawn Logic")]
    [SerializeField] private float spawnDelay = 2f;
    [SerializeField] private int maxTargets = 5;

    [Header("Global References")]
    [SerializeField] private Transform playerTransform;

    private BoxCollider spawnZone;
    private List<GameObject> activeTargets = new List<GameObject>();

    private void Awake()
    {
        spawnZone = GetComponent<BoxCollider>();
        spawnZone.isTrigger = true;
    }

    private void Start()
    {
        if (targetPool.Count == 0)
        {
            Debug.LogError("Target Pool is empty on " + gameObject.name);
            return;
        }
        StartCoroutine(SpawnRoutine());
    }

    private IEnumerator SpawnRoutine()
    {
        while (true)
        {
            activeTargets.RemoveAll(item => item == null);

            if (activeTargets.Count < maxTargets)
            {
                SpawnRandomTarget();
            }

            yield return new WaitForSeconds(spawnDelay);
        }
    }

    private void SpawnRandomTarget()
    {
        TargetSettings settings = GetRandomTargetByWeight();
        
        if (settings.prefab == null) return;

        Vector3 spawnPoint = GetRandomPointInBounds(spawnZone.bounds);
        GameObject newTarget = Instantiate(settings.prefab, spawnPoint, Quaternion.identity);
        
        activeTargets.Add(newTarget);

        // Inject granular settings into the target
        MovingTarget targetScript = newTarget.GetComponent<MovingTarget>();
        if (targetScript != null)
        {
            targetScript.SetPlayerTransform(playerTransform);
            targetScript.StartLifetime(settings.customLifetime > 0 ? settings.customLifetime : 10f);
            
            // Override speed if specified (ensure speed is public or has a setter in MovingTarget)
            if (settings.customSpeed > 0)
            {
                targetScript.SetSpeed(settings.customSpeed);
            }
        }
    }

    private TargetSettings GetRandomTargetByWeight()
    {
        float totalWeight = 0;
        foreach (var item in targetPool) totalWeight += item.spawnChance;

        float randomValue = Random.Range(0, totalWeight);
        float currentWeight = 0;

        foreach (var item in targetPool)
        {
            currentWeight += item.spawnChance;
            if (randomValue <= currentWeight) return item;
        }

        return targetPool[0];
    }

    private Vector3 GetRandomPointInBounds(Bounds bounds)
    {
        return new Vector3(
            Random.Range(bounds.min.x, bounds.max.x),
            Random.Range(bounds.min.y, bounds.max.y),
            Random.Range(bounds.min.z, bounds.max.z)
        );
    }
}