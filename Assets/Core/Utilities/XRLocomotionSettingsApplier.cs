using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;

public class XRLocomotionSettingsApplier : MonoBehaviour
{
    [SerializeField] private ContinuousTurnProvider continuousTurn;
    [SerializeField] private SnapTurnProvider       snapTurn;

    private void OnEnable()
    {
        if (SettingsManager.Instance != null)
            SettingsManager.Instance.OnSettingsChanged += ApplySettings;
        ApplySettings();
    }

    private void OnDisable()
    {
        if (SettingsManager.Instance != null)
            SettingsManager.Instance.OnSettingsChanged -= ApplySettings;
    }

    private void ApplySettings()
    {
        if (SettingsManager.Instance == null) return;

        bool useContinuous = SettingsManager.Instance.TurnMode == 0;

        if (continuousTurn != null)
        {
            continuousTurn.enabled   = useContinuous;
            continuousTurn.turnSpeed = SettingsManager.Instance.ContSpeed;
        }

        if (snapTurn != null)
        {
            snapTurn.enabled     = !useContinuous;
            snapTurn.turnAmount  = SettingsManager.Instance.SnapAmount;
        }
    }
}