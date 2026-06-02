using Godot;
using System;

public partial class Player : CharacterBody3D
{
	// --- Movement Constants ---
	public const float NormalSpeed = 5.0f;
	[Export] public float SprintSpeed = 8.5f; 
	public const float JumpVelocity = 4.5f;

	// --- Stamina / Sprint Timeout Variables ---
	[Export] public float MaxStamina = 100.0f;
	private float _currentStamina;
	private const float StaminaDrainRate = 20.0f;     // Drains fully over 5 seconds (100 / 20 = 5s)
	private const float StaminaRegenRate = 25.0f;     // Recharges fully over 4 seconds (100 / 25 = 4s)
	private bool _isFatigued = false;                 // Locked out of sprinting if stamina hits 0

	[Export] public float MaxHealth = 100.0f;
	public float CurrentHealth { get; private set; }

	[Export] public float MouseSensitivity = 0.005f;
	[Export] public PackedScene SpearProjectile; 
	[Export] public Node3D SpearContainer;      
	[Export] public float CooldownTime = 0.5f;

	[Export] public Control DamageOverlayLayer;
	[Export] public AudioStreamPlayer DamageAudioPlayer;
	
	[Export] public Label HealthLabel;
	[Export] public ProgressBar StaminaBar;   // Drag your UI Progress Bar here in the Inspector!
	
	private Node3D _head;
	private Camera3D _camera;
	private float _cameraPitch = 0.0f;
	private Tween _spearTween;
	private bool _canThrow = true;
	private Timer _cooldownTimer;
	private bool _isDrawing = false;

	private float _flashDurationTimer = 0.0f;
	private const float MaxFlashDuration = 0.15f; 

	private bool _isMenuOpen = false;

	public override void _Ready()
	{
		CurrentHealth = MaxHealth;
		_currentStamina = MaxStamina; // Start with full stamina

		_head = GetNode<Node3D>("Head");
		_camera = GetNode<Camera3D>("Head/Camera3D");
		
		if (SpearContainer == null) 
			SpearContainer = GetNodeOrNull<Node3D>("Head/SpearMarker");

		if (DamageAudioPlayer == null)
			DamageAudioPlayer = GetNodeOrNull<AudioStreamPlayer>("DamageAudioPlayer");

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

		SyncMouseVisibilityState();
		
		_cooldownTimer = new Timer();
		_cooldownTimer.WaitTime = CooldownTime;
		_cooldownTimer.OneShot = true;
		_cooldownTimer.Timeout += () => _canThrow = true;
		AddChild(_cooldownTimer);

		if (HealthLabel == null)
		{
			HealthLabel = GetTree().Root.GetNodeOrNull<Label>("/root/Main/ArcadeUI/HealthLabel") ?? 
						  GetNodeOrNull<Label>("../ArcadeUI/HealthLabel");
		}

		// Fallback UI scene path lookup for the Stamina Progress bar element
		if (StaminaBar == null)
		{
			StaminaBar = GetTree().Root.GetNodeOrNull<ProgressBar>("/root/Main/ArcadeUI/StaminaBar") ??
						 GetNodeOrNull<ProgressBar>("../ArcadeUI/StaminaBar");
		}
		
		UpdateHealthUI();
		UpdateStaminaUI();
	}

	public void TakeDamage(float amount)
	{
		if (!ArcadeSaveSystem.IsGamePlaying) return;

		if (DamageOverlayLayer == null)
		{
			DamageOverlayLayer = GetTree().Root.GetNodeOrNull<Control>("/root/Main/DamagerOverlayManager/DamageOverlay");
		}

		if (HealthLabel == null)
		{
			HealthLabel = GetTree().Root.GetNodeOrNull<Label>("/root/Main/ArcadeUI/HealthLabel") ?? 
						  GetNodeOrNull<Label>("../ArcadeUI/HealthLabel");
		}

		CurrentHealth -= amount;
		GD.Print($"[PLAYER HEALTH] Damaged by {amount:F1}. Status: {CurrentHealth:F1}/{MaxHealth}");

		UpdateHealthUI();

		if (DamageOverlayLayer != null)
		{
			DamageOverlayLayer.Visible = true;
			Color curColor = DamageOverlayLayer.SelfModulate;
			curColor.A = 1.0f;
			DamageOverlayLayer.SelfModulate = curColor;
			_flashDurationTimer = MaxFlashDuration; 
		}
		else
		{
			GD.PrintErr("❌ PATH ERROR: Player script cannot find: /root/Main/DamagerOverlayManager/DamageOverlay");
		}

		if (DamageAudioPlayer != null && !DamageAudioPlayer.Playing)
		{
			DamageAudioPlayer.Play();
		}

		if (CurrentHealth <= 0)
		{
			GameOver();
		}
	}

