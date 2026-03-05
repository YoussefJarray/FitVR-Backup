using UnityEngine;

namespace FitVR.Core
{
    public interface IXRPlayerRig
    {
        Transform Head { get; }
        Transform LeftHand { get; }
        Transform RightHand { get; }
    }
}



/* this will not let mini games reference the XRPlayerRig directly,
 but instead will allow them to reference the interface, 
 which will be implemented by the XRPlayerRig class. 
 This way, mini games can use the interface 
 to access the player's head and hand transforms
  without needing to know about the specific implementation of the XRPlayerRig class.
*/