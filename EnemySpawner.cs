using Godot;
using System;
using System.Collections.Generic;

public partial class EnemySpawner : Node3D
{
	[Export] public float SpawnRadius = 25.0f;
	[Export] public float MinSpawnDistance = 12.0f; // Safety buffer: Enemies cannot spawn closer than 12 meters

	private Node3D _player;
	private int _currentWave = 0;
	private int _enemiesToSpawn = 3;
	private float _waveDifficultyMultiplier = 1.0f;

	private bool _isWaveSpawning = false;

	public override void _Ready()
	{
		if (!ArcadeSaveSystem.IsGamePlaying) return;

		// Robust player tracking: Try direct scene path first, then fall back to groups if necessary
		_player = GetTree().Root.GetNodeOrNull<Node3D>("Main/Player");
		if (_player == null)
		{
			_player = GetTree().GetFirstNodeInGroup("Player") as Node3D;
		}
		
		Callable.From(StartNextWave).CallDeferred();
	}

	public override void _Process(double delta)
	{
		if (!ArcadeSaveSystem.IsGamePlaying || _isWaveSpawning) return;

		var activeEnemies = GetTree().GetNodesInGroup("enemies");
		
		if (activeEnemies.Count == 0 && 
			GameDataManager.CurrentLevelData?.Waves != null && 
			GameDataManager.CurrentLevelData.Waves.Count > 0)
		{
			StartNextWave();
		}
	}

	private void StartNextWave()
	{
		List<EnemyData> registry = GameDataManager.CurrentLevelData?.Waves;
		if (registry == null || registry.Count == 0) return;

		_isWaveSpawning = true; 
		_currentWave++;
		
		_enemiesToSpawn = 3 + (_currentWave * 2); 
		_waveDifficultyMultiplier = (1.0f + (_currentWave * 0.15f)) * ArcadeSaveSystem.DifficultyMultiplier;

		GD.Print($"\n=== ARCADE MODE: WAVE {_currentWave} BEGINS ===");
		GD.Print($"Spawning {_enemiesToSpawn} AI-generated variants at ({_waveDifficultyMultiplier * 100f:F0}% overall intensity)!");

		for (int i = 0; i < _enemiesToSpawn; i++)
		{
			SpawnRandomEnemy(registry);
		}

		_isWaveSpawning = false;
	}

	private void SpawnRandomEnemy(List<EnemyData> registry)
	{
		int randomIndex = GD.RandRange(0, registry.Count - 1);
		EnemyData randomData = registry[randomIndex];

		EnemyData scaledData = new EnemyData
		{
			name = randomData.name,
			health = randomData.health * _waveDifficultyMultiplier,
			speed = randomData.speed * _waveDifficultyMultiplier,
			scale_uniform = randomData.scale_uniform,
			scale_y = randomData.scale_y,
			scene_path = randomData.scene_path
		};

		if (string.IsNullOrEmpty(scaledData.scene_path))
		{
			GD.PrintErr($"❌ ERROR: Scene path is null or empty for enemy template index: {randomIndex}");
			return;
		}

		PackedScene baseScene = GD.Load<PackedScene>(scaledData.scene_path);
		if (baseScene == null)
		{
			GD.PrintErr($"❌ ERROR: Failed to load PackedScene resource at: {scaledData.scene_path}");
			return;
		}
		
		Enemy enemyInstance = baseScene.Instantiate<Enemy>();

		// 1. Initialize stats first
		enemyInstance.Initialize(scaledData);

		// 2. SAFETY FIX: Disable physics body collision
		var enemyCollisionShape = enemyInstance.GetNodeOrNull<CollisionShape3D>("CollisionShape3D");
		if (enemyCollisionShape != null)
		{
			enemyCollisionShape.Disabled = true;
		}

		// 3. Attach the enemy instance directly to the Level Root scene container ("Main")
		var mainNode = GetTree().Root.GetNodeOrNull("Main");
		if (mainNode != null)
		{
			mainNode.AddChild(enemyInstance);
		}
		else
		{
			AddChild(enemyInstance);
		}

		enemyInstance.Position = Vector3.Zero;

		// 4. Fetch player position safely
		Vector3 playerPos = (_player != null && IsInstanceValid(_player)) ? _player.GlobalPosition : Vector3.Zero;
		
		// FIX: Safety loop to guarantee the position is away from the player AFTER nav-mesh snapping
		Vector3 finalSafeSpawnPosition = playerPos;
		Rid mapRid = GetWorld3D().NavigationMap;
		int maxAttempts = 10;
		bool validSpotFound = false;

		for (int attempt = 0; attempt < maxAttempts; attempt++)
		{
			// Calculate a circular spawn offset
			float randomAngle = (float)GD.RandRange(0, Mathf.Tau);
			float randomDistance = (float)GD.RandRange(MinSpawnDistance, SpawnRadius);
			
			Vector3 spawnOffset = new Vector3(Mathf.Cos(randomAngle), 0, Mathf.Sin(randomAngle)) * randomDistance;
			Vector3 rawSpawnPosition = playerPos + spawnOffset;

			// Force-snap onto the navigation mesh
			Vector3 testSnappedPosition = NavigationServer3D.MapGetClosestPoint(mapRid, rawSpawnPosition);

			// CRITICAL FIX: Measure actual distance to the player AFTER navigation snapping has occurred
			float actualDistanceToPlayer = testSnappedPosition.DistanceTo(playerPos);

			if (actualDistanceToPlayer >= MinSpawnDistance)
			{
				finalSafeSpawnPosition = testSnappedPosition;
				validSpotFound = true;
				break; // The location is safe, exit the check loop
			}
			
			GD.Print($"⚠️ attempt {attempt + 1}: NavMesh snapped enemy too close ({actualDistanceToPlayer:F1}m). Retrying...");
		}

		// Fallback: If your map layout failed to find a valid spot out of bounds in 10 tries, 
		// force the mathematical raw spot away from the player as an emergency backup.
		if (!validSpotFound)
		{
			float randomAngle = (float)GD.RandRange(0, Mathf.Tau);
			Vector3 fallbackOffset = new Vector3(Mathf.Cos(randomAngle), 0, Mathf.Sin(randomAngle)) * MinSpawnDistance;
			finalSafeSpawnPosition = playerPos + fallbackOffset;
			GD.PrintErr("🚨 WARNING: Failed to find safe NavMesh position after 10 attempts! Using forced fallback distance.");
		}

		// 5. Apply the safe positions
		enemyInstance.GlobalPosition = finalSafeSpawnPosition;

		// 6. RE-ENABLE HITBOX
		if (enemyCollisionShape != null)
		{
			enemyCollisionShape.Disabled = false;
		}
	}
}
