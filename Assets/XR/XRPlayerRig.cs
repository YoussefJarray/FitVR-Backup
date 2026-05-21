/* this will use the interface we creted in Core that prevents
mini games from accessing the rig directly
check, (Core/Interfaces))
*/

using FitVR.Core;
using Unity.XR.CoreUtils;
using UnityEngine;

public class XRPlayerRig : MonoBehaviour, IXRPlayerRig
{
    [SerializeField]
    private Transform head;

    [SerializeField]
    private Transform leftHand;

    [SerializeField]
    private Transform rightHand;

    public Transform Head => head;
    public Transform LeftHand => leftHand;
    public Transform RightHand => rightHand;

    private void Awake()
    {
        ServiceLocator.Register<IXRPlayerRig>(this);
    }

    private void OnDestroy()
    {
        ServiceLocator.Unregister<IXRPlayerRig>();
    }
}
