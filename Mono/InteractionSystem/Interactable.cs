using Godot;
using System;

public partial class Interactable : Node
{
	[Export] public CollisionObject3D InteractionCollider;
	
	[Signal] public delegate void InteractEventHandler();
}
