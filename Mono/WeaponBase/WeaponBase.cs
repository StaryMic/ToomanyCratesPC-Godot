using Godot;
using System;
using Godot.Collections;

public partial class WeaponBase : Node3D
{
	// Visuals
	[Export] public MeshInstance3D _weaponModel;
	[Export] public AnimationPlayer _animation;
	
	// Firing Properties
	[Export] public bool _isMelee;
	[Export] public int _magCapacity;
	[Export] public int _bullets;
	[Export] public float _fireCooldown = 1f;
	[Export] public float _firingDistance = 15f; // Meters?
	[Export(PropertyHint.Layers3DPhysics)] private uint _collisionMask;

	// Events
	[Signal] public delegate void ShootGunEventHandler();
	[Signal] public delegate void ReloadGunEventHandler();
	
	// Variables
	public Timer _shotTimer = new Timer();
}
