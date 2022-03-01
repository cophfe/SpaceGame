using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class ChunkDrawer : MonoBehaviour
{
	#region Editors
	[SerializeField] Transform[] stencilVisualisations;
	[SerializeField] Material stencilMaterial;
	[SerializeField] bool snapToGrid = false;
	#endregion

	#region Private
	Stencil stencil;
	//input
	bool actionButtonHeld = false;
	#endregion

	private void Awake()
	{
		stencil = new SquareStencil(1.0f, 2, StencilModifierType.Add);
	}


	private void Update()
	{
		bool isColliding = false;

		stencil.Position = GameManager.Instance.MainCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());

		foreach (var map in GameManager.Instance.Maps)
		{
			if (map.IsColliding(stencil))
			{
				isColliding = true;
				//should intelligently choose the map instead of just choosing the first map found

				Vector2 stencilPos = map.transform.InverseTransformPoint(stencil.Position);
				//stencilPos += 0.5f * map.Size;
				if (snapToGrid)
				{
					stencilPos.x = ((int)(stencilPos.x / map.CellSize)) * map.CellSize;
					stencilPos.y = ((int)(stencilPos.y / map.CellSize)) * map.CellSize;
				}
				stencilPos = map.transform.TransformPoint(stencilPos);
				stencil.Position = stencilPos;
				//now apply changes to voxelMap
				if (EvaluateActionHeld())
				{
					map.ApplyStencil(stencil);
				}
				break;
			}
		}

		UpdateVisualisation(isColliding);
	}

	void UpdateVisualisation(bool enabled)
	{

		Transform visualization = stencilVisualisations[(int)stencil.StencilType];
		visualization.position = new Vector3(stencil.Position.x, stencil.Position.y, -2);
		visualization.localScale = Vector2.one * stencil.Radius * 2f;
		visualization.gameObject.SetActive(enabled);
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

	public void OnSwitchFill(InputAction.CallbackContext ctx)
	{
		if (ctx.performed)
		{
			stencil.Strength = -stencil.Strength;
			Debug.Log("fill: " + stencil.Strength);

		}
	}

	public void OnChangeRadius(InputAction.CallbackContext ctx)
	{
		if (ctx.performed)
		{
			if (ctx.ReadValue<float>() > 0)
			{
				stencil.Radius = stencil.Radius + 1;
			}
			else
			{
				stencil.Radius = stencil.Radius - 1;
			}
			Debug.Log("radius: " + stencil.Radius);

		}
	}

	public void OnChangeStencil(InputAction.CallbackContext ctx)
	{
		if (ctx.performed)
		{
			//change stencil visualisation
			stencilVisualisations[(int)stencil.StencilType].gameObject.SetActive(false);

			//if is a circle, switch to square
			if (stencil.StencilType == StencilType.Circle)
			{
				stencil = new SquareStencil(stencil.Strength, stencil.Radius, stencil.ModifierType);
				Debug.Log("Switched to square");
			}
			else
			{
				stencil = new CircleStencil(stencil.Strength, stencil.Radius, stencil.ModifierType);
				Debug.Log("Switched to circle");
			}
		}

	}

	public void OnChangeModifierType(InputAction.CallbackContext ctx)
	{
		if (ctx.performed)
		{
			stencil.ModifierType = (StencilModifierType)((int)(stencil.ModifierType + 1) % (int)StencilModifierType.Count);
		}
	}
	#endregion
}
