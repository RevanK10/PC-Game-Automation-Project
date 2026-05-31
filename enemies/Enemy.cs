using Godot;
using System;

public partial class Enemy : CharacterBody3D
{
	// Properties set dynamically by the JSON data
	public string EnemyName { get; set; }
	public float Health { get; set; }
	public float Speed { get; set; }

	private Node3D _playerTarget;

	public override void _Ready()
	{
		_playerTarget = GetTree().Root.GetNodeOrNull<Node3D>("Main/Player");
	}

	public override void _PhysicsProcess(double delta)
	{
		Vector3 velocity = Velocity;

		if (!IsOnFloor())
		{
			velocity += GetGravity() * (float)delta;
		}

		if (_playerTarget != null && IsInstanceValid(_playerTarget))
		{
			Vector3 playerPosition = _playerTarget.GlobalPosition;
			Vector3 targetDirection = playerPosition - GlobalPosition;
			targetDirection.Y = 0; 
			targetDirection = targetDirection.Normalized();

			if (targetDirection != Vector3.Zero)
			{
				Vector3 lookTarget = GlobalPosition + targetDirection;
				LookAt(lookTarget, Vector3.Up);
			}

			// Kept the speed modifier here so they remain at a manageable pace
			float prototypeSpeedModifier = 0.3f;
			velocity.X = targetDirection.X * Speed * prototypeSpeedModifier;
			velocity.Z = targetDirection.Z * Speed * prototypeSpeedModifier;
		}
		else
		{
			velocity.X = Mathf.MoveToward(Velocity.X, 0, Speed);
			velocity.Z = Mathf.MoveToward(Velocity.Z, 0, Speed);
		}

		Velocity = velocity;
		
		// 1. Execute standard kinematic movement
		MoveAndSlide();

		// 2. CONTACT DAMAGE ENGINE: Parse physical wall/body contact parameters
		for (int i = 0; i < GetSlideCollisionCount(); i++)
		{
			KinematicCollision3D collision = GetSlideCollision(i);
			Node collider = (Node)collision.GetCollider();

			// If the intersecting body is the Player node, deliver damage frame-by-frame
			if (collider is Player player)
			{
				// 0.2f keeps the drain steady without deleting the player in 2 frames
				float damagePerFrame = 0.2f; 
				player.TakeDamage(damagePerFrame);
			}
		}
	}

	public void Initialize(EnemyData data)
	{
		EnemyName = data.name;
		Health = data.health;
		Speed = data.speed;

		var meshInstance = GetNode<MeshInstance3D>("MeshInstance3D");
		var collisionShape = GetNode<CollisionShape3D>("CollisionShape3D");
		
		// ADDED: Fetch the Label3D node child
		var nameLabel = GetNodeOrNull<Label3D>("Label3D");
		
		AddToGroup("enemies");
		GD.Print($"[Spawned] {EnemyName} added to 'enemies' group.");

		// --- UPDATED DYNAMIC ARCHETYPE VISUALS & PHYSICS ENGINE ---
		var material = new StandardMaterial3D();
		meshInstance.Mesh = new CapsuleMesh();
		meshInstance.Rotation = Vector3.Zero;

		// Establish layout scaling defaults
		float targetScaleUniform = 1.0f;
		float targetScaleY = 1.0f;

		// Explicitly check for our exact 3 cloud-directed archetypes
		if (EnemyName == "Big Red Demon")
		{
			material.AlbedoColor = new Color(0.8f, 0.1f, 0.1f); // Crimson Red
			targetScaleUniform = 2.2f; // Extra wide/girthy
			targetScaleY = 2.5f;       // Towering height
		}
		else if (EnemyName == "Small Brown Robber")
		{
			material.AlbedoColor = new Color(0.45f, 0.28f, 0.15f); // Leather Brown
			targetScaleUniform = 0.8f; // Thin
			targetScaleY = 0.9f;       // Short
		}
		else if (EnemyName == "Medium Silver Soldier")
		{
			material.AlbedoColor = new Color(0.75f, 0.75f, 0.8f); // Metallic Silver
			material.Metallic = 0.8f;   // Shiny steel finish
			material.Roughness = 0.2f;
			targetScaleUniform = 1.4f; // Clean baseline proportions
			targetScaleY = 1.5f;
		}
		else // Fallback safety layer if name mapping text drifts
		{
			material.AlbedoColor = Colors.DarkGray;
			targetScaleUniform = Mathf.Max(data.scale_uniform, 1.0f);
			targetScaleY = Mathf.Max(data.scale_y, 1.0f);
		}

		meshInstance.MaterialOverride = material;

		// Scale the visual Mesh based on our locked archetype values
		meshInstance.Scale = new Vector3(targetScaleUniform, targetScaleY, targetScaleUniform);

		// Match the physical Jolt collision shape perfectly to the custom scales
		if (collisionShape.Shape is CapsuleShape3D standardCapsule)
		{
			var uniqueCapsule = (CapsuleShape3D)standardCapsule.Duplicate();
			
			collisionShape.Rotation = meshInstance.Rotation;

			// Base capsule properties (0.5m radius, 2.0m height) multiplied by our scaling metrics
			uniqueCapsule.Radius = 0.5f * targetScaleUniform;
			uniqueCapsule.Height = 2.0f * targetScaleY;

			collisionShape.Shape = uniqueCapsule;
		}

		// --- UPDATED FLOATING TEXT SETTING ---
		if (nameLabel != null)
		{
			nameLabel.Text = EnemyName;
			
			// Dynamic Height Adjustment: Sets the text offset comfortably above the capsule's total height
			// Base height is 2.0m * targetScaleY. We cut it in half because the origin is at the center, 
			// then add an extra 0.5 meters of clean air padding.
			float floatingHeightPadding = (2.0f * targetScaleY / 2.0f) + 0.5f;
			nameLabel.Position = new Vector3(0, floatingHeightPadding, 0);
		}

		GD.Print($"[AI Ingest Success] {EnemyName} initialized with {Health} HP, {Speed} Speed. Radius: {0.5f * targetScaleUniform}, Height: {2.0f * targetScaleY}");
	}

	public void TakeDamage(float amount)
	{
		Health -= amount;
		GD.Print($"{EnemyName} took {amount} damage! HP remaining: {Health}");

		if (Health <= 0)
		{
			Die();
		}
	}

	private void Die()
	{
		GD.Print($"{EnemyName} has been destroyed.");

		var playerStats = GetTree().Root.GetNodeOrNull<PlayerStats>("Main/PlayerStats");
		if (playerStats != null)
		{
			int expReward = (Health > 250 || EnemyName.Contains("Demon")) ? 50 : 15;
			playerStats.RegisterKill(expReward);
		}

		QueueFree(); 
	}
}
