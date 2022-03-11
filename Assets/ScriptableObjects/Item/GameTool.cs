using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class GameTool : GameItem
{
	public override int StackLimit => 1;
	public override bool FloatingPointAmount => false;
	public override ItemType ItemType => ItemType.Tool;
	public abstract ToolType ToolType { get; }
}

public enum ToolType
{
	Miner,
	Placer,
	Weapon
}
