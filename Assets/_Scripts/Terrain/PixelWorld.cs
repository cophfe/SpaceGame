using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PixelWorld : MonoBehaviour
{
	//Exposed
	[SerializeField] PixelChunk chunkPrefab = null;
	[SerializeField] PixelGenerators mapGenerator;
	[SerializeField] PhysicsMaterial2D physicsMaterial;

	//Private
	PixelChunk[,] chunks;

	private void Awake()
	{
	}
	private void Start()
	{
		CreateChunks();

		//register self to GameManager
		GameManager.Instance.RegisterMap(this);
	}

	public void ApplyStencil(PixelModifier modifier)
	{
		Vector2 sPos = modifier.Centre;
		Vector2 point = transform.InverseTransformPoint(modifier.Centre);
		point += HalfSize;
		modifier.Centre = point;

		//assuming stencil position is relative to the centre of the map
		//find the chunks affected
		int xStart = (int)((modifier.XStart - CellSize) / ChunkSize);
		if (xStart < 0)
			xStart = 0;
		
		int xEnd = (int)((modifier.XEnd + CellSize) / ChunkSize);
		if (xEnd >= ChunkResolution.x)
			xEnd = ChunkResolution.x - 1;
		
		int yStart = (int)((modifier.YStart - CellSize) / ChunkSize);
		if (yStart < 0)
			yStart = 0;
		
		int yEnd = (int)((modifier.YEnd + CellSize) / ChunkSize);
		if (yEnd >= ChunkResolution.y)
			yEnd = ChunkResolution.y - 1;

		Vector2 startPoint = (Vector2)transform.position - HalfSize;
		Debug.DrawLine(startPoint + ChunkSize * new Vector2(xStart, yStart), startPoint + ChunkSize * new Vector2(xStart, yEnd + 1), Color.blue, 0, false);
		Debug.DrawLine(startPoint + ChunkSize * new Vector2(xStart, yEnd + 1), startPoint + ChunkSize * new Vector2(xEnd + 1, yEnd + 1), Color.blue, 0, false);
		Debug.DrawLine(startPoint + ChunkSize * new Vector2(xEnd + 1, yEnd + 1), startPoint + ChunkSize * new Vector2(xEnd + 1, yStart), Color.blue, 0, false);
		Debug.DrawLine(startPoint + ChunkSize * new Vector2(xEnd + 1, yStart), startPoint + ChunkSize * new Vector2(xStart, yStart), Color.blue, 0, false);
		//Debug.Log($"centre: ({point.x}, {point.y}) chunks X: s: {xStart}, e: {xEnd}. Y: s: {yStart}, e: {yEnd}");

		for (int y = yEnd; y >= yStart; y--)
		{
			for (int x = xEnd; x >= xStart; x--)
			{
				modifier.Centre = new Vector2(point.x - x * ChunkSize, point.y - y * ChunkSize);
				chunks[x, y].ApplyModifier(modifier);
			}
		}

		modifier.Centre = sPos;
	}

	//check if a stencil is colliding with the map
	public bool IsColliding(PixelModifier modifier)
	{
		Vector2 pos = transform.position;
		//just do a 2d AABB check with the stencil
		return (modifier.XStart	< pos.x + HalfSize.x)
			&& (modifier.XEnd	> pos.x - HalfSize.x)
			&& (modifier.YStart	< pos.y + HalfSize.y)
			&& (modifier.YEnd	> pos.y - HalfSize.y);
	}

	private void OnValidate()
	{
		if (chunks != null)
		{
			for (int x = 0; x < ChunkResolution.x; x++)
			{
				for (int y = 0; y < ChunkResolution.y; y++)
				{
					//Pixel[,] v = chunks[x, y].Pixels;
					//GetChunkData(x, y, ref v);
					//chunks[x, y].Pixels = v;

					chunks[x, y].Generate();
				}
			}
		}
		
	}

	public void SetGenerator(PixelGenerators generator) //only has an effect before start is called
	{
		mapGenerator = generator;
	}

	void CreateChunks()
	{
		chunks = new PixelChunk[mapGenerator.ChunkResolution.x, mapGenerator.ChunkResolution.y];
		for (int x = mapGenerator.ChunkResolution.x - 1; x >= 0 ; x--)
		{
			for (int y = mapGenerator.ChunkResolution.x - 1; y >= 0; y--)
			{
				CreateChunk(x, y);

				//if (x > 0)
				//{
				//	chunks[x - 1, y].right = chunks[x, y];
				//	chunks[x, y].left = chunks[x - 1, y];
				//}
				//if (y > 0)
				//{
				//	chunks[x, y - 1].up = chunks[x, y];
				//	chunks[x, y].down = chunks[x, y - 1];
				//}
			}
		}

		
	}

	void CreateChunk(int x, int y)
	{
		PixelChunk chunk = Instantiate(chunkPrefab, transform);
		chunk.gameObject.name = $"Chunk {x}, {y}";
		chunk.transform.localPosition = new Vector2(x * ChunkSize - HalfSize.x, y * ChunkSize - HalfSize.y);
		
		chunk.ChunkCoord = new Vector2Int(x, y);
		//set pixel data
		Pixel[,] pixels = mapGenerator.GenerateChunkData(this, new Vector2Int(x,y));

		chunk.Initialize(this, pixels);

		chunks[x, y] = chunk;
	}

	public void Generate()
	{
		for (int x = 0; x < ChunkResolution.x; x++)
		{
			for (int y = 0; y < ChunkResolution.y; y++)
			{
				chunks[x, y].Generate();
			}
		}
	}

	public Bounds2D GetBounds()
	{
		return new Bounds2D(transform.position, HalfSize.x * 2, HalfSize.y * 2);
	}
	
	public Vector2 HalfSize { get => mapGenerator.HalfSize; }
	public Vector2Int ChunkResolution { get => mapGenerator.ChunkResolution; }
	public int CellResolution { get => mapGenerator.CellResolution; }
	public float CellSize { get => mapGenerator.CellSize; }
	public float ChunkSize { get => mapGenerator.ChunkSize; }
	public float ValueThreshold { get => mapGenerator.ValueThreshold; }

	public PixelChunk[,] Chunks { get => chunks; }
	public PhysicsMaterial2D Material { get => physicsMaterial; }
}

[System.Serializable]
public struct Bounds2D
{
	public Vector2 bottomLeft;
	public Vector2 topRight;

	public Bounds2D(Vector2 bottomLeft, Vector2 topRight)
	{
		this.bottomLeft = bottomLeft;
		this.topRight = topRight;
	}

	public Bounds2D(Vector2 centre, float width, float height)
	{
		this.bottomLeft = centre - new Vector2(width, height) * 0.5f;
		this.topRight = centre + new Vector2(width, height) * 0.5f;
	}

	public bool Contains(Vector2 point)
	{
		return point.x > bottomLeft.x && point.x < topRight.x
			&& point.y > bottomLeft.y && point.y < topRight.y;
	}

	public bool Intersects(Bounds2D other)
	{
		return !(other.bottomLeft.x > topRight.x
		|| other.topRight.x < bottomLeft.x
		|| other.topRight.y < bottomLeft.y
		|| other.bottomLeft.y > topRight.y);
	}

	public static bool Intersect(Bounds2D a, Bounds2D b)
	{
		return !(b.bottomLeft.x > a.topRight.x
		|| b.topRight.x < a.bottomLeft.x
		|| b.topRight.y < a.bottomLeft.y
		|| b.bottomLeft.y > a.topRight.y);
	}
}
