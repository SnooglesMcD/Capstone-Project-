using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    public GameObject pause_menu_ui;
    public GameObject pause_menu_buttons;           
    public FirstPersonController playerController; 
    private bool is_paused = false;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (is_paused)
                Resume();
            else
                Pause();
        }
    }

    void Pause()
    {
        pause_menu_ui.SetActive(true);
        pause_menu_buttons.SetActive(true);
        Time.timeScale = 0f;
        is_paused = true;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        playerController.enabled = false; // stop player movement
    }

    public void Resume()
    {
        pause_menu_ui.SetActive(false);
        pause_menu_buttons.SetActive(false);
        Time.timeScale = 1f;
        is_paused = false;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        playerController.enabled = true; // resume player movement
    }


    public void QuitGame()
    {
        Debug.Log("Quitting game...");
        SceneManager.LoadScene("Main menu");
    }
}
