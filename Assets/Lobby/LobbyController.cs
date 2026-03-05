using UnityEngine;
using FitVR.Core;

public class LobbyController : MonoBehaviour
{
    public void StartFakeGame()
    {
        ServiceLocator
            .Get<IGameFlowManager>()
            .StartMiniGame("BoxingScene");
    }
}


/* fake for testing , will change later 

*/