using Godot;
using System;

public partial class Enemy : CharacterBody3D
{
	public string EnemyName { get; set; }
	public float Health { get; set; }
	public float Speed { get; set; }

	// --- ARCADE MODIFIER STATE TRACKERS ---
	private float _knockbackTimer = 0.0f;
	private Vector3 _externalVelocity = Vector3.Zero;

	private Node3D _playerTarget;
	private MeshInstance3D _meshInstance;
	private CollisionShape3D _collisionShape;

	// Optimization: Pre-generate structural shapes statically in memory.
	// Since Jolt forces uniform scales on primitive collision shapes at runtime,
	// we define the exact, correct dimensions right here so no scaling is needed!
	private static readonly CapsuleShape3D DemonShape = new CapsuleShape3D { Radius = 1.1f, Height = 5.0f };
	private static readonly CapsuleShape3D RobberShape = new CapsuleShape3D { Radius = 0.4f, Height = 1.8f };
	private static readonly CapsuleShape3D SoldierShape = new CapsuleShape3D { Radius = 0.7f, Height = 3.0f };
	private static readonly CapsuleShape3D DefaultShape = new CapsuleShape3D { Radius = 0.5f, Height = 2.0f };

	private bool _isTouchingPlayer = false;
	private Player _playerRef = null;

	public override void _Ready()
	{
		_playerTarget = GetTree().Root.GetNodeOrNull<Node3D>("Main/Player");

		// Hook into an Area3D child node for damage tracking
		var damageArea = GetNodeOrNull<Area3D>("DamageArea");
		if (damageArea != null)
		{
			damageArea.BodyEntered += OnDamageAreaBodyEntered;
			damageArea.BodyExited += OnDamageAreaBodyExited;
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		Vector3 velocity = Velocity;

		// 1. Handle standard map gravity gravity calculations
		if (!IsOnFloor())
		{
			velocity += GetGravity() * (float)delta;
		}

		// 2. Handle External Force Interrupts (Explosions, Gravity pulls, etc.)
		if (_knockbackTimer > 0.0f)
		{
			_knockbackTimer -= (float)delta;
			
			// Inject the explosive payload force or gravity vector smoothly 
			velocity.X = _externalVelocity.X;
			velocity.Z = _externalVelocity.Z;

			// If the force was an explosion knockback pushing upwards, apply the Y velocity
			if (_externalVelocity.Y != 0)
			{
				velocity.Y = _externalVelocity.Y;
				_externalVelocity.Y = 0; // Handed off to gravity loop for tracking the fallout arc
			}

			// Clean up velocity stack once the physics window times out
			if (_knockbackTimer <= 0.0f)
			{
				_externalVelocity = Vector3.Zero;
			}
		}
		// 3. Normal Pathfinding Logic (Only tracking if not stunned/immobilized by lightning)
		else if (_playerTarget != null && IsInstanceValid(_playerTarget) && Speed > 0.0f)
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

			float prototypeSpeedModifier = 0.3f;
			velocity.X = targetDirection.X * Speed * prototypeSpeedModifier;
			velocity.Z = targetDirection.Z * Speed * prototypeSpeedModifier;
		}
		else
		{
			// Friction slowdown if the target is lost or completely paralyzed (Speed == 0)
			velocity.X = Mathf.MoveToward(Velocity.X, 0, Speed > 0 ? Speed : 10.0f);
			velocity.Z = Mathf.MoveToward(Velocity.Z, 0, Speed > 0 ? Speed : 10.0f);
		}

		Velocity = velocity;
		MoveAndSlide();

		// CONTACT DAMAGE ENGINE: Highly optimized signal tracking loop
		if (_isTouchingPlayer && _playerRef != null && IsInstanceValid(_playerRef))
		{
			_playerRef.TakeDamage(0.2f);
		}
	}

	// Called by SpearProjectile to apply knockbacks and gravitational vortex updates
	public void ApplyExternalForce(Vector3 force, float duration)
	{
		_knockbackTimer = duration;
		_externalVelocity = force;
	}

