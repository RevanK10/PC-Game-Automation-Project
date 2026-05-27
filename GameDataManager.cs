using Godot;
using System;
using System.Text;
using System.Text.Json;
using System.Collections.Generic;

// Explicitly defining the LevelConfig data structure container class
public class LevelConfig
{
	public string LevelId { get; set; }
	public string DifficultyName { get; set; }
	public bool IsBossLevel { get; set; }
	public List<EnemyData> Waves { get; set; }
}

public partial class GameDataManager : Node
{
	public static LevelConfig CurrentLevelData { get; private set; }
	
	private const string GeminiUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent";
	private HttpRequest _httpNode;
	private bool _isRequesting = false;

	public override void _Ready()
	{
		_httpNode = new HttpRequest();
		AddChild(_httpNode);
		_httpNode.RequestCompleted += OnRequestCompleted;

		// FIXED: Changed ArcadeSaveSystem.HighScore to ArcadeSaveSystem.HighestScore
		int warmUpCache = ArcadeSaveSystem.HighestScore;
	}

	public void RequestAutomaticLevel(string difficultySetting)
	{
		if (_isRequesting)
		{
			GD.Print("⏳ Still waiting on a live API response. Ignoring extra click.");
			return;
		}

		string apiKey = OS.GetEnvironment("GEMINI_API_KEY");
		if (string.IsNullOrEmpty(apiKey))
		{
			GD.PrintErr("❌ ERROR: GEMINI_API_KEY environment variable is not set!");
			return;
		}

		_isRequesting = true;
		string fullUrl = $"{GeminiUrl}?key={apiKey}";

		// 1. STRICT SYSTEM INSTRUCTION CONTRACT
		// We explicitly command Gemini to never hallucinate or invent random names.
		string systemInstruction = 
			"You are an automated game-engine balance system. Your sole job is to interpret " +
			"the selected difficulty tier and return a raw JSON configuration dataset balancing enemy stats. " +
			"CRITICAL RULES:\n" +
			"1. You are ONLY allowed to generate enemies chosen from this exact pool of 3 distinct names:\n" +
			"   - 'Big Red Demon'\n" +
			"   - 'Small Brown Robber'\n" +
			"   - 'Medium Silver Soldier'\n" +
			"2. Never use any other names.\n" +
			"3. Do not output markdown text or markdown wrappers like ```json.";

		// 2. AUTOMATIC DIFFICULTY GENERATION RULES WITH VARIANT BOUNDARIES
		string difficultyDirectives = "";
		if (difficultySetting == "Easy")
		{
			difficultyDirectives = "Generate an introductory layout. Include exactly 3 enemies in total (one of each type). " +
								   "For 'Small Brown Robber', set speed around 3.5, health to 60. " +
								   "For 'Medium Silver Soldier', set speed around 2.5, health to 90. " +
								   "For 'Big Red Demon', set speed around 1.5, health to 120. Set exp_reward high (30-50).";
		}
		else if (difficultySetting == "Hard")
		{
			difficultyDirectives = "Generate an intense arcade layout. Include exactly 3 enemies in total (one of each type). " +
								   "Scale stats aggressively! For 'Small Brown Robber', push speed up to 5.5, health to 200. " +
								   "For 'Medium Silver Soldier', push speed to 4.0, health to 300. " +
								   "For 'Big Red Demon', push speed to 3.0, health to 450. Set exp_reward tightly around 10-15.";
		}
		else 
		{
			difficultyDirectives = "Generate a standard, balanced arcade layout. Include exactly 3 enemies in total (one of each type). " +
								   "For 'Small Brown Robber', speed 4.0, health 100. " +
								   "For 'Medium Silver Soldier', speed 3.0, health 180. " +
								   "For 'Big Red Demon', speed 2.0, health 250. Keep exp_reward balanced around 20-25.";
		}

		string promptText = $"{difficultyDirectives} " +
							"Structure: {\"level_id\": string, \"difficulty_name\": string, \"is_boss_level\": bool, " +
							"\"waves\": [{\"name\": string, \"health\": float, \"speed\": float, " +
							"\"scene_path\": \"res://enemies/base_enemy.tscn\", \"scale_uniform\": float, \"scale_y\": float, \"exp_reward\": int}]}";

		var requestBody = new
		{
			contents = new[] { new { parts = new[] { new { text = promptText } } } },
			systemInstruction = new { parts = new[] { new { text = systemInstruction } } },
			generationConfig = new { responseMimeType = "application/json" }
		};

		string jsonPayload = JsonSerializer.Serialize(requestBody);
		string[] headers = new string[] { "Content-Type: application/json" };
		
		GD.Print($"📡 Automatically building live '{difficultySetting}' level configuration from cloud API...");
		_httpNode.Request(fullUrl, headers, HttpClient.Method.Post, jsonPayload);
	}

	private void OnRequestCompleted(long result, long responseCode, string[] headers, byte[] body)
	{
		_isRequesting = false;

		if (responseCode != 200)
		{
			GD.PrintErr($"❌ HTTP Request Failed with code: {responseCode}");
			return;
		}

		try
		{
			string rawResponseText = Encoding.UTF8.GetString(body);
			
			using JsonDocument doc = JsonDocument.Parse(rawResponseText);
			string innerJson = doc.RootElement
				.GetProperty("candidates")[0]
				.GetProperty("content")
				.GetProperty("parts")[0]
				.GetProperty("text").GetString();

			var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
			CurrentLevelData = JsonSerializer.Deserialize<LevelConfig>(innerJson, options);
			
			GD.Print($"🎉 Online AI Generation Success! Loaded Level: {CurrentLevelData.DifficultyName}");
			
			// THE LOADING SCREEN TRICK: Switches scenes to the game arena now that data is successfully saved in RAM
			GetTree().ChangeSceneToFile("res://main.tscn"); 
		}
		catch (Exception e)
		{
			GD.PrintErr($"❌ Failed parsing online AI response data: {e.Message}");
		}
	}
}
