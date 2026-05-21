using UnityEngine;
using TMPro;
using System.Collections;

public class TableTennisGameManager : MonoBehaviour
{
    public static TableTennisGameManager Instance;

    [Header("UI & Debug")]
    public TextMeshProUGUI statusDisplay;
    public TextMeshProUGUI scoreDisplay;

    [Header("Table & World Colliders")]
    public Collider playerSideCollider;
    public Collider cpuSideCollider;
    public Collider netCollider;
    public Collider floorCollider;

    [Header("Ball Setup")]
    public GameObject ballPrefab;
    [Tooltip("Floats in front of player paddle when it is their serve")]
    public Transform  playerServePoint;
    [Tooltip("Floats at NPC paddle when it is the NPC's serve")]
    public Transform  cpuServePoint;

    [Header("NPC")]
    public TableTennisNPC npcController;

    [Header("Game State")]
    public int  playerScore         = 0;
    public int  cpuScore            = 0;
    public bool isPlayerTurnToServe = true;

    public enum LastHit { None, Player, CPU }
    public LastHit lastTouch           = LastHit.None;
    public bool    ballHasBouncedOnce  = false;
    public bool    isServicePhase      = true;

    // Alias for any script still using the old name
    public bool ballHasBouncedOnValidSide
    {
        get => ballHasBouncedOnce;
        set => ballHasBouncedOnce = value;
    }

    private GameObject currentBall;
    private bool       pointInProgress = false;

    // ── Unity ─────────────────────────────────────────────────────
    void Awake() => Instance = this;
    void Start()  { UpdateUI(); ResetBall(); }

    // ── Public API ────────────────────────────────────────────────

    /// <summary>Call from player paddle OnCollisionEnter.</summary>
    public void RegisterHit(LastHit who)
    {
        lastTouch         = who;
        ballHasBouncedOnce = false;
    }

    /// <summary>Call from BallCollision component on every physics contact.</summary>
    public void RegisterCollision(Collider hitCollider, GameObject ball)
    {
        if (pointInProgress) return;

        if      (hitCollider == netCollider)        HandleNetHit();
        else if (hitCollider == floorCollider)      HandleFloorHit();
        else if (hitCollider == playerSideCollider) HandleBounce("Player");
        else if (hitCollider == cpuSideCollider)    HandleBounce("CPU");
    }

    /// <summary>
    /// Bind this to your serve button / swing gesture.
    /// Releases the kinematic hold and tosses the ball.
    /// </summary>
    public void PlayerLaunchServe()
    {
        if (!isPlayerTurnToServe || !isServicePhase || currentBall == null) return;
        Rigidbody rb = currentBall.GetComponent<Rigidbody>();
        if (rb == null) return;

        rb.isKinematic = false;
        rb.linearVelocity = new Vector3(
            Random.Range(-0.3f, 0.3f),
            3.5f,
           -1.2f);   // toward net (player faces +Z net, so negative Z goes toward it)

        LogState("Serving…");
    }

    // ── Collision handlers ────────────────────────────────────────

    void HandleNetHit()
    {
        string loser  = lastTouch == LastHit.Player ? "Player" : "CPU";
        string winner = loser    == "Player"        ? "CPU"    : "Player";
        LogState($"Net! Point to {winner}");
        AwardPoint(winner);
    }

    void HandleFloorHit()
    {
        string winner;
        if (lastTouch == LastHit.None)
        {
            // Ball never hit — server faulted
            winner = isPlayerTurnToServe ? "CPU" : "Player";
        }
        else
        {
            string loser = lastTouch == LastHit.Player ? "Player" : "CPU";
            winner = loser == "Player" ? "CPU" : "Player";
        }
        LogState($"Off the table! Point to {winner}");
        AwardPoint(winner);
    }

    void HandleBounce(string side)
    {
        if (isServicePhase) HandleServiceBounce(side);
        else                HandleRallyBounce(side);
    }

