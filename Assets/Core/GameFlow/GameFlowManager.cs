using UnityEngine;

namespace FitVR.Core
{
    public class GameFlowManager : IGameFlowManager
    {
        private readonly ISceneLoader _sceneLoader;
        private readonly IAppStateMachine _stateMachine;

        private string _currentMiniGame;

        public GameFlowManager(ISceneLoader sceneLoader, IAppStateMachine stateMachine)
        {
            _sceneLoader = sceneLoader;
            _stateMachine = stateMachine;
        }

        public void LoadLobby()
        {
            _stateMachine.ChangeState(AppState.Lobby);
            _sceneLoader.LoadScene("LobbyScene");
        }

        public void StartMiniGame(string gameId)
        {
            _currentMiniGame = gameId;

            _stateMachine.ChangeState(AppState.LoadingMiniGame);

            // For now, gameId == scene name                 //// Maybe keep it like this for now, since we don't have a lot of mini-games
            _sceneLoader.LoadScene(gameId);

            _stateMachine.ChangeState(AppState.PlayingMiniGame);
        }

        public void EndMiniGame()
        {
            _currentMiniGame = null;
            LoadLobby();
        }
    }
}


/* our gameFlowManager aka the "director" of our game, is responsible for managing the flow of the game, 
such as loading scenes and changing states. 
It uses an ISceneLoader to load scenes and an IAppStateMachine to manage the app's state. 
The LoadLobby method loads the lobby scene and changes the state to Lobby. 
The StartMiniGame method takes a gameId (which is currently the same as the scene name), changes the state to LoadingMiniGame, loads the mini-game scene, 
and then changes the state to PlayingMiniGame. The EndMiniGame method resets the current mini-game and loads the lobby again. */

/* Code explanation : 
- The GameFlowManager class implements the IGameFlowManager interface, which defines the contract for managing game flow.
- The constructor takes an ISceneLoader and an IAppStateMachine as dependencies, which are used to load scenes and manage app states, respectively.
- The LoadLobby method changes the app state to Lobby and loads the "LobbyScene".
- The StartMiniGame method sets the current mini-game, changes the state to LoadingMiniGame
    and loads the scene corresponding to the gameId, then changes the state to PlayingMiniGame.
- The EndMiniGame method resets the current mini-game and calls LoadLobby to return to the lobby scene. */

// and obviously not a monobehaviour, since it's a pure logic class that doesn't need to be attached to a GameObject.