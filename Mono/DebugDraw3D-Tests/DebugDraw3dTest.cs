using Godot;
using System;

[Tool]
public partial class DebugDraw3dTest : Node3D
{
	public override void _Process(double delta)
	{
		if (Engine.IsEditorHint())
		{
			DebugDraw3D.DrawCylinderAb(this.GlobalPosition, this.GlobalPosition + new Vector3(0,1,0), 0.1f, Colors.Beige);
			if (Time.GetTicksMsec() % 1000 == 0)
			{
				GD.Print("balls");
			}
		}
	}
}
