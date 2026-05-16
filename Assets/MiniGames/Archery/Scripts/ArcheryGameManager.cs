using UnityEngine;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit;

public class ArcheryGameManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TargetSpawner spawner;
    [SerializeField] private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable bowInteractable;
    
    [Header("UI Canvases")]
    [SerializeField] private GameObject tutorialUI;
    [SerializeField] private GameObject scoreCanvas;

    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI timerText;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip gameDoneSound;

    [Header("Game Settings")]
    [SerializeField] private float gameDuration = 60f;

    private int currentScore = 0;
    private float timeRemaining;
    private bool isGameActive = false;
    private bool hasGameEnded = false;

    private void OnEnable()
    {
        MovingTarget.OnTargetHit += UpdateScore;
        bowInteractable.selectEntered.AddListener(OnBowGrabbed);
    }

    private void OnDisable()
    {
        MovingTarget.OnTargetHit -= UpdateScore;
        bowInteractable.selectEntered.RemoveListener(OnBowGrabbed);
    }

    private void Start()
    {
        spawner.enabled = false;
        
        if(tutorialUI != null) tutorialUI.SetActive(true);
        if(scoreCanvas != null) scoreCanvas.SetActive(false);

        timeRemaining = gameDuration;
        UpdateUI();
    }

    private void Update()
    {
        if (isGameActive)
        {
            if (timeRemaining > 0)
            {
                timeRemaining -= Time.deltaTime;
                UpdateUI();
            }
            else
            {
                EndGame();
            }
        }
    }

    private void OnBowGrabbed(SelectEnterEventArgs args)
    {
        if (!isGameActive && !hasGameEnded) 
        {
            if (tutorialUI != null) tutorialUI.SetActive(false);
            if (scoreCanvas != null) scoreCanvas.SetActive(true);
            StartGame();
        }
    }

    private void StartGame()
    {
        isGameActive = true;
        hasGameEnded = false;
        currentScore = 0;
        timeRemaining = gameDuration;
        spawner.enabled = true;
    }

    private void EndGame()
    {
        isGameActive = false;
        hasGameEnded = true;
        
        spawner.enabled = false;
        spawner.StopAllCoroutines(); // Kill the spawn loop

        if (audioSource != null && gameDoneSound != null)
        {
            audioSource.PlayOneShot(gameDoneSound);
        }

        if (timerText != null) timerText.text = "FINISHED";
    }

    private void UpdateScore(int points)
    {
        if (!isGameActive) return;
        currentScore += points;
        UpdateUI();
    }

    private void UpdateUI()
    {
        if(scoreText != null) scoreText.text = $"{currentScore}";
        if(timerText != null && isGameActive) timerText.text = $"{Mathf.Max(0, timeRemaining):F1}s";
    }
}