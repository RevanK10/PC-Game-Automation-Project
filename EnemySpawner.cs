using Godot;
using System;
using System.Collections.Generic;

public partial class EnemySpawner : Node3D
{
	[Export] public float SpawnRadius = 15.0f;

	private Node3D _player;
	private int _currentWave = 0;
	private int _enemiesToSpawn = 3;
	private float _waveDifficultyMultiplier = 1.0f;

	// FIX: State lock variable prevents the frame-by-frame _Process loop 
	// from re-triggering StartNextWave() while entities are still loading.
	private bool _isWaveSpawning = false;

	public override void _Ready()
	{
		// Shuts down the spawner system loop completely if sitting inside active menus
		if (!ArcadeSaveSystem.IsGamePlaying) return;

		_player = GetTree().Root.GetNodeOrNull<Node3D>("Main/Player");
		
		// Defer execution by one frame to guarantee GameDataManager has loaded the file first
		Callable.From(StartNextWave).CallDeferred();
	}

	public override void _Process(double delta)
	{
		// Do not calculate waves or run timers if a match isn't currently active,
		// or if a wave is currently in the middle of generation cycles.
		if (!ArcadeSaveSystem.IsGamePlaying || _isWaveSpawning) return;

		// Monitor the 'enemies' group to see if the arena has been cleared
		var activeEnemies = GetTree().GetNodesInGroup("enemies");
		
		// Only progress if the tree registry exists, the arena is clear, and the game has properly initialized
		if (activeEnemies.Count == 0 && GameDataManager.EnemyRegistry != null && GameDataManager.EnemyRegistry.Count > 0 && _currentWave > 0)
		{
			StartNextWave();
		}
	}

	private void StartNextWave()
	{
		List<EnemyData> registry = GameDataManager.EnemyRegistry;
		if (registry == null || registry.Count == 0) return;

		// Engage lock to shield against execution overlaps from rapid frame ticks
		_isWaveSpawning = true; 

		_currentWave++;
		
		// Core progression calculation factoring in the chosen user difficulty multiplier setting
		_enemiesToSpawn = 3 + (_currentWave * 2); // Wave 1 = 5, Wave 2 = 7, Wave 3 = 9...
		_waveDifficultyMultiplier = (1.0f + (_currentWave * 0.15f)) * ArcadeSaveSystem.DifficultyMultiplier;

		GD.Print($"\n=== ARCADE MODE: WAVE {_currentWave} BEGINS ===");
		GD.Print($"Spawning {_enemiesToSpawn} AI-generated variants at ({_waveDifficultyMultiplier * 100f:F0}% overall intensity)!");

		for (int i = 0; i < _enemiesToSpawn; i++)
		{
			SpawnRandomEnemy(registry);
		}

		// Release lock now that execution loop iteration has filled target constraints
		_isWaveSpawning = false;
	}

	private void SpawnRandomEnemy(List<EnemyData> registry)
	{
		// 1. Pick a completely random enemy template from your live AI Studio registry data
		int randomIndex = GD.RandRange(0, registry.Count - 1);
		EnemyData randomData = registry[randomIndex];

		// 2. Clone the template data into an arcade-scaled version so we don't overwrite the base stats
		EnemyData scaledData = new EnemyData
		{
			name = randomData.name,
			health = randomData.health * _waveDifficultyMultiplier,
			speed = randomData.speed * _waveDifficultyMultiplier,
			scale_uniform = randomData.scale_uniform,
			scale_y = randomData.scale_y,
			scene_path = randomData.scene_path
		};

		// 3. Dynamically instantiate their preset tscn scene resource paths
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

		// 4. Set up stats, scale, and Jolt hitboxes using our Initialize rules
		enemyInstance.Initialize(scaledData);

		// 5. Inject into the active world tree
		AddChild(enemyInstance);

		// 6. Calculate a circular spawn offset around the player's current location
		Vector3 playerPos = (_player != null && IsInstanceValid(_player)) ? _player.GlobalPosition : Vector3.Zero;
		float randomAngle = (float)GD.RandRange(0, Mathf.Tau);
		Vector3 spawnOffset = new Vector3(Mathf.Cos(randomAngle), 0, Mathf.Sin(randomAngle)) * SpawnRadius;

		// Move the enemy to their final perimeter ring coordinates
		enemyInstance.GlobalPosition = playerPos + spawnOffset;
	}
}
