using UnityEngine;

/// <summary>
/// Attach this to the ball prefab (same GameObject as the Rigidbody).
///
/// It forwards every collision and trigger event to TableTennisGameManager
/// so the manager can apply scoring logic without polling every frame.
///
/// Setup checklist
/// ───────────────
///  • Ball prefab     — has Rigidbody + Collider + this script + tag "Ball"
///  • Player side     — BoxCollider (isTrigger = false), tag anything, assigned to playerSideCollider
///  • CPU side        — same
///  • Net             — BoxCollider (isTrigger = true),  assigned to netCollider
///  • Floor           — large flat BoxCollider covering the ground, assigned to floorCollider
///                      Place it well below the table so off-table balls hit it.
/// </summary>
public class BallCollision : MonoBehaviour
{
    // ── Collision (non-trigger surfaces: table sides, floor) ──────
    private void OnCollisionEnter(Collision col)
    {
        TableTennisGameManager.Instance?.RegisterCollision(col.collider, gameObject);
    }

    // ── Trigger (net) ─────────────────────────────────────────────
    private void OnTriggerEnter(Collider other)
    {
        TableTennisGameManager.Instance?.RegisterCollision(other, gameObject);
    }
}