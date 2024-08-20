using Godot;
using System;

public partial class PlayerCharacter : CharacterBody3D
{
	public const float Speed = 5.0f;
	public const float JumpVelocity = 4.5f;
	public const float SprintSpeed = 7.5f;
	public const float SlowdownSpeed = 0.2f;
	public const float PushForce = 0.75f;

	public const float MouseSensitivity = 0.3f;
	
	[Export] private PackedScene TempImpactParticles;

	// Get the gravity from the project settings to be synced with RigidBody nodes.
	public float Gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();
	
	// Get Nodes from scene
	private Camera3D _camera;
	private RayCast3D _rayCast;
	private Node3D _grabPoint;
	private Camera3D _viewmodelCamera;

	// Tracking active objects
	private RigidBody3D _grabbedRigidBody;

	public override void _Ready()
	{
		Input.MouseMode = Input.MouseModeEnum.Captured;
		_camera = GetNode<Camera3D>("Camera3D");
		_rayCast = GetNode<RayCast3D>("Camera3D/GrabRaycast");
		_grabPoint = GetNode<Node3D>("Camera3D/GrabPoint");
		_viewmodelCamera = GetNode<Camera3D>("Camera3D/SubViewportContainer/SubViewport/Camera3D");
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
		if (_grabbedRigidBody != null)
		{
			// Remove gravity so interactions work nicer.
			Vector3 removeYVelocity = new Vector3(_grabbedRigidBody.LinearVelocity.X, 0, _grabbedRigidBody.LinearVelocity.Z);
			_grabbedRigidBody.LinearVelocity = removeYVelocity;
			
			// Moves the item to the grab position
			_grabbedRigidBody.LinearVelocity = (_grabPoint.GlobalPosition - _grabbedRigidBody.GlobalPosition) * 10;
			
			// Make sure player isn't standing on top of the object
			// ...cheating bastards
			for (int i = 0; i < GetSlideCollisionCount(); i++)
			{
				if (GetSlideCollision(i).GetCollider() is RigidBody3D rigidBody3D && rigidBody3D.GetRid() == _grabbedRigidBody.GetRid())
				{
					if (_grabbedRigidBody.GlobalPosition.Y < this.GlobalPosition.Y)
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
			if (_grabbedRigidBody != null)
			{
				DropRigidBody();
				return;
			}
			
			if (_rayCast.IsColliding() && _rayCast.GetCollider() is RigidBody3D body && body.Mass <= 5)
			{
				GrabRigidBody(body);
				GD.Print("Grabbed " + body.Name);
			}
		}
	}

	private void GrabRigidBody(RigidBody3D body)
	{
		GD.Print("Grabbing");
		_grabbedRigidBody = body;
	}

	private void DropRigidBody()
	{
		GD.Print("Dropping");
		_grabbedRigidBody = null;
	}
}
