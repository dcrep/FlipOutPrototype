using UnityEngine;
using UnityEditor;

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
