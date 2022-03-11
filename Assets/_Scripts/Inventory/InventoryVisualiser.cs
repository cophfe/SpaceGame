using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventoryVisualiser : MonoBehaviour
{
	[SerializeField] RectTransform slotPrefab;
	[SerializeField] RectTransform slotParent;
	[SerializeField] Vector2 xyOffset;
	[SerializeField] float slotOffset;
	[SerializeField] Color selectedTint = Color.white;
	[SerializeField] Color nonSelectedTint = Color.white;

	int length;

	VisualiserSlot[] slots;

	private void Start()
	{
		PlayerAction action = GameManager.Instance.Player.Action;
		action.SetInventoryVisualiser(this);
		length = action.Inventory.Slots.Length;

		slots = new VisualiserSlot[length];
		Rect slotRect = slotPrefab.rect;

		for (int i = 0; i < length; i++)
		{
			RectTransform rectTransform = Instantiate<RectTransform>(slotPrefab, slotParent);

			rectTransform.localPosition = xyOffset + new Vector2(i * (slotRect.width + slotOffset), 0);
			slots[i] = new VisualiserSlot();
			slots[i].backgroundImage = rectTransform.GetComponent<Image>();
			slots[i].foregroundImage = rectTransform.GetChild(0).GetComponent<Image>();
			slots[i].text = rectTransform.GetComponentInChildren<TextMeshProUGUI>();
		}

		UpdateVisualisation();
	}

	public void UpdateVisualisation()
	{
		PlayerAction action = GameManager.Instance.Player.Action;
		int selected = action.SelectedSlot;

		for (int i = 0; i < length; i++)
		{
			slots[i].backgroundImage.color = nonSelectedTint;
			Inventory.Slot slot = action.Inventory.Slots[i];

			if (slot.item)
			{
				slots[i].foregroundImage.sprite = slot.item.sprite;
				slots[i].foregroundImage.color = slot.item.spriteTint;
				slots[i].text.text = slot.item.FloatingPointAmount ? slot.amount.ToString("0.##") : ((int)slot.amount).ToString();
				slots[i].foregroundImage.enabled = true;
				slots[i].text.enabled = true;
			}
			else
			{
				slots[i].text.enabled = false;
				slots[i].foregroundImage.enabled = false;
			}
		}
		slots[selected].backgroundImage.color = selectedTint;

	}

	struct VisualiserSlot
	{
		public Image backgroundImage;
		public Image foregroundImage;
		public TextMeshProUGUI text;
	}
}
