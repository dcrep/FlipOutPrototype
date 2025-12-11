using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
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
            AudioManager.Mute();
        else
            AudioManager.UnMute();
    }

    public void VolumeChange()
    {
        Debug.Log("Volume changed");
    }

    public void PlayerBackButton()
    {
        InputField playerNameInput = GameObject.Find("PlayerNameInput").GetComponent<InputField>();
        string name = playerNameInput.text;
        if (name != "")
        {
            GameManager.Instance.SetLocalPlayerName(name);
            Debug.Log("Player name changed to: " + name);
        }
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
