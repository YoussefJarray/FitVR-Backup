using UnityEngine;
using FitVR.Core;

public class SceneButton : MonoBehaviour
{
    private static IGameFlowManager Flow
    {
        get
        {
            if (!ServiceLocator.IsRegistered<IGameFlowManager>())
                BootstrapInstaller.Initialize();
            return ServiceLocator.Get<IGameFlowManager>();
        }
    }

    public void LoadLobby()
    {
        Flow.LoadLobby();
    }

    public void StartMiniGame(string gameId)
    {
        Flow.StartMiniGame(gameId);
    }

    public void EndMiniGame()
    {
        Flow.EndMiniGame();
    }
}
