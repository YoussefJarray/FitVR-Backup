namespace FitVR.Core
{
    public interface IAppStateMachine
    {
        AppState CurrentState { get; }
        void ChangeState(AppState newState);
    }
}
