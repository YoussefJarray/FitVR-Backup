using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

/// <summary>
/// Attach this to your Game Over Modal prefab (the World Space Canvas root).
///
/// HIERARCHY expected (all slots are optional — assign what you have):
///   Panel
///     ├── Game Over          (title TMP)
///     ├── Secondary Text
///     │     ├── Targets Hit  (label TMP)
///     │     ├── Targets Count (value TMP)
///     │     ├── Score         (label TMP)
///     │     └── Score Count   (value TMP)
///     ├── Play Again Button  (Button)
///     └── Stars
///           ├── Star 1       (RawImage)
///           ├── Star 2       (RawImage)
///           └── Star 3       (RawImage)
///
/// SETUP:
///   1. Make the Canvas World Space.
///   2. Drag this prefab into ArcheryGameManager's "Game Over Modal Prefab" slot.
///   3. Fill the Inspector slots below and tweak star thresholds / styles.
/// </summary>
public class GameOverModal : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────
    //  UI Slots
    // ─────────────────────────────────────────────────────────────
    [Header("─── Text Elements ────────────────────────")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI targetsCountText;
    [SerializeField] private TextMeshProUGUI scoreCountText;

    [Header("─── Buttons ─────────────────────────────")]
    [SerializeField] private Button playAgainButton;

    [Header("─── Stars (assign in order: 1, 2, 3) ────")]
    [SerializeField] private RawImage[] starImages = new RawImage[3];

    // ─────────────────────────────────────────────────────────────
    //  Star Styles
    // ─────────────────────────────────────────────────────────────
    [Header("─── Star Style — Filled (Texture2D) ──────────────────")]
    [SerializeField] private Texture2D filledStarSprite;
    [SerializeField] private Color  filledStarColor  = Color.yellow;

    [Header("─── Star Style — Unfilled (Texture2D) ─────────────────")]
    [SerializeField] private Texture2D unfilledStarSprite;
    [SerializeField] private Color  unfilledStarColor = new Color(0.3f, 0.3f, 0.3f, 1f);

    // ─────────────────────────────────────────────────────────────
    //  Star Thresholds
    // ─────────────────────────────────────────────────────────────
    [Header("─── Star Score Thresholds ──────────────────")]
    [Tooltip("Minimum score to earn 1 star.")]
    [SerializeField] private int oneStarThreshold   = 100;
    [Tooltip("Minimum score to earn 2 stars.")]
    [SerializeField] private int twoStarThreshold   = 300;
    [Tooltip("Minimum score to earn 3 stars.")]
    [SerializeField] private int threeStarThreshold = 600;

    // ─────────────────────────────────────────────────────────────
    //  Star Animation
    // ─────────────────────────────────────────────────────────────
    [Header("─── Star Animation ──────────────────────")]
    [Tooltip("Delay before stars start popping in after the modal appears.")]
    [SerializeField] private float starRevealDelay    = 0.4f;
    [Tooltip("Delay between each star popping in.")]
    [SerializeField] private float starStaggerDelay   = 0.25f;
    [Tooltip("Scale the star bounces up to before settling.")]
    [SerializeField] private float starBouncePeak     = 1.4f;
    [SerializeField] private float starBounceDuration = 0.3f;

    // ─────────────────────────────────────────────────────────────
    //  Panel Entrance Animation
    // ─────────────────────────────────────────────────────────────
    [Header("─── Panel Entrance ──────────────────────")]
    [Tooltip("Fade the whole panel in on show.")]
    [SerializeField] private bool  fadeIn       = true;
    [SerializeField] private float fadeDuration = 0.4f;
    [Tooltip("Slide in from below on appear.")]
    [SerializeField] private bool  slideIn      = true;
    [SerializeField] private float slideOffset  = 0.15f;   // metres

    // ─────────────────────────────────────────────────────────────
    //  Title Strings
    // ─────────────────────────────────────────────────────────────
    [Header("─── Title Per Star Count ─────────────────")]
    [SerializeField] private string titleZeroStars  = "Better Luck Next Time";
    [SerializeField] private string titleOneStar    = "Good Effort!";
    [SerializeField] private string titleTwoStars   = "Nice Shooting!";
    [SerializeField] private string titleThreeStars = "Perfect Shot!";

    // ─────────────────────────────────────────────────────────────
    //  Events
    // ─────────────────────────────────────────────────────────────
    [Header("─── Events ──────────────────────────────")]
    [Tooltip("Fired when Play Again is clicked (after fade-out). " +
             "Wire this to ArcheryGameManager.RestartGame() in the Inspector.")]
    public UnityEvent onPlayAgain;

    // ─────────────────────────────────────────────────────────────
    //  Private
    // ─────────────────────────────────────────────────────────────
    private CanvasGroup canvasGroup;
    private Vector3[]   starOriginalScales;

    // ─────────────────────────────────────────────────────────────
    //  Unity lifecycle
    // ─────────────────────────────────────────────────────────────
    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        // Cache star original scales
        starOriginalScales = new Vector3[starImages.Length];
        for (int i = 0; i < starImages.Length; i++)
        {
            if (starImages[i] != null)
                starOriginalScales[i] = starImages[i].transform.localScale;
        }

        playAgainButton?.onClick.AddListener(OnPlayAgainClicked);

        // Start invisible
        canvasGroup.alpha          = 0f;
        canvasGroup.interactable   = false;
        canvasGroup.blocksRaycasts = false;
    }

    // ─────────────────────────────────────────────────────────────
    //  Public API — called by ArcheryGameManager after instantiation
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Populate all data and play the entrance animation.
    /// </summary>
    public void Populate(int score, int targetsHit)
    {
        // Safety: coroutines cannot run on inactive GameObjects
        if (!gameObject.activeInHierarchy)
            gameObject.SetActive(true);

        int stars = CalculateStars(score);

        // Text
        if (targetsCountText != null) targetsCountText.text = targetsHit.ToString();
        if (scoreCountText   != null) scoreCountText.text   = score.ToString();
        if (titleText        != null) titleText.text        = GetTitle(stars);

        // Reset all stars to unfilled immediately (no animation yet)
        SetAllStars(0);

        // Entrance then stars
        StartCoroutine(EntranceSequence(stars));
    }

    // ─────────────────────────────────────────────────────────────
    //  Entrance sequence
    // ─────────────────────────────────────────────────────────────
    private IEnumerator EntranceSequence(int starsToFill)
    {
        Vector3 originalPos = transform.localPosition;

        if (slideIn)
            transform.localPosition = originalPos - Vector3.up * slideOffset;

        // Fade + slide in
        float elapsed = 0f;
        canvasGroup.interactable   = false;
        canvasGroup.blocksRaycasts = false;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t  = Mathf.Clamp01(elapsed / fadeDuration);
            float e  = 1f - (1f - t) * (1f - t);  // ease-out quad

            if (fadeIn) canvasGroup.alpha = e;
            if (slideIn)
                transform.localPosition = Vector3.Lerp(
                    originalPos - Vector3.up * slideOffset,
                    originalPos, e);

            yield return null;
        }

        canvasGroup.alpha          = 1f;
        canvasGroup.interactable   = true;
        canvasGroup.blocksRaycasts = true;
        transform.localPosition    = originalPos;

        // Wait before revealing stars
        yield return new WaitForSeconds(starRevealDelay);

        // Pop in each earned star
        for (int i = 0; i < starImages.Length; i++)
        {
            bool fill = i < starsToFill;
            ApplyStarStyle(i, fill);

            if (starImages[i] != null)
                StartCoroutine(BounceScale(starImages[i].transform,
                                           starOriginalScales[i],
                                           starBouncePeak,
                                           starBounceDuration));

            yield return new WaitForSeconds(starStaggerDelay);
        }
    }

    // ─────────────────────────────────────────────────────────────
    //  Star helpers
    // ─────────────────────────────────────────────────────────────
    private int CalculateStars(int score)
    {
        if (score >= threeStarThreshold) return 3;
        if (score >= twoStarThreshold)   return 2;
        if (score >= oneStarThreshold)   return 1;
        return 0;
    }

    private void SetAllStars(int filledCount)
    {
        for (int i = 0; i < starImages.Length; i++)
            ApplyStarStyle(i, i < filledCount);
    }

    private void ApplyStarStyle(int index, bool filled)
    {
        if (index >= starImages.Length || starImages[index] == null) return;

        RawImage img = starImages[index];

        if (filled)
        {
            if (filledStarSprite   != null) img.texture = filledStarSprite;
            img.color = filledStarColor;
        }
        else
        {
            if (unfilledStarSprite != null) img.texture = unfilledStarSprite;
            img.color = unfilledStarColor;
        }
    }

    private string GetTitle(int stars) => stars switch
    {
        3 => titleThreeStars,
        2 => titleTwoStars,
        1 => titleOneStar,
        _ => titleZeroStars,
    };

    // ─────────────────────────────────────────────────────────────
    //  Bounce animation
    // ─────────────────────────────────────────────────────────────
    private IEnumerator BounceScale(Transform t, Vector3 original, float peak, float duration)
    {
        float half    = duration * 0.5f;
        float elapsed = 0f;

        // Scale up
        while (elapsed < half)
        {
            elapsed        += Time.deltaTime;
            float frac      = elapsed / half;
            t.localScale    = Vector3.LerpUnclamped(original, original * peak, frac);
            yield return null;
        }

        elapsed = 0f;

        // Scale back with slight overshoot (elastic feel)
        while (elapsed < half)
        {
            elapsed        += Time.deltaTime;
            float frac      = elapsed / half;
            // Overshoot settle
            float overshoot = Mathf.Sin(frac * Mathf.PI) * 0.08f;
            t.localScale    = Vector3.LerpUnclamped(original * peak, original, frac)
                              + original * overshoot;
            yield return null;
        }

        t.localScale = original;
    }

    // ─────────────────────────────────────────────────────────────
    //  Button
    // ─────────────────────────────────────────────────────────────
    private void OnPlayAgainClicked()
    {
        canvasGroup.interactable   = false;
        canvasGroup.blocksRaycasts = false;
        StartCoroutine(FadeOutThenFire());
    }

    private IEnumerator FadeOutThenFire()
    {
        float elapsed = 0f;
        float start   = canvasGroup.alpha;

        while (elapsed < fadeDuration)
        {
            elapsed           += Time.deltaTime;
            canvasGroup.alpha  = Mathf.Lerp(start, 0f, elapsed / fadeDuration);
            yield return null;
        }

        onPlayAgain?.Invoke();
        Destroy(gameObject);
    }
}