using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// Central static class to manage persistent arcade data (high score, difficulty, and special ammo counts).
/// Saving/loading is handled via JSON so the same file can grow if new fields are added in the future.
/// </summary>
public enum SpearType
{
	None,
	Lightning,
	Gravity,
	Explosive
}

public static class ArcadeSaveSystem
{
	private class SaveData
	{
		public int highestScore = 0;
		public float difficultyMultiplier = 1.0f;
		public Dictionary<SpearType, int> spearAmmo = new Dictionary<SpearType, int>()
		{
			{ SpearType.Lightning, 5 },
			{ SpearType.Gravity, 5 },
			{ SpearType.Explosive, 5 }
		};
	}

	private static SaveData _data = new SaveData();
	private static bool _isLoaded = false;
	private const string SaveFilePath = "user://arcade_save.json";
	private const string LegacyHighScorePath = "user://highscore.save";

	private static void EnsureLoaded()
	{
		if (_isLoaded)
			return;
		LoadStateFromDisk();
		_isLoaded = true;
	}

	public static SpearType SelectedSpear { get; set; } = SpearType.None;

	public static Dictionary<SpearType, int> SpearAmmo
	{
		get
		{
			EnsureLoaded();
			return _data.spearAmmo;
		}
	}

	public static void ResetSpecialAmmo()
	{
		EnsureLoaded();
		_data.spearAmmo[SpearType.Lightning] = 5;
		_data.spearAmmo[SpearType.Gravity] = 5;
		_data.spearAmmo[SpearType.Explosive] = 5;
		SaveStateToDisk();
	}

	public static int HighestScore
	{
		get
		{
			EnsureLoaded();
			return _data.highestScore;
		}
		set
		{
			EnsureLoaded();
			if (value > _data.highestScore)
			{
				_data.highestScore = value;
				SaveStateToDisk();
			}
		}
	}

	public static int MostRecentScore { get; set; } = 0;
	public static int CurrentScore { get; set; } = 0;

	public static float DifficultyMultiplier
	{
		get
		{
			EnsureLoaded();
			return _data.difficultyMultiplier;
		}
		set
		{
			EnsureLoaded();
			_data.difficultyMultiplier = value;
			SaveStateToDisk();
		}
	}

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

	private static void LoadStateFromDisk()
	{
		if (FileAccess.FileExists(SaveFilePath))
		{
			using var file = FileAccess.Open(SaveFilePath, FileAccess.ModeFlags.Read);
			if (file != null)
			{
				var jsonText = file.GetAsText();
				var parseResult = Godot.JSON.ParseString(jsonText);
				if (parseResult.Result == Error.Ok && parseResult.ResultValue is Godot.Collections.Dictionary dict)
				{
					if (dict.TryGetValue("highest_score", out var highestScore))
						_data.highestScore = Convert.ToInt32(highestScore);
					if (dict.TryGetValue("difficulty_multiplier", out var difficultyMultiplier))
						_data.difficultyMultiplier = Convert.ToSingle(difficultyMultiplier);
					if (dict.TryGetValue("spear_ammo", out var ammoObj) && ammoObj is Godot.Collections.Dictionary ammoDict)
					{
						foreach (var key in ammoDict.Keys)
						{
							if (Enum.TryParse(key.ToString(), out SpearType st))
							{
								_data.spearAmmo[st] = Convert.ToInt32(ammoDict[key]);
							}
						}
					}
					GD.Print($"📦 Loaded save data from disk: HighScore={_data.highestScore}");
				}
				else
				{
					GD.PrintErr("Failed to parse save data JSON, trying to load legacy high score.");
					LoadLegacyHighScore();
				}
			}
		}
		else
		{
			LoadLegacyHighScore();
		}
	}

	private static void LoadLegacyHighScore()
	{
		if (FileAccess.FileExists(LegacyHighScorePath))
		{
			using var file = FileAccess.Open(LegacyHighScorePath, FileAccess.ModeFlags.Read);
			if (file != null)
			{
				_data.highestScore = (int)file.Get32();
				GD.Print($"📦 Loaded legacy high score: {_data.highestScore}");
			}
		}
	}

	public static void SaveStateToDisk()
	{
		var dict = new Godot.Collections.Dictionary
		{
			["highest_score"] = _data.highestScore,
			["difficulty_multiplier"] = _data.difficultyMultiplier
		};
		var ammoDict = new Godot.Collections.Dictionary();
		foreach (var kvp in _data.spearAmmo)
			ammoDict[kvp.Key.ToString()] = kvp.Value;
		dict["spear_ammo"] = ammoDict;

		var jsonText = Godot.JSON.Stringify(dict);
		using var file = FileAccess.Open(SaveFilePath, FileAccess.ModeFlags.Write);
		if (file != null)
		{
			file.StoreString(jsonText);
			GD.Print($"💾 Saved game data to disk: HighScore={_data.highestScore}");
		}
	}
}
