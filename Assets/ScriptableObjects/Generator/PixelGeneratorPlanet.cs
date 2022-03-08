using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PlanetGenerator", menuName = "Pixel Generators/Planet Generator", order = 1)]
public class PixelGeneratorPlanet : PixelGenerators
{
	public float planetRadius = 50; //radius of the actual planet in cells
	public float planetAtmosphereSize = 30; //height of the atmosphere in cells
	public NoiseData[] surfaceNoise = new NoiseData[0];
	public Pixel.MaterialType groundMaterial = Pixel.MaterialType.Stone;
	public int mountainSeed = 1;


	protected override sealed void OnEnable()
	{
		base.OnEnable();

		Random.InitState(mountainSeed);
		for (int i = 0; i < surfaceNoise.Length; i++)
		{
			surfaceNoise[i].seed = Random.value;
		}
		int radius = Mathf.CeilToInt((planetAtmosphereSize + planetRadius) / ChunkSize);
		chunkResolution = new Vector2Int(radius * 2, radius * 2);
		HalfSize = new Vector2(ChunkSize * chunkResolution.x * 0.5f, ChunkSize * chunkResolution.y * 0.5f);
	}

	public override sealed Pixel[,] GenerateChunkData(PixelWorld map, Vector2Int chunkCoordinate)
	{
		Vector2 centre = ChunkSize * (Vector2)chunkResolution / 2.0f;
		Vector2 chunkOffset = new Vector2(chunkCoordinate.x * ChunkSize, chunkCoordinate.y * ChunkSize) - centre;
		
		Pixel[,] pixels = new Pixel[map.CellResolution + 1, map.CellResolution + 1];
		
		for (int x = 0; x <= map.CellResolution; x++)
		{
			for (int y = 0; y <= map.CellResolution; y++)
			{
				Vector2 position = chunkOffset + new Vector2(x, y) * cellSize;

				//using a circular sample of noise guarantees looping noise
				Vector2 noiseSample = position.normalized;

				float radiusVarience = 0;
				for (int i = 0; i < surfaceNoise.Length; i++)
				{
					radiusVarience += surfaceNoise[i].Sample(noiseSample);
				}

				pixels[x, y].value1 = Mathf.Clamp01((planetRadius + radiusVarience) * cellSize + valueThreshold - (position).magnitude);
				pixels[x, y].type1 = pixels[x, y].value1 <= 0 ? Pixel.MaterialType.None : groundMaterial;
				pixels[x, y].type2 = Pixel.MaterialType.None;
			}
		}
		return pixels;
	}

	public override sealed GeneratorType Type { get => GeneratorType.Noise; }

	[System.Serializable]
	public struct NoiseData
	{
		public float frequency;
		public float strength;
		[System.NonSerialized]
		public float seed;

		public float Sample(Vector2 v)
		{
			return Mathf.PerlinNoise(v.x* frequency + seed, v.y * frequency + seed) * strength;
		}
	}
}
