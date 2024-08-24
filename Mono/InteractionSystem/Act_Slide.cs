using Godot;
using System;

//TODO: uh idk where this node is gonna go tbh.

[Tool]
public partial class Act_Slide : Node3D
{ 
	private Vector3 _startPos;
	[Export] private Vector3 _endPos = new Vector3(0, 0, 0);
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_startPos = this.GlobalPosition;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (Engine.IsEditorHint())
		{
			_startPos = this.GlobalPosition;
			DebugDraw3D.DrawLine(_startPos, _startPos + _endPos);
		}
	}
}
