using UnityEngine;

public struct Pixel
{
	//between 0 and 1
	public float value1;
	public MaterialType type1;

	public float value2;
	public MaterialType type2;


	public float Value { get { return value1 + value2; } } //total value
	public float Material1Percentage{ get { return value1 / (value1 + value2); } } //has nan potential

	public static float MaxValue { get { return 1; } }

	public bool HasMaterial(MaterialType type)
	{
		return type1 == type || value1 == 0 || type2 == type || value2 == 0;
	}
	
	public Color32 GetPixelInfo()
	{
		return new Color32((byte)type1, (byte)type2, (byte)((1-Material1Percentage) * byte.MaxValue), 0);
	}

	public enum MaterialType : byte
	{
		Dirt,
		Stone,
		Grass,
		Mud,
		Leaves,
		Metal,
		Count,
		None = 0
	}
}


