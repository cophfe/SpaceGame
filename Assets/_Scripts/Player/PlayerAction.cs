using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

//Controls mining, inventory, and other stuff
public class PlayerAction : MonoBehaviour
{
	[SerializeField] Transform miningRaycastOrigin = null;
	[SerializeField] float placingStrength = 1;
	[SerializeField] float miningDistance = 1;
	[SerializeField] float miningRadius = 2;
	[SerializeField] float miningCircleCastRadius = 0.4f;
	[SerializeField] LayerMask itemMask;
	[SerializeField] LayerMask groundMask;
	[SerializeField] LineRenderer miningRenderer;
	[SerializeField] int circleVertices = 16;
	[SerializeField] Transform visualiser;
	[SerializeField] Color miningVisualiserColour = Color.white;
	[SerializeField] Color placingVisualiserColour = Color.white;
	[SerializeField] Color neutralVisualiserColour = Color.white;
	[SerializeField] SpriteRenderer heldItem;

	bool currentItemEnabled = false;
	InventoryVisualiser inventoryVisualiser;
	[field: SerializeField] public Inventory Inventory { get; private set; }
	public int SelectedSlot => selectedHotBarSlot;
	//reference
	PlayerController controller;
	//used for mining and placing
	PixelModifier modifier;
	float totalAmountAdded = 0;
	//Inventory stuff
	int selectedHotBarSlot = 0;
	Inventory.Slot inventorySlot = null;
	
	List<FloorItem> floorItems = new List<FloorItem>();
	//input
	bool actionButtonHeld = false;
	float[] removeDictionary;

	private void Awake()
	{
		miningRenderer.positionCount = circleVertices;
		for (int i = 0; i < circleVertices; i++)
		{
			float x = Mathf.PI * 2 * i / (float)circleVertices;

			Vector2 pos = new Vector2(Mathf.Cos(x), Mathf.Sin(x));
			miningRenderer.SetPosition(i, pos);
		}


		controller = GetComponentInParent<PlayerController>();

		modifier = new PixelModifier(placingStrength, miningCircleCastRadius, ModifierType.AddOvertime, Pixel.MaterialType.Stone, new PixelStencilCircle());
		removeDictionary = new float[(int)Pixel.MaterialType.Count];

		modifier.SetOnAdd(OnPixelAdd);
		modifier.SetOnRemove(OnPixelRemove);
		inventorySlot = Inventory.Slots[selectedHotBarSlot];

		SetVisualiserState(0);
	}

	public void SetInventoryVisualiser(InventoryVisualiser vis)
	{
		inventoryVisualiser = vis;
	}

	private void Update()
	{
		if (inventorySlot.item)
		{
			inventorySlot.item.WhileEnabled(controller);
			UpdateHeldVisual();
		}
		else
		{
			controller.Animator.SetLeftArmHeld(false);
			heldItem.sprite = null;
		}
	}

	private void FixedUpdate()
	{
		EvaluateItem();
		UpdateFloorItems();
	}
	
	public void UpdateVisualisationPosition()
	{
		Vector2 direction = GetMouseVector();
		Vector2 centrePos = (Vector2)miningRaycastOrigin.position + Vector2.ClampMagnitude(direction, miningDistance);
		visualiser.position = centrePos;
	}

	public void EnableVisualisation(bool enable, float scaleMod)
	{
		visualiser.gameObject.SetActive(enable);
		visualiser.localScale = Vector3.one * (scaleMod * miningRadius);
	}

	public void SetVisualiserState(int state)
	{
		//state == 0 is none, state == -1 is mining, state == 1 is placing

		switch (state)
		{
			case -1:
				miningRenderer.startColor = miningVisualiserColour;
				miningRenderer.endColor = miningVisualiserColour;
				break;
			case 0:
				miningRenderer.startColor = neutralVisualiserColour;
				miningRenderer.endColor = neutralVisualiserColour;
				break;
			case 1:
				miningRenderer.startColor = placingVisualiserColour;
				miningRenderer.endColor = placingVisualiserColour;
				break;
		}
	}

	void EvaluateItem()
	{
		if (controller.EvaluateActionPressed())
		{
			actionButtonHeld = true;

			if (inventorySlot.item)
				inventorySlot.item.ActionPressed(controller);
		}
		if (actionButtonHeld && inventorySlot.item)
		{
			inventorySlot.item.ActionHeld(controller);
		}
		if (controller.EvaluateActionReleased())
		{
			actionButtonHeld = false;

			if (inventorySlot.item)
				inventorySlot.item.ActionReleased(controller);
		}
	}

