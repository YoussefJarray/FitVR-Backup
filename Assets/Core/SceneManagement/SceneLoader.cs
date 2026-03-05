using UnityEngine;
using UnityEngine.SceneManagement;

namespace FitVR.Core
{
    public class SceneLoader : ISceneLoader
    {
        public void LoadScene(string sceneName)
        {
            SceneManager.LoadScene(sceneName);
        }
    }
}


/* not monobehaviour, 
so we can use it in any state without worrying about scene loading order.

GameFlowManager will use it.
Bootstrap will register it.

*/