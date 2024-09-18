using Godot;
using System;

public partial class PlayerCharacter : CharacterBody3D
{
	public Vector3 RespawnPoint;
	
	public const float JumpVelocity = 4.5f;
	public const float CrouchJumpVelocity = 3f;
	
	public const float Speed = 5.0f;
	public const float SprintSpeed = 7.5f;
	public const float CrouchSpeed = 3f;
	public const float SlowdownSpeed = 0.35f;
	public const float MaxSpeed = 10f;
	
	public const float PushForce = 0.75f;
	public const float ThrowForce = 1.5f;

	public const float MouseSensitivity = 0.3f;
	
	// Get the gravity from the project settings to be synced with RigidBody nodes.
	public float Gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();
	
	// Get Nodes from scene
	[Export] public Node3D CameraAnchor;
	[Export] public Camera3D Camera;
	[Export] private RayCast3D _rayCast;
	[Export] private Node3D _grabPoint;
	[Export] private ShapeCast3D _crouchCast; 
	private CapsuleShape3D _collisionShape;
	[Export] private Flashlight _flashlight;

	// Tracking active objects
	public RigidBody3D GrabbedRigidBody;
	
	// Simple movement states stuff
	private bool _isCrouched;
	private bool _wasAirborne;
	private float _timeOffGround;
	
	// RNG
	private RandomNumberGenerator _rng = new();

	public override void _Ready()
	{
		RespawnPoint = GlobalPosition;
		Input.MouseMode = Input.MouseModeEnum.Captured;
		_collisionShape = GetNode<CollisionShape3D>("CollisionShape3D").Shape as CapsuleShape3D;
	}

