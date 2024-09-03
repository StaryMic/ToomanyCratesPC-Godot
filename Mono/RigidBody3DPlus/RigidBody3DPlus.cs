using Godot;
using System;
using System.Numerics;
using Vector3 = Godot.Vector3;
[GlobalClass]
public partial class RigidBody3DPlus : RigidBody3D
{
    // Tracked variables
    private Vector3 _prevVelocity;
    
    // Setup variables.
    [Export] private AudioStreamRandomizer _impactAudio;
    [Export] private AudioStreamRandomizer _breakAudio;

    public override void _Ready()
    {
        SetContactMonitor(true);
        SetMaxContactsReported(1);
        
        this.BodyShapeEntered += OnBodyEntered;
    }

    private void OnBodyEntered(Rid bodyRid, Node body, long bodyShapeIndex, long localShapeIndex)
    {
        GD.Print(GetForce());
    }

    public float GetForce()
    {
        return MathF.Abs((_prevVelocity.Length() - LinearVelocity.Length()) * Mass);
    }
    
    public Vector3 GetVelocityDelta()
    {
        return (LinearVelocity - _prevVelocity).Abs();
    }
    public override void _PhysicsProcess(double delta)
    {
        _prevVelocity = LinearVelocity;
    }
}
