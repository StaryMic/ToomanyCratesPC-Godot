using Godot;
using System;

[GlobalClass]
public partial class Destructable : Node3D
{
	// Exported Variables and Scenes
	[ExportGroup("Health")]
	[Export] private int _health;
	[Export] private int _maxHealth; // May go unused

	[ExportGroup("Effects")]
	[Export] private PackedScene _gibParticlePackedScene;
	[Export] private AudioStreamRandomizer _damageAudioStreams;
	[Export] private AudioStreamRandomizer _breakAudioStreams;
	
	[ExportGroup("Physics")]
	[Export] private bool _physicsDamage;
	[Export] private float _forceBreakThreshold = 2.5f;
	[Export] private AudioStreamRandomizer _impactAudioStreams;
	
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
		GD.Print(_rigidBody3D.GetForce());
		if (_rigidBody3D.GetForce() >= _forceBreakThreshold)
		{
			GD.Print("Force over break threshold!");
			OnDamage(99);
		}

		if (_rigidBody3D.GetForce() > 0.5f)
		{
			// float desiredVolume = MathF.Log(_rigidBody3D.GetForce() / 10);
			float desiredVolume = Mathf.Min(-15 + (10 * Mathf.Log(_rigidBody3D.GetForce())-10), -10);
			GD.Print(desiredVolume);
			PlayAudioStreamAtCurrentPosition(_impactAudioStreams, "Props", desiredVolume);
		}
	}

	private void OnDamage(int damage)
	{
		_health -= damage;

		if (_health <= 0 && !this.GetParent().IsQueuedForDeletion())
		{
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
