using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PixelMaterialData", menuName = "Other/PixelMaterialData", order = 0)]
public class PixelMaterialObject : ScriptableObject
{
	//each pixelMaterial has a pixelData associated with it
	[SerializeField] public PixelMaterialData[] materialData = new PixelMaterialData[(int)Pixel.MaterialType.Count];

	public PixelMaterialData[] MaterialData => materialData;

	public GameMaterial[] Materials { get; private set; }

	public Sprite sprite;
	public Vector2 offset;

	private void Awake()
	{
		
	}

	private void OnEnable()
	{
		Materials = new GameMaterial[(int)Pixel.MaterialType.Count];
		for (int i = 0; i < Materials.Length; i++)
		{
			Materials[i] = CreateInstance<GameMaterial>();
		}
		SetGameMaterials();
	}

	private void OnValidate()
	{
		SetArrayLengths();
	}

	void SetGameMaterials()
	{
		for (int i = 0; i < Materials.Length; i++)
		{
			if (Materials[i] == null)
			{
				Debug.Log($"index {i} is null!");
				continue;
			}
			Materials[i].holdOffset = offset;
			Materials[i].sprite = sprite;
			Materials[i].name = materialData[i].name;
			Materials[i].description = materialData[i].description;
			Materials[i].spriteTint = materialData[i].spriteColour;
			Materials[i].placeSpeedModifier = materialData[i].placeSpeedModifier;
			Materials[i].materialType = (Pixel.MaterialType)i;
		}
	}

	void SetArrayLengths()
	{
		int size = (int)Pixel.MaterialType.Count;
		if (materialData == null)
			materialData = new PixelMaterialData[size];

		if (materialData.Length > size)
		{
			PixelMaterialData[] newMaterialData = new PixelMaterialData[size];
			for (int i = 0; i < size; i++)
			{
				newMaterialData[i] = materialData[i];
			}
			materialData = newMaterialData;
		}
		else if (materialData.Length < size)
		{
			PixelMaterialData[] newMaterialData = new PixelMaterialData[size];

			for (int i = 0; i < materialData.Length; i++)
			{
				newMaterialData[i] = materialData[i];
			}
			materialData = newMaterialData;
		}
	}
	
}

[System.Serializable]
public class PixelMaterialData
{
	public string name;
	public string description;
	public Color spriteColour = Color.white;
	public float placeSpeedModifier = 1;
}