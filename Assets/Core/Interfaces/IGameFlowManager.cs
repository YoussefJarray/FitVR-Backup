namespace FitVR.Core
{
    public interface IGameFlowManager
    {
        void LoadLobby();
        void StartMiniGame(string gameId);
        void EndMiniGame();
    }
}


// just for testing , missing implementations
//add stuff like ResultScreen , pause , restart etc. later on

