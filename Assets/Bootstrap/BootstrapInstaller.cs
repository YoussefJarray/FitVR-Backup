using UnityEngine;
using FitVR.Core;

public class BootstrapInstaller : MonoBehaviour
{
    private void Awake()
    {
        // Create core systems
        var stateMachine = new AppStateMachine();
        var sceneLoader = new SceneLoader();
        var gameFlow = new GameFlowManager(sceneLoader, stateMachine);

        // Register services
        ServiceLocator.Register<IAppStateMachine>(stateMachine);
        ServiceLocator.Register<ISceneLoader>(sceneLoader);
        ServiceLocator.Register<IGameFlowManager>(gameFlow);

        // Start application
        gameFlow.LoadLobby();
    }
}


/* this is a simple bootstrap installer that sets up the core systems of the FitVR application. 
It creates instances of the AppStateMachine, SceneLoader, and GameFlowManager, and registers them with a ServiceLocator for easy access throughout the application. 
Finally, it starts the application by loading the lobby scene. */