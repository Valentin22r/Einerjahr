using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [Tooltip("Catalogue de divinités utilisé pour fallback si aucune n'a été sélectionnée avant de lancer la partie.")]
    public DivinityCatalog divinityCatalog;

    [Tooltip("Nom de la scène in-game lancée par PlayGame() / LobbyPlayGame().")]
    public string gameSceneName = "SampleScene";

    public void PlayGame()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    /// <summary>
    /// Lance la partie depuis le lobby (WeaponUpgrade). Vérifie qu'une divinité est
    /// sélectionnée — sinon utilise la default du catalog. Empêche de lancer "à vide".
    /// </summary>
    public void LobbyPlayGame()
    {
        if (DivinitySelection.Selected == null)
        {
            var fallback = DivinitySelection.GetOrFallback(divinityCatalog);
            if (fallback == null)
            {
                Debug.LogWarning("[MainMenu] Aucune divinité sélectionnée et aucun catalog. La partie ne lance pas.");
                return;
            }
            DivinitySelection.Select(fallback);
        }
        SceneManager.LoadScene(gameSceneName);
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