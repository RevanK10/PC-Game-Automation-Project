using Godot;
using System;
using System.Text;
using System.Text.Json;
using System.Collections.Generic;

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
	
	// Hardcoded URL endpoint for the Gemini API model
	private const string GeminiUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent";
	
	private HttpRequest _httpNode;

	public override void _Ready()
	{
		// Add a temporary HTTPRequest node to the tree dynamically
		_httpNode = new HttpRequest();
		AddChild(_httpNode);
		_httpNode.RequestCompleted += OnRequestCompleted;
	}

	/// <summary>
	/// Triggered automatically by the Main Menu UI selections (Easy, Medium, Hard)
	/// </summary>
	public void RequestAutomaticLevel(string difficultySetting)
	{
		// Fetch your API Key from system environment variables for security
		string apiKey = OS.GetEnvironment("GEMINI_API_KEY");
		
		if (string.IsNullOrEmpty(apiKey))
		{
			GD.PrintErr("❌ ERROR: GEMINI_API_KEY environment variable is not set on this machine!");
			// Fallback to avoid freezing the player if the API key is missing
			GetTree().ReloadCurrentScene(); 
			return;
		}

		string fullUrl = $"{GeminiUrl}?key={apiKey}";

		// 1. HIDDEN SYSTEM PROMPT ENGINE
		// Dictates exactly how the AI must interpret our arcade environment rules
		string systemInstruction = 
			"You are an automated game-engine balance system. Your sole job is to interpret " +
			"the selected difficulty tier and return a raw JSON configuration dataset balancing enemy stats. " +
			"Do not output markdown text or markdown wrappers like ```json.";

		// 2. AUTOMATIC DIFFICULTY GENERATION RULES
		// Explicitly tells Gemini what kind of math boundaries to apply based on the menu button clicked
		string difficultyDirectives = "";
		if (difficultySetting == "Easy")
		{
			difficultyDirectives = "Generate an introductory layout. Include 3 to 4 total enemies. " +
								   "Keep enemy movement speeds below 3.5 and health capped below 120.0. " +
								   "Set exp_reward tokens high (between 30 and 50) to reward the player.";
		}
		else if (difficultySetting == "Hard")
		{
			difficultyDirectives = "Generate an intense arcade layout. Include 8 to 12 total enemies. " +
								   "Scale enemy speeds aggressively up to 6.0 and health parameters up to 450.0. " +
								   "Set exp_reward tokens tightly balanced around 10 to 15 per kill.";
		}
		else // Medium / Default
		{
			difficultyDirectives = "Generate a standard, balanced arcade layout. Include 5 to 7 total enemies. " +
								   "Keep speeds moderate around 3.0 to 4.5, and health pools stable around 150.0 to 200.0.";
		}

		// Complete the structural format mapping contract
		string promptText = $"{difficultyDirectives} " +
							"Structure: {\"level_id\": string, \"difficulty_name\": string, \"is_boss_level\": bool, " +
							"\"waves\": [{\"name\": string, \"health\": float, \"speed\": float, " +
							"\"scene_path\": \"res://enemies/base_enemy.tscn\", \"scale_uniform\": float, \"scale_y\": float, \"exp_reward\": int}]}";

		// Format the request payload exactly how the Google Gemini API expects it
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
		if (responseCode != 200)
		{
			GD.PrintErr($"❌ HTTP Request Failed with code: {responseCode}");
			// Return control back to scene so the UI doesn't hang indefinitely on a bad request
			GetTree().ReloadCurrentScene();
			return;
		}

		try
		{
			string rawResponseText = Encoding.UTF8.GetString(body);
			
			// Parse Gemini's envelope to get the text inside choices/candidates
			using JsonDocument doc = JsonDocument.Parse(rawResponseText);
			string innerJson = doc.RootElement
				.GetProperty("candidates")[0]
				.GetProperty("content")
				.GetProperty("parts")[0]
				.GetProperty("text").GetString();

			var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
			
			// Ingest the AI data straight into your working system contract
			CurrentLevelData = JsonSerializer.Deserialize<LevelConfig>(innerJson, options);
			
			GD.Print($"🎉 Online AI Generation Success! Loaded Level: {CurrentLevelData.DifficultyName}");
			
			// Change or reload scenes to start the game loop with our new dynamic data active
			GetTree().ReloadCurrentScene();
		}
		catch (Exception e)
		{
			GD.PrintErr($"❌ Failed parsing online AI response data: {e.Message}");
			GetTree().ReloadCurrentScene();
		}
	}
}
