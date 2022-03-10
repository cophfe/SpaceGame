using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAction : MonoBehaviour
{
	[SerializeField] Transform miningRaycastOrigin;
	[SerializeField] float miningStrength;
	[SerializeField] float miningDistance;
	[SerializeField] float miningRadius;
	[SerializeField] LayerMask groundMask;

	//Controls mining, inventory, and other stuff
	PixelModifier modifier;
	//input
	bool actionButtonHeld = false;

	private void Awake()
	{
		modifier = new PixelModifier(miningStrength, miningRadius, ModifierType.AddOvertime, Pixel.MaterialType.Stone, new PixelStencilCircle());
	}
	private void FixedUpdate()
	{
		if (EvaluateActionHeld())
		{
			Vector2 mousePos = GameManager.Instance.MainCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
			Vector2 direction = mousePos - (Vector2)miningRaycastOrigin.position;

			RaycastHit2D hit = Physics2D.CircleCast(miningRaycastOrigin.position, miningRadius, direction, miningDistance, groundMask.value);
			if (hit)
			{
				PixelWorld world = hit.collider.GetComponentInParent<PixelWorld>();
				modifier.Centre = hit.point;

				if (world && world.IsColliding(modifier))
				{
					Vector2 stencilPos = world.transform.InverseTransformPoint(modifier.Centre);
					//stencilPos += 0.5f * map.Size;

					stencilPos = world.transform.TransformPoint(stencilPos);
					modifier.Centre = stencilPos;
					
					//now apply changes to pixel world
					world.ApplyStencil(modifier);
				}
			}
		}
		


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
