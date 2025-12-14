using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    
    public Button[] buttons;
    public InputField playerNameInput;

    public Slider mainVolumeSlider;
    public Toggle muteToggle;

    public void Awake()
    {
        foreach (Button b in buttons)
        {
            b.onClick.AddListener(ButtonSound);
        }
        playerNameInput.onEndEdit.AddListener(SetPlayerName);
        mainVolumeSlider.onValueChanged.AddListener(delegate { VolumeChange(); });
        muteToggle.onValueChanged.AddListener(delegate { MuteToggle(muteToggle.isOn); });
    }

    void Start()
    {
        playerNameInput.text = PlayerPreferences.Instance.playerName;
        mainVolumeSlider.value = PlayerPreferences.Instance.mainVolume;
        muteToggle.isOn = PlayerPreferences.Instance.mainMuted;
    }

    public void ButtonSound()
    {
        AudioManager.PlaySoundAt(AudioManager.audioSourcesSO.UIMenuClick, 1f);
    }

    public void StartLocalGameButton()
    {
        GameManager.Instance.LoadScene(Scenes.LobbyLocal);
    }
    public void StartOnlineGameButton()
    {
        GameManager.Instance.LoadScene(Scenes.LobbyOnline);
    }

    public void MuteToggle(bool isMuted)
    {
        Debug.Log("Mute called, bool = " + isMuted);
        if (isMuted)
        {
            AudioManager.Mute();
            AudioListener.volume = 0f;
            PlayerPreferences.Instance.mainMuted = true;
            PlayerPreferences.Instance.SavePreferences();
        }
        else
        {
            AudioManager.UnMute();
            AudioListener.volume = PlayerPreferences.Instance.mainVolume;
            PlayerPreferences.Instance.mainMuted = false;
            PlayerPreferences.Instance.SavePreferences();
        }
    }

    public void VolumeChange()
    {
        PlayerPreferences.Instance.SetMainVolume(mainVolumeSlider.value);
        Debug.Log("Volume changed to " + mainVolumeSlider.value);
    }

    public void PlayerBackButton()
    {
        /*InputField playerNameInput = GameObject.Find("PlayerNameInput").GetComponent<InputField>();
        string name = playerNameInput.text;
        if (name != "")
        {
            GameManager.Instance.SetLocalPlayerName(name);
            Debug.Log("Player name changed to: " + name);
        }*/
    }

    public void SetPlayerName(string name)
    {
        //InputField playerNameInput = GameObject.Find("PlayerNameInput").GetComponent<InputField>();
        PlayerPreferences.Instance.SetPlayerName(name);
        Debug.Log("Player name set to: " + name);
    }

    public void QuitButton()
    {
#if UNITY_EDITOR
        EditorApplication.ExitPlaymode();
#else
        Application.Quit(); // For standalone builds
#endif
        Debug.Log("Player Has Quit the Game");
    }
}
