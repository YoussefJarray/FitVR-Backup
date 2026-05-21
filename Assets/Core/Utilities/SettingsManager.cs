using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Persistent singleton. Stores all player settings in PlayerPrefs and
/// broadcasts them to whatever scene is currently loaded.
/// </summary>
public class SettingsManager : MonoBehaviour
{
    // ── Singleton ────────────────────────────────────────────────
    public static SettingsManager Instance { get; private set; }

    // ── Keys ─────────────────────────────────────────────────────
    const string KEY_TURN_MODE    = "TurnMode";       // 0 = continuous, 1 = snap
    const string KEY_CONT_SPEED   = "ContTurnSpeed";
    const string KEY_SNAP_AMOUNT  = "SnapTurnAmount";
    const string KEY_VOL_MASTER   = "VolMaster";
    const string KEY_VOL_MUSIC    = "VolMusic";
    const string KEY_VOL_SFX      = "VolSFX";

    // ── Defaults ─────────────────────────────────────────────────
    const int   DEFAULT_TURN_MODE   = 0;    // continuous
    const float DEFAULT_CONT_SPEED  = 60f;
    const float DEFAULT_SNAP_AMOUNT = 45f;
    const float DEFAULT_VOLUME      = 1f;

    // ── Audio Mixer (assign in Inspector, or auto-found) ─────────
    [Header("Audio Mixer")]
    [Tooltip("The project's main AudioMixer asset.")]
    [SerializeField] private AudioMixer audioMixer;

    private const string MixerResourcesPath = "GameAudioMixer";

    [Tooltip("Exposed parameter name for master volume in the mixer.")]
    [SerializeField] private string masterParam = "VolMaster";
    [SerializeField] private string musicParam  = "VolMusic";
    [SerializeField] private string sfxParam    = "VolSFX";

    // ── Public read-only state ───────────────────────────────────
    public int   TurnMode    { get; private set; }
    public float ContSpeed   { get; private set; }
    public float SnapAmount  { get; private set; }
    public float VolMaster   { get; private set; }
    public float VolMusic    { get; private set; }
    public float VolSFX      { get; private set; }

    // ── Event ────────────────────────────────────────────────────
    /// <summary>Fired whenever any setting changes so scene objects can react.</summary>
    public event System.Action OnSettingsChanged;

    // ─────────────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        ResolveMixer();
        Load();
    }

    private void ResolveMixer()
    {
        if (audioMixer != null) return;
        audioMixer = Resources.Load<AudioMixer>(MixerResourcesPath);
    }

    // ── Load / Save ──────────────────────────────────────────────
    private void Load()
    {
        TurnMode   = PlayerPrefs.GetInt  (KEY_TURN_MODE,   DEFAULT_TURN_MODE);
        ContSpeed  = PlayerPrefs.GetFloat(KEY_CONT_SPEED,  DEFAULT_CONT_SPEED);
        SnapAmount = PlayerPrefs.GetFloat(KEY_SNAP_AMOUNT, DEFAULT_SNAP_AMOUNT);
        VolMaster  = PlayerPrefs.GetFloat(KEY_VOL_MASTER,  DEFAULT_VOLUME);
        VolMusic   = PlayerPrefs.GetFloat(KEY_VOL_MUSIC,   DEFAULT_VOLUME);
        VolSFX     = PlayerPrefs.GetFloat(KEY_VOL_SFX,     DEFAULT_VOLUME);
        Apply();
    }

    private void Save()
    {
        PlayerPrefs.SetInt  (KEY_TURN_MODE,   TurnMode);
        PlayerPrefs.SetFloat(KEY_CONT_SPEED,  ContSpeed);
        PlayerPrefs.SetFloat(KEY_SNAP_AMOUNT, SnapAmount);
        PlayerPrefs.SetFloat(KEY_VOL_MASTER,  VolMaster);
        PlayerPrefs.SetFloat(KEY_VOL_MUSIC,   VolMusic);
        PlayerPrefs.SetFloat(KEY_VOL_SFX,     VolSFX);
        PlayerPrefs.Save();
    }

    /// <summary>Push all current values to audio mixer etc.</summary>
    private void Apply()
    {
        SetMixerVolume(masterParam, VolMaster);
        SetMixerVolume(musicParam,  VolMusic);
        SetMixerVolume(sfxParam,    VolSFX);
        OnSettingsChanged?.Invoke();
    }

    // Converts 0-1 linear slider value → decibels for AudioMixer
    private void SetMixerVolume(string param, float linear)
    {
        if (audioMixer == null) return;
        float db = linear > 0.0001f ? Mathf.Log10(linear) * 20f : -80f;
        audioMixer.SetFloat(param, db);
    }

    // ── Public setters (called by UI) ────────────────────────────
    public void SetTurnMode(int mode)       { TurnMode   = mode;  Save(); Apply(); }
    public void SetContSpeed(float v)       { ContSpeed  = v;     Save(); Apply(); }
    public void SetSnapAmount(float v)      { SnapAmount = v;     Save(); Apply(); }
    public void SetVolMaster(float v)       { VolMaster  = v;     Save(); Apply(); }
    public void SetVolMusic(float v)        { VolMusic   = v;     Save(); Apply(); }
    public void SetVolSFX(float v)          { VolSFX     = v;     Save(); Apply(); }
}