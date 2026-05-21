using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Canvas))]
[RequireComponent(typeof(CanvasGroup))]
public class GazeFollowUI : MonoBehaviour
{
    [Header("─── Gaze Tracking ───────────────────────")]
    [Tooltip("Leave blank — auto-finds Camera.main or first active camera.")]
    [SerializeField] private Transform gazeTransform;

    [Tooltip("Distance in metres the panel floats in front of the camera.")]
    [SerializeField] private float panelDistance = 2f;

    [Tooltip("Lower = lazier drift. 1-3 = dreamy, 6-10 = snappy.")]
    [SerializeField][Range(0.5f, 15f)] private float followSpeed = 1.5f;

    [Tooltip("Nudge down from straight-ahead for comfortable eye level.")]
    [SerializeField] private float verticalOffset = -0.15f;

    [Header("─── Fade ───────────────────────────────")]
    [SerializeField] private float fadeDuration = 0.35f;

    [Header("─── UI Slots (all optional) ─────────────")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI bodyText;
    [SerializeField] private Button          confirmButton;
    [SerializeField] private Button          cancelButton;

    [Header("─── Events ─────────────────────────────")]
    public UnityEvent onConfirm;
    public UnityEvent onCancel;
    public UnityEvent onShown;
    public UnityEvent onHidden;

    private CanvasGroup canvasGroup;
    private bool        isVisible = false;
    private Coroutine   fadeCoroutine;

    // ─────────────────────────────────────────────────────────────
    //  Unity lifecycle
    // ─────────────────────────────────────────────────────────────
    private void Awake()
    {
        GetComponent<Canvas>().renderMode  = RenderMode.WorldSpace;
        canvasGroup                        = GetComponent<CanvasGroup>();

        // Start hidden via alpha — NOT SetActive(false).
        // SetActive(false) kills Update/LateUpdate so the follow never runs.
        canvasGroup.alpha              = 0f;
        canvasGroup.interactable       = false;
        canvasGroup.blocksRaycasts     = false;
    }

    private void Start()
    {
        ResolveCamera();
        confirmButton?.onClick.AddListener(OnConfirmClicked);
        cancelButton?.onClick.AddListener(OnCancelClicked);
    }

    // Always running, every frame — this IS the follow system.
    private void LateUpdate()
    {
        if (gazeTransform == null) { ResolveCamera(); return; }

        Vector3 targetPos = GetTargetPosition();

        // Move a fraction of remaining distance each frame — exponential ease,
        // always chasing, never fully stopping.
        transform.position += (targetPos - transform.position) * followSpeed * Time.deltaTime;

        // Always face the camera
        Vector3 lookDir = transform.position - gazeTransform.position;
        if (lookDir.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(lookDir, Vector3.up);
    }

    // ─────────────────────────────────────────────────────────────
    //  Public API
    // ─────────────────────────────────────────────────────────────
    public void Show()
    {
        if (isVisible) return;
        isVisible = true;

        // Snap to current gaze so it doesn't drift in from far away
        ResolveCamera();
        transform.position = GetTargetPosition();

        SwapCoroutine(ref fadeCoroutine, FadeTo(1f));
    }

    public void Hide()
    {
        if (!isVisible) return;
        isVisible = false;
        SwapCoroutine(ref fadeCoroutine, FadeOutAndHide());
    }

    public void Toggle() { if (isVisible) Hide(); else Show(); }

    public void SetTitle(string value) { if (titleText != null) titleText.text = value; }
    public void SetBody(string value)  { if (bodyText  != null) bodyText.text  = value; }

    // ─────────────────────────────────────────────────────────────
    //  Button handlers
    // ─────────────────────────────────────────────────────────────
    private void OnConfirmClicked()
    {
        Hide();
        StartCoroutine(FireAfterDelay(onConfirm, fadeDuration * 0.5f));
    }

    private void OnCancelClicked()
    {
        Hide();
        StartCoroutine(FireAfterDelay(onCancel, fadeDuration * 0.5f));
    }

    private IEnumerator FireAfterDelay(UnityEvent evt, float delay)
    {
        yield return new WaitForSeconds(delay);
        evt?.Invoke();
    }

    // ─────────────────────────────────────────────────────────────
    //  Position
    // ─────────────────────────────────────────────────────────────
    private Vector3 GetTargetPosition()
    {
        if (gazeTransform == null) return transform.position;
        Vector3 dir = (gazeTransform.forward + Vector3.up * verticalOffset).normalized;
        return gazeTransform.position + dir * panelDistance;
    }

    // ─────────────────────────────────────────────────────────────
    //  Fade
    // ─────────────────────────────────────────────────────────────
    private IEnumerator FadeTo(float target)
    {
        canvasGroup.interactable   = target > 0f;
        canvasGroup.blocksRaycasts = target > 0f;

        float start   = canvasGroup.alpha;
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed           += Time.deltaTime;
            canvasGroup.alpha  = Mathf.Lerp(start, target, elapsed / fadeDuration);
            yield return null;
        }
        canvasGroup.alpha = target;
        if (target >= 1f) onShown?.Invoke();
    }

    private IEnumerator FadeOutAndHide()
    {
        yield return FadeTo(0f);
        canvasGroup.interactable   = false;
        canvasGroup.blocksRaycasts = false;
        onHidden?.Invoke();
        // NOTE: no SetActive(false) — keeping it active so LateUpdate keeps running
    }

    // ─────────────────────────────────────────────────────────────
    //  Camera
    // ─────────────────────────────────────────────────────────────
    private void ResolveCamera()
    {
        if (gazeTransform != null) return;
        if (Camera.main   != null) { gazeTransform = Camera.main.transform; return; }
        Camera fallback = FindFirstObjectByType<Camera>();
        if (fallback != null) gazeTransform = fallback.transform;
    }

    private void SwapCoroutine(ref Coroutine slot, IEnumerator routine)
    {
        if (slot != null) StopCoroutine(slot);
        slot = StartCoroutine(routine);
    }
}