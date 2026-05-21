using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.Rendering;
using TMPro;

public class PauseMenuController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject settingsPanel;

    [Header("Input")]
    [SerializeField] private InputActionReference menuButtonAction;

    [Header("Settings UI")]
    [SerializeField] private TMP_Dropdown turnModeDropdown;
    [SerializeField] private Slider      sensitivitySlider;
    [SerializeField] private Slider      sfxSlider;
    [SerializeField] private Slider      musicSlider;

    [Header("Pause Volume")]
    [SerializeField] private Volume pauseVolume;
    [SerializeField] private float  volumeFadeDuration = 0.3f;

    [Header("Objects To Pause")]
    [SerializeField] private GameObject[] objectsToPause;

    private CanvasGroup canvasGroup;
    private Coroutine   volumeFadeCoroutine;
    private bool        isOpen = false;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        if (pauseVolume != null)
            pauseVolume.weight = 0f;

        Hide();
    }

    private void OnEnable()
    {
        if (menuButtonAction != null)
        {
            menuButtonAction.action.Enable();
            menuButtonAction.action.performed += OnMenuButton;
        }

        if (turnModeDropdown != null)
            turnModeDropdown.onValueChanged.AddListener(OnTurnModeChanged);
        if (sensitivitySlider != null)
            sensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);
        if (sfxSlider != null)
            sfxSlider.onValueChanged.AddListener(v => SettingsManager.Instance?.SetVolSFX(v));
        if (musicSlider != null)
            musicSlider.onValueChanged.AddListener(v => SettingsManager.Instance?.SetVolMusic(v));
    }

    private void OnDisable()
    {
        if (menuButtonAction != null)
        {
            menuButtonAction.action.performed -= OnMenuButton;
            menuButtonAction.action.Disable();
        }
    }

    private void Start()
    {
        RefreshUI();
    }

    // ── Show / Hide ───────────────────────────────────────────────
    private void Hide()
    {
        canvasGroup.alpha          = 0f;
        canvasGroup.interactable   = false;
        canvasGroup.blocksRaycasts = false;
        isOpen                     = false;

        FadeVolume(0f);
        SetPausedObjects(true);
    }

    private void Show()
    {
        canvasGroup.alpha          = 1f;
        canvasGroup.interactable   = true;
        canvasGroup.blocksRaycasts = true;
        isOpen                     = true;

        FadeVolume(1f);
        SetPausedObjects(false);
    }

    // ── Volume Fade ───────────────────────────────────────────────
    private void FadeVolume(float target)
    {
        if (pauseVolume == null) return;
        if (volumeFadeCoroutine != null) StopCoroutine(volumeFadeCoroutine);
        volumeFadeCoroutine = StartCoroutine(FadeVolumeRoutine(target));
    }

    private IEnumerator FadeVolumeRoutine(float target)
    {
        float start   = pauseVolume.weight;
        float elapsed = 0f;

        while (elapsed < volumeFadeDuration)
        {
            elapsed            += Time.unscaledDeltaTime;
            pauseVolume.weight  = Mathf.Lerp(start, target, elapsed / volumeFadeDuration);
            yield return null;
        }

        pauseVolume.weight = target;
    }

    // ── Pause Objects ─────────────────────────────────────────────
    private void SetPausedObjects(bool active)
    {
        if (objectsToPause == null) return;
        foreach (var obj in objectsToPause)
            if (obj != null) obj.SetActive(active);
    }

    // ── Open / Close ─────────────────────────────────────────────
    private void OnMenuButton(InputAction.CallbackContext _)
    {
        if (isOpen) Close(); else Open();
    }

    private void Open()
    {
        Show();
        mainPanel.SetActive(true);
        settingsPanel.SetActive(false);
        RefreshUI();
    }

    public void Close()
    {
        Hide();
        mainPanel.SetActive(true);
        settingsPanel.SetActive(false);
    }

    // ── Panel Switching ───────────────────────────────────────────
    public void OnClickSettings()
    {
        mainPanel.SetActive(false);
        settingsPanel.SetActive(true);
    }

    public void OnClickBack()
    {
        settingsPanel.SetActive(false);
        mainPanel.SetActive(true);
    }

    // ── Listeners ─────────────────────────────────────────────────
    private void OnTurnModeChanged(int value)
    {
        SettingsManager.Instance?.SetTurnMode(value);
        RefreshSensitivitySlider(value);
    }

    private void OnSensitivityChanged(float value)
    {
        if (SettingsManager.Instance == null) return;
        if (turnModeDropdown.value == 0)
            SettingsManager.Instance.SetContSpeed(value);
        else
            SettingsManager.Instance.SetSnapAmount(value);
    }

    // ── Sync UI ───────────────────────────────────────────────────
    private void RefreshUI()
    {
        var s = SettingsManager.Instance;
        if (s == null) return;

        if (turnModeDropdown != null)
            turnModeDropdown.SetValueWithoutNotify(s.TurnMode);

        RefreshSensitivitySlider(s.TurnMode);

        if (sfxSlider != null)   sfxSlider.SetValueWithoutNotify(s.VolSFX);
        if (musicSlider != null) musicSlider.SetValueWithoutNotify(s.VolMusic);
    }

    private void RefreshSensitivitySlider(int turnMode)
    {
        if (sensitivitySlider == null || SettingsManager.Instance == null) return;

        if (turnMode == 0)
        {
            sensitivitySlider.minValue = 10f;
            sensitivitySlider.maxValue = 180f;
            sensitivitySlider.SetValueWithoutNotify(SettingsManager.Instance.ContSpeed);
        }
        else
        {
            sensitivitySlider.minValue = 15f;
            sensitivitySlider.maxValue = 90f;
            sensitivitySlider.SetValueWithoutNotify(SettingsManager.Instance.SnapAmount);
        }
    }
}