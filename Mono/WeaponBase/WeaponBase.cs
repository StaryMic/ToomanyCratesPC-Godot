using Godot;
using System;
using System.ComponentModel;
using System.Linq.Expressions;
using Godot.Collections;
using TooManyCratesPC.Mono.WeaponBase;

public partial class WeaponBase : Node3D
{
    // Setup
    [Export] public WeaponResource WeaponDescriptor;
    [Export] public Timer CooldownTimer;
    [Export] public Timer ReloadTimer;
    [Export] public RayCast3D RayCast3D;
    [Export] public AudioStreamPlayer AudioStreamPlayer;
    
    // Dynamically found items
    private Node3D modelNode;
    private AnimationPlayer animationPlayer;
    private MeshInstance3D weaponMesh;
    
    private RandomNumberGenerator rng = new RandomNumberGenerator();
    private int _bulletsInMagazine;
    private float _aimSpreadFactor;

    private PackedScene? _impactParticleScene;

    public override void _Ready()
    {
        SwapWeapon();
    }

    public void SwapWeapon()
    {
        // Stop old things
        animationPlayer?.Stop();
        modelNode?.QueueFree();
        CooldownTimer.Stop();
        ReloadTimer.Stop();
        AudioStreamPlayer.Stop();
        
        // Init and load new things
        AudioStreamPlayer.Stream = WeaponDescriptor.FiringSounds;
        CooldownTimer.WaitTime = WeaponDescriptor.CooldownPerShot;
        ReloadTimer.WaitTime = WeaponDescriptor.ReloadTime;
        RayCast3D.TargetPosition = new Vector3(0, 0, -WeaponDescriptor.Range);
        _impactParticleScene = WeaponDescriptor.ImpactParticles;
        
        // Spawn the new weapon model
        modelNode = WeaponDescriptor.MeshScene.Instantiate<Node3D>();
        this.AddChild(modelNode);
        
        // Find the animationplayer for the new model
        animationPlayer = modelNode.GetChild<AnimationPlayer>(1);
        
        // Find and set mesh to be on the Viewmodel layer (2)
        weaponMesh = modelNode.GetChild(0).GetChild<MeshInstance3D>(0);
        weaponMesh.Layers = 2;
    }

    private void PrimaryAction()
    {
        if (CooldownTimer.IsStopped())
        {
            switch (WeaponDescriptor.Type)
            {
                case 0: //Melee
                    if (animationPlayer.IsPlaying())
                    {
                        animationPlayer.Stop();
                    }
                    animationPlayer.Play(WeaponDescriptor.FiringAnimations[rng.RandiRange(0, WeaponDescriptor.FiringAnimations.Length - 1)]);
                    AudioStreamPlayer.Play();
                    FireRaycast();
                    CooldownTimer.Start(WeaponDescriptor.CooldownPerShot);
                    break;
                case 1: //Automatic
                    break;
                    
                
                default:
                    throw new WarningException("Unable to determine weapon type.");
            }
        }
    }

    private void SecondaryAction()
    {
        throw new NotImplementedException("SecondaryAction ain't there yet mate.");
    }

    private void FireRaycast()
    {
        // Prevent sound spam
        int soundsPlayed = 0;
        int maxSoundsPlayed = 2;
        
        for (int i = 0; i < WeaponDescriptor.BulletsPerShot; i++)
        {
            // Fire off raycast with random offset determined by weapon spread and spread factor
            GD.Print("Firing bullet " + i);
            Vector3 RaycastOffset = new Vector3(
                rng.RandfRange(-WeaponDescriptor.SpreadInDegrees, WeaponDescriptor.SpreadInDegrees) * _aimSpreadFactor,
                rng.RandfRange(-WeaponDescriptor.SpreadInDegrees, WeaponDescriptor.SpreadInDegrees) * _aimSpreadFactor,
                0);
            RayCast3D.RotationDegrees = RaycastOffset;
            GD.Print(RaycastOffset);
            
            // Make it update.
            RayCast3D.ForceRaycastUpdate();
            
            if (RayCast3D.IsColliding())
            {
                // TODO: Deal damage to anything with the damageable component
                
                // Spawn impact particles
                if (_impactParticleScene != null)
                {
                    ImpactGPUParticles impactGpuParticles = (ImpactGPUParticles)_impactParticleScene.Instantiate();
                    GetTree().Root.GetChild(-1).AddChild(impactGpuParticles);
                    GD.Print(impactGpuParticles.Name);

                    impactGpuParticles.GlobalPosition = RayCast3D.GetCollisionPoint();
                    
                    // Orient particles to normal vector
                    // https://kidscancode.org/godot_recipes/3.x/3d/3d_align_surface/index.html
                    // This saved my ass ^^^
                    Vector3 basisY = RayCast3D.GetCollisionNormal();
                    Vector3 basisX = -impactGpuParticles.GlobalTransform.Basis.X.Cross(RayCast3D.GetCollisionNormal());
                    Vector3 basisZ = impactGpuParticles.GlobalTransform.Basis.Z;
                    impactGpuParticles.Basis = new Basis(basisX,basisY,basisZ).Orthonormalized();
                    GD.Print(impactGpuParticles.Basis);
                }
                else
                {
                    GD.Print("Failed to spawn impact particle scene. Scene was null.");
                }
                
                // Spawn impact sounds
                if (WeaponDescriptor.ImpactSounds != null && soundsPlayed <= maxSoundsPlayed)
                {
                    ImpactAudioPlayer3D impactPlayer = new ImpactAudioPlayer3D();
                    impactPlayer.Autoplay = true;
                    impactPlayer.Stream = WeaponDescriptor.ImpactSounds;
                    GetTree().Root.GetChild(-1).AddChild(impactPlayer);
                    impactPlayer.GlobalPosition = RayCast3D.GetCollisionPoint();
                    soundsPlayed++;
                }
                
                // Apply physics impulses
                if (RayCast3D.GetCollider() is RigidBody3D physBody)
                {
                    // physBody.ApplyImpulse(
                    //     (RayCast3D.GetCollisionPoint() - RayCast3D.GlobalPosition).Normalized() *
                    //     WeaponDescriptor.ForceOnImpact, RayCast3D.GetCollisionPoint() - physBody.GlobalPosition);
                    
                    // This method makes things move nicer than using the above method
                    physBody.ApplyTorqueImpulse((RayCast3D.GetCollisionPoint() - physBody.GlobalPosition).Normalized() * 0.5f);
                    physBody.ApplyImpulse((RayCast3D.GetCollisionPoint() - RayCast3D.GlobalPosition).Normalized() * WeaponDescriptor.ForceOnImpact);
                }
            }

            _aimSpreadFactor += 0.1f;
        }
    }

    public override void _Process(double delta)
    {
        if (Input.IsActionPressed("PrimaryAction"))
        {
            PrimaryAction();
        }

        if (Input.IsActionPressed("SecondaryAction"))
        {
            SecondaryAction();
        }
        
        _aimSpreadFactor = Mathf.MoveToward(_aimSpreadFactor, 0, (float)0.01);
        _aimSpreadFactor = Mathf.Clamp(_aimSpreadFactor, 0, 1);
    }
}
