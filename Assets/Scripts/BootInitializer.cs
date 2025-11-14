using UnityEngine;

// Important: Create prefab named 'BootInitializer' in Resources folder and attach this script to it
// (actually, attaching is optional, but this way it can use MonoBehaviour and more of Unity's features)
public class BootInitializer : MonoBehaviour {
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void Load() {
        //Debug.Log("BootInitializer->Load()");
	    GameObject bootInit = GameObject.Instantiate(Resources.Load("BootInitializer")) as GameObject;
	    GameObject.DontDestroyOnLoad(bootInit);
        
        // !! IMPORTANT: Order of Initialization is important in case there's a dependency on another script !!
        // SoundManager create object + script component (can also be done in Script
        // with RuntimeInitializeOnLoadMethod, but this way keeps it centralized)
        /*GameObject soundManagerObject = new("SoundManager");
        soundManagerObject.AddComponent<SoundManager>();
        DontDestroyOnLoad(soundManagerObject);
        Debug.Log("[BI]: SoundManager initialized.");*/

        // GameManager create object + script component (can also be done in Script
        // with RuntimeInitializeOnLoadMethod, but this way keeps it centralized)
        GameObject gameManagerObject = new("GameManager");
        gameManagerObject.AddComponent<GameManager>();
        DontDestroyOnLoad(gameManagerObject);
        Debug.Log("[BI]: GameManager initialized..");

        // Input Manager
        /*GameObject inputManagerObject = new("InputManager");
        inputManagerObject.AddComponent<InputManager>();
        DontDestroyOnLoad(inputManagerObject);
        Debug.Log("[BI]: InputManager initialized..");*/

		// GameManager - reference InputManager
        //gameManagerObject.GetComponent<GameManager>().inputManager = inputManagerObject.GetComponent<InputManager>();
    } 
}