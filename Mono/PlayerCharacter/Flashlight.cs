using Godot;
using System;

public partial class Flashlight : SpotLight3D
{
	[Export] private float _enabledEnergy = 10f;
	private bool _isFlashlightOn;
	
	public void ToggleFlashlight()
	{
		if (_isFlashlightOn)
		{
			_isFlashlightOn = false;
			LightEnergy = 0;
			return;
		}

		if (_isFlashlightOn == false)
		{
			_isFlashlightOn = true;
			LightEnergy = _enabledEnergy;
		}
	}
}
