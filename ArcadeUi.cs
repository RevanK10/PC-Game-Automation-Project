using Godot;

public partial class ArcadeUi : CanvasLayer
{
	[Export] public Control MainMenuPanel;
	[Export] public Control DifficultyMenuPanel;
	[Export] public Control GameOverPanel;
	[Export] public Label ScoreDisplayLabel;

	public override void _Ready()
	{
		HideAllPanels();

		// Now it checks the explicit flag we set when the player dies
		if (ArcadeSaveSystem.IsGameOver)
		{
			ShowGameOver();
			ArcadeSaveSystem.IsGameOver = false; // Reset it so it doesn't show again
		}
		else if (!ArcadeSaveSystem.IsGamePlaying)
		{
			ShowMainMenu();
		}
		else
		{
			Input.MouseMode = Input.MouseModeEnum.Captured;
		}
	}

	public void ShowMainMenu()
	{
		HideAllPanels();
		MainMenuPanel?.Show();
		Input.MouseMode = Input.MouseModeEnum.Visible;
		GetTree().Paused = false;
	}

	// Connected to your StartGameButton 'pressed()' signal
	public void _on_start_menu_button_pressed()
	{
		MainMenuPanel?.Hide();
		DifficultyMenuPanel?.Show();
	}

	public void SelectDifficulty(float multiplier)
	{
		// Save the multiplier for your baseline fallback scaling
		ArcadeSaveSystem.DifficultyMultiplier = multiplier;
		ArcadeSaveSystem.IsGamePlaying = true;

		// Find the manager and tell it which difficulty setting to build a prompt for
		var dataManager = GetTree().Root.GetNodeOrNull<GameDataManager>("Main/GameDataManager");
		if (dataManager != null)
		{
			// Translate the float multiplier into a clean string identifier
			string settingName = "Medium";
			if (multiplier <= 1.0f) settingName = "Easy";
			else if (multiplier >= 2.0f) settingName = "Hard";

			// Contact Gemini using our hidden automated prompt engine
			dataManager.RequestAutomaticLevel(settingName);
		}
		else
		{
			// Fallback: If manager is missing, just reload standard local properties
			GetTree().ReloadCurrentScene();
		}
	}

	public void ShowGameOver()
	{
		HideAllPanels();
		GameOverPanel?.Show();
		Input.MouseMode = Input.MouseModeEnum.Visible;

		if (ScoreDisplayLabel != null)
		{
			ScoreDisplayLabel.Text = $"Most Recent Score: {ArcadeSaveSystem.MostRecentScore}\nHighest Score: {ArcadeSaveSystem.HighestScore}";
		}
	}

	// Connected to your RestartButton 'pressed()' signal
	public void _on_restart_button_pressed()
	{
		ArcadeSaveSystem.IsGamePlaying = true;
		GetTree().ReloadCurrentScene();
	}

	public void HideAllPanels()
	{
		MainMenuPanel?.Hide();
		DifficultyMenuPanel?.Hide();
		GameOverPanel?.Hide();
	}

	// Difficulty Button Direct Callbacks
	public void _on_easy_mode_button_pressed() => SelectDifficulty(1.0f);
	public void _on_medium_mode_button_pressed() => SelectDifficulty(1.5f);
	public void _on_hard_mode_button_pressed() => SelectDifficulty(2.0f);
}
