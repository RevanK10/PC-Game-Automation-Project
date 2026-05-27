using Godot;
using System;

public static class ArcadeSaveSystem
{
	private static int _highestScore = 0;
	private const string SaveFilePath = "user://highscore.save";

	// UPGRADED: HighestScore now automatically checks the local disk and auto-saves records
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
	public static float DifficultyMultiplier { get; set; } = 1.0f;
	public static bool IsGamePlaying { get; set; } = false;
	public static bool IsGameOver { get; set; } = false; 

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
