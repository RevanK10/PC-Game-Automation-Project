using Godot;
using System;

public partial class ArcadeUi : CanvasLayer
{
	[Export] public Control MainMenuPanel;
	[Export] public Control DifficultyMenuPanel;
	[Export] public Control GameOverPanel;
	[Export] public Control InstructionsPanel;
	[Export] public Control OptionsPanel;
	[Export] public Control LoadingPanel;

	private LineEdit _apiKeyInput;
	private Label _statusLabel;
	private Label _scoreLabel;
	private GameDataManager _gameDataManager;

	public override void _Ready()
	{
		GD.Print($"[ArcadeUI] Initializing... IsGamePlaying: {ArcadeSaveSystem.IsGamePlaying}, IsGameOver: {ArcadeSaveSystem.IsGameOver}");

		// Use exports if assigned, otherwise fall back to name-based lookup
		MainMenuPanel ??= GetNodeOrNull<Control>("MainMenu");
		DifficultyMenuPanel ??= GetNodeOrNull<Control>("DifficultyMenu");
		GameOverPanel ??= GetNodeOrNull<Control>("GameOver");
		InstructionsPanel ??= GetNodeOrNull<Control>("Instructions");
		OptionsPanel ??= GetNodeOrNull<Control>("Options");
		LoadingPanel ??= GetNodeOrNull<Control>("LoadingPanel");

		// Find inputs/labels using unique names
		_apiKeyInput = GetNodeOrNull<LineEdit>("%GeminiApiKeyInput");
		_statusLabel = GetNodeOrNull<Label>("%StatusLabel");
		_scoreLabel = GetNodeOrNull<Label>("%ScoreLabel");
		
		// Find GameDataManager - Prioritize Autoload, then scene
		_gameDataManager = GetNodeOrNull<GameDataManager>("/root/GameDataManager") ?? 
						   GetNodeOrNull<GameDataManager>("/root/Main/GameDataManager");

		// Connect buttons with robust manual setup
		SetupButton("%StartGameButton", OnStartGameButtonPressed);
		SetupButton("%InstructionsButton", OnInstructionsButtonPressed);
		SetupButton("%OptionsButton", OnOptionsButtonPressed);
		
		SetupButton("%EasyModeButton", OnEasyModeButtonPressed);
		SetupButton("%MediumModeButton", OnMediumModeButtonPressed);
		SetupButton("%HardModeButton", OnHardModeButtonPressed);
		SetupButton("%BackFromDifficulty", ShowMainMenu);
		
		SetupButton("%RestartButton", OnRestartButtonPressed);
		SetupButton("%BackFromInstructions", ShowMainMenu);
		SetupButton("%BackFromOptions", ShowMainMenu);

		LoadApiKey();

		// INITIAL STATE LOGIC
					if (ArcadeSaveSystem.IsGamePlaying || (GameDataManager.CurrentLevelData != null && !ArcadeSaveSystem.IsGameOver))
		{
			GD.Print("[ArcadeUI] State: PLAYING. Hiding all menus.");
			HideAllPanels();
			Input.MouseMode = Input.MouseModeEnum.Captured;
		}
		else if (ArcadeSaveSystem.IsGameOver)
		{
			GD.Print("[ArcadeUI] State: GAME OVER. Showing Game Over screen.");
			ShowGameOver();
		}
		else
		{
			GD.Print("[ArcadeUI] State: IDLE. Showing Main Menu.");
			ShowMainMenu();
		}
	}

	public void ShowGameOver()
	{
		HideAllPanels();
		if (GameOverPanel != null) GameOverPanel.Show();
		
		if (_scoreLabel != null)
		{
			_scoreLabel.Text = $"Last Score: {ArcadeSaveSystem.MostRecentScore} | High Score: {ArcadeSaveSystem.HighestScore}";
		}
		Input.MouseMode = Input.MouseModeEnum.Visible;
	}

	private void SetupButton(string uniqueName, Action action)
	{
		var btn = GetNodeOrNull<Button>(uniqueName);
		if (btn != null)
		{
			// Disconnect existing if any (to avoid double connections)
			foreach (var conn in btn.GetSignalConnectionList("pressed"))
			{
				btn.Disconnect("pressed", (Callable)conn["callable"]);
			}
			btn.Pressed += () => action();
			GD.Print($"[ArcadeUI] Connected: {uniqueName}");
		}
		else
		{
			GD.PrintErr($"[ArcadeUI] Button not found: {uniqueName}");
		}
	}

