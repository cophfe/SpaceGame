using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class GameItem : ScriptableObject
{
	new public string name;
	public string description;
	public Sprite sprite;
	public Color spriteTint = Color.white;
	public Vector2 holdOffset;
	public float rotation;

	public abstract bool FloatingPointAmount { get; }
	public abstract int StackLimit { get; }
    public abstract ItemType ItemType { get; }

	//How the item is held (depending on action being pressed or not)
	public abstract ItemActionVisual ActionVisual { get; } 
	public abstract ItemActionVisual NormalVisual { get; }


	//called based on player holding, on update (usually visual)
	public abstract void Enabled(PlayerController player);
	public abstract void WhileEnabled(PlayerController player);
	public abstract void Disabled(PlayerController player);

	//called based on player input, on fixed update (usually more than visual)
	public abstract void ActionPressed(PlayerController player);
	public abstract void ActionHeld(PlayerController player);
	public abstract void ActionReleased(PlayerController player);

}

public enum ItemActionVisual
{
	Hold,
	HoldPoint,
	Nothing
}
public enum ItemType
{
    Tool,
    Material,
    Default
}