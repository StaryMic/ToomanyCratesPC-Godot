using Godot;
using System;

[GlobalClass]
public partial class Destructable : Node3D
{
	// Exported Variables and Scenes
	[ExportGroup("Health")]
	[Export] public int Health;

	[ExportGroup("Effects")]
	[Export] private PackedScene _gibParticlePackedScene;
	[Export] private AudioStreamRandomizer _damageAudioStreams;
	[Export] private AudioStreamRandomizer _breakAudioStreams;
	
	[ExportGroup("Physics")]
	[Export] private bool _physicsDamage;
	[Export] private float _forceBreakThreshold = 2.5f;
	[Export] private AudioStreamRandomizer _impactAudioStreams;
	
	// Toggles
	[ExportGroup("Spawns")]
	[Export] private PackedScene _breakScene;
	[Export] private float _breakForce = 1f;
	
	// External references
	private RigidBody3DPlus _rigidBody3D;
	
	// Signals
	[Signal] public delegate void DamageEventHandler(int damage);
	[Signal] public delegate void BreakEventHandler();
	// To be used by a future crate manager or anything that needs to detect a break
	
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Damage += OnDamage;

		if (_physicsDamage)
		{
			_rigidBody3D = this.GetParent<RigidBody3DPlus>();
			_rigidBody3D.BodyShapeEntered += OnCollide;
		}
	}

	private void OnCollide(Rid bodyRid, Node body, long bodyShapeIndex, long localShapeIndex)
	{
		// GD.Print(_rigidBody3D.GetForce());
		if (_rigidBody3D.GetForce().Length() >= _forceBreakThreshold)
		{
			GD.Print("Force over break threshold!");
			OnDamage(99);
		}

		if (_rigidBody3D.GetForce().Length() > 0.5f)
		{
			// float desiredVolume = MathF.Log(_rigidBody3D.GetForce() / 10);
			float desiredVolume = Mathf.Min(-15 + (10 * Mathf.Log(_rigidBody3D.GetForce().Length())-10), -10);
			GD.Print(desiredVolume);
			PlayAudioStreamAtCurrentPosition(_impactAudioStreams, "Props", desiredVolume);
		}
	}

	private void OnDamage(int damage)
	{
		Health -= damage;

		if (Health <= 0 && !this.GetParent().IsQueuedForDeletion())
		{
			if (this.GetParent() is RigidBody3D rigidBody3D)
			{
				// Disable collision.
				rigidBody3D.CollisionMask = 0;
			}
			
			if (_breakAudioStreams != null)
			{
				PlayAudioStreamAtCurrentPosition(_breakAudioStreams);
			}
			
			if(_gibParticlePackedScene != null)
			{
				ImpactGPUParticles impactGPUParticles = _gibParticlePackedScene.Instantiate<ImpactGPUParticles>();
				this.GetTree().Root.GetChild(-1).AddChild(impactGPUParticles);
				impactGPUParticles.GlobalPosition = this.GetParent<Node3D>().GlobalPosition;
			}

			if (_breakScene != null)
			{
				Node3D spawnedScene = _breakScene.Instantiate<Node3D>();
				this.GetTree().Root.GetChild(-1).AddChild(spawnedScene);
				spawnedScene.GlobalPosition = this.GlobalPosition;

				for (int i = 0; i < spawnedScene.GetChildCount(); i++)
				{
					if (spawnedScene.GetChild(i) is RigidBody3D body)
					{
						GD.Print(body.Name);
						Vector3 pushDirection = (body.GlobalPosition - spawnedScene.GlobalPosition).Normalized() * _breakForce;
						body.LinearVelocity = pushDirection + _rigidBody3D.LinearVelocity;
					}
				}
			}
			
			// Queue deletion
			this.GetParent().QueueFree();
		}
		
		if (_damageAudioStreams != null && !this.GetParent().IsQueuedForDeletion())
		{
			PlayAudioStreamAtCurrentPosition(_damageAudioStreams);
		}
	}

	private void PlayAudioStreamAtCurrentPosition(AudioStream stream, string bus = "Props", float volume = 0)
	{
		// Play audio
		ImpactAudioPlayer3D impactAudioPlayer3D = new ImpactAudioPlayer3D();
		impactAudioPlayer3D.Autoplay = true;
		impactAudioPlayer3D.Stream = stream;
		impactAudioPlayer3D.Bus = bus;
		impactAudioPlayer3D.SetVolumeDb(volume);
		this.GetTree().Root.GetChild(-1).AddChild(impactAudioPlayer3D);
		impactAudioPlayer3D.GlobalPosition = this.GetParent<Node3D>().GlobalPosition;
	}
}
