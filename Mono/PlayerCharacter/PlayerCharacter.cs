using Godot;
using System;

public partial class PlayerCharacter : CharacterBody3D
{
	public const float Speed = 5.0f;
	public const float JumpVelocity = 4.5f;
	public const float SprintSpeed = 7.5f;
	public const float SlowdownSpeed = 0.25f;
	public const float PushForce = 0.5f;

	public const float MouseSensitivity = 0.5f;

	// Get the gravity from the project settings to be synced with RigidBody nodes.
	public float Gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();
	
	// Get Nodes from scene
	private Camera3D _camera;
	private RayCast3D _rayCast;
	private Node3D _grabPoint;

	// Tracking active objects
	private RigidBody3D _grabbedRigidBody;

	public override void _Ready()
	{
		Input.MouseMode = Input.MouseModeEnum.Captured;
		_camera = GetNode<Camera3D>("Camera3D");
		_rayCast = GetNode<RayCast3D>("Camera3D/GrabRaycast");
		_grabPoint = GetNode<Node3D>("Camera3D/GrabPoint");
	}

	public override void _Process(double delta)
	{
		_camera.Rotation = new Vector3(Mathf.DegToRad(Mathf.Clamp(_camera.RotationDegrees.X, -85, 85)),0,0);
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
			velocity.X = direction.X * Speed;
			velocity.Z = direction.Z * Speed;
		} 
		if (direction != Vector3.Zero && Input.IsActionPressed("Sprint"))
		{
			velocity.X = direction.X * SprintSpeed;
			velocity.Z = direction.Z * SprintSpeed;
		}
		if(direction == Vector3.Zero)
		{
			velocity.X = Mathf.MoveToward(Velocity.X, 0, SlowdownSpeed);
			velocity.Z = Mathf.MoveToward(Velocity.Z, 0, SlowdownSpeed);
		}

		Velocity = velocity;
		
		// Handle grabbed objects
		if (_grabbedRigidBody != null)
		{
			// Moves the item to the grab position
			_grabbedRigidBody.LinearVelocity = Vector3.Zero;
			KinematicCollision3D RigidBodyCollision = _grabbedRigidBody.MoveAndCollide(
				(_grabPoint.GlobalPosition - _grabbedRigidBody.GlobalPosition).Normalized() *
				_grabbedRigidBody.GlobalPosition.DistanceTo(_grabPoint.GlobalPosition) *
				10 *
				(float)delta,
				false,
				0.001f,
				true,
				3);
			
			GD.Print("distance to grab: " + _grabbedRigidBody.GlobalPosition.DistanceTo(_grabPoint.GlobalPosition));
			if (_grabbedRigidBody.GlobalPosition.DistanceTo(_grabPoint.GlobalPosition) > 1.25f)
			{
				_grabbedRigidBody = null;
			}

			if (RigidBodyCollision != null)
			{
				GD.Print("Hit " + RigidBodyCollision.GetCollider().GetClass());
				if (RigidBodyCollision.GetCollider() is CharacterBody3D player)
				{
					if (player.GlobalPosition.Y > _grabbedRigidBody.GlobalPosition.Y)
					{
						_grabbedRigidBody = null;
					}
				}
			}
		}

		MoveAndSlide();

		for (int i = 0; i < GetSlideCollisionCount(); i++)
		{
			if (GetSlideCollision(i).GetCollider() is RigidBody3D rigidBody3D && this.GlobalPosition.Y < rigidBody3D.GlobalPosition.Y)
			{
				Vector3 PushDir = new Vector3(-GetSlideCollision(i).GetNormal().X, 0,
					-GetSlideCollision(i).GetNormal().Z);
				rigidBody3D.ApplyCentralImpulse(PushDir * PushForce);
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
				_grabbedRigidBody = null;
				GD.Print("Ungrabbed");
				return;
			}
			
			if (_rayCast.IsColliding() && _rayCast.GetCollider() is RigidBody3D body && body.Mass <= 5)
			{
				_grabbedRigidBody = body;
				GD.Print("Grabbed " + body.Name);
			}
		}
	}
}
