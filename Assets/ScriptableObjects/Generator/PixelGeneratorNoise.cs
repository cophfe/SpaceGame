using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PixelGeneratorNoise", menuName = "Pixel Generators/Noise Generator", order = 1)]
public class PixelGeneratorNoise : PixelGenerators
{
	public float perlinNoiseSampleSize = 0.1f;
	public Vector2Int chunkDimensions = new Vector2Int(5, 5);

	protected override sealed void OnEnable()
	{
		//calculate chunkResolution
		chunkResolution = chunkDimensions;

		base.OnEnable();
	}

	public Vector2Int GetChunkResolution()
	{
		return chunkResolution;
	}

	public override Pixel[,] GenerateChunkData(PixelWorld map, Vector2Int chunkCoordinate)
	{
		//cell resolution is + 1 for the connecting rows
		Vector2 offset = new Vector2(chunkCoordinate.x * (cellResolution + 1)* perlinNoiseSampleSize, chunkCoordinate.y * (cellResolution + 1) * perlinNoiseSampleSize);
		Pixel[,] pixels = new Pixel[cellResolution + 1, cellResolution + 1];

		for (int x = 0; x < cellResolution + 1; x++)
		{
			for (int y = 0; y < cellResolution + 1; y++)
			{
				float perlinValue = Mathf.PerlinNoise(x * perlinNoiseSampleSize + offset.x, y * perlinNoiseSampleSize + offset.y);
				pixels[x, y].value = Mathf.Clamp01(perlinValue);
			}
		}

		return pixels;
	}

	public override sealed GeneratorType Type { get => GeneratorType.Noise; }

}