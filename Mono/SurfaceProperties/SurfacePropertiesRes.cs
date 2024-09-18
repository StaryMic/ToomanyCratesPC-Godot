using Godot;
using Godot.Collections;
[GlobalClass]
public partial class SurfacePropertiesRes : Resource
{
    [ExportCategory("Sounds")]
    [Export] public Array<AudioStream> StepSounds;
    [Export] public Array<AudioStream> ImpactSounds;
    
    [ExportCategory("Particles")]
    [Export] public PackedScene ImpactParticles = new();
}
