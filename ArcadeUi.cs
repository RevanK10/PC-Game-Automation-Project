using Godot;
using System;

public partial class ArcadeUi : CanvasLayer
{
	[Export] public Control MainMenuPanel;
	[Export] public Control DifficultyMenuPanel;
	[Export] public Control GameOverPanel;
	[Export] public Control LoadingPanel; 
	[Export] public Label ScoreDisplayLabel;
	
	// ADDED: Drag your new LineEdit node here in the Inspector
	[Export] public LineEdit ApiKeyInput; 

	public override void _Ready()
	{
		HideAllPanels();

		// NOW AUTOMATED: If the player already saved a key previously, auto-fill it for convenience!
		if (ApiKeyInput != null && FileAccess.FileExists("user://api_key.txt"))
		{
			using var file = FileAccess.Open("user://api_key.txt", FileAccess.ModeFlags.Read);
			ApiKeyInput.Text = file.GetAsText().Trim();
		}

		if (ArcadeSaveSystem.IsGameOver)
		{
			ShowGameOver();
			ArcadeSaveSystem.IsGameOver = false; 
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
		// MODIFIED: Save the text from the LineEdit to local storage right when they click "Play"
		if (ApiKeyInput != null)
		{
			string enteredKey = ApiKeyInput.Text.StripEdges();
			if (!string.IsNullOrEmpty(enteredKey))
			{
				using var file = FileAccess.Open("user://api_key.txt", FileAccess.ModeFlags.Write);
				file.StoreString(enteredKey);
			}
		}

		MainMenuPanel?.Hide();
		DifficultyMenuPanel?.Show();
	}

	public void SelectDifficulty(float multiplier)
	{
		ArcadeSaveSystem.DifficultyMultiplier = multiplier;
		ArcadeSaveSystem.IsGamePlaying = true;

		DifficultyMenuPanel?.Hide();
		LoadingPanel?.Show();

		var dataManager = GetTree().Root.GetNodeOrNull<GameDataManager>("Main/GameDataManager");
		if (dataManager != null)
		{
			string settingName = "Medium";
			if (multiplier <= 1.0f) settingName = "Easy";
			else if (multiplier >= 2.0f) settingName = "Hard";

			dataManager.RequestAutomaticLevel(settingName);
		}
		else
		{
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

	public void _on_restart_button_pressed()
	{
		ArcadeSaveSystem.IsGamePlaying = false;
		ArcadeSaveSystem.IsGameOver = false;
		GetTree().ReloadCurrentScene();
	}

	public void HideAllPanels()
	{
		MainMenuPanel?.Hide();
		DifficultyMenuPanel?.Hide();
		GameOverPanel?.Hide();
		LoadingPanel?.Hide();
	}

	public void _on_easy_mode_button_pressed() => SelectDifficulty(1.0f);
	public void _on_medium_mode_button_pressed() => SelectDifficulty(1.5f);
	public void _on_hard_mode_button_pressed() => SelectDifficulty(2.0f);
}
