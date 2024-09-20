using Godot;
using System;
using System.Numerics;
using Vector3 = Godot.Vector3;
[GlobalClass]

public partial class RigidBody3DPlus : RigidBody3D
{
    ///<summary>
    /// Extension method of RigidBody3D.
    /// Made for aiding physics stuff.
    ///</summary>
    
    // Tracked variables
    private Vector3 _prevVelocity;

    public override void _Ready()
    {
        SetContactMonitor(true);
        SetMaxContactsReported(1);
        
        this.BodyShapeEntered += OnBodyEntered;
    }

    private void OnBodyEntered(Rid bodyRid, Node body, long bodyShapeIndex, long localShapeIndex)
    {
        // GD.Print(this.Name + ": " + GetForce().Length());
    }

    public Vector3 GetForce()
    {
        return ((_prevVelocity - LinearVelocity) * Mass).Abs();
    }
    
    public Vector3 GetVelocityDelta()
    {
        return (LinearVelocity - _prevVelocity).Abs() * (float)GetPhysicsProcessDeltaTime();
    }
    public override void _PhysicsProcess(double delta)
    {
        _prevVelocity = LinearVelocity;
    }
}
