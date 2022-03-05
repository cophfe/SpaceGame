using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum GeneratorType
{
	PLANET,
	NOISE
}

public abstract class Generator : ScriptableObject
{
	public int cellResolution = 32;
	[Min(0.001f)] public float cellSize = 1.0f;
	[Range(0, 1)] public float valueThreshold = 0.5f;

	protected Vector2Int chunkResolution;

	public abstract Voxel[,] GenerateChunkData(Map map, Vector2Int chunkCoordinate);
}

[CreateAssetMenu(fileName = "DefaultTerrain", menuName = "ScriptableObjects/DefaultTerrain", order = 1)]
public class DefaultGenerator : Generator
{
	public float perlinNoiseSampleSize = 0.1f;
	public Vector2Int chunkDimensions = new Vector2Int(5,5);

	private void Awake()
	{
		//calculate chunkResolution
		chunkResolution = chunkDimensions;
	}

	public Vector2Int GetChunkResolution()
	{
		return chunkResolution;
	}

	public override Voxel[,] GenerateChunkData(Map map, Vector2Int chunkCoordinate)
	{
		Vector2 offset = new Vector2(chunkCoordinate.x * cellResolution * perlinNoiseSampleSize, chunkCoordinate.y * cellResolution * perlinNoiseSampleSize);
		Voxel[,] voxels = new Voxel[cellResolution + 1, cellResolution + 1];

		for (int x = 0; x < cellResolution + 1; x++)
		{
			for (int y = 0; y < cellResolution + 1; y++)
			{
				voxels[x, y].value = Mathf.Clamp01(Mathf.PerlinNoise(x * perlinNoiseSampleSize + offset.x, y * perlinNoiseSampleSize + offset.y));
			}
		}

		return voxels;
	}
}

[CreateAssetMenu(fileName = "PlanetTerrain", menuName = "ScriptableObjects/PlanetTerrain", order = 1)]
public class PlanetGenerator : Generator
{
	public float planetRadius = 50; //radius of the actual planet in cells
	public float planetAtmosphereSize = 30; //height of the atmosphere in cells

	public override Voxel[,] GenerateChunkData(Map map, Vector2Int chunkCoordinate)
	{
		return null;
	}
}
