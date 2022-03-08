using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

public class ChunkDrawer : MonoBehaviour
{
	#region Editors
	[SerializeField, Min(0)] float modifyStrength = 4;
	[SerializeField, Min(0)] float modifyRemoveStrength = 4;
	[SerializeField] Pixel.MaterialType drawMaterial = Pixel.MaterialType.Dirt;
	[SerializeField] Transform[] stencilVisualisations;
	[SerializeField] Material stencilMaterial;
	[SerializeField] bool snapToGrid = false;
	[SerializeField] TextMeshProUGUI radText;
	[SerializeField] TextMeshProUGUI modText;
	[SerializeField] TextMeshProUGUI strText;
	[SerializeField] TextMeshProUGUI shaText;
	#endregion

	#region Private
	PixelModifier modifier;
	//input
	bool actionButtonHeld = false;
	//visualisation updating
	bool isColliding = false;
	#endregion

	private void Awake()
	{
		modifier = new PixelModifier(modifyStrength, 2, ModifierType.AddOvertime, drawMaterial, new PixelStencilCircle());
		UpdateText();
	}

	private void OnValidate()
	{
		if (modifier != null)
		{
			modifier.Strength = modifyStrength;
			modifier.PixelMaterial = drawMaterial;
		}
	}

	private void Update()
	{
		modifier.Centre = GameManager.Instance.MainCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
		UpdateVisualisation(isColliding);
	}

	private void FixedUpdate()
	{
		modifier.Centre = GameManager.Instance.MainCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());

		foreach (var world in GameManager.Instance.Worlds)
		{
			modifier.World = world;
			if (world.IsColliding(modifier))
			{
				isColliding = true;
				//should intelligently choose the map instead of just choosing the first map found

				Vector2 stencilPos = world.transform.InverseTransformPoint(modifier.Centre);
				//stencilPos += 0.5f * map.Size;
				if (snapToGrid)
				{
					stencilPos.x = ((int)(stencilPos.x / world.CellSize)) * world.CellSize;
					stencilPos.y = ((int)(stencilPos.y / world.CellSize)) * world.CellSize;
				}
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

	void UpdateVisualisation(bool enabled)
	{

		Transform visualization = stencilVisualisations[(int)modifier.Stencil.Type];
		visualization.position = new Vector3(modifier.Centre.x, modifier.Centre.y, -2);
		visualization.localScale = Vector2.one * modifier.Radius * 2f;
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

	public void OnChangeRadius(InputAction.CallbackContext ctx)
	{
		if (ctx.performed)
		{
			if (ctx.ReadValue<float>() > 0)
			{
				modifier.Radius = modifier.Radius + 1;
			}
			else
			{
				modifier.Radius = modifier.Radius - 1;
			}
			UpdateText();
		}
	}

	public void OnChangeStencil(InputAction.CallbackContext ctx)
	{
		if (ctx.performed)
		{
			//change stencil visualisation
			stencilVisualisations[(int)modifier.Stencil.Type].gameObject.SetActive(false);

			//if is a circle, switch to square
			if (modifier.Stencil.Type == StencilType.Circle)
			{
				modifier.Stencil = new PixelStencilRectangle();
			}
			else
			{
				modifier.Stencil = new PixelStencilCircle();
			}
			UpdateText();
		}

	}

	public void OnChangeModifierType(InputAction.CallbackContext ctx)
	{
		if (ctx.performed)
		{
			if (modifier.Type == ModifierType.AddOvertime)
			{
				modifier.Type = ModifierType.RemoveOvertime;
				modifier.Strength = modifyRemoveStrength;
			}
			else
			{
				modifier.Type = ModifierType.AddOvertime;
				modifier.Strength = modifyStrength;
			}

			//modifier.Type = (ModifierType)((int)(modifier.Type + 1) % (int)ModifierType.Count);
			UpdateText();
		}
	}

	void UpdateText()
	{
		radText.text = modifier.Radius.ToString();
		strText.text = modifier.Strength.ToString();

		switch (modifier.Type)
		{
			case ModifierType.Fill:
				modText.text = "Fill";
				break;
			case ModifierType.Delete:
				modText.text = "Delete";
				break;
			case ModifierType.AddOvertime:
				modText.text = "Add";
				break;
			case ModifierType.RemoveOvertime:
				modText.text = "Remove";
				break;
			default:
				modText.text = "Error";
				break;
		}

		if (modifier.Stencil.Type == StencilType.Circle)
		{
			shaText.text = "Circle";
		}
		else
		{
			shaText.text = "Square";
		}
	}	
	#endregion
}
