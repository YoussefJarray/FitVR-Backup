// for testing fake 

using UnityEngine;
using System.Collections;
using FitVR.Core;

public class FakeGameController : MonoBehaviour
{
    private void Start()
    {
        StartCoroutine(ReturnToLobbyAfterDelay());
    }

    private IEnumerator ReturnToLobbyAfterDelay()
    {
        yield return new WaitForSeconds(5f);

        ServiceLocator
            .Get<IGameFlowManager>()
            .EndMiniGame();
    }
}
