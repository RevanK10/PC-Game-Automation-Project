using Godot;

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
		
		AddToGroup("enemies");
		GD.Print($"[Spawned] {EnemyName} added to 'enemies' group.");

		// --- NEW DYNAMIC AI INGESTION LOGIC ---
		var material = new StandardMaterial3D();
		meshInstance.Mesh = new CapsuleMesh();
		meshInstance.Rotation = Vector3.Zero;

		// Use basic name checks to decide color variants dynamically
		if (EnemyName.Contains("Demon") || EnemyName.Contains("Golem"))
		{
			material.AlbedoColor = Colors.Red;
		}
		else if (EnemyName.Contains("Phantom") || EnemyName.Contains("Skull"))
		{
			material.AlbedoColor = Colors.Purple;
		}
		else
		{
			material.AlbedoColor = Colors.DarkGray;
		}
		meshInstance.MaterialOverride = material;

		// --- CRITICAL JOLT PHYSICS CRASH SAFEGUARDS ---
		float safeUniformScale = Mathf.Max(data.scale_uniform, 1.0f);
		float safeHeightScale = Mathf.Max(data.scale_y, 1.0f);

		// Scale the visual Mesh safely
		meshInstance.Scale = new Vector3(safeUniformScale, safeHeightScale, safeUniformScale);

		// Match the physical collision shape to the guaranteed safe scales
		if (collisionShape.Shape is CapsuleShape3D standardCapsule)
		{
			var uniqueCapsule = (CapsuleShape3D)standardCapsule.Duplicate();
			
			collisionShape.Rotation = meshInstance.Rotation;

			uniqueCapsule.Radius = 0.5f * safeUniformScale;
			uniqueCapsule.Height = 2.0f * safeHeightScale;

			collisionShape.Shape = uniqueCapsule;
		}

		GD.Print($"[AI Ingest Success] {EnemyName} initialized with {Health} HP, {Speed} Speed. Radius: {0.5f * safeUniformScale}, Height: {2.0f * safeHeightScale}");
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