	public void OnSwitchInventorySlot(int offset) //if offset is more than Inventory.Slots.Count this will break, but it should always be 1 or -1 anyway
	{
		if (offset == 0)
			return;

		selectedHotBarSlot = selectedHotBarSlot + offset;
		if (selectedHotBarSlot < 0)
			selectedHotBarSlot += Inventory.Slots.Length;
		else if (selectedHotBarSlot >= Inventory.Slots.Length)
			selectedHotBarSlot -= Inventory.Slots.Length;
		if (currentItemEnabled && inventorySlot.item)
			inventorySlot.item.Disabled(controller);
		inventorySlot = Inventory.Slots[selectedHotBarSlot];

		currentItemEnabled = false;
		OnItemChange();
	}

	public PixelModifier Modifier => modifier;
	public void Place(float strengthModifier, float radiusModifier, bool changeAmount) //change amount is for whether or not should care about the amount of material
	{
		Vector2 direction = GetMouseVector();
		float distance = direction.sqrMagnitude < miningDistance * miningDistance ? direction.magnitude : miningDistance;

		Vector2 centrePos = (Vector2)miningRaycastOrigin.position + Vector2.ClampMagnitude(direction, miningDistance);
		
		foreach (var world in GameManager.Instance.Worlds)
		{
			modifier.World = world;
			modifier.Centre = centrePos;
			if (world && world.IsColliding(modifier))
			{
				Vector2 stencilPos = world.transform.InverseTransformPoint(modifier.Centre);
				//stencilPos += 0.5f * map.Size;

				stencilPos = world.transform.TransformPoint(stencilPos);
				modifier.Centre = stencilPos;

				modifier.Strength = placingStrength * strengthModifier;
				modifier.Type = ModifierType.AddOvertime;
				if (changeAmount)
				{
					totalAmountAdded = 0;
					modifier.SetOnAdd(OnPixelAdd);
					modifier.SetOnRemove(OnPixelRemove);
					//now apply changes to pixel world
					world.ApplyStencil(modifier);
					ApplyRemoveDictionary();

					if (inventorySlot.amount <= 0 && currentItemEnabled && inventorySlot.item)
					{
						inventorySlot.item.Disabled(controller);
						inventorySlot.item = null;
						currentItemEnabled = false;
					}
					OnItemChange();
				}
				else
				{
					modifier.SetOnAdd(modifier.DefaultOnAdd);
					modifier.SetOnRemove(modifier.DefaultOnRemove);
					//now apply changes to pixel world
					world.ApplyStencil(modifier);
				}
			}
		}
	}

	float OnPixelAdd(float amountAdded, Pixel.MaterialType type)
	{
		amountAdded = amountAdded - Inventory.RemoveAmountFromSlot(amountAdded, inventorySlot);
		return amountAdded;
	}

	void OnPixelRemove(float amountRemoved, float materialDistribution, Pixel.MaterialType type1, Pixel.MaterialType type2)
	{
		removeDictionary[(int)type1] += -amountRemoved * materialDistribution;
		removeDictionary[(int)type2] += -amountRemoved * (1 - materialDistribution);
	}

	void ApplyRemoveDictionary()
	{
		for (int i = 0; i < removeDictionary.Length; i++)
		{
			if (removeDictionary[i] > 0)
			{
				GameItem item = GameManager.Instance.PixelMaterialData.Materials[i];
				float rejected = Inventory.AddItem(item, removeDictionary[i]);
				if (rejected > 0)
				{
					GameManager.Instance.CreateDrop(item, rejected, controller.PlayerPosition);
				}
				removeDictionary[i] = 0;
			}
		}
	}

	public void Mine(float strength, float radiusModifier, bool changeAmount) 
	{
		modifier.Radius = miningRadius * radiusModifier;
		modifier.Type = ModifierType.RemoveOvertime;


		Vector2 direction = GetMouseVector();
		float distance = direction.sqrMagnitude < miningDistance * miningDistance ? direction.magnitude : miningDistance;

		Vector2 centrePos = (Vector2)miningRaycastOrigin.position + Vector2.ClampMagnitude(direction, miningDistance);

		foreach (var world in GameManager.Instance.Worlds)
		{
			modifier.World = world;
			modifier.Centre = centrePos;

			if (world && world.IsColliding(modifier))
			{
				Vector2 stencilPos = world.transform.InverseTransformPoint(modifier.Centre);
				//stencilPos += 0.5f * map.Size;

				stencilPos = world.transform.TransformPoint(stencilPos);
				modifier.Centre = stencilPos;

				modifier.Strength = strength;
				modifier.Type = ModifierType.RemoveOvertime;
				if (changeAmount)
				{
					modifier.SetOnRemove(OnPixelRemove);
					//now apply changes to pixel world
					world.ApplyStencil(modifier);
					ApplyRemoveDictionary();
					OnItemChange();
				}
				else
				{
					modifier.SetOnRemove(modifier.DefaultOnRemove);
					//now apply changes to pixel world
					world.ApplyStencil(modifier);
				}
			}
		}
	}

