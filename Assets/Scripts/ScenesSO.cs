using UnityEngine;

[CreateAssetMenu(fileName = "ScenesSO", menuName = "Scriptable Objects/ScenesSO")]
public class ScenesSO : ScriptableObject
{
    // Unfortunately Unity has no built-in SceneReference type
    // There are community implementations, but for now we'll just use strings
    //public SceneReference mainMenuScene;
    public string mainMenuScene;
    public Scenes mainMenuSceneEnum = Scenes.MainMenu;

    public string HotseatLobbyScene;
    public Scenes HotseatLobbySceneEnum = Scenes.LobbyLocal;
    public string OnlineLobbyScene;
    public Scenes OnlineLobbySceneEnum = Scenes.LobbyOnline;
    public string gameScene;
    public Scenes gameSceneEnum = Scenes.Game;
    public string gameOverScene;
    public Scenes gameOverSceneEnum = Scenes.GameOver;
    //public string creditsScene;
    public string DCExperimentsScene;
    public Scenes DCExperimentsSceneEnum = Scenes.Game; //Scenes.DCExperiments;
}
