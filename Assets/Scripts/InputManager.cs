using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    public InputSystem_Actions playerControls;
    //private InputAction numKeyAction;
    //private InputAction mouseUpDown;
    private InputAction mouseRightClickAction;
    private InputAction mouseWheelAction;

    public PlayerX activePlayer = null;

    // UI Manager (?)
    GameObject pauseMenuPrefab = null;
    GameObject pauseMenuInstance = null;
    bool pauseMenuOpen = false;

    void Awake()
    {
        playerControls = new InputSystem_Actions();

        // UI Manager:
        /*pauseMenuPrefab = Resources.Load<GameObject>("Prefabs/" + "PauseModalDialog");
        if (pauseMenuPrefab == null)
        {
            Debug.Log("Pause menu prefab not found!");
            return;
        }*/
    }
    void OnEnable()
    {
        // Enable ALL Player Controls on the new InputSystem (optional if we only use specific subsets)
        playerControls.Enable();
        
        mouseRightClickAction = playerControls.Player.RightClick;
        mouseRightClickAction.Enable();
        mouseRightClickAction.performed += MouseRightButtonPressed;
        mouseRightClickAction.canceled += MouseRightButtonReleased;

        mouseWheelAction = playerControls.Player.ScrollWheel;
        mouseWheelAction.performed += MouseWheelScrolled;
        mouseWheelAction.Enable();

    }
    void OnDisable()
    {
        mouseWheelAction.performed -= MouseWheelScrolled;
        mouseWheelAction.Disable();

        mouseRightClickAction.performed -= MouseRightButtonPressed;
        mouseRightClickAction.canceled -= MouseRightButtonReleased;
        mouseRightClickAction.Disable();

        // Disable ALL player Controls on the new InputSystem
        playerControls.Disable();
    }
    // Update is called once per frame
    void Update()
    {
        if (playerControls.Player.Pause.triggered)
        {
            Debug.Log("Esc/Pause triggered!");
            //PauseMenuOpen();
            //UIManager.Instance.PauseMenu();
        }
        else if (playerControls.Player.Mute.triggered)
        {
            //GameManager.Instance.MuteGame();
            AudioManager.PauseToggle();
        }
        else if (playerControls.Player.Quit.triggered)
        {
            //Debug.Log("Quit triggered!");
            GameManager.Quit();
        }
    }
    /*public bool PauseMenuClose()
    {
        if (pauseMenuOpen)
        {
            pauseMenuInstance.SetActive(false);
            Destroy(pauseMenuInstance);
            pauseMenuInstance = null;
            Debug.Log("Pause menu closed!");
            // Unfreeze game
            //Time.timeScale = 1;           
            GameManager.Instance.ResumeGame();
            pauseMenuOpen = false;
        }
        return true;
    }
    private bool PauseMenuOpen()
    {
        if (pauseMenuOpen)
        {
            return PauseMenuClose();                
        }
        else if (GameManager.gameState == GameManager.GameState.Playing)
        {
            Debug.Log("Pause triggered!");
            if (pauseMenuPrefab != null)
            {
                pauseMenuInstance = Instantiate(pauseMenuPrefab, Vector3.zero, Quaternion.identity);
                if (pauseMenuInstance == null)
                {
                    Debug.LogError("Pause menu prefab not found!");
                    return false;
                }
                var canvas = GameObject.Find("Canvas");
                if (canvas == null)
                {
                    Debug.LogError("Canvas not found for Pause Menu!");
                    return false;
                }
                pauseMenuInstance.transform.SetParent(GameObject.Find("Canvas").transform, false);
                pauseMenuInstance.SetActive(true);

                GameManager.Instance.PauseGame();
                pauseMenuOpen = true;
            }
        }
        return pauseMenuOpen;
    }*/

    void MouseRightButtonPressed(InputAction.CallbackContext context)
    {
        if (GameManager.Instance.currentGameState != GameState.Playing)
            return;

        // Ignore for now..
    }
    void MouseRightButtonReleased(InputAction.CallbackContext context)
    {
        // Ignore for now..
    }

    private void MouseWheelScrolled(InputAction.CallbackContext context)
    {
        if (GameManager.Instance.currentGameState != GameState.Playing)
            return;
        //Debug.Log("Mouse Wheel scrolled!");
        float scrollValue = context.ReadValue<float>();
        if (Math.Abs(scrollValue) < 0.01)
            return; // Ignore small scroll values

        /*
        // TODO: change? Doesn't seem all that noticeable
        // ! Maybe move this into Update() checks instead of Input event system due to EventSystem last-frame warning
        if (EventSystem.current.IsPointerOverGameObject())
        {
            Debug.Log("Mouse Wheel scrolled over UI! [maybe should unsubscribe from event and check in Update() loop because of EventSystem last-frame error]");
            return; // Ignore if mouse is over UI element
        }
        GameManager.Instance.cameraMovement.CameraZoomOnUpdate(scrollValue / 120);
        */
    }

}
