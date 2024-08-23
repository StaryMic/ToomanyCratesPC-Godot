using Godot;
using System;

public partial class PlayerCharacter : CharacterBody3D
{
	public const float Speed = 5.0f;
	public const float JumpVelocity = 4.5f;
	public const float SprintSpeed = 7.5f;
	public const float SlowdownSpeed = 0.35f;
	public const float PushForce = 0.75f;
	public const float ThrowForce = 1.5f;

	public const float MouseSensitivity = 0.3f;
	
	// Get the gravity from the project settings to be synced with RigidBody nodes.
	public float Gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();
	
	// Get Nodes from scene
	private Camera3D _camera;
	private RayCast3D _rayCast;
	private Node3D _grabPoint;
	private Camera3D _viewmodelCamera;
	private RayCast3D _crouchCast;
	private CapsuleShape3D _collisionShape;

	// Tracking active objects
	public RigidBody3D GrabbedRigidBody;
	
	// Simple movement states stuff
	private bool _isCrouching;

	public override void _Ready()
	{
		Input.MouseMode = Input.MouseModeEnum.Captured;
		_camera = GetNode<Camera3D>("Camera3D");
		_rayCast = GetNode<RayCast3D>("Camera3D/GrabRaycast");
		_grabPoint = GetNode<Node3D>("Camera3D/GrabPoint");
		_viewmodelCamera = GetNode<Camera3D>("Camera3D/SubViewportContainer/SubViewport/Camera3D");
		_crouchCast = GetNode<RayCast3D>("CrouchCast");
		_collisionShape = GetNode<CollisionShape3D>("CollisionShape3D").Shape as CapsuleShape3D;
	}

	public override void _Process(double delta)
	{
		// Clamp camera
		_camera.Rotation = new Vector3(Mathf.DegToRad(Mathf.Clamp(_camera.RotationDegrees.X, -85, 85)),0,0);
		
		// Align viewport cam with normal cam.
		_viewmodelCamera.GlobalTransform = _camera.GlobalTransform;
	}

	public override void _PhysicsProcess(double delta)
	{
		Vector3 velocity = Velocity;

		// Add the gravity.
		if (!IsOnFloor())
			velocity.Y -= Gravity * (float)delta;

		// Handle Jump.
		if (Input.IsActionPressed("Jump") && IsOnFloor())
			velocity.Y = JumpVelocity;
		
		// Handle crouching
		if (_isCrouching && _collisionShape.Height > 1.4f)
		{
			_collisionShape.Height = 1.4f;
			_camera.Position = new Vector3(0, 1.4f, 0);
		}

		if (!_isCrouching && !_crouchCast.IsColliding() && _collisionShape.Height <= 1.4f)
		{
			_collisionShape.Height = 2;
			_camera.Position = new Vector3(0, 1.8f, 0);
		}

		// Get the input direction and handle the movement/deceleration.
		// As good practice, you should replace UI actions with custom gameplay actions.
		Vector2 inputDir = Input.GetVector("MoveLeft", "MoveRight", "MoveForward", "MoveBackward");
		Vector3 direction = (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();
		if (direction != Vector3.Zero && !Input.IsActionPressed("Sprint"))
		{
			velocity.Z = direction.Z * Speed;
			velocity.X = direction.X * Speed;
		} 
		if (direction != Vector3.Zero && Input.IsActionPressed("Sprint"))
		{
			velocity.X = direction.X * SprintSpeed;
			velocity.Z = direction.Z * SprintSpeed;
		}
		if (direction == Vector3.Zero)
		{
			// velocity.X = Mathf.MoveToward(Velocity.X, 0, SlowdownSpeed);
			// velocity.Z = Mathf.MoveToward(Velocity.Z, 0, SlowdownSpeed);
			
			// Vector3 tempVelocity = velocity.MoveToward(Vector3.Zero, 0.2f);
			// tempVelocity.Y = velocity.Y;
			// velocity = tempVelocity;
			
			velocity = velocity.MoveToward(new Vector3(0, velocity.Y, 0), SlowdownSpeed);
			
		}

		Velocity = velocity;
		
		// Handle grabbed objects
		if (GrabbedRigidBody != null)
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
		}

		MoveAndSlide();

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
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventMouseMotion motion)
		{
			this.RotateY(-Mathf.DegToRad(motion.Relative.X * MouseSensitivity));
			_camera.RotateX(-Mathf.DegToRad(motion.Relative.Y * MouseSensitivity));
		}

		if (Input.IsActionJustPressed("Use"))
		{
			if (GrabbedRigidBody != null)
			{
				DropRigidBody();
				return;
			}
			
			if (_rayCast.IsColliding() && _rayCast.GetCollider() is RigidBody3D body && body.Mass <= 5)
			{
				GrabRigidBody(body);
				GD.Print("Grabbed " + body.Name);
			}

			if (_rayCast.IsColliding() && _rayCast.GetCollider() is Node3D collision)
			{
				Interact(collision);
			}
		}

		if (Input.IsActionJustPressed("Crouch"))
		{
			_isCrouching = true;
		}

		if (Input.IsActionJustReleased("Crouch"))
		{
			_isCrouching = false;
		}
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
		GrabbedRigidBody = body;
	}

	private void DropRigidBody()
	{
		GD.Print("Dropping");
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
		body.ApplyImpulse(-_camera.GlobalBasis.Z.Normalized() * ThrowForce);
	}
}
