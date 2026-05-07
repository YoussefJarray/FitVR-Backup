using UnityEngine;
using TMPro;
using System.Collections;

public class TableTennisGameManager : MonoBehaviour
{
    public static TableTennisGameManager Instance;

    [Header("UI & Debug")]
    public TextMeshProUGUI statusDisplay;
    public TextMeshProUGUI scoreDisplay;

    [Header("Table Colliders")]
    public Collider playerSideCollider;
    public Collider cpuSideCollider;
    public Collider netCollider;
    public Collider floorCollider;

    [Header("Ball Setup")]
    public GameObject ballPrefab;
    public Transform playerServePoint;
    public Transform cpuServePoint;
    
    [Header("GameState")]
    public int playerScore = 0;
    public int cpuScore = 0;
    public bool isPlayerTurnToServe = true;
    
    public enum LastHit { None, Player, CPU }
    public LastHit lastTouch = LastHit.None;
    public bool ballHasBouncedOnValidSide = false;
    public bool isServicePhase = true;

    private GameObject currentBall;

    void Awake() => Instance = this;

    void Start()
    {
        UpdateUI();
        ResetBall();
    }

    public void RegisterCollision(Collider hitCollider, GameObject ball)
    {
        if (hitCollider == netCollider)
        {
            LogState("Net Hit! Point to " + (lastTouch == LastHit.Player ? "CPU" : "Player"));
            AwardPoint(lastTouch == LastHit.Player ? "CPU" : "Player");
        }
        else if (hitCollider == floorCollider)
        {
            LogState("Out of Bounds! Point to " + (lastTouch == LastHit.Player ? "CPU" : "Player"));
            AwardPoint(lastTouch == LastHit.Player ? "CPU" : "Player");
        }
        else if (hitCollider == playerSideCollider)
        {
            HandleBounce("Player");
        }
        else if (hitCollider == cpuSideCollider)
        {
            HandleBounce("CPU");
        }
    }

    void HandleBounce(string side)
    {
        // 1. Service Logic
        if (isServicePhase)
        {
            if (isPlayerTurnToServe && side == "Player" && !ballHasBouncedOnValidSide)
            {
                ballHasBouncedOnValidSide = true;
                LogState("Good Serve Start! Clear the net.");
            }
            else if (isPlayerTurnToServe && side == "CPU" && ballHasBouncedOnValidSide)
            {
                isServicePhase = false;
                ballHasBouncedOnValidSide = false; // Reset for rally double-bounce check
                LogState("Rally Started!");
            }
            else if (!isPlayerTurnToServe && side == "CPU" && !ballHasBouncedOnValidSide)
            {
                ballHasBouncedOnValidSide = true;
                LogState("CPU Serve Start!");
            }
            else if (!isPlayerTurnToServe && side == "Player" && ballHasBouncedOnValidSide)
            {
                isServicePhase = false;
                ballHasBouncedOnValidSide = false;
                LogState("Rally Started!");
            }
            else
            {
                LogState("Service Foul!");
                AwardPoint(isPlayerTurnToServe ? "CPU" : "Player");
            }
            return;
        }

        // 2. Rally Logic
        if (side == "Player")
        {
            if (lastTouch == LastHit.Player || ballHasBouncedOnValidSide) AwardPoint("CPU");
            else ballHasBouncedOnValidSide = true;
        }
        else
        {
            if (lastTouch == LastHit.CPU || ballHasBouncedOnValidSide) AwardPoint("Player");
            else ballHasBouncedOnValidSide = true;
        }
    }

    public void AwardPoint(string winner)
    {
        if (winner == "Player") playerScore++; else cpuScore++;
        UpdateUI();
        
        // Switch server every 2 points
        isPlayerTurnToServe = ((playerScore + cpuScore) / 2) % 2 == 0;
        
        StartCoroutine(RespawnSequence());
    }

    IEnumerator RespawnSequence()
    {
        yield return new WaitForSeconds(1.5f);
        ResetBall();
    }

    public void ResetBall()
    {
        if (currentBall != null) Destroy(currentBall);
        
        lastTouch = LastHit.None;
        ballHasBouncedOnValidSide = false;
        isServicePhase = true;

        Transform spawn = isPlayerTurnToServe ? playerServePoint : cpuServePoint;
        currentBall = Instantiate(ballPrefab, spawn.position, Quaternion.identity);
        
        LogState(isPlayerTurnToServe ? "Your Serve" : "CPU Serve");
    }

    void LogState(string message)
    {
        Debug.Log($"[GAME]: {message}");
        if (statusDisplay != null) statusDisplay.text = message;
    }

    void UpdateUI()
    {
        if (scoreDisplay != null) scoreDisplay.text = $"{playerScore} - {cpuScore}";
    }
}