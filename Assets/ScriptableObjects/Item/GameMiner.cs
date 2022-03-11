using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Miner", menuName = "Items/Miner", order = 1)]
public class GameMiner : GameTool
{
	[SerializeField] float miningStrength = 3;
	[SerializeField] float miningRadiusModifier = 1;

	public override ToolType ToolType => ToolType.Miner;

	public override ItemActionVisual ActionVisual => ItemActionVisual.HoldPoint;

	public override ItemActionVisual NormalVisual => ItemActionVisual.Hold;

	public override void ActionPressed(PlayerController player)
	{
		player.Action.SetVisualiserState(-1);
	}

	public override void ActionHeld(PlayerController player)
	{
		player.Action.Mine(miningStrength, miningRadiusModifier, true);
	}
	public override void ActionReleased(PlayerController player)
	{
		player.Action.SetVisualiserState(0);
	}

	public override void Enabled(PlayerController player)
	{
		player.Action.EnableVisualisation(true, miningRadiusModifier);
	}

	public override void WhileEnabled(PlayerController player)
	{
		player.Action.UpdateVisualisationPosition();
	}

	public override void Disabled(PlayerController player)
	{
		player.Action.EnableVisualisation(false, miningRadiusModifier);
	}

	
}
