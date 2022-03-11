using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Inventory", menuName = "Items/Inventory", order = 0)]

public class Inventory : ScriptableObject
{
	[SerializeField] int maxSlots;
	[field: SerializeField]
	public Slot[] Slots { get; private set; }

	private void Awake()
	{
		Slots = new Slot[maxSlots];
	}

	private void OnValidate()
	{
		if (Slots != null)
		{
			if (Slots.Length > maxSlots)
			{
				Slot[] newSlots = new Slot[maxSlots];
				for (int i = 0; i < maxSlots; i++)
				{
					newSlots[i] = Slots[i];
				}
				Slots = newSlots;
			}
			else if (Slots.Length < maxSlots)
			{
				Slot[] newSlots = new Slot[maxSlots];
				for (int i = 0; i < Slots.Length; i++)
				{
					newSlots[i] = Slots[i];
				}
				Slots = newSlots;

			}
		}
	}

	public void ClearInventory()
	{
		for (int i = 0; i < Slots.Length; i++)
		{
			Slots[i] = new Slot();
		}
	}

	//return amount not added
	public float AddItem (GameItem item, float amount)
	{
		if (amount == 0) 
			return 0;

		foreach (Slot slot in Slots)
		{
			if (slot.item == null)
			{
				slot.item = item;
				amount = slot.AddAmount(amount);
			}
			else if (slot.item == item)
			{
				amount = slot.AddAmount(amount);
			}

			if (amount <= 0)
				return 0;
		}
		
		return amount; //the rest of amount was rejected
	}

	public Slot FindItemSlot(GameItem item)
	{
		foreach (Slot slot in Slots)
		{
			if (slot.item == item)
			{
				return slot;
			}
		}

		return null; //could not find item
	}

	public float RemoveAmountFromSlot(float amount, Slot slot)
	{
		float amountNotRemoved = slot.RemoveAmount(amount);
		return amountNotRemoved;
	}

	public bool InventoryIsFull()
	{
		return false;
	}

	[System.Serializable]
	public class Slot
	{
		public GameItem item;
		public float amount; //amount is a float because some items (namely, placement materials) can have floating point amounts.

		//returns the amount not added
		public virtual float AddAmount(float amount)
		{
			float oldAmount = this.amount;
			float newAmount = Mathf.Min(this.amount + amount, item.StackLimit);
			this.amount = newAmount;

			return oldAmount + amount - item.StackLimit;
		}

		//return amount not removed
		public float RemoveAmount(float amount)
		{
			float oldAmount = this.amount;
			float newAmount = this.amount - amount;
			if (newAmount <= 0)
			{
				this.amount = 0;
				return amount - oldAmount;
			}
			else
			{
				this.amount = newAmount;
				return 0;
			}

		}
	}
}

