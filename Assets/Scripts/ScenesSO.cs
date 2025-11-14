using UnityEngine;

[CreateAssetMenu(fileName = "ScenesSO", menuName = "Scriptable Objects/ScenesSO")]
public class ScenesSO : ScriptableObject
{
    // Unfortunately Unity has no built-in SceneReference type
    // There are community implementations, but for now we'll just use strings
    //public SceneReference mainMenuScene;
    public string mainMenuScene;
    public string gameScene;
    public string gameOverScene;
    //public string creditsScene;
    public string DCExperimentsScene;
}