	private void UpdateHealthUI()
	{
		if (HealthLabel != null)
		{
			HealthLabel.Text = $"HP: {Mathf.Max(CurrentHealth, 0.0f):F0} / {MaxHealth:F0}";
			GD.Print($"[UI SYNC] Updated HealthLabel text to: {HealthLabel.Text}");
		}
		else
		{
			GD.PrintErr("❌ UI LINKAGE ERROR: HealthLabel reference is null. Text cannot be set!");
		}
	}

	private void UpdateStaminaUI()
	{
		if (StaminaBar != null)
		{
			StaminaBar.Value = _currentStamina;
			
			// Visual cue: Tints the bar slightly red if the player is entirely burned out/fatigued
			StaminaBar.Modulate = _isFatigued ? new Color(1.0f, 0.35f, 0.35f) : new Color(1.0f, 1.0f, 1.0f);
		}
		else
		{
			// This will yell at you in the debugger if your player can't see the bar!
			GD.PrintErr("🚨 STAMINA LINKAGE MISSING: The player script cannot find the StaminaBar node!");
		}
	}

	private void GameOver()
	{
		GD.Print("💀 GAME OVER! Recording arcade statistics...");
		
		ArcadeSaveSystem.IsGamePlaying = false;
		ArcadeSaveSystem.IsGameOver = true;

		Input.MouseMode = Input.MouseModeEnum.Visible;

		var playerStats = GetTree().Root.GetNodeOrNull<PlayerStats>("Main/PlayerStats");
		int finalScore = playerStats != null ? playerStats.TotalKills : 0; 

		ArcadeSaveSystem.MostRecentScore = finalScore;
		if (finalScore > ArcadeSaveSystem.HighestScore)
		{
			ArcadeSaveSystem.HighestScore = finalScore;
			GD.Print($"🏆 NEW HIGH SCORE: {ArcadeSaveSystem.HighestScore}!");
		}

		if (DamageOverlayLayer != null)
		{
			DamageOverlayLayer.Visible = false;
		}

		GetTree().ReloadCurrentScene();
	}

	public override void _Process(double delta)
	{
		SyncMouseVisibilityState();

		if (!ArcadeSaveSystem.IsGamePlaying || DamageOverlayLayer == null || !DamageOverlayLayer.Visible) return;

		if (_flashDurationTimer > 0.0f)
		{
			_flashDurationTimer -= (float)delta;
			Color curColor = DamageOverlayLayer.SelfModulate;
			curColor.A = Mathf.Max(_flashDurationTimer / MaxFlashDuration, 0.0f);
			DamageOverlayLayer.SelfModulate = curColor;

			if (_flashDurationTimer <= 0.0f)
			{
				DamageOverlayLayer.Visible = false;
			}
		}
	}

	private void SyncMouseVisibilityState()
	{
		if (_isMenuOpen)
		{
			if (Input.MouseMode != Input.MouseModeEnum.Visible)
			{
				Input.MouseMode = Input.MouseModeEnum.Visible;
			}
			return;
		}

		if (ArcadeSaveSystem.IsGamePlaying)
		{
			if (Input.MouseMode != Input.MouseModeEnum.Captured && Input.MouseMode != Input.MouseModeEnum.ConfinedHidden)
			{
				Input.MouseMode = Input.MouseModeEnum.Captured;
			}
		}
		else
		{
			if (Input.MouseMode != Input.MouseModeEnum.Visible)
			{
				Input.MouseMode = Input.MouseModeEnum.Visible;
			}
		}
	}

