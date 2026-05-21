using UnityEngine;
using TMPro;

[ExecuteInEditMode]
[RequireComponent(typeof(TMP_Text))]
public class TMPTextCurve : MonoBehaviour
{
    [Tooltip("The curve that defines the shape of the text. Make sure it goes from 0 to 1 on the X axis.")]
    public AnimationCurve curve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.5f, 1f), new Keyframe(1f, 0f));

    [Tooltip("Multiplier for the curve's height.")]
    public float curveScale = 50f;

    private TMP_Text textComponent;
    private bool isUpdatingMesh = false;

    private void Awake()
    {
        textComponent = GetComponent<TMP_Text>();
    }

    private void OnEnable()
    {
        TMPro_EventManager.TEXT_CHANGED_EVENT.Add(OnTextChanged);
    }

    private void OnDisable()
    {
        TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(OnTextChanged);
    }

    private void LateUpdate()
    {
        if (transform.hasChanged)
        {
            ApplyCurveEffect();
            transform.hasChanged = false;
        }
    }

    private void OnValidate()
    {
        if (textComponent == null) textComponent = GetComponent<TMP_Text>();
        ApplyCurveEffect();
    }

    private void OnTextChanged(Object obj)
    {
        if (obj == textComponent)
        {
            ApplyCurveEffect();
        }
    }

    public void ApplyCurveEffect()
    {
        if (textComponent == null || isUpdatingMesh) return;

        isUpdatingMesh = true;

        textComponent.ForceMeshUpdate(false, false);
        TMP_TextInfo textInfo = textComponent.textInfo;
        int characterCount = textInfo.characterCount;

        if (characterCount == 0)
        {
            isUpdatingMesh = false;
            return;
        }

        float boundsMinX = textComponent.bounds.min.x;
        float boundsMaxX = textComponent.bounds.max.x;
        float textWidth = boundsMaxX - boundsMinX;

        if (textWidth <= 0)
        {
            isUpdatingMesh = false;
            return;
        }

        for (int i = 0; i < characterCount; i++)
        {
            TMP_CharacterInfo charInfo = textInfo.characterInfo[i];

            if (!charInfo.isVisible) continue;

            int materialIndex = charInfo.materialReferenceIndex;
            int vertexIndex = charInfo.vertexIndex;

            Vector3[] vertices = textInfo.meshInfo[materialIndex].vertices;

            // 1. Find the local baseline center of this individual character before moving it
            float charCenterX = (vertices[vertexIndex + 0].x + vertices[vertexIndex + 2].x) * 0.5f;
            float charCenterY = (vertices[vertexIndex + 0].y + vertices[vertexIndex + 2].y) * 0.5f;
            Vector3 charCenter = new Vector3(charCenterX, charCenterY, 0f);

            // 2. Determine progress across the overall text sequence width (0.0 to 1.0)
            float normalizedX = (charCenterX - boundsMinX) / textWidth;

            // 3. Sample height on the curve
            float yOffset = curve.Evaluate(normalizedX) * curveScale;

            // 4. Calculate curve slope/tangent using a tiny look-ahead window to get the true tangent angle
            float lookAheadX = Mathf.Min(normalizedX + 0.001f, 1f);
            float currentY = curve.Evaluate(normalizedX) * curveScale;
            float nextY = curve.Evaluate(lookAheadX) * curveScale;
            
            float deltaX = (lookAheadX - normalizedX) * textWidth;
            float deltaY = nextY - currentY;
            
            // Calculate the angle of rotation following the line slope
            float angle = Mathf.Atan2(deltaY, deltaX) * Mathf.Rad2Deg;
            Quaternion rotation = Quaternion.Euler(0f, 0f, angle);

            // 5. Transform all 4 corners of the character quad relative to its own center, rotate, then apply offset
            for (int j = 0; j < 4; j++)
            {
                Vector3 origVertex = vertices[vertexIndex + j];
                
                // Zero out around the character center point
                Vector3 zeroedPos = origVertex - charCenter;
                
                // Spin the character to look along the tangent curve angle
                Vector3 rotatedPos = rotation * zeroedPos;
                
                // Shift it back and add the final baseline curve height
                vertices[vertexIndex + j] = rotatedPos + charCenter + new Vector3(0f, yOffset, 0f);
            }
        }

        // Apply updated vertex structure positions directly to target meshes
        for (int i = 0; i < textInfo.meshInfo.Length; i++)
        {
            textInfo.meshInfo[i].mesh.vertices = textInfo.meshInfo[i].vertices;
            textComponent.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
        }

        isUpdatingMesh = false;
    }
}