	private void OnApplicationQuit()
	{
		Inventory.ClearInventory();
	}

	private void OnTriggerEnter2D(Collider2D other)
	{
		if (!Inventory.InventoryIsFull() && ((1 << other.gameObject.layer) & itemMask.value) != 0)
		{
			FloorItem floorItem = other.gameObject.GetComponent<FloorItem>();
			if (floorItem)
			{
				floorItems.Add(floorItem);
			}
		}
	}

	private void OnTriggerExit2D(Collider2D other)
	{
		if (((1 << other.gameObject.layer) & itemMask.value) != 0)
		{
			FloorItem floorItem = other.gameObject.GetComponent<FloorItem>();
			if (floorItem)
			{
				floorItems.Remove(floorItem);
			}
		}
	}
	private void UpdateFloorItems()
	{
		if (Inventory.InventoryIsFull())
		{
			floorItems.Clear();
			return;
		}

		for (int i = 0; i < floorItems.Count; i++)
		{
			var floorItem = floorItems[i];

			floorItem.Attract(controller);
			if (floorItem.ShouldPickup(controller))
			{
				floorItem.PickUp(controller);
				floorItems.Remove(floorItem);
				i--;
				OnItemChange();
			}
		}
	}

	void OnItemChange()
	{
		if (inventorySlot.item && !currentItemEnabled)
		{
			inventorySlot.item.Enabled(controller);
			currentItemEnabled = true;
		}

		if (inventoryVisualiser)
		{
			inventoryVisualiser.UpdateVisualisation();
		}	

		if (inventorySlot.item)
		{
			heldItem.color = inventorySlot.item.spriteTint;
			heldItem.transform.localPosition = inventorySlot.item.holdOffset;
			heldItem.transform.localRotation = Quaternion.Euler(0, 0, inventorySlot.item.rotation);
		}
	}

	Vector2 GetMouseVector()
	{
		//GameManager.Instance.MainCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue()
		Vector2 mousePos = GameManager.Instance.MainCamera.ScreenToWorldPoint(controller.LookPosition);
		return mousePos - (Vector2)miningRaycastOrigin.position;
	}

	void UpdateHeldVisual()
	{
		GameItem item = inventorySlot.item;
		ItemActionVisual val = actionButtonHeld ? item.ActionVisual : item.NormalVisual;

		switch (val)
		{
			case ItemActionVisual.Hold:
				{
					heldItem.sprite = inventorySlot.item.sprite;

					controller.Animator.SetLeftArmHeld(false);

					Vector2 pos = heldItem.transform.localPosition;
					if (controller.Animator.PlayerPointingLeft)
					{
						pos.x = -Mathf.Sign(inventorySlot.item.holdOffset.x) * inventorySlot.item.holdOffset.x;
						heldItem.flipY = true;
					}
					else
					{
						pos.x = inventorySlot.item.holdOffset.x;
						heldItem.flipY = false;
					}
					heldItem.transform.localPosition = pos;

					break;
				}
			case ItemActionVisual.HoldPoint:
				{
					heldItem.sprite = inventorySlot.item.sprite;

					//point hand toward direction
					PlayerAnimator.HoldData data = new PlayerAnimator.HoldData();
					data.isHoldingItem = true;
					data.itemSprite = heldItem;

					Vector2 mousePos = GameManager.Instance.MainCamera.ScreenToWorldPoint(controller.LookPosition);
					Vector2 mouseVector2 = mousePos - (Vector2)controller.Animator.LeftArm.armAttachPoint.position;
					data.handDistance = mouseVector2.magnitude;
					data.handAngle = Vector2.SignedAngle(controller.Motor.UpDirection, mouseVector2) + Mathf.PI/4;
					controller.Animator.SetLeftArmHeld(data);

					Vector2 pos = heldItem.transform.localPosition;
					if (data.handAngle > 0)
					{
						pos.x = -Mathf.Sign(inventorySlot.item.holdOffset.x) * inventorySlot.item.holdOffset.x;
						heldItem.flipY = true;
						controller.Animator.OverridePlayerPointing(true);
					}
					else
					{
						pos.x = inventorySlot.item.holdOffset.x;
						controller.Animator.OverridePlayerPointing(false);
						heldItem.flipY = false;
					}
					heldItem.transform.localPosition = pos;
					
					break;
				}
			case ItemActionVisual.Nothing:
				{
					heldItem.sprite = null;
					controller.Animator.SetLeftArmHeld(false);
					break;
				}
			default:
				return;

		}
	}
}
