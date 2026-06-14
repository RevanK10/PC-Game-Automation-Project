using Godot;
using System;

// --- THE STRUCTURAL FIX: GLOBAL ENUM DECLARATION ---
public enum SpearType
{
	None,
	Lightning,
	Gravity,
	Explosive
}

public static class ArcadeSaveSystem
{
	private static int _highestScore = 0;
	private const string SaveFilePath = "user://highscore.save";
	

	// Tracks which modification type was selected in the main menu
	public static SpearType SelectedSpear { get; set; } = SpearType.None;
	public static System.Collections.Generic.Dictionary<SpearType, int> SpearAmmo { get; set; } = 
	 new System.Collections.Generic.Dictionary<SpearType, int>()
	{
		{ SpearType.Lightning, 5 },
		{ SpearType.Gravity, 5 },
		{ SpearType.Explosive, 5 }
	};
	
	public static void ResetSpecialAmmo()
	{
		SpearAmmo[SpearType.Lightning] = 5;
		SpearAmmo[SpearType.Gravity] = 5;
		SpearAmmo[SpearType.Explosive] = 5;
	}

	public static int HighestScore 
	{ 
		get
		{
			// If local RAM variable is zero, see if a score was previously saved to the hard drive
			if (_highestScore == 0)
			{
				LoadHighScoreFromDisk();
			}
			return _highestScore;
		}
		set
		{
			// Only overwrite and rewrite the local file if they beat their old personal best record
			if (value > _highestScore)
			{
				_highestScore = value;
				SaveHighScoreToDisk();
			}
		}
	}

	public static int MostRecentScore { get; set; } = 0;
	public static int CurrentScore { get; set; } = 0;
	public static float DifficultyMultiplier { get; set; } = 1.0f;
	public static bool IsGamePlaying { get; set; } = false;
	public static bool IsGameOver { get; set; } = false; 
	public static bool PlayerDied { get; set; } = false;

	public static void ResetGame()
	{
		IsGamePlaying = false;
		IsGameOver = false;
		PlayerDied = false;
		CurrentScore = 0;
		ResetSpecialAmmo();
	}

	private static void SaveHighScoreToDisk()
	{
		using var file = FileAccess.Open(SaveFilePath, FileAccess.ModeFlags.Write);
		if (file != null)
		{
			file.Store32((uint)_highestScore);
			GD.Print($"💾 Permanent Storage Success! New Highest Score saved to local disk: {_highestScore}");
		}
	}

	private static void LoadHighScoreFromDisk()
	{
		if (!FileAccess.FileExists(SaveFilePath)) return;

		using var file = FileAccess.Open(SaveFilePath, FileAccess.ModeFlags.Read);
		if (file != null)
		{
			_highestScore = (int)file.Get32();
			GD.Print($"📦 Welcome Back! Loaded persistent highest score from local disk: {_highestScore}");
		}
	}
}
