using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class Poof : MonoBehaviour
{
    private ParticleSystem ps;

    private void Awake()
    {
        ps = GetComponent<ParticleSystem>();
        SetupMeshRenderer();
    }

    public void PlayPoof()
    {
        if (ps == null)
        {
            ps = GetComponent<ParticleSystem>();
        }

        SetupMeshRenderer();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ps.Play();
    }

    private void SetupMeshRenderer()
    {
        var main = ps.main;
        main.loop = false;
        main.duration = 0.35f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.2f, 0.4f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(4f, 7f);
        main.startSize3D = false;
        main.startSize = new ParticleSystem.MinMaxCurve(0.4f, 0.9f);
        main.startRotationX = new ParticleSystem.MinMaxCurve(0f, 360f);
        main.startRotationY = new ParticleSystem.MinMaxCurve(0f, 360f);
        main.startRotationZ = new ParticleSystem.MinMaxCurve(0f, 360f);
        main.gravityModifier = -0.05f;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        ParticleSystem.Burst burst = new ParticleSystem.Burst(0f, 12, 18);
        emission.SetBursts(new ParticleSystem.Burst[] { burst });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.15f;

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { 
                new GradientColorKey(new Color(0.85f, 0.4f, 1.0f), 0.0f), 
                new GradientColorKey(new Color(0.55f, 0.1f, 0.85f), 0.6f),
                new GradientColorKey(new Color(0.25f, 0.0f, 0.4f), 1.0f) 
            },
            new GradientAlphaKey[] { 
                new GradientAlphaKey(1.0f, 0.0f), 
                new GradientAlphaKey(0.9f, 0.5f), 
                new GradientAlphaKey(0.0f, 1.0f) 
            }
        );
        colorOverLifetime.color = gradient;

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 0.1f);
        sizeCurve.AddKey(0.12f, 1.3f);
        sizeCurve.AddKey(0.4f, 0.9f);
        sizeCurve.AddKey(1f, 0f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1.0f, sizeCurve);

        var limitVelocityOverLifetime = ps.limitVelocityOverLifetime;
        limitVelocityOverLifetime.enabled = true;
        limitVelocityOverLifetime.limit = 0.2f;
        limitVelocityOverLifetime.dampen = 0.3f;

        ParticleSystemRenderer renderer = GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Mesh;

        GameObject primitive = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Mesh cubeMesh = primitive.GetComponent<MeshFilter>().sharedMesh;
        DestroyImmediate(primitive);

        if (cubeMesh != null)
        {
            renderer.mesh = cubeMesh;
        }

        Shader activeShader = Shader.Find("Universal Render Pipeline/Lit");
        if (activeShader == null)
        {
            activeShader = Shader.Find("Standard");
        }

        if (renderer.sharedMaterial == null || renderer.sharedMaterial.shader.name.Contains("InternalErrorShader"))
        {
            Material validMaterial = new Material(activeShader);
            renderer.sharedMaterial = validMaterial;
        }
    }
}

#if UNITY_EDITOR
[UnityEditor.CustomEditor(typeof(Poof))]
public class PoofEditor : UnityEditor.Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        Poof generator = (Poof)target;

        GUILayout.Space(10);
        if (GUILayout.Button("Test Toon Mesh Poof", GUILayout.Height(30)))
        {
            generator.PlayPoof();
        }
    }
}
#endif