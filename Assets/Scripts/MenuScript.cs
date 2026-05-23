using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public void PlayGame()
    {
        SceneManager.LoadScene("SampleScene");
    }

    public void QuitGame()
    {
        Debug.Log("QUIT!");
        Application.Quit();
    }

    public void OpenOptions()
    {
        Debug.Log("Options menu opened");
    }

    public void OpenUpgrade()
    {
        SceneManager.LoadScene("WeaponUpgrade");
    }

    public void OpenMenu()
    {
        SceneManager.LoadScene("GameMenu");
    }
}