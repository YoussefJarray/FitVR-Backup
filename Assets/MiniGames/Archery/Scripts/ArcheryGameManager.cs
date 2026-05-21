using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using TMPro;

/// <summary>
/// Central game manager for the VR archery experience.
/// </summary>
public class ArcheryGameManager : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────
    //  Static Events
    // ─────────────────────────────────────────────────────────────
    public static event Action        OnGameStarted;
    public static event Action<int>   OnGameEnded;
    public static event Action<int>   OnScoreChanged;

    // ─────────────────────────────────────────────────────────────
    //  Inspector
    // ─────────────────────────────────────────────────────────────
    [Header("─── Scene References ────────────────────")]
    [SerializeField] private TargetSpawner      spawner;
    [SerializeField] private XRGrabInteractable bowInteractable;

    [Header("─── Game Over Modal ────────────────────")]
    [SerializeField] private GameObject gameOverModalPrefab;
    [SerializeField] private Transform  playerCamera;
    [SerializeField] private float      modalSpawnDistance  = 2f;
    [SerializeField] private float      modalVerticalOffset = -0.1f;
    [SerializeField] private float      modalShowDelay      = 0.8f;

    [Header("─── HUD Canvases ─────────────────────────")]
    [SerializeField] private GameObject tutorialUI;
    [SerializeField] private GameObject scoreCanvas;

    [Header("─── HUD Text ───────────────────────────")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI timerText;

    [Header("─── Audio ───────────────────────────────")]
    [Tooltip("AudioSource used for the game-done sound AND countdown beeps.")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip   gameDoneSound;
    [Tooltip("Short beep played once per second during the final 5 seconds.")]
    [SerializeField] private AudioClip   countdownBeep;

    [Header("─── Game Settings ──────────────────────")]
    [SerializeField] private float gameDuration = 60f;
    [SerializeField] private float startDelay   = 1.5f;

    // ─────────────────────────────────────────────────────────────
    //  State
    // ─────────────────────────────────────────────────────────────
    public enum GameState { WaitingToStart, Active, Ended }

    private GameState  state          = GameState.WaitingToStart;
    private int        currentScore;
    private int        targetsHit;
    private float      timeRemaining;
    private bool       readyForInput  = false;
    private float      lastBeepSecond = -1f;
    private GameObject activeModal;

    // ─────────────────────────────────────────────────────────────
    //  Unity lifecycle
    // ─────────────────────────────────────────────────────────────
    private void OnEnable()
    {
        MovingTarget.OnTargetHit += HandleTargetHit;
        bowInteractable.selectEntered.AddListener(OnBowGrabbed);
    }

    private void OnDisable()
    {
        MovingTarget.OnTargetHit -= HandleTargetHit;
        bowInteractable.selectEntered.RemoveListener(OnBowGrabbed);
    }

    private void Start()
    {
        spawner.Deactivate();

        if (playerCamera == null && Camera.main != null)
            playerCamera = Camera.main.transform;

        SetUI_Tutorial();
        ResetTimerDisplay();
        Invoke(nameof(AllowInput), startDelay);
    }

    private void AllowInput() => readyForInput = true;

    private void Update()
    {
        if (state != GameState.Active) return;

        if (timeRemaining > 0f)
        {
            timeRemaining -= Time.deltaTime;
            UpdateTimerDisplay();
            TryPlayCountdownBeep();
        }
        else
        {
            EndGame();
        }
    }

    // ─────────────────────────────────────────────────────────────
    //  Bow grab
    // ─────────────────────────────────────────────────────────────
    private void OnBowGrabbed(SelectEnterEventArgs args)
    {
        if (!readyForInput)                    return;
        if (state != GameState.WaitingToStart) return;
        StartGame();
    }

    // ─────────────────────────────────────────────────────────────
    //  Game flow
    // ─────────────────────────────────────────────────────────────
    private void StartGame()
    {
        state          = GameState.Active;
        currentScore   = 0;
        targetsHit     = 0;
        timeRemaining  = gameDuration;
        lastBeepSecond = -1f;

        if (activeModal != null) Destroy(activeModal);

        spawner.Activate();

        SetUI_Game();
        UpdateScoreDisplay();
        OnGameStarted?.Invoke();
    }

    private void EndGame()
    {
        if (state == GameState.Ended) return;
        state = GameState.Ended;

        timeRemaining = 0f;

        spawner.Deactivate();

        // FIX 1: Stop any currently playing audio (countdown beep may still be queued)
        // then play the game-done sound on the same AudioSource.
        if (audioSource != null)
        {
            audioSource.Stop();                          // kills any looping / queued beep
            if (gameDoneSound != null)
                audioSource.PlayOneShot(gameDoneSound);  // plays over silence
        }

        if (timerText != null) timerText.text = "FINISHED";

        OnGameEnded?.Invoke(currentScore);
        Invoke(nameof(SpawnModal), modalShowDelay);
    }

    // ─────────────────────────────────────────────────────────────
    //  Modal
    // ─────────────────────────────────────────────────────────────
    private void SpawnModal()
    {
        if (gameOverModalPrefab == null || playerCamera == null) return;

        Vector3    spawnDir = (playerCamera.forward + Vector3.up * modalVerticalOffset).normalized;
        Vector3    spawnPos = playerCamera.position + spawnDir * modalSpawnDistance;
        Quaternion spawnRot = Quaternion.LookRotation(spawnPos - playerCamera.position, Vector3.up);

        activeModal = Instantiate(gameOverModalPrefab, spawnPos, spawnRot);
        activeModal.SetActive(true);

        GameOverModal modal = activeModal.GetComponent<GameOverModal>();
        if (modal != null)
        {
            modal.onPlayAgain.AddListener(RestartGame);
            modal.Populate(currentScore, targetsHit);
        }
    }

    // ─────────────────────────────────────────────────────────────
    //  Restart
    // ─────────────────────────────────────────────────────────────
    public void RestartGame()
    {
        if (state == GameState.Active) return;

        state        = GameState.WaitingToStart;
        currentScore = 0;
        targetsHit   = 0;

        if (activeModal != null) { Destroy(activeModal); activeModal = null; }

        spawner.Deactivate();

        SetUI_Tutorial();
        ResetTimerDisplay();
        readyForInput = true;
    }

    // ─────────────────────────────────────────────────────────────
    //  Score / hit tracking
    // ─────────────────────────────────────────────────────────────
    private void HandleTargetHit(int points)
    {
        if (state != GameState.Active) return;

        currentScore += points;
        targetsHit++;

        UpdateScoreDisplay();
        OnScoreChanged?.Invoke(currentScore);
    }

    // ─────────────────────────────────────────────────────────────
    //  UI helpers
    // ─────────────────────────────────────────────────────────────
    private void SetUI_Tutorial()
    {
        if (tutorialUI  != null) tutorialUI.SetActive(true);
        if (scoreCanvas != null) scoreCanvas.SetActive(false);
    }

    private void SetUI_Game()
    {
        if (tutorialUI  != null) tutorialUI.SetActive(false);
        if (scoreCanvas != null) scoreCanvas.SetActive(true);
    }

    private void UpdateScoreDisplay()
    {
        if (scoreText != null) scoreText.text = currentScore.ToString();
    }

    private void UpdateTimerDisplay()
    {
        if (timerText != null && state == GameState.Active)
            timerText.text = $"{Mathf.Max(0f, timeRemaining):F1}s";
    }

    private void ResetTimerDisplay()
    {
        if (timerText != null) timerText.text = $"{gameDuration:F1}s";
    }

    // ─────────────────────────────────────────────────────────────
    //  Countdown beep
    // ─────────────────────────────────────────────────────────────
    private void TryPlayCountdownBeep()
    {
        // FIX 2: Guard against beeping after game has ended (state check)
        if (countdownBeep == null || audioSource == null) return;
        if (state != GameState.Active)                    return;
        if (timeRemaining > 5f)                           return;

        int second = Mathf.CeilToInt(timeRemaining);
        if (second != lastBeepSecond && second > 0)
        {
            lastBeepSecond = second;
            audioSource.PlayOneShot(countdownBeep);
        }
    }

    // ─────────────────────────────────────────────────────────────
    //  Accessors
    // ─────────────────────────────────────────────────────────────
    public GameState CurrentState  => state;
    public int       CurrentScore  => currentScore;
    public int       TargetsHit    => targetsHit;
    public float     TimeRemaining => timeRemaining;
}