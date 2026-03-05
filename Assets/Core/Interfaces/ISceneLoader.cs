namespace FitVR.Core
{
    public interface ISceneLoader
    {
        void LoadScene(string sceneName);
    }
}


// using this so no other system can call the scene manager directly, and we can swap out the implementation if we want to change how scenes are loaded (e.g. async loading, loading screens, etc.)
// this will make sure that all scene loading goes through this interface, and we can easily change the implementation without affecting the rest of the codebase.
//preventing direct calls to the scene manager 
//also allows us to add additional functionality in the future, such as tracking loading progress, 
//handling errors, or adding custom loading screens without having to change the rest of the codebase.