	public override void _Process(double delta)
	{
		// Clamp camera
		CameraAnchor.Rotation = new Vector3(Mathf.DegToRad(Mathf.Clamp(CameraAnchor.RotationDegrees.X, -85, 85)),0,0);

		if (GlobalPosition.Y < -25)
		{
			Velocity = Vector3.Zero;
			GlobalPosition = RespawnPoint;
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		Vector3 velocity = Velocity;
		Vector3 desiredVelocity = Velocity; // Handles flat movement (X,Z). Y is always equal to Velocity.

		// Add the gravity.
		if (!IsOnFloor())
			velocity.Y -= Gravity * (float)delta;

		// Handle Jump.
		if (Input.IsActionPressed("Jump") && IsOnFloor())
		{
			velocity.Y = _isCrouched ? CrouchJumpVelocity : JumpVelocity;
		}
		
		// Handle Uncrouching
		if (_isCrouched && !Input.IsActionPressed("Crouch"))
		{
			_tryUncrouch();
		}
		
		// Get the input direction and handle the movement/deceleration.
		// As good practice, you should replace UI actions with custom gameplay actions.
		Vector2 inputDir = Input.GetVector("MoveLeft", "MoveRight", "MoveForward", "MoveBackward");
		Vector3 direction = (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();
		
		// Landing Detection
		if (IsOnFloor() && _wasAirborne)
		{
			if (_timeOffGround > 1)
			{
				SimpleViewpunch();
			}
			_timeOffGround = 0;
		}
		
		//Handle movement here
		if (inputDir != Vector2.Zero)
		{
			if (IsOnFloor())
			{
				if (Input.IsActionPressed("Sprint") && !_isCrouched) // Sprinting
				{
					desiredVelocity = direction * SprintSpeed;
				}
				if (!Input.IsActionPressed("Sprint")) // Not Sprinting
				{
					desiredVelocity = direction * Speed;
				}

				if (_isCrouched)
				{
					desiredVelocity = direction * CrouchSpeed;
				}
			}

			if (!IsOnFloor())
			{
				desiredVelocity += (direction * Speed) * 0.05f;
			}
		}
		
		// Friction on ground
		if (IsOnFloor())
		{
			desiredVelocity = desiredVelocity.MoveToward(Vector3.Zero, SlowdownSpeed);
		}
		
		// Limit to max speed before applying it.
		desiredVelocity = desiredVelocity.LimitLength(MaxSpeed);
		
		// Keep Y Velocity
		desiredVelocity.Y = velocity.Y;

		Velocity = velocity.MoveToward(desiredVelocity, 0.5f);
		
		// If we are holding an object, and it is going to be deleted, drop it.
		if (GrabbedRigidBody != null && GrabbedRigidBody.IsQueuedForDeletion())
		{
			DropRigidBody();
		}
		
		// Handle grabbed objects
		if (GrabbedRigidBody != null && !GrabbedRigidBody.IsQueuedForDeletion())
		{
			// Remove gravity so interactions work nicer.
			Vector3 removeYVelocity = new Vector3(GrabbedRigidBody.LinearVelocity.X, 0, GrabbedRigidBody.LinearVelocity.Z);
			GrabbedRigidBody.LinearVelocity = removeYVelocity;
			
			// Moves the item to the grab position
			GrabbedRigidBody.LinearVelocity = (_grabPoint.GlobalPosition - GrabbedRigidBody.GlobalPosition) * 10;
			
			// Make sure player isn't standing on top of the object
			// ...cheating bastards
			for (int i = 0; i < GetSlideCollisionCount(); i++)
			{
				if (GetSlideCollision(i).GetCollider() is RigidBody3D rigidBody3D && rigidBody3D.GetRid() == GrabbedRigidBody.GetRid())
				{
					if (GrabbedRigidBody.GlobalPosition.Y < this.GlobalPosition.Y)
					{
						DropRigidBody();
					}
				}
			}
			
			// also make sure the prop isn't behind the player
			if (GrabbedRigidBody != null && !GrabbedRigidBody.IsQueuedForDeletion())
			{
				Vector3 VectorBetweenPlayerAndObject =
					(GrabbedRigidBody.GlobalPosition - this.GlobalPosition); //* new Vector3(1, 0, 1);
				GD.Print(VectorBetweenPlayerAndObject.Normalized().Dot(-this.GlobalBasis.Z));
				if (VectorBetweenPlayerAndObject.Normalized().Dot(-this.GlobalBasis.Z) < -0.3f)
				{
					DropRigidBody();
				}
			}
		}
		_wasAirborne = !IsOnFloor();
		MoveAndSlide();
		
		// Add to airborne timer
		if(!IsOnFloor()) _timeOffGround += (float)delta;
		// GD.Print(_timeOffGround);

		// Push phys props out of the way
		for (int i = 0; i < GetSlideCollisionCount(); i++)
		{
			if (GetSlideCollision(i).GetCollider() is RigidBody3D rigidBody3D)
			{
				if (rigidBody3D.GlobalPosition.Y < GlobalPosition.Y)
				{
					// If we are standing on top of a prop, do not push it.
					break;
				}
				
				// Push props out of the way
				rigidBody3D.ApplyCentralImpulse(-GetSlideCollision(i).GetNormal() * PushForce);
				rigidBody3D.ApplyImpulse(-GetSlideCollision(i).GetNormal() * PushForce * 0.5f,
					GetSlideCollision(i).GetPosition() -
					(Vector3)GetSlideCollision(i).GetCollider().Get(Node3D.PropertyName.GlobalPosition));
			}
		}
		
		// Move camera rotation back to (0,0,0)
		if (Camera.Rotation != Vector3.Zero)
		{
			Camera.Rotation = Camera.Rotation.MoveToward(Vector3.Zero, 0.025f);
		}
		
		// Debug
		DebugDraw3D.DrawLine(this.GlobalPosition, this.GlobalPosition + desiredVelocity, Colors.Blue);
		DebugDraw3D.DrawLine(this.GlobalPosition, this.GlobalPosition + Velocity, Colors.Green);
	}

	public override void _Input(InputEvent @event)
	{
		if (Input.IsActionJustPressed(action: "SimulateViewPunch"))
		{
			SimpleViewpunch();
		}
		
		if (@event is InputEventMouseMotion motion)
		{
			this.RotateY(angle: -Mathf.DegToRad(deg: motion.Relative.X * MouseSensitivity));
			CameraAnchor.RotateX(angle: -Mathf.DegToRad(deg: motion.Relative.Y * MouseSensitivity));
		}

		if (Input.IsActionJustPressed(action: "Use"))
		{
			if (GrabbedRigidBody != null)
			{
				DropRigidBody();
				return;
			}
			
			if (_rayCast.IsColliding() && _rayCast.GetCollider() is RigidBody3D body && body.Mass <= 5)
			{
				GrabRigidBody(body: body);
				GD.Print(what: "Grabbed " + body.Name);
			}

			if (_rayCast.IsColliding() && _rayCast.GetCollider() is Node3D collision)
			{
				Interact(collision: collision);
			}
		}

		if (Input.IsActionJustPressed(action: "Crouch"))
		{
			_crouch();
		}

		if (Input.IsActionJustPressed(action: "Flashlight"))
		{
			_flashlight.ToggleFlashlight();
		}
	}

	public void SimpleViewpunch()
	{
		Camera.RotationDegrees += new Vector3(
			x: _rng.RandfRange(from: -10, to: 10),
			y: _rng.RandfRange(from: -10, to: 10),
			z: _rng.RandfRange(from: -10, to: 10));
	}

	private void Interact(Node3D collision)
	{
		for (int i = 0; i < collision.GetChildCount(); i++)
		{
			if (collision.GetChildOrNull<Interactable>(i) is Interactable interactable)
			{
				GD.Print(interactable.Name);
				interactable.EmitSignal(Interactable.SignalName.Interact);
			}
		}
	}
	
	private void GrabRigidBody(RigidBody3D body)
	{
		GD.Print("Grabbing");
		this.AddCollisionExceptionWith(body);
		GrabbedRigidBody = body;
	}

	private void DropRigidBody()
	{
		GD.Print("Dropping");
		this.RemoveCollisionExceptionWith(GrabbedRigidBody);
		GrabbedRigidBody = null;
	}

	public void ThrowRigidBody()
	{
		GD.Print("Throwing");
		// Store a temp reference to the rigidbody
		RigidBody3D body = GrabbedRigidBody;
		
		// Drop it
		DropRigidBody();
		
		// Apply a force from the player camera's forward axis
		body.ApplyImpulse(-CameraAnchor.GlobalBasis.Z.Normalized() * ThrowForce);
	}

	private void _crouch()
	{
		_collisionShape.SetHeight(1f);
		this.GlobalPosition = new Vector3(GlobalPosition.X, GlobalPosition.Y - 0.4f, GlobalPosition.Z);
		CameraAnchor.Position = new Vector3(CameraAnchor.Position.X, 1.2f, CameraAnchor.Position.Z);
		_isCrouched = true;
	}

	private void _tryUncrouch()
	{
		if (!_crouchCast.IsColliding())
		{
			this.GlobalPosition = new Vector3(GlobalPosition.X, GlobalPosition.Y + 0.4f, GlobalPosition.Z);
			_collisionShape.SetHeight(2f);
			CameraAnchor.Position = new Vector3(CameraAnchor.Position.X, 1.8f, CameraAnchor.Position.Z);
			_isCrouched = false;
		}
	}
}
