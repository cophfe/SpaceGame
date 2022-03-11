using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class FloorItem : MonoBehaviour
{
	[SerializeField] GameItem item;
	[SerializeField] float amount;
	[SerializeField] float attractStrength = 10;
	[SerializeField] float pickupRadius = 1;
	[SerializeField] new CircleCollider2D collider;

	Rigidbody2D rb;
	SpriteRenderer sR;
	private void Awake()
	{
		rb = GetComponent<Rigidbody2D>();
		sR = GetComponentInChildren<SpriteRenderer>();
		UpdateVisual();
	}

	public bool ShouldPickup (PlayerController player)
	{
		return (player.PlayerPosition - (Vector2)transform.position).sqrMagnitude < pickupRadius * pickupRadius;
	}

	public void PickUp(PlayerController player)
	{
		float rejectedAmount = player.Action.Inventory.AddItem(item, amount);
		if (rejectedAmount > 0)
		{
			amount = rejectedAmount;
		}
		else
		{
			Destroy(gameObject); //should use a object pool
		}
	}

	public void Attract(PlayerController player)
	{
		rb.AddForce((player.PlayerPosition - (Vector2)transform.position) * attractStrength);
	}

	public static FloorItem CreateFloorItem(GameItem item, float amount)
	{
		if (GameManager.Instance && GameManager.Instance.FloorItemPrefab)
		{
			FloorItem fItem = Instantiate(GameManager.Instance.FloorItemPrefab, null);
			fItem.item = item;
			fItem.amount = amount;
			fItem.UpdateVisual();
			return fItem;
		}
		else
			return null;
	}
	
	void UpdateVisual()
	{
		if (item)
		{
			sR.sprite = item.sprite;
			sR.color = item.spriteTint;

			Vector2 extents = sR.bounds.extents;
			collider.radius = Mathf.Min(extents.x, extents.y);
		}
	}
}