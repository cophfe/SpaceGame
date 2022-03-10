using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAction : MonoBehaviour
{
	GravityWell gravity;
	//Controls mining, inventory, and other stuff
	PixelModifier modifier;

	//input
	bool actionButtonHeld = false;

	private void FixedUpdate()
	{
		modifier.Centre = GameManager.Instance.MainCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());

		foreach (var world in GameManager.Instance.Worlds)
		{
			modifier.World = world;
			if (world.IsColliding(modifier))
			{
				Vector2 stencilPos = world.transform.InverseTransformPoint(modifier.Centre);
				//stencilPos += 0.5f * map.Size;
				
				stencilPos = world.transform.TransformPoint(stencilPos);
				modifier.Centre = stencilPos;
				//now apply changes to pixel world
				if (EvaluateActionHeld())
				{
					world.ApplyStencil(modifier);
				}
				break;
			}
		}
	}

	#region Evaluate Functions

	public bool EvaluateActionHeld()
	{
		return actionButtonHeld;
	}

	#endregion

	//functions called by player input component
	#region Input Functions

	public void OnActionPressed(InputAction.CallbackContext ctx)
	{
		actionButtonHeld = ctx.phase == InputActionPhase.Performed;
	}
	#endregion

}
