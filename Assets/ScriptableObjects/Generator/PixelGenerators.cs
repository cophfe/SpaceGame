using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum GeneratorType
{
	Planet,
	Noise,
	Count
}

public abstract class PixelGenerators : ScriptableObject
{
	[SerializeField] protected int cellResolution = 32;
	[SerializeField, Min(0.001f)] protected float cellSize = 1.0f;
	[SerializeField, Range(0, 1)] protected float valueThreshold = 0.5f;

	[HideInInspector] protected Vector2Int chunkResolution;
	
	protected virtual void OnEnable()
	{
		//chunk have 1 extra line of cells accross the right and up areas (connecting the chunks)
		ChunkSize = cellResolution * cellSize + cellSize;
		HalfSize = new Vector2(ChunkSize * chunkResolution.x * 0.5f, ChunkSize * chunkResolution.y * 0.5f);
	}

	public abstract Pixel[,] GenerateChunkData(PixelWorld map, Vector2Int chunkCoordinate);
	public abstract GeneratorType Type { get; }


	public Vector2Int ChunkResolution { get => chunkResolution; }
	public int CellResolution { get => cellResolution; }
	public float CellSize { get => cellSize; }
	public float ValueThreshold { get => valueThreshold; }
	public float ChunkSize { get; protected set; }
	public Vector2 HalfSize { get; protected set; }

}