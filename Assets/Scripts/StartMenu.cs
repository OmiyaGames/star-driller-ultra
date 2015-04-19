using UnityEngine;
using System.Collections;

public class StartMenu : MonoBehaviour
{
    [SerializeField]
    UnityEngine.UI.Text levelName = null;
    
    GameSettings settings = null;

    public bool IsVisible
    {
        get
        {
            return gameObject.activeSelf;
        }
    }

    void Setup()
    {
        if (settings == null)
        {
            // Retrieve settings
            settings = Singleton.Get<GameSettings>();

            // Check if we need to update the menu label
            if (levelName != null)
            {
                // Update the menu label
                levelName.text = settings.CurrentLevel.DisplayName;
            }
        }
    }

    public void Show()
    {
        Setup();
        if (IsVisible == false)
        {
            // Make the game object active
            gameObject.SetActive(true);
        }
    }

    public void Hide()
    {
        // Make the game object inactive
        Setup();
        gameObject.SetActive(false);
    }

    public void OnStartClicked()
    {
        // Hide the panel
        Hide();

        // Transition to the current level
        Time.timeScale = 1;
    }
}
