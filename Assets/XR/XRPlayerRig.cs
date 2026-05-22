/* this will use the interface we creted in Core that prevents
mini games from accessing the rig directly
check, (Core/Interfaces))
*/

using FitVR.Core;
using UnityEngine;
using UnityEngine.EventSystems;

public class XRPlayerRig : MonoBehaviour, IXRPlayerRig
{
    private static XRPlayerRig _instance;

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
        if (_instance != null)
        {
            DestroyImmediate(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
        ServiceLocator.Register<IXRPlayerRig>(this);
    }

    private void Start()
    {
        var eventSystems = FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
        for (int i = 1; i < eventSystems.Length; i++)
        {
            DestroyImmediate(eventSystems[i].gameObject);
        }
        if (eventSystems.Length > 0)
        {
            eventSystems[0].transform.SetParent(null);
            DontDestroyOnLoad(eventSystems[0].gameObject);
        }
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
            ServiceLocator.Unregister<IXRPlayerRig>();
        }
    }
}
