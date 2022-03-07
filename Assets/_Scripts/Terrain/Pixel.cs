public struct Pixel
{
	//between 0 and 1
	public float value;

	public float Value { get { return value; } } //total value

	//should be:

	//between 0 and 255
	//public byte value1;
	//public MaterialType type1

	//public byte value2;
	//public MaterialType type2
	//public float MaterialT { get { value1 / (value1 + value2) }

	public enum MaterialType : byte
	{
		Dirt,
		Stone,
		Grass,
		Count
	}
}


