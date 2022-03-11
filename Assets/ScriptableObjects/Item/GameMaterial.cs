using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Material", menuName = "Items/Material", order = 1)]
public class GameMaterial : GameItem
{
	public float placeSpeedModifier = 1;
	public Pixel.MaterialType materialType;

	public override bool FloatingPointAmount => true;

	public override int StackLimit => 9999;

	public override ItemType ItemType => ItemType.Material;

	public override ItemActionVisual ActionVisual => ItemActionVisual.HoldPoint;

	public override ItemActionVisual NormalVisual => ItemActionVisual.Hold;

	public override void ActionHeld(PlayerController player)
	{
		player.Action.Modifier.PixelMaterial = materialType;
		player.Action.Place(placeSpeedModifier, 1, true);
	}

	public override void ActionPressed(PlayerController player)
	{
		player.Action.SetVisualiserState(1);
	}

	public override void ActionReleased(PlayerController player)
	{
		player.Action.SetVisualiserState(0);
	}

	public override void Enabled(PlayerController player)
	{
		player.Action.EnableVisualisation(true, 1);
	}

	public override void WhileEnabled(PlayerController player)
	{
		player.Action.UpdateVisualisationPosition();
	}

	public override void Disabled(PlayerController player)
	{
		player.Action.EnableVisualisation(false, 1);
	}
}