using UnityEngine;
using FitVR.Core;

public static class BootstrapInstaller
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void Initialize()
    {
        var stateMachine = new AppStateMachine();
        var sceneLoader = new SceneLoader();
        var gameFlow = new GameFlowManager(sceneLoader, stateMachine);

        ServiceLocator.Register<IAppStateMachine>(stateMachine);
        ServiceLocator.Register<ISceneLoader>(sceneLoader);
        ServiceLocator.Register<IGameFlowManager>(gameFlow);

        if (SettingsManager.Instance == null)
        {
            var go = new GameObject("SettingsManager");
            go.AddComponent<SettingsManager>();
        }
    }
}


/* this is a simple bootstrap installer that sets up the core systems of the FitVR application. 
It creates instances of the AppStateMachine, SceneLoader, and GameFlowManager, and registers them with a ServiceLocator for easy access throughout the application. 
Finally, it starts the application by loading the lobby scene. */