    void HandleServiceBounce(string side)
    {
        string serverSide   = isPlayerTurnToServe ? "Player" : "CPU";
        string receiverSide = isPlayerTurnToServe ? "CPU"    : "Player";

        if (side == serverSide && !ballHasBouncedOnce)
        {
            ballHasBouncedOnce = true;
            LogState("Good serve!");
        }
        else if (side == receiverSide && ballHasBouncedOnce)
        {
            isServicePhase     = false;
            ballHasBouncedOnce = false;
            LogState("Rally!");
        }
        else
        {
            LogState("Service fault!");
            AwardPoint(receiverSide);
        }
    }

    void HandleRallyBounce(string side)
    {
        bool landedOnPlayerSide = side == "Player";

        if (ballHasBouncedOnce)
        {
            string winner = landedOnPlayerSide ? "Player" : "CPU";
            LogState($"Double bounce! Point to {winner}");
            AwardPoint(winner);
            return;
        }

        if ((landedOnPlayerSide && lastTouch == LastHit.Player) ||
            (!landedOnPlayerSide && lastTouch == LastHit.CPU))
        {
            string winner = landedOnPlayerSide ? "CPU" : "Player";
            LogState($"Didn't cross! Point to {winner}");
            AwardPoint(winner);
            return;
        }

        ballHasBouncedOnce = true;
    }

    // ── Scoring ───────────────────────────────────────────────────

    public void AwardPoint(string winner)
    {
        if (pointInProgress) return;
        pointInProgress = true;

        if (winner == "Player") playerScore++;
        else                    cpuScore++;

        UpdateUI();

        int total = playerScore + cpuScore;
        isPlayerTurnToServe = (total / 2) % 2 == 0;

        StartCoroutine(RespawnSequence());
    }

    IEnumerator RespawnSequence()
    {
        yield return new WaitForSeconds(1.5f);
        ResetBall();
        pointInProgress = false;
    }

    // ── Ball reset ────────────────────────────────────────────────
    /// <summary>
    /// Spawns ball kinematic (frozen) at the correct server's position.
    /// Player must press serve button; NPC auto-serves after a short delay.
    /// </summary>
    public void ResetBall()
    {
        if (currentBall != null) Destroy(currentBall);

        lastTouch          = LastHit.None;
        ballHasBouncedOnce = false;
        isServicePhase     = true;

        Transform spawnPoint = isPlayerTurnToServe ? playerServePoint : cpuServePoint;
        currentBall = Instantiate(ballPrefab, spawnPoint.position, Quaternion.identity);

        // Freeze in place until served
        Rigidbody rb = currentBall.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        if (isPlayerTurnToServe)
        {
            LogState("Your Serve — press Serve!");
        }
        else
        {
            LogState("CPU Serving…");
            StartCoroutine(NPCServeSequence());
        }
    }

    // ── NPC auto-serve ────────────────────────────────────────────

    IEnumerator NPCServeSequence()
    {
        yield return new WaitForSeconds(1.2f);
        if (currentBall == null) yield break;

        Rigidbody rb = currentBall.GetComponent<Rigidbody>();
        if (rb == null) yield break;

        rb.isKinematic = false;

        // Launch toward player side with an upward arc
        rb.linearVelocity = new Vector3(
            Random.Range(-0.4f, 0.4f),
            3.5f,
           -1.3f);   // negative Z = toward player

        lastTouch = LastHit.CPU;
        npcController?.OnNPCServed();
        LogState("CPU served!");
    }

    // ── Helpers ───────────────────────────────────────────────────
    public GameObject GetCurrentBall() => currentBall;

    void LogState(string msg)
    {
        Debug.Log($"[GAME]: {msg}");
        if (statusDisplay != null) statusDisplay.text = msg;
    }

    void UpdateUI()
    {
        if (scoreDisplay != null) scoreDisplay.text = $"{playerScore} - {cpuScore}";
    }
}