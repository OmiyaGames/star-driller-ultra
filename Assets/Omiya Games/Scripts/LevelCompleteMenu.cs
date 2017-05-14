using UnityEngine;
using System.Collections;

public class LevelCompleteMenu : MonoBehaviour
{
    [SerializeField]
    GameObject levelCompletePanel;
    [SerializeField]
    UnityEngine.UI.Text returnToMenuLabel = null;
    [SerializeField]
    UnityEngine.UI.Button nextLevelButton = null;
    [SerializeField]
    UnityEngine.UI.Text completeLabel = null;
    [SerializeField]
    string displayString = "{0} complete!";

    GameSettings settings = null;

    public bool IsVisible
    {
        get
        {
            return levelCompletePanel.activeSelf;
        }
    }

    void Setup()
    {
        if (settings == null)
        {
            // Retrieve settings
            settings = Singleton.Get<GameSettings>();

            // Check if we need to update the menu label
            if ((returnToMenuLabel != null) && (string.IsNullOrEmpty(settings.ReturnToMenuText) == false))
            {
                // Update the menu label
                returnToMenuLabel.text = string.Format(settings.ReturnToMenuText, settings.MenuLevel.DisplayName);
            }

            // Check if we need to disable the next level button
            if((nextLevelButton != null) && (settings.NextLevel == null))
            {
                nextLevelButton.interactable = false;
            }

            // Setup complete label
            if((completeLabel != null) && (string.IsNullOrEmpty(displayString) == false))
            {
                completeLabel.text = string.Format(displayString, settings.CurrentLevel.DisplayName);
            }
        }
    }

    public void Show()
    {
        Setup();
        if (IsVisible == false)
        {
            // Make the game object active
            levelCompletePanel.SetActive(true);
        }
    }

    public void Hide()
    {
        // Make the game object inactive
        Setup();
        levelCompletePanel.SetActive(false);
    }

    public void OnNextLevelClicked()
    {
        // Hide the panel
        Hide();

        // Transition to the current level
        SceneTransition transition = Singleton.Get<SceneTransition>();
        transition.LoadLevel(settings.NextLevel);
    }

    public void OnRestartClicked()
    {
        // Hide the panel
        Hide();

        // Transition to the current level
        SceneTransition transition = Singleton.Get<SceneTransition>();
        transition.LoadLevel(settings.CurrentLevel);
    }

    public void OnReturnToMenuClicked()
    {
        // Hide the panel
        Hide();

        // Transition to the menu
        SceneTransition transition = Singleton.Get<SceneTransition>();
        transition.LoadLevel(settings.MenuLevel);
    }
}
