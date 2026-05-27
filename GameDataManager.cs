using Godot;
using System;
using System.Text;
using System.Text.Json;

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
	/// Call this function from your UI "Generate Level" button!
	/// </summary>
	public void RequestNewLevelFromAi(string userIntentPrompt)
	{
		// Fetch your API Key from system environment variables for security
		string apiKey = OS.GetEnvironment("GEMINI_API_KEY");
		
		if (string.IsNullOrEmpty(apiKey))
		{
			GD.PrintErr("❌ ERROR: GEMINI_API_KEY environment variable is not set on this machine!");
			return;
		}

		string fullUrl = $"{GeminiUrl}?key={apiKey}";

		// Set up the structured system instructions forcing Gemini to respond with your exact JSON contract
		string systemInstruction = "You are a Game Director AI. Return a single JSON object matching the requested schema. No markdown wrappers.";
		
		string promptText = $"Generate an arcade wave configuration based on: '{userIntentPrompt}'. " +
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
		
		GD.Print("🧠 Sending Live Request to online Gemini API...");
		_httpNode.Request(fullUrl, headers, HttpClient.Method.Post, jsonPayload);
	}

	private void OnRequestCompleted(long result, long responseCode, string[] headers, byte[] body)
	{
		if (responseCode != 200)
		{
			GD.PrintErr($"❌ HTTP Request Failed with code: {responseCode}");
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
			
			// Change or reload scenes to start the game loop
			GetTree().ReloadCurrentScene();
		}
		catch (Exception e)
		{
			GD.PrintErr($"❌ Failed parsing online AI response data: {e.Message}");
		}
	}
}
