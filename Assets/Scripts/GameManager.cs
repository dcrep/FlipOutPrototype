using System;
using UnityEngine;
using UnityEngine.SceneManagement;

[Serializable]
public enum Scenes
{
    MainMenu,
    Game,
    GameOver,
    DCExperiments
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public ScenesSO scenesSO;

    public Scenes currentScene = Scenes.MainMenu;   // Loading/title scene?

    void Awake()
    {
        Debug.Log("GameManager->Awake()");
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);            
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("GameManager->Start()");
        /*AudioClip clip = Resources.Load<AudioClip>("Music/OVCasual Vol5 House Building Intensity 1");
        if (clip == null)
        {
            Debug.LogError("Failed to load audio clip from Resources folder.");
            return;
        }
        SoundManager.Play(clip, 0.7f);
        SoundManager.Loop();
        Debug.Log("Playing music: " + clip.name);*/
#if UNITY_EDITOR
        // Keep current editor level if in Editor
        //LevelCurrentInternalInit();
#else
        //LoadLevel(GameManager.Level.MainMenu);
#endif
    }

    public void SceneAwake()
    {
        VerifyCurrentScene();
        Debug.Log("GameManager->SceneAwake() for scene: " + SceneManager.GetActiveScene().name);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void SceneStart()
    {
        Debug.Log("GameManager->SceneStart() for scene: " + SceneManager.GetActiveScene().name);
    }

    public void LoadScene(Scenes scene)
    {
        switch (scene)
        {
            case Scenes.MainMenu:
                LoadScene(scenesSO.mainMenuScene);
                currentScene = Scenes.MainMenu;
                break;
            case Scenes.Game:
                LoadScene(scenesSO.gameScene);
                currentScene = Scenes.Game;
                break;
            case Scenes.GameOver:
                LoadScene(scenesSO.gameOverScene);
                currentScene = Scenes.GameOver;
                break;
            case Scenes.DCExperiments:
                LoadScene(scenesSO.DCExperimentsScene);
                currentScene = Scenes.DCExperiments;
                break;
            default:
                Debug.LogError("Unknown scene: " + scene);
                break;
        }
    }

    public void VerifyCurrentScene()
    {
        string activeSceneName = SceneManager.GetActiveScene().name;
        if (activeSceneName == scenesSO.mainMenuScene)
        {
            activeSceneName = scenesSO.mainMenuScene;
            currentScene = Scenes.MainMenu;
        }
        else if (activeSceneName == scenesSO.gameScene)
        {
            currentScene = Scenes.Game;
        }
        else if (activeSceneName == scenesSO.gameOverScene)
        {
            currentScene = Scenes.GameOver;
        }
        else if (activeSceneName == scenesSO.DCExperimentsScene)
        {
            currentScene = Scenes.DCExperiments;
        }
        else
        {
            Debug.LogWarning("Active scene does not match any known scenes in ScenesSO: " + activeSceneName);
        }
    }

    public void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    // Update is called once per frame
    void Update()
    {
        // Quick test: press L to load the configured scene
        if (Input.GetKeyDown(KeyCode.L))
        {
            //LoadScene("xDCExperiments");
            LoadScene(Scenes.DCExperiments);
        }
        
    }
}