	public override void _Input(InputEvent @event)
	{
		if (@event.IsActionPressed("ui_cancel"))
		{
			_isMenuOpen = !_isMenuOpen;
			GetViewport().SetInputAsHandled(); 
			return;
		}

		if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo)
		{
			if (keyEvent.Keycode == Key.F)
			{
				_isMenuOpen = !_isMenuOpen;
				return;
			}
		}

		if (_isMenuOpen || !ArcadeSaveSystem.IsGamePlaying) return;

		if (@event is InputEventMouseMotion mouseMotion && Input.MouseMode == Input.MouseModeEnum.Captured)
		{
			RotateY(-mouseMotion.Relative.X * MouseSensitivity);
			_cameraPitch = Mathf.Clamp(_cameraPitch - mouseMotion.Relative.Y * MouseSensitivity, Mathf.DegToRad(-80f), Mathf.DegToRad(80f));
			_head.Rotation = new Vector3(_cameraPitch, 0, 0);
		}
		
		if (@event.IsActionPressed("primary_fire") && _canThrow && Input.MouseMode == Input.MouseModeEnum.Captured)
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

	private Vector3 GetCrosshairWorldPosition()
	{
		if (_camera == null) return GlobalPosition - Transform.Basis.Z * 20.0f;

		Vector2 screenSize = GetViewport().GetVisibleRect().Size;
		Vector2 screenCenter = screenSize / 2.0f;

		var spaceState = GetWorld3D().DirectSpaceState;

		Vector3 rayOrigin = _camera.ProjectRayOrigin(screenCenter);
		Vector3 rayNormal = _camera.ProjectRayNormal(screenCenter);
		Vector3 rayEnd = rayOrigin + (rayNormal * 200.0f);

		var query = PhysicsRayQueryParameters3D.Create(rayOrigin, rayEnd);
		query.Exclude = new Godot.Collections.Array<Rid> { GetRid() };

		var result = spaceState.IntersectRay(query);

		if (result.Count > 0)
		{
			return (Vector3)result["position"];
		}

		return rayEnd;
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
		
		Vector3 lookTargetPoint = GetCrosshairWorldPosition();
		Vector3 throwDir = (lookTargetPoint - spear.GlobalPosition).Normalized();

		spear.LookAt(spear.GlobalPosition + throwDir, Vector3.Up);
		spear.Launch(throwDir, 30.0f);
	}

	public override void _PhysicsProcess(double delta)
	{
		if (!ArcadeSaveSystem.IsGamePlaying || _isMenuOpen) return;

		Vector3 velocity = Velocity;
		if (!IsOnFloor()) velocity += GetGravity() * (float)delta;
		
		Vector2 inputDir = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
		Vector3 direction = (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();
		
		bool isMoving = direction != Vector3.Zero;
		bool wantsToSprint = Input.IsActionPressed("ui_sprint");

		// Player can only sprint if they are moving, pressing down the action key, and not exhausted
		bool isSprinting = wantsToSprint && isMoving && !_isFatigued;

		if (isSprinting)
		{
			_currentStamina -= StaminaDrainRate * (float)delta;
			if (_currentStamina <= 0.0f)
			{
				_currentStamina = 0.0f;
				_isFatigued = true; // Trigger fatigue lock out
			}
		}
		else
		{
			_currentStamina += StaminaRegenRate * (float)delta;
			if (_currentStamina >= MaxStamina)
			{
				_currentStamina = MaxStamina;
				_isFatigued = false; // Fully recovered, lift lock out state
			}
		}

		// Push modifications directly to the UI bar container
		UpdateStaminaUI();

		float currentSpeed = isSprinting ? SprintSpeed : NormalSpeed;

		if (isMoving)
		{
			velocity.X = direction.X * currentSpeed;
			velocity.Z = direction.Z * currentSpeed;
		}
		else
		{
			velocity.X = Mathf.MoveToward(Velocity.X, 0, currentSpeed);
			velocity.Z = Mathf.MoveToward(Velocity.Z, 0, currentSpeed);
		}
		Velocity = velocity;
		MoveAndSlide();
	}
}
