using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GravityWell : MonoBehaviour
{
	[SerializeField] public float gravityMod = 1.0f; //gravity wells currently only modify the default gravity
	[SerializeField] public float radius = 10.0f;

	PlayerController player;

	private void Start()
	{
		player = GameManager.Instance.Player;
		if (player == null)
			enabled = false;
	}

	private void FixedUpdate()
	{
		player.Motor.TrySetWell(this); //attempt to set the player's gravity well to this. this will succeed if it is the closest well (right now the player can not exist without a well
	}

	public virtual float GetGravity(float gravity)
	{
		//consider using linear gravity like outer wilds does (so it is more intuitive than square cube law gravity for smaller planets)

		return gravity * gravityMod;
	}

	public virtual Vector2 GetUpDirection()
	{
		return (player.PlayerPosition - (Vector2)transform.position).normalized;
	}
}

public class SimpleWell : GravityWell
{
	public Vector2 upDirection = Vector2.up;

}
