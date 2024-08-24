using Godot;
using System;
using System.ComponentModel;

public partial class WeaponBase : Node3D
{
    // Setup
    [Export] public WeaponResource WeaponDescriptor;
    [Export] public Timer CooldownTimer;
    [Export] public RayCast3D RayCast3D;
    [Export] public AudioStreamPlayer AudioStreamPlayer;
    [Export] public PlayerCharacter PlayerCharacter;
    
    // Dynamically found items
    private Node3D modelNode;
    private AnimationPlayer animationPlayer;
    private MeshInstance3D weaponMesh;
    
    private RandomNumberGenerator rng = new RandomNumberGenerator();
    private int _bulletsInMagazine;
    private float _aimSpreadFactor;

    private PackedScene? _impactParticleScene;
    
    // Tracking variables
    private bool _reloading = false;

    public override void _Ready()
    {
        SwapWeapon();
    }

    public void SwapWeapon()
    {
        // Stop old things
        if (animationPlayer != null)
        {
            animationPlayer.AnimationFinished -= AnimationPlayerOnAnimationFinished;
            animationPlayer?.Stop();
        }
        modelNode?.QueueFree();
        CooldownTimer.Stop();
        AudioStreamPlayer.Stop();
        
        // Init and load new things
        AudioStreamPlayer.Stream = WeaponDescriptor.FiringSounds;
        CooldownTimer.WaitTime = WeaponDescriptor.CooldownPerShot;
        RayCast3D.TargetPosition = new Vector3(0, 0, -WeaponDescriptor.Range);
        _impactParticleScene = WeaponDescriptor.ImpactParticles;
        
        // Spawn the new weapon model
        modelNode = WeaponDescriptor.MeshScene.Instantiate<Node3D>();
        this.AddChild(modelNode);
        
        // Find the animationplayer for the new model
        animationPlayer = modelNode.GetChild<AnimationPlayer>(1);
        // Connect animation player to reload check
        animationPlayer.AnimationFinished += AnimationPlayerOnAnimationFinished;
        
        // Find and set mesh to be on the Viewmodel layer (2)
        weaponMesh = modelNode.GetChild(0).GetChild<MeshInstance3D>(0);
        weaponMesh.Layers = 2;
        
        // TODO: Load previous ammo value for each weapon.
    }

    private void PrimaryAction()
    {
        if (PlayerCharacter.GrabbedRigidBody != null)
        {
            PlayerCharacter.ThrowRigidBody();
            CooldownTimer.Start(0.75f);
        }
        
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
                    if (_bulletsInMagazine > 0 && _reloading == false) // if we have ammo and we are NOT reloading
                    {
                        if (animationPlayer.IsPlaying())
                        {
                            animationPlayer.Stop();
                        }
                        animationPlayer.Play(WeaponDescriptor.FiringAnimations[rng.RandiRange(0, WeaponDescriptor.FiringAnimations.Length - 1)]);
                        AudioStreamPlayer.Play();
                        FireRaycast();
                        CooldownTimer.Start(WeaponDescriptor.CooldownPerShot);
                        GD.Print(_bulletsInMagazine);
                        _bulletsInMagazine--;
                        break;
                    }
                    
                    if(_bulletsInMagazine <= 0) Reload();
                    
                    break;
                    
                
                default:
                    GD.PrintErr("Weapon type not recognized.");
                    break;
            }
        }
    }

    private void SecondaryAction()
    {
        throw new NotImplementedException("SecondaryAction ain't there yet mate.");
    }

    private void Reload()
    {
        if (WeaponDescriptor.Type != 0) //Not a melee weapon
        {
            animationPlayer.Play(
                WeaponDescriptor.ReloadAnimations[rng.RandiRange(0, WeaponDescriptor.ReloadAnimations.Length - 1)]);
            _reloading = true;
        }
    }
    
    private void AnimationPlayerOnAnimationFinished(StringName animname)
    {
        // Basically a "reload check"
        if (animname.ToString().ToLower().Contains("reload"))
        {
            _bulletsInMagazine = WeaponDescriptor.BulletsPerMagazine;
            GD.Print("Reload finished.");
            _reloading = false;
        }
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
                    Vector3 basisX = -impactGpuParticles.Transform.Basis.Z.Cross(RayCast3D.GetCollisionNormal());
                    Vector3 basisZ = impactGpuParticles.Transform.Basis.Z;
                    impactGpuParticles.Basis = new Basis(basisX,basisY,basisZ).Orthonormalized();
                    GD.Print(impactGpuParticles.Basis);
                    GD.Print(impactGpuParticles.GlobalPosition);
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
                
                // Deal damage if the hit has a Destructable component
                if (RayCast3D.GetCollider() is Node3D baseNode &&
                    baseNode.FindChild("Destructable") is Destructable destructable)
                {
                    destructable.EmitSignal(Destructable.SignalName.Damage, WeaponDescriptor.Damage);
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

        if (Input.IsActionPressed("Reload") && !_reloading)
        {
            Reload();
        }

        if (Input.IsActionJustPressed("SwapCrowbar") && !_reloading)
        {
            WeaponDescriptor = GD.Load<WeaponResource>("res://Items/Weapons/Crowbar/Crowbar_desc.tres");
            SwapWeapon();
        }

        if (Input.IsActionJustPressed("SwapPistol") && !_reloading)
        {
            WeaponDescriptor = GD.Load<WeaponResource>("res://Items/Weapons/TestPistol/TestPistol_desc.tres");
            SwapWeapon();
        }
        
        _aimSpreadFactor = Mathf.MoveToward(_aimSpreadFactor, 0, (float)0.01);
        _aimSpreadFactor = Mathf.Clamp(_aimSpreadFactor, 0, 1);
    }
    
    
}
