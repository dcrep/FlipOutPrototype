using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PauseScript : MonoBehaviour
{
    public void ResumeGamePressed()
    {
        Debug.Log("Resume menu button!");
        GameManager.Instance.ResumeGame();
    }
    public void MainMenuButtonPressed()
    {
        Debug.Log("Main menu button!");
        // This functionality has been moved to GameManager.LoadScene()
        //GameManager.Instance.uiManager.PauseMenuClose();
        GameManager.Instance.LoadScene(Scenes.MainMenu);
    }
}
