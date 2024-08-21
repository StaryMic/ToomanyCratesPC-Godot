using Godot;

[GlobalClass]
public partial class WeaponResource : Resource
{
    [ExportGroup("Weapon Properties")]
    [Export] public StringName WeaponName;
    [Export(PropertyHint.Enum, "Melee, Automatic, Projectile")] public int Type;
    [Export] public int Damage;
    [Export] public float Range;

    [ExportGroup("Visuals")]
    [Export] public PackedScene MeshScene;
    [ExportSubgroup("Particles")]
    [Export] public PackedScene ImpactParticles;
    [Export] public PackedScene FiringParticles;
    
    [ExportSubgroup("Animations")]
    [Export] public string[] FiringAnimations;
    [Export] public string[] ReloadAnimations;
    [Export] public string[] SecondaryAnimations;
    
    [ExportGroup("Projectile Info")]
    [Export] public float SpreadInDegrees;
    [Export] public int BulletsPerShot; // Used for Shotguns
    [Export] public int BulletsPerMagazine;
    [Export] public float CooldownPerShot;
    [Export] public PackedScene? ProjectileScene;
    
    [ExportGroup("Sounds")]
    [Export] public AudioStreamRandomizer? FiringSounds;
    [Export] public AudioStreamRandomizer? ReloadSounds;
    [Export] public AudioStreamRandomizer? ImpactSounds;

    [ExportGroup("Physics")]
    [Export] public float ForceOnImpact;
}