	public void ShowMainMenu()
	{
		GD.Print("[ArcadeUI] Showing Main Menu");
		HideAllPanels();
		if (MainMenuPanel != null) MainMenuPanel.Show();
		Input.MouseMode = Input.MouseModeEnum.Visible;
		
		if (_statusLabel != null) _statusLabel.Text = "";
	}

	public void HideAllPanels()
	{
		MainMenuPanel?.Hide();
		DifficultyMenuPanel?.Hide();
		GameOverPanel?.Hide();
		InstructionsPanel?.Hide();
		OptionsPanel?.Hide();
		LoadingPanel?.Hide();
	}

	public void OnStartGameButtonPressed()
	{
		string key = _apiKeyInput?.Text?.Trim() ?? "";
		if (string.IsNullOrEmpty(key))
		{
			if (_statusLabel != null)
			{
				_statusLabel.Text = "API KEY REQUIRED TO START";
				_statusLabel.Modulate = new Color(1, 0, 0);
			}
			return;
		}

		SaveApiKey(key);
		HideAllPanels();
		if (DifficultyMenuPanel != null) DifficultyMenuPanel.Show();
	}

	public void OnInstructionsButtonPressed()
	{
		HideAllPanels();
		if (InstructionsPanel != null) InstructionsPanel.Show();
	}

	public void OnOptionsButtonPressed()
	{
		HideAllPanels();
		if (OptionsPanel != null) OptionsPanel.Show();
	}

	public void OnEasyModeButtonPressed() => StartWithDifficulty("Easy");
	public void OnMediumModeButtonPressed() => StartWithDifficulty("Medium");
	public void OnHardModeButtonPressed() => StartWithDifficulty("Hard");

	private void StartWithDifficulty(string difficulty)
	{
		GD.Print($"🚀 ArcadeUI: Starting Game - Difficulty: {difficulty}");
		
		string key = _apiKeyInput?.Text?.Trim() ?? "";
		if (string.IsNullOrEmpty(key))
		{
			ShowMainMenu();
			if (_statusLabel != null) _statusLabel.Text = "API KEY LOST? RE-ENTER IT";
			return;
		}
		
		HideAllPanels();
		if (LoadingPanel != null) LoadingPanel.Show();

		if (_gameDataManager != null)
		{
			_gameDataManager.RequestAutomaticLevel(difficulty);
		}
		else
		{
			GD.PrintErr("❌ ArcadeUI: GameDataManager is NULL!");
			if (_statusLabel != null) _statusLabel.Text = "GAME ENGINE ERROR: DATA MANAGER MISSING";
			ShowMainMenu();
		}
	}

	public void OnRestartButtonPressed()
	{
		// Reset game state and clear level data to return to Main Menu on restart.
		ArcadeSaveSystem.ResetGame();
		GameDataManager.ResetCurrentLevelData();
		GetTree().ReloadCurrentScene();
	}

	private void SaveApiKey(string key)
	{
		try 
		{
			using var file = FileAccess.Open("user://gemini_api_key.txt", FileAccess.ModeFlags.Write);
			if (file != null)
			{
				file.StoreString(key);
				GD.Print("🔑 ArcadeUI: API Key saved.");
			}
		}
		catch (Exception e)
		{
			GD.PrintErr($"❌ ArcadeUI: Failed to save API Key: {e.Message}");
		}
	}

	private void LoadApiKey()
	{
		if (FileAccess.FileExists("user://gemini_api_key.txt"))
		{
			try
			{
				using var file = FileAccess.Open("user://gemini_api_key.txt", FileAccess.ModeFlags.Read);
				if (file != null)
				{
					string key = file.GetAsText().Trim();
					if (_apiKeyInput != null) _apiKeyInput.Text = key;
					GD.Print("🔑 ArcadeUI: API Key loaded.");
				}
			}
			catch (Exception e)
			{
				GD.PrintErr($"❌ ArcadeUI: Failed to load API Key: {e.Message}");
			}
		}
	}
}
