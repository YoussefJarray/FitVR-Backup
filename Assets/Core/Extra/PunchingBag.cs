using UnityEngine;

public class PunchingBag : MonoBehaviour
{
    [Header("Hinge Setup")]
    [SerializeField] private Transform attachPoint; // The ceiling/stand mount point where the bag hangs
    [SerializeField] private Rigidbody bagRigidbody; // The Rigidbody on the punching bag itself

    [Header("Physics Settings")]
    [SerializeField] private float bagMass = 15f;
    [SerializeField] private float linearDamping = 0.5f;
    [SerializeField] private float angularDamping = 0.5f;

    [Header("Punch Settings")]
    [SerializeField] private float punchForceMultiplier = 12f;
    [SerializeField] private float minimumVelocityToPunch = 0.3f;

    private ConfigurableJoint joint;

    private void Start()
    {
        SetupComponents();
        CreateConfigurableJoint();
    }

    private void SetupComponents()
    {
        Rigidbody localRb = GetComponent<Rigidbody>();
        if (localRb != null && localRb != bagRigidbody)
        {
            localRb.isKinematic = true;
        }

        if (bagRigidbody == null)
        {
            Debug.LogError("PunchingBag: Please assign the specific Punching Bag Rigidbody component in the inspector!", this);
            return;
        }

        bagRigidbody.mass = bagMass;
        bagRigidbody.linearDamping = linearDamping;
        bagRigidbody.angularDamping = angularDamping;
        bagRigidbody.useGravity = true;
        bagRigidbody.isKinematic = false;
        bagRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        // Ensure the moving bag collider is also non-trigger for hard physics contact
        Collider bagCollider = bagRigidbody.GetComponent<Collider>();
        if (bagCollider != null)
        {
            bagCollider.isTrigger = false;
        }
    }

    private void CreateConfigurableJoint()
    {
        if (attachPoint == null || bagRigidbody == null) return;

        joint = bagRigidbody.gameObject.GetComponent<ConfigurableJoint>();
        if (joint == null)
        {
            joint = bagRigidbody.gameObject.AddComponent<ConfigurableJoint>();
        }
        
        joint.xMotion = ConfigurableJointMotion.Locked;
        joint.yMotion = ConfigurableJointMotion.Locked;
        joint.zMotion = ConfigurableJointMotion.Locked;

        joint.angularXMotion = ConfigurableJointMotion.Free;
        joint.angularYMotion = ConfigurableJointMotion.Free;
        joint.angularZMotion = ConfigurableJointMotion.Free;

        Rigidbody anchorRb = attachPoint.GetComponent<Rigidbody>();
        if (anchorRb != null)
        {
            joint.connectedBody = anchorRb;
        }
        else
        {
            joint.connectedAnchor = attachPoint.position;
        }

        Vector3 localAnchorOffset = bagRigidbody.transform.InverseTransformPoint(attachPoint.position);
        joint.anchor = localAnchorOffset;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (bagRigidbody == null) return;

        // Check if hit by the new physical Boxing Collider
        Rigidbody handRb = collision.rigidbody;
        if (handRb != null)
        {
            // Use the tracking script/rigidbody linear velocity
            Vector3 handVelocity = handRb.linearVelocity;

            // If the parent tracking overrides velocity properties, fallback to checking relative magnitude via contact point details
            if (handVelocity.magnitude < minimumVelocityToPunch && collision.relativeVelocity.magnitude > minimumVelocityToPunch)
            {
                handVelocity = -collision.relativeVelocity;
            }

            if (handVelocity.magnitude > minimumVelocityToPunch)
            {
                Vector3 punchForce = handVelocity * punchForceMultiplier;
                
                // Perfect physical impulse registration at the exact hit point
                bagRigidbody.AddForceAtPosition(punchForce, collision.contacts[0].point, ForceMode.Impulse);
            }
        }
    }
}