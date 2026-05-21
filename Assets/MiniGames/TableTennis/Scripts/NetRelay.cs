using UnityEngine;

/// <summary>
/// Attach to the net trigger collider.
/// Routes ball-crossing events to the NPC so it starts reacting immediately,
/// rather than waiting for the NPC's per-frame poll in UpdateIdle().
/// </summary>
public class NetRelay : MonoBehaviour
{
    [SerializeField] private TableTennisNPC npc;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Ball"))
            npc.OnBallCrossedToNPCSide();
    }
}

// ─────────────────────────────────────────────────────────────────
// NOTE — Player Hit Registration
// ─────────────────────────────────────────────────────────────────
// Your player paddle needs to call TableTennisGameManager.Instance.RegisterHit()
// whenever it makes contact with the ball, e.g.:
//
//   void OnCollisionEnter(Collision col)
//   {
//       if (col.gameObject.CompareTag("Ball"))
//           TableTennisGameManager.Instance.RegisterHit(TableTennisGameManager.LastHit.Player);
//   }
//
// Without this call, lastTouch stays None and rally scoring won't work.
// ─────────────────────────────────────────────────────────────────