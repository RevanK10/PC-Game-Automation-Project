using Godot;
using System;

public partial class KillZone : Area3D
{
	public override void _Ready()
	{
		BodyEntered += OnBodyEntered;
	}

	private void OnBodyEntered(Node3D body)
	{
		if (body is Player player)
		{
			GD.Print("🕳️ Player fell out of bounds! Saving scores and triggering Game Over...");

			// 1. SAVE THE ACTIVE MATCH SCORE BEFORE RELOADING!
			// We check your root tree for the running player stats manager node
			var playerStats = GetTree().Root.GetNodeOrNull<PlayerStats>("Main/PlayerStats");
			if (playerStats != null)
			{
				// If your score variable has a slightly different name (like CurrentScore or Score), 
				// change '.MostRecentScore' here to match your exact variable name!
				ArcadeSaveSystem.MostRecentScore = playerStats.TotalKills;
			}
			else
			{
				// Fallback: If you keep your score inside an integer directly on your Player.cs script,
				// uncomment the line below and change 'CurrentMatchScore' to match your player's score variable:
				// ArcadeSaveSystem.MostRecentScore = player.CurrentMatchScore;
			}

			// 2. Also run the HighestScore setter check so records are written to your local hard drive disk
			ArcadeSaveSystem.HighestScore = ArcadeSaveSystem.MostRecentScore;

			// 3. Establish your standard UI Game Over architecture state flags
			ArcadeSaveSystem.IsGamePlaying = false;
			ArcadeSaveSystem.IsGameOver = true;

			// 4. Safely defer the scene reload command
			CallDeferred(MethodName.DeferredReload);
		}
		else if (body is Enemy enemy)
		{
			enemy.QueueFree();
		}
	}

	private void DeferredReload()
	{
		GetTree().ReloadCurrentScene();
	}
}
