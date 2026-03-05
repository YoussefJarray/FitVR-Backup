namespace FitVR.Core
{
    public class AppStateMachine : IAppStateMachine
    {
        public AppState CurrentState { get; private set; }

        public AppStateMachine()
        {
            CurrentState = AppState.Bootstrap;
        }

        public void ChangeState(AppState newState)
        {
            CurrentState = newState;
        }
    }
}


/* AppStateMachine is a simple implementation of the IAppStateMachine interface. 
It maintains the current state of the application and allows changing it through the ChangeState method. 
The initial state is set to Bootstrap, which can be changed as needed during the application's lifecycle.

*/

// Missing transitions logic and state-specific behavior can be implemented as needed, later