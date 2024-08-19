using Godot;
using System;
using Godot.Collections;
using TooManyCratesPC.Mono.WeaponBase;

public partial class WeaponBase : Node3D
{
    [Export] public WeaponResource WeaponDescriptor;
    [Export] public MeshInstance3D WeaponModel;
    [Export] public AnimationPlayer AnimationPlayer;
    

    private int BulletsInMagazine;

    public override void _Ready()
    {
        SwapWeapon();
    }

    public void SwapWeapon()
    {
        WeaponModel.SetMesh(WeaponDescriptor.Mesh);
    }
}