	public void Initialize(EnemyData data)
	{
		EnemyName = data.name;
		Health = data.health;
		Speed = data.speed;

		_meshInstance = GetNode<MeshInstance3D>("MeshInstance3D");
		_collisionShape = GetNode<CollisionShape3D>("CollisionShape3D");
		var nameLabel = GetNodeOrNull<Label3D>("Label3D");
		var damageArea = GetNodeOrNull<Area3D>("DamageArea");

		AddToGroup("enemies");

		var material = new StandardMaterial3D();
		var capsuleMesh = new CapsuleMesh();
		
		// Baseline dimensions
		float radius = 0.5f;
		float height = 2.0f;

		if (EnemyName == "Big Red Demon")
		{
			material.AlbedoColor = new Color(0.3f, 0.02f, 0.02f);
			material.Roughness = 0.8f;

			// --- PROCEDURAL MAGMA CRACKS LAYER ---
			var lavaNoise = new NoiseTexture2D();
			lavaNoise.Seamless = true;
			lavaNoise.Width = 512;
			lavaNoise.Height = 512;

			var noisePreset = new FastNoiseLite();
			noisePreset.NoiseType = FastNoiseLite.NoiseTypeEnum.Perlin;
			noisePreset.Frequency = 0.04f; 
			noisePreset.FractalOctaves = 3;
			lavaNoise.Noise = noisePreset;

			material.EmissionEnabled = true;
			material.EmissionTexture = lavaNoise;
			material.Emission = new Color(0.8f, 0.15f, 0.0f); 
			material.EmissionEnergyMultiplier = 0.8f;

			radius = 1.1f;
			height = 5.0f;
			
			_collisionShape.Shape = DemonShape;
		}
		else if (EnemyName == "Small Brown Robber")
		{
			material.AlbedoColor = new Color(0.25f, 0.15f, 0.08f); 
			material.Roughness = 0.95f; 
			
			material.RimEnabled = true;
			material.Rim = 0.6f;
			material.RimTint = 0.2f;

			radius = 0.4f;
			height = 1.8f;       
			
			_collisionShape.Shape = RobberShape;
		}
		else if (EnemyName == "Medium Silver Soldier")
		{
			var armorGradientTex = new GradientTexture2D { Fill = GradientTexture2D.FillEnum.Linear, Repeat = (GradientTexture2D.RepeatEnum)1 };
			armorGradientTex.FillFrom = new Vector2(0f, 0f);
			armorGradientTex.FillTo = new Vector2(0f, 0.2f); 

			var grayRamp = new Gradient();
			grayRamp.Offsets = new float[] { 0.0f, 0.4f, 1.0f };
			grayRamp.Colors = new Color[] { 
				new Color(0.7f, 0.7f, 0.75f), // Steel plate
				new Color(0.4f, 0.4f, 0.45f), // Midtone iron
				new Color(0.1f, 0.1f, 0.12f)  // Segment shadow
			};
			armorGradientTex.Gradient = grayRamp;

			material.AlbedoTexture = armorGradientTex;
			material.Metallic = 0.9f;  
			material.Roughness = 0.2f; 

			radius = 0.7f;
			height = 3.0f;
			
			_collisionShape.Shape = SoldierShape;
		}
		else 
		{
			material.AlbedoColor = Colors.DarkGray;
			_collisionShape.Shape = DefaultShape;
		}

		capsuleMesh.Radius = radius;
		capsuleMesh.Height = height;
		_meshInstance.Mesh = capsuleMesh;
		_meshInstance.MaterialOverride = material;
		_meshInstance.Scale = Vector3.One;

		_meshInstance.Position = new Vector3(0, height / 2.0f, 0);
		_collisionShape.Position = new Vector3(0, height / 2.0f, 0);

		if (damageArea != null)
		{
			damageArea.Position = new Vector3(0, height / 2.0f, 0);
			var areaCollision = damageArea.GetNodeOrNull<CollisionShape3D>("CollisionShape3D");
			if (areaCollision != null)
			{
				var hitboxBox = new BoxShape3D();
				hitboxBox.Size = new Vector3(radius * 2.4f, height * 1.1f, radius * 2.4f);
				areaCollision.Shape = hitboxBox;
			}
		}

		if (nameLabel != null)
		{
			nameLabel.Text = EnemyName;
			float floatingHeightPadding = height + 0.5f;
			nameLabel.Position = new Vector3(0, floatingHeightPadding, 0);
		}
	}

	private void OnDamageAreaBodyEntered(Node body)
	{
		if (body is Player player)
		{
			_playerRef = player;
			_isTouchingPlayer = true;
		}
	}

	private void OnDamageAreaBodyExited(Node body)
	{
		if (body is Player player)
		{
			_isTouchingPlayer = false;
			_playerRef = null;
		}
	}

	public void TakeDamage(float amount)
	{
		Health -= amount;
		if (Health <= 0) Die();
	}

	private void Die()
	{
		var playerStats = GetTree().Root.GetNodeOrNull<PlayerStats>("Main/PlayerStats");
		if (playerStats != null)
		{
			int expReward = (Health > 250 || EnemyName.Contains("Demon")) ? 50 : 15;
			playerStats.RegisterKill(expReward);
		}
		QueueFree(); 
	}
}
