using Godot;
using System;

public partial class Player : CharacterBody3D
{
	public const float Speed = 5.0f;
	public const float JumpVelocity = 4.5f;

	[Export] public float MaxHealth = 100.0f;
	public float CurrentHealth { get; private set; }

	[Export] public float MouseSensitivity = 0.003f;
	[Export] public PackedScene SpearProjectile; 
	[Export] public Node3D SpearContainer;      
	[Export] public float CooldownTime = 0.5f;
	
	private Node3D _head;
	private Camera3D _camera;
	private float _cameraPitch = 0.0f;
	private Tween _spearTween;
	private bool _canThrow = true;
	private Timer _cooldownTimer;
	private bool _isDrawing = false;

	public override void _Ready()
	{
		CurrentHealth = MaxHealth;

		_head = GetNode<Node3D>("Head");
		_camera = GetNode<Camera3D>("Head/Camera3D");
		
		if (SpearContainer == null) 
			SpearContainer = GetNodeOrNull<Node3D>("Head/SpearMarker");

		// --- ANTI-LAUNCH DEPLOYMENT ---
		Velocity = Vector3.Zero;
		uint originalMask = CollisionMask;
		CollisionMask = 0; 

		GlobalPosition = new Vector3(GlobalPosition.X, GlobalPosition.Y + 1.5f, GlobalPosition.Z);

		GetTree().CreateTimer(0.1f).Timeout += () => 
		{
			CollisionMask = originalMask;
			Velocity = Vector3.Zero;
		};
		// -------------------------------

		if (ArcadeSaveSystem.IsGamePlaying)
		{
			Input.MouseMode = Input.MouseModeEnum.Captured;
		}
		else
		{
			Input.MouseMode = Input.MouseModeEnum.Visible;
		}
		
		_cooldownTimer = new Timer();
		_cooldownTimer.WaitTime = CooldownTime;
		_cooldownTimer.OneShot = true;
		_cooldownTimer.Timeout += () => _canThrow = true;
		AddChild(_cooldownTimer);
	}

	public void TakeDamage(float amount)
	{
		if (!ArcadeSaveSystem.IsGamePlaying) return;

		CurrentHealth -= amount;
		GD.Print($"[PLAYER HEALTH] Damaged by {amount:F1}. Status: {CurrentHealth:F1}/{MaxHealth}");

		if (CurrentHealth <= 0)
		{
			GameOver();
		}
	}

	private void GameOver()
	{
		GD.Print("💀 GAME OVER! Recording arcade statistics...");
		
		ArcadeSaveSystem.IsGamePlaying = false;
		ArcadeSaveSystem.IsGameOver = true;

		var playerStats = GetTree().Root.GetNodeOrNull<PlayerStats>("Main/PlayerStats");
		int finalScore = playerStats != null ? playerStats.TotalKills : 0; 

		ArcadeSaveSystem.MostRecentScore = finalScore;
		if (finalScore > ArcadeSaveSystem.HighestScore)
		{
			ArcadeSaveSystem.HighestScore = finalScore;
			GD.Print($"🏆 NEW HIGH SCORE: {ArcadeSaveSystem.HighestScore}!");
		}

		GetTree().ReloadCurrentScene();
	}

	public override void _Input(InputEvent @event)
	{
		if (!ArcadeSaveSystem.IsGamePlaying) return;

		if (@event is InputEventMouseMotion mouseMotion)
		{
			RotateY(-mouseMotion.Relative.X * MouseSensitivity);
			_cameraPitch = Mathf.Clamp(_cameraPitch - mouseMotion.Relative.Y * MouseSensitivity, Mathf.DegToRad(-80f), Mathf.DegToRad(80f));
			_head.Rotation = new Vector3(_cameraPitch, 0, 0);
		}

		if (@event.IsActionPressed("ui_cancel")) Input.MouseMode = Input.MouseModeEnum.Visible;
		
		// SPEAR THROW LOGIC WITH CRASH PROTECTION
		if (@event.IsActionPressed("primary_fire") && _canThrow)
		{
			if (SpearContainer != null)
			{
				_isDrawing = true;
				_spearTween?.Kill();
				_spearTween = CreateTween();
				_spearTween.TweenProperty(SpearContainer, "position:z", -0.3f, 0.3f);
			}
			else
			{
				GD.PrintErr("❌ ERROR: SpearContainer is null! Check your Player Node Hierarchy.");
			}
		}
		else if (@event.IsActionReleased("primary_fire") && _isDrawing)
		{
			_isDrawing = false;
			_spearTween?.Kill();
			
			if (SpearContainer != null)
			{
				SpearContainer.Position = Vector3.Zero;
			}
			
			ThrowSpear();
		}
	}

	private void ThrowSpear()
	{
		if (SpearProjectile == null)
		{
			GD.PrintErr("❌ ERROR: SpearProjectile PackedScene is missing in Inspector!");
			return;
		}

		_canThrow = false;
		_cooldownTimer.Start();

		var spear = SpearProjectile.Instantiate<SpearProjectile>();
		GetTree().Root.AddChild(spear);
		
		if (SpearContainer != null)
		{
			spear.GlobalTransform = SpearContainer.GlobalTransform;
			spear.GlobalPosition += -_camera.GlobalTransform.Basis.Z * 1.0f; 
		}
		else
		{
			spear.GlobalTransform = _camera.GlobalTransform;
		}
		
		Vector3 throwDir = -_camera.GlobalTransform.Basis.Z;
		spear.Launch(throwDir, 30.0f);
	}

	public override void _PhysicsProcess(double delta)
	{
		if (!ArcadeSaveSystem.IsGamePlaying) return;

		Vector3 velocity = Velocity;
		if (!IsOnFloor()) velocity += GetGravity() * (float)delta;
		
		Vector2 inputDir = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
		Vector3 direction = (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();
		
		if (direction != Vector3.Zero)
		{
			velocity.X = direction.X * Speed;
			velocity.Z = direction.Z * Speed;
		}
		else
		{
			velocity.X = Mathf.MoveToward(Velocity.X, 0, Speed);
			velocity.Z = Mathf.MoveToward(Velocity.Z, 0, Speed);
		}
		Velocity = velocity;
		MoveAndSlide();
	}
}
