//#define TIMED

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class PixelChunk : MonoBehaviour
{
	//Private
	Pixel[,] pixels = null;
	Mesh mesh;

	//definitely shouldn't store these (although it will increase performance slightly)
	List<Vector3> vertices; 
	List<int> triangles;
	List<Color32> vertexColor; 
	
	PixelWorld world;
	public Vector2Int ChunkCoord { get; set; }

	//debug
#if TIMED
	Stopwatch timer;
#endif

	//collision
	//all colliders for this chunk
#if USE_POLYGONS
	List<PolygonCollider2D> colliders = null;
#else
	List<EdgeCollider2D> colliders = null;
#endif
	//holds information about all edge nodes that haven't been used to make a collider yet
	List<EdgeNodeData> edges = null;

	public void Initialize(PixelWorld world, Pixel[,] values)
	{
		mesh = new Mesh();
		vertices = new List<Vector3>();
		triangles = new List<int>();
		GetComponent<MeshFilter>().mesh = mesh;
		pixels = values;
		this.world = world;

#if TIMED
		timer = new Stopwatch();
#endif

		Generate();
	}

	//temporarily public set
	public Pixel[,] Pixels { get => pixels; set => pixels = value; }

	public void ApplyModifier(PixelModifier modifier)
	{
		int xStart = (int)(modifier.XStart / world.CellSize) - 1;
		if (xStart < 0)
		{
			xStart = 0;
		}
		int xEnd = (int)(modifier.XEnd / world.CellSize) + 2;
		if (xEnd >= world.CellResolution + 1)
		{
			xEnd = world.CellResolution;
		}
		int yStart = (int)(modifier.YStart / world.CellSize) - 1;
		if (yStart < 0)
		{
			yStart = 0;
		}
		int yEnd = (int)(modifier.YEnd / world.CellSize) + 2;
		if (yEnd >= world.CellResolution + 1)
		{
			yEnd = world.CellResolution;
		}

		UnityEngine.Debug.DrawLine((Vector2)transform.position + GetPointFromIndex(xStart, yStart), (Vector2)transform.position + GetPointFromIndex(xStart, yEnd), Color.red);
		UnityEngine.Debug.DrawLine((Vector2)transform.position + GetPointFromIndex(xStart, yEnd), (Vector2)transform.position + GetPointFromIndex(xEnd, yEnd), Color.red);
		UnityEngine.Debug.DrawLine((Vector2)transform.position + GetPointFromIndex(xEnd, yEnd), (Vector2)transform.position + GetPointFromIndex(xEnd, yStart), Color.red);
		UnityEngine.Debug.DrawLine((Vector2)transform.position + GetPointFromIndex(xEnd, yStart), (Vector2)transform.position + GetPointFromIndex(xStart, yStart), Color.red);

		if (modifier.IsRemoving())
		{
			List<Vector2Int> applyList = new List<Vector2Int>(); //should reserve some stuff here

			for (int y = yStart; y <= yEnd; y++)
			{
				for (int x = xStart; x <= xEnd; x++)
				{
					if (HasNeighbourLessThanThreshold(x, y))
						applyList.Add(new Vector2Int(x, y));
				}
			}

			for (int i = 0; i < applyList.Count; i++)
			{
				modifier.Apply(ref pixels[applyList[i].x, applyList[i].y], GetPointFromIndex(applyList[i].x, applyList[i].y));
			}
		}
		else
		{
			List<Vector2Int> applyList = new List<Vector2Int>(); //should reserve some stuff here

			for (int y = yStart; y <= yEnd; y++)
			{
				for (int x = xStart; x <= xEnd; x++)
				{
					if (HasNeighbourMoreThanThreshold(x, y))
						applyList.Add(new Vector2Int(x, y));
				}
			}

			for (int i = 0; i < applyList.Count; i++)
			{
				modifier.Apply(ref pixels[applyList[i].x, applyList[i].y], GetPointFromIndex(applyList[i].x, applyList[i].y));
			}
		}

		Generate();
	}

	bool HasNeighbourMoreThanThreshold(int x, int y)
	{
		bool yes = false;

		bool xMinValid = x > 0;
		bool yMinValid = y > 0;
		bool xMaxValid = x < world.CellResolution;
		bool yMaxValid = y < world.CellResolution;

		//this is a lot longer than I feel it needs to be.
		if (xMinValid)
		{
			yes |= pixels[x - 1, y].Value >= world.ValueThreshold;

			if (yMinValid)
			{
				yes |= pixels[x - 1, y - 1].Value >= world.ValueThreshold;

				yes |= pixels[x, y - 1].Value >= world.ValueThreshold;
			}
			else if (LeftValid)
			{
				yes |= Down.Pixels[x - 1, world.CellResolution].Value >= world.ValueThreshold;

				yes |= Down.Pixels[x, world.CellResolution].Value >= world.ValueThreshold;
			}

			if (yMaxValid)
			{
				yes |= pixels[x - 1, y + 1].Value >= world.ValueThreshold;

				yes |= pixels[x, y + 1].Value >= world.ValueThreshold;
			}
			else if (UpValid)
			{
				yes |= Up.Pixels[x - 1, 0].Value >= world.ValueThreshold;

				yes |= Up.Pixels[x, 0].Value >= world.ValueThreshold;
			}
		}
		else if (LeftValid)
		{
			yes |= Left.Pixels[world.CellResolution, y].Value >= world.ValueThreshold;

			if (yMinValid)
			{
				yes |= Left.pixels[world.CellResolution, y - 1].Value >= world.ValueThreshold;

				yes |= pixels[x, y - 1].Value >= world.ValueThreshold;
			}
			else if (DownValid)
			{
				yes |= LeftDown.Pixels[world.CellResolution, world.CellResolution].Value >= world.ValueThreshold;

				yes |= Down.Pixels[x, world.CellResolution].Value >= world.ValueThreshold;
			}

			if (yMaxValid)
			{
				yes |= Left.Pixels[world.CellResolution, y + 1].Value >= world.ValueThreshold;

				yes |= pixels[x, y + 1].Value >= world.ValueThreshold;
			}
			else if (UpValid)
			{
				yes |= LeftUp.Pixels[world.CellResolution, 0].Value >= world.ValueThreshold;

				yes |= Up.Pixels[x, 0].Value >= world.ValueThreshold;
			}
		}

		if (xMaxValid)
		{
			yes |= pixels[x + 1, y].Value >= world.ValueThreshold;

			if (yMinValid)
			{
				yes |= pixels[x + 1, y - 1].Value >= world.ValueThreshold;

				yes |= pixels[x, y - 1].Value >= world.ValueThreshold;
			}
			else if (DownValid)
			{
				yes |= Down.Pixels[x + 1, world.CellResolution].Value >= world.ValueThreshold;

				yes |= Down.Pixels[x, world.CellResolution].Value >= world.ValueThreshold;
			}

			if (yMaxValid)
			{
				yes |= pixels[x + 1, y + 1].Value >= world.ValueThreshold;

				yes |= pixels[x, y + 1].Value >= world.ValueThreshold;
			}
			else if (UpValid)
			{
				yes |= Up.Pixels[x + 1, 0].Value >= world.ValueThreshold;

				yes |= Up.Pixels[x, 0].Value >= world.ValueThreshold;
			}
		}
		else if (RightValid)
		{
			yes |= Right.Pixels[0, y].Value >= world.ValueThreshold;

			if (yMinValid)
			{
				yes |= Right.pixels[0, y - 1].Value >= world.ValueThreshold;

				yes |= pixels[x, y - 1].Value >= world.ValueThreshold;
			}
			else if (DownValid)
			{
				yes |= RightDown.Pixels[0, world.CellResolution].Value >= world.ValueThreshold;

				yes |= Down.Pixels[x, world.CellResolution].Value >= world.ValueThreshold;
			}

			if (yMaxValid)
			{
				yes |= Right.Pixels[0, y + 1].Value >= world.ValueThreshold;

				yes |= pixels[x, y + 1].Value >= world.ValueThreshold;
			}
			else if (UpValid)
			{
				yes |= RightUp.Pixels[0, 0].Value >= world.ValueThreshold;

				yes |= Up.Pixels[x, 0].Value >= world.ValueThreshold;
			}
		}

		return yes;
	}

	bool HasNeighbourLessThanThreshold(int x, int y)
	{
		bool yes = false;

		bool xMinValid = x - 1 > 0;
		bool yMinValid = y - 1 > 0;
		bool xMaxValid = x + 1 < world.CellResolution + 1;
		bool yMaxValid = y + 1 < world.CellResolution + 1;

		//this is a lot longer than I feel it needs to be.
		if (xMinValid)
		{
			yes |= pixels[x - 1, y].Value <= world.ValueThreshold;

			if (yMinValid)
			{
				yes |= pixels[x - 1, y - 1].Value <= world.ValueThreshold;

				yes |= pixels[x, y - 1].Value <= world.ValueThreshold;
			}
			else if (DownValid)
			{
				yes |= Down.Pixels[x - 1, world.CellResolution].Value <= world.ValueThreshold;

				yes |= Down.Pixels[x, world.CellResolution].Value <= world.ValueThreshold;
			}

			if (yMaxValid)
			{
				yes |= pixels[x - 1, y + 1].Value <= world.ValueThreshold;

				yes |= pixels[x, y + 1].Value <= world.ValueThreshold;
			}
			else if (UpValid)
			{
				yes |= Up.Pixels[x - 1, 0].Value <= world.ValueThreshold;

				yes |= Up.Pixels[x, 0].Value <= world.ValueThreshold;
			}
		}
		else if (LeftValid)
		{
			yes |= Left.Pixels[world.CellResolution, y].Value <= world.ValueThreshold;

			if (yMinValid)
			{
				yes |= Left.pixels[world.CellResolution, y - 1].Value <= world.ValueThreshold;

				yes |= pixels[x, y - 1].Value <= world.ValueThreshold;
			}
			else if (DownValid)
			{
				yes |= LeftDown.Pixels[world.CellResolution, world.CellResolution].Value <= world.ValueThreshold;

				yes |= Down.Pixels[x, world.CellResolution].Value <= world.ValueThreshold;
			}

			if (yMaxValid)
			{
				yes |= Left.Pixels[world.CellResolution, y + 1].Value <= world.ValueThreshold;

				yes |= pixels[x, y + 1].Value <= world.ValueThreshold;
			}
			else if (UpValid)
			{
				yes |= LeftUp.Pixels[world.CellResolution, 0].Value <= world.ValueThreshold;

				yes |= Up.Pixels[x, 0].Value <= world.ValueThreshold;
			}
		}

		if (xMaxValid)
		{
			yes |= pixels[x + 1, y].Value <= world.ValueThreshold;

			if (yMinValid)
			{
				yes |= pixels[x + 1, y - 1].Value <= world.ValueThreshold;

				yes |= pixels[x, y - 1].Value <= world.ValueThreshold;
			}
			else if (DownValid)
			{
				yes |= Down.Pixels[x + 1, world.CellResolution].Value <= world.ValueThreshold;

				yes |= Down.Pixels[x, world.CellResolution].Value <= world.ValueThreshold;
			}

			if (yMaxValid)
			{
				yes |= pixels[x + 1, y + 1].Value <= world.ValueThreshold;

				yes |= pixels[x, y + 1].Value <= world.ValueThreshold;
			}
			else if (UpValid)
			{
				yes |= Up.Pixels[x + 1, 0].Value <= world.ValueThreshold;

				yes |= Up.Pixels[x, 0].Value <= world.ValueThreshold;
			}
		}
		else if (RightValid)
		{
			yes |= Right.Pixels[0, y].Value <= world.ValueThreshold;

			if (yMinValid)
			{
				yes |= Right.pixels[0, y - 1].Value <= world.ValueThreshold;

				yes |= pixels[x, y - 1].Value <= world.ValueThreshold;
			}
			else if (DownValid)
			{
				yes |= RightDown.Pixels[0, world.CellResolution].Value <= world.ValueThreshold;

				yes |= Down.Pixels[x, world.CellResolution].Value <= world.ValueThreshold;
			}

			if (yMaxValid)
			{
				yes |= Right.Pixels[0, y + 1].Value <= world.ValueThreshold;

				yes |= pixels[x, y + 1].Value <= world.ValueThreshold;
			}
			else if (UpValid)
			{
				yes |= RightUp.Pixels[0, 0].Value <= world.ValueThreshold;

				yes |= Up.Pixels[x, 0].Value <= world.ValueThreshold;
			}
		}

		return yes;
	}

	public void Generate()
	{
		edges = new List<EdgeNodeData>();

#if TIMED
		timer.Restart();
		string debugStr;
#endif

		GenerateMesh();

#if TIMED
		timer.Stop();
		debugStr = $"{gameObject.name} Time: Mesh: {timer.Elapsed.TotalMilliseconds} ms";
		timer.Restart();
#endif

		GeneratePhysicsEdge();

#if TIMED
		timer.Stop();
		debugStr += $", Collider: {timer.Elapsed.TotalMilliseconds} ms";
		UnityEngine.Debug.Log(debugStr);
#endif
	}

	public void GenerateMesh()
	{
		mesh.Clear();

		if (vertices == null || triangles == null || vertexColor == null)
		{
			vertices = new List<Vector3>();
			triangles = new List<int>();
			vertexColor = new List<Color32>();
		}
		else
		{
			vertices.Clear();
			triangles.Clear();
			vertexColor.Clear();
		}
		
		for (int x = 0; x < world.CellResolution; x++)
		{
			for (int y = 0; y < world.CellResolution; y++)
			{
				March(x, y);
			}
		}
		//generate pixels connecting chunks
		GenerateConnectingPixels();

		mesh.indexFormat = vertices.Count >= 65535 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
		mesh.SetVertices(vertices);
		mesh.SetTriangles(triangles, 0);
		mesh.SetColors(vertexColor);

		//vertices = null;
		//triangles = null;
		//vertexColor = null;
	}

	public void March(int x, int y)
	{
		byte cellType = GetCellType(x, y);
		switch (cellType)
		{
			//case 0:
			case 1:
				AddTriangle(
					GetPointFromIndex(x, y),
					GetPointBetweenPoints(x, y, x, y + 1),
					GetPointBetweenPoints(x, y, x + 1, y)
					);
				AddTriVertexColour(pixels[x, y].GetPixelInfo());
				
				AddCellEdge(x, y, cellType);
				break;
			case 2:
				AddTriangle(
					GetPointFromIndex(x + 1, y),
					GetPointBetweenPoints(x, y, x + 1, y),
					GetPointBetweenPoints(x + 1, y, x + 1, y + 1)
					);
				AddTriVertexColour(pixels[x + 1, y].GetPixelInfo());

				AddCellEdge(x, y, cellType);
				break;
			case 3:
				AddQuad(
					GetPointFromIndex(x, y),
					GetPointBetweenPoints(x, y, x, y + 1),
					GetPointBetweenPoints(x + 1, y, x + 1, y + 1),
					GetPointFromIndex(x + 1, y)
					);
				vertexColor.Add(pixels[x, y].GetPixelInfo());
				vertexColor.Add(pixels[x, y].GetPixelInfo());
				vertexColor.Add(pixels[x + 1, y].GetPixelInfo());
				vertexColor.Add(pixels[x + 1, y].GetPixelInfo());

				AddCellEdge(x, y, cellType);
				break;
			case 4:
				AddTriangle(
					GetPointFromIndex(x, y + 1),
					GetPointBetweenPoints(x, y + 1, x + 1, y + 1),
					GetPointBetweenPoints(x, y + 1, x, y)
					);
				AddTriVertexColour(pixels[x, y + 1].GetPixelInfo());

				AddCellEdge(x, y, cellType);
				break;
			case 5:
				AddQuad(
					GetPointFromIndex(x, y),
					GetPointFromIndex(x, y + 1),
					GetPointBetweenPoints(x, y + 1, x + 1, y + 1),
					GetPointBetweenPoints(x, y, x + 1, y)
					);
				vertexColor.Add(pixels[x, y].GetPixelInfo());
				vertexColor.Add(pixels[x, y + 1].GetPixelInfo());
				vertexColor.Add(pixels[x, y + 1].GetPixelInfo());
				vertexColor.Add(pixels[x, y].GetPixelInfo());

				AddCellEdge(x, y, cellType);
				break;
			case 6:
				AddTriangle(
					GetPointFromIndex(x, y + 1),
					GetPointBetweenPoints(x, y + 1, x + 1, y + 1),
					GetPointBetweenPoints(x, y + 1, x, y)
					);
				AddTriVertexColour(pixels[x, y + 1].GetPixelInfo());

				AddTriangle(
					GetPointFromIndex(x + 1, y),
					GetPointBetweenPoints(x, y, x + 1, y),
					GetPointBetweenPoints(x + 1, y, x + 1, y + 1)
					);
				AddTriVertexColour(pixels[x + 1, y].GetPixelInfo());

				AddDoubleCellEdge(x, y, cellType);
				break;
			case 7:
				AddPentagon(
					GetPointFromIndex(x, y + 1),
					GetPointBetweenPoints(x, y + 1, x + 1, y + 1),
					GetPointBetweenPoints(x + 1, y, x + 1, y + 1),
					GetPointFromIndex(x + 1, y),
					GetPointFromIndex(x, y)
					);
				vertexColor.Add(pixels[x, y + 1].GetPixelInfo());
				vertexColor.Add(pixels[x, y + 1].GetPixelInfo());
				vertexColor.Add(pixels[x + 1, y].GetPixelInfo());
				vertexColor.Add(pixels[x + 1, y].GetPixelInfo());
				vertexColor.Add(pixels[x, y].GetPixelInfo());

				AddCellEdge(x, y, cellType);
				break;
			case 8:
				AddTriangle(
					GetPointFromIndex(x + 1, y + 1),
					GetPointBetweenPoints(x + 1, y + 1, x + 1, y),
					GetPointBetweenPoints(x + 1, y + 1, x, y + 1)
					);
				AddTriVertexColour(pixels[x + 1, y].GetPixelInfo());

				AddCellEdge(x, y, cellType);
				break;
			case 9:
				AddTriangle(
					GetPointFromIndex(x + 1, y + 1),
					GetPointBetweenPoints(x + 1, y + 1, x + 1, y),
					GetPointBetweenPoints(x + 1, y + 1, x, y + 1)
					);
				AddTriVertexColour(pixels[x + 1, y + 1].GetPixelInfo());

				AddTriangle(
					GetPointFromIndex(x, y),
					GetPointBetweenPoints(x, y, x, y + 1),
					GetPointBetweenPoints(x, y, x + 1, y)
					);
				AddTriVertexColour(pixels[x, y].GetPixelInfo());

				AddDoubleCellEdge(x, y, cellType);
				break;
			case 10:
				AddQuad(
					GetPointFromIndex(x + 1, y + 1),
					GetPointFromIndex(x + 1, y),
					GetPointBetweenPoints(x + 1, y, x, y),
					GetPointBetweenPoints(x + 1, y + 1, x, y + 1)
					);
				vertexColor.Add(pixels[x + 1, y + 1].GetPixelInfo());
				vertexColor.Add(pixels[x + 1, y].GetPixelInfo());
				vertexColor.Add(pixels[x + 1, y].GetPixelInfo());
				vertexColor.Add(pixels[x + 1, y + 1].GetPixelInfo());

				AddCellEdge(x, y, cellType);
				break;
			case 11:
				AddPentagon(
					GetPointFromIndex(x, y),
					GetPointBetweenPoints(x, y, x, y + 1),
					GetPointBetweenPoints(x, y + 1, x + 1, y + 1),
					GetPointFromIndex(x + 1, y + 1),
					GetPointFromIndex(x + 1, y)
					);
				vertexColor.Add(pixels[x, y].GetPixelInfo());
				vertexColor.Add(pixels[x, y].GetPixelInfo());
				vertexColor.Add(pixels[x + 1, y + 1].GetPixelInfo());
				vertexColor.Add(pixels[x + 1, y + 1].GetPixelInfo());
				vertexColor.Add(pixels[x + 1, y].GetPixelInfo());

				AddCellEdge(x, y, cellType);
				break;
			case 12:
				AddQuad(
					GetPointFromIndex(x, y + 1),
					GetPointFromIndex(x + 1, y + 1),
					GetPointBetweenPoints(x + 1, y + 1, x + 1, y),
					GetPointBetweenPoints(x, y + 1, x, y)
					);
				vertexColor.Add(pixels[x, y + 1].GetPixelInfo());
				vertexColor.Add(pixels[x + 1, y + 1].GetPixelInfo());
				vertexColor.Add(pixels[x + 1, y + 1].GetPixelInfo());
				vertexColor.Add(pixels[x, y + 1].GetPixelInfo());

				AddCellEdge(x, y, cellType);
				break;
			case 13:
				AddPentagon(
					GetPointFromIndex(x + 1, y + 1),
					GetPointBetweenPoints(x + 1, y + 1, x + 1, y),
					GetPointBetweenPoints(x, y, x + 1, y),
					GetPointFromIndex(x, y),
					GetPointFromIndex(x, y + 1)
					);
				vertexColor.Add(pixels[x + 1, y + 1].GetPixelInfo());
				vertexColor.Add(pixels[x + 1, y + 1].GetPixelInfo());
				vertexColor.Add(pixels[x, y].GetPixelInfo());
				vertexColor.Add(pixels[x, y].GetPixelInfo());
				vertexColor.Add(pixels[x, y + 1].GetPixelInfo());


				AddCellEdge(x, y, cellType);
				break;
			case 14:
				AddPentagon(
					GetPointFromIndex(x + 1, y),
					GetPointBetweenPoints(x + 1, y, x, y),
					GetPointBetweenPoints(x, y, x, y + 1),
					GetPointFromIndex(x, y + 1),
					GetPointFromIndex(x + 1, y + 1)
					);
				vertexColor.Add(pixels[x + 1, y].GetPixelInfo());
				vertexColor.Add(pixels[x + 1, y].GetPixelInfo());
				vertexColor.Add(pixels[x, y + 1].GetPixelInfo());
				vertexColor.Add(pixels[x, y + 1].GetPixelInfo());
				vertexColor.Add(pixels[x + 1, y + 1].GetPixelInfo());

				AddCellEdge(x, y, cellType);
				break;
			case 15: //full
				AddQuad(
					GetPointFromIndex(x, y),
					GetPointFromIndex(x, y + 1),
					GetPointFromIndex(x + 1, y + 1),
					GetPointFromIndex(x + 1, y)
					);
				vertexColor.Add(pixels[x, y].GetPixelInfo());
				vertexColor.Add(pixels[x, y + 1].GetPixelInfo());
				vertexColor.Add(pixels[x + 1, y + 1].GetPixelInfo());
				vertexColor.Add(pixels[x + 1, y].GetPixelInfo());


				break;
		}
	}

	void AddCellEdge(int x, int y, byte cellType)
	{
		edges.Add(new EdgeNodeData(x, y, cellType: cellType));
	}

	void AddDoubleCellEdge(int x, int y, byte cellType)
	{
		edges.Add(new EdgeNodeData(x, y, Direction.up, cellType: cellType));
		edges.Add(new EdgeNodeData(x, y, Direction.down, cellType: cellType));
	}

	byte GetCellType(int x, int y)
	{
		byte cellType = 0;

		if (pixels[x, y].Value > world.ValueThreshold)
			cellType = 0b0001;
		if (pixels[x + 1, y].Value > world.ValueThreshold)
			cellType |= 0b0010;
		if (pixels[x, y + 1].Value > world.ValueThreshold)
			cellType |= 0b0100;
		if (pixels[x + 1, y + 1].Value > world.ValueThreshold)
			cellType |= 0b1000;

		return cellType;
	}

	bool GetIsCellOutOfBounds(int x, int y)
	{
		return x >= world.CellResolution || y >= world.CellResolution || x < 0 || y < 0;
	}

	bool GetIsCellOutOfXBounds(int x)
	{
		return x >= world.CellResolution || x < 0;
	}

	bool GetIsCellOutOfYBounds(int y)
	{
		return y >= world.CellResolution || y < 0;
	}

	bool DoesCellHaveEdge(int x, int y)
	{

		return ! //not
			((pixels[x, y].Value > world.ValueThreshold && pixels[x + 1, y].Value > world.ValueThreshold && pixels[x, y + 1].Value > world.ValueThreshold && pixels[x + 1, y + 1].Value > world.ValueThreshold) // cell full
			|| (pixels[x, y].Value < world.ValueThreshold && pixels[x + 1, y].Value < world.ValueThreshold && pixels[x, y + 1].Value < world.ValueThreshold && pixels[x + 1, y + 1].Value < world.ValueThreshold)); // cell empty
	}

	//used to check if a physics edge has already been found
	private struct EdgeNodeData
	{

		public EdgeNodeData(int x, int y, Direction lastDirection = Direction.unset, byte cellType = 0)
		{
			this.x = x;
			this.y = y;
			this.lastDirection = lastDirection;
			this.cellType = cellType;
		}

		public int x, y;
		public byte cellType;
		//this is only used to identify if an edge has already been added for cells that have more than one edge
		public Direction lastDirection;

		public Vector2 Coord { get { return new Vector2Int(x, y); } }
	}

	enum Direction : byte
	{
		up,
		down,
		left,
		right,
		unset
	}

	public void GeneratePhysicsEdge()
	{
		if (colliders == null)
		{
#if USE_POLYGONS
			colliders = new List<PolygonCollider2D>();
#else
			colliders = new List<EdgeCollider2D>();
#endif
		}
		else
		{
			for (int i = 0; i < colliders.Count; i++)
			{
				Destroy(colliders[i]);
			}
			colliders.Clear();
		}

		//holds all information about edges that 
		List<Vector2> currentEdgeList = null;

		EdgeNodeData edgeInfo = new EdgeNodeData(0, 0);

		//find a node on an edge that isn't on the edgelist 
		while (FindEdge(ref edgeInfo))
		{
			//if this edge can be found there is a collider to be added here.
			currentEdgeList = new List<Vector2>();

			Vector2Int coord = new Vector2Int(edgeInfo.x, edgeInfo.y);
			//the first edge should have itself called twice, once per direction that has the potential to have an edge in it
			switch (edgeInfo.cellType)
			{
				case 1:
					CalculateEdge(coord, edgeInfo.cellType, Direction.left);
					CalculateEdge(coord, edgeInfo.cellType, Direction.down, true);
					break;
				case 2:
					CalculateEdge(coord, edgeInfo.cellType, Direction.right);
					CalculateEdge(coord, edgeInfo.cellType, Direction.down, true);
					break;
				case 3:
					CalculateEdge(coord, edgeInfo.cellType, Direction.left);
					CalculateEdge(coord, edgeInfo.cellType, Direction.right, true);
					break;
				case 4:
					CalculateEdge(coord, edgeInfo.cellType, Direction.up);
					CalculateEdge(coord, edgeInfo.cellType, Direction.left, true);
					break;
				case 5:
					CalculateEdge(coord, edgeInfo.cellType, Direction.up);
					CalculateEdge(coord, edgeInfo.cellType, Direction.down, true);
					break;
				case 6:
					if (edgeInfo.lastDirection == Direction.up || edgeInfo.lastDirection == Direction.left)
					{
						CalculateEdge(coord, edgeInfo.cellType, Direction.left);
						CalculateEdge(coord, edgeInfo.cellType, Direction.up, true);
					}
					else
					{
						CalculateEdge(coord, edgeInfo.cellType, Direction.right);
						CalculateEdge(coord, edgeInfo.cellType, Direction.down, true);
					}

					break;
				case 7:
					CalculateEdge(coord, edgeInfo.cellType, Direction.up);
					CalculateEdge(coord, edgeInfo.cellType, Direction.right, true);
					break;
				case 8:
					CalculateEdge(coord, edgeInfo.cellType, Direction.up);
					CalculateEdge(coord, edgeInfo.cellType, Direction.right, true);
					break;
				case 9:
					if (edgeInfo.lastDirection == Direction.up || edgeInfo.lastDirection == Direction.right)
					{
						CalculateEdge(coord, edgeInfo.cellType, Direction.right);
						CalculateEdge(coord, edgeInfo.cellType, Direction.up, true);
					}
					else
					{
						CalculateEdge(coord, edgeInfo.cellType, Direction.left);
						CalculateEdge(coord, edgeInfo.cellType, Direction.down, true);
					}
					break;
				case 10:
					CalculateEdge(coord, edgeInfo.cellType, Direction.up);
					CalculateEdge(coord, edgeInfo.cellType, Direction.down, true);
					break;
				case 11:
					CalculateEdge(coord, edgeInfo.cellType, Direction.left);
					CalculateEdge(coord, edgeInfo.cellType, Direction.up, true);
					break;
				case 12:
					CalculateEdge(coord, edgeInfo.cellType, Direction.left);
					CalculateEdge(coord, edgeInfo.cellType, Direction.right, true);
					break;
				case 13:
					CalculateEdge(coord, edgeInfo.cellType, Direction.right);
					CalculateEdge(coord, edgeInfo.cellType, Direction.down, true);
					break;
				case 14:
					CalculateEdge(coord, edgeInfo.cellType, Direction.left);
					CalculateEdge(coord, edgeInfo.cellType, Direction.down, true);
					break;
			}

			//now add new collider
#if USE_POLYGONS
			PolygonCollider2D collider = gameObject.AddComponent<PolygonCollider2D>();
			collider.points = currentEdgeList.ToArray();
#else
			EdgeCollider2D collider = gameObject.AddComponent<EdgeCollider2D>();
			collider.SetPoints(currentEdgeList);
#endif
			colliders.Add(collider);
			collider.sharedMaterial = world.Material;
		}

		//recursively calculate edge vertices and add them to array
		void CalculateEdge(Vector2Int coord, byte cellType, Direction previousCoordOffset, bool insertBefore = false)
		{
			//edge vertices are the ones that are interpolated
			//neighbours are based on the type of cell and the previous coordinate
			// some cells can have two edges running through them (cases 9 and 6)

			switch (cellType)
			{
				case 1:
					//we need to add the vertex in the opposite direction of the previous coord
					if (previousCoordOffset == Direction.left)
						//iterate to the cell below this one, if it is valid
						IterateDown();
					else
						IterateLeft();
					break;
				case 2:
					if (previousCoordOffset == Direction.down)
						IterateRight();
					else
						IterateDown();
					break;
				case 3:
					if (previousCoordOffset == Direction.left)
						IterateRight();
					else
						IterateLeft();
					break;
				case 4:
					if (previousCoordOffset == Direction.up)
						IterateLeft();
					else
						IterateUp();
					break;
				case 5:
					if (previousCoordOffset == Direction.up)
						IterateDown();
					else
						IterateUp();
					break;
				case 6:
					//special
					switch (previousCoordOffset)
					{
						case Direction.up:
							IterateLeft();
							break;
						case Direction.down:
							IterateRight();
							break;
						case Direction.left:
							IterateUp();
							break;
						case Direction.right:
							IterateDown();
							break;
					}
					break;
				case 7:
					if (previousCoordOffset == Direction.up)
						IterateRight();
					else
						IterateUp();
					break;
				case 8:
					goto case 7;
				case 9:
					//special
					switch (previousCoordOffset)
					{
						case Direction.up:
							IterateRight();
							break;
						case Direction.down:
							IterateLeft();
							break;
						case Direction.left:
							IterateDown();
							break;
						case Direction.right:
							IterateUp();
							break;
					}
					break;
				case 10:
					goto case 5;
				case 11:
					goto case 4;
				case 12:
					goto case 3;
				case 13:
					goto case 2;
				case 14:
					goto case 1;

				default:
					//no edge
					break;
			}

			void IterateDown()
			{
				//add point
				Vector2 point;

				//if cell is out of non-connector cell bounds:
				if (GetIsCellOutOfYBounds(coord.y))
				{
					if (GetIsCellOutOfXBounds(coord.x))
					{
						// if going down from diagonal cell
						if (coord.y == world.CellResolution && coord.x == world.CellResolution && RightValid)
							point = GetPointBetweenPoints(
								pixels[coord.x, coord.y], GetPointFromIndex(coord.x, coord.y),
								Right.Pixels[0, coord.y], GetPointFromIndex(coord.x + 1, coord.y)
							);
						else
							return; // a different chunk handles this cell or it is out of bounds
					}
					//else if going down from up connector
					else if (coord.y == world.CellResolution)
						point = GetPointBetweenPoints(coord.x, coord.y, coord.x + 1, coord.y);
					else return; // a different chunk handles this cell or it is out of bounds
				}
				else if (GetIsCellOutOfXBounds(coord.x)) 
				{
					if (coord.x == world.CellResolution && ChunkCoord.x < world.ChunkResolution.x)
					{
						point = GetPointBetweenPoints(
							pixels[coord.x, coord.y], GetPointFromIndex(coord.x, coord.y),
							Right.Pixels[0, coord.y], GetPointFromIndex(coord.x + 1, coord.y)
							);
					}
					else 
						return; // a different chunk handles this cell or it is out of bounds
				}
				else
					point = GetPointBetweenPoints(coord.x, coord.y, coord.x + 1, coord.y);

				AddPoint(point);

				//set coord to be true for the next cell coord (which is one down in this case)
				coord.y--;

				//calculate the next edge, unless the next edge does not exist
				EdgeNodeData data = new EdgeNodeData(coord.x, coord.y, Direction.up);

				if (coord.y >= 0 && CalculateNewEdge(ref data)) //if next edge exists and is not in edgeloop already (Should not have to use DoesCellHaveEdge(coord.x, coord.y) because if that false something is not being done properly and this whole thing will break anyway)
				{
					//iterate to next edge
					CalculateEdge(coord, data.cellType, Direction.up, insertBefore);
				}
			}
			void IterateUp()
			{
				Vector2 point;
				if (GetIsCellOutOfYBounds(coord.y))
				{
					//if diagonal going up
					if (GetIsCellOutOfXBounds(coord.x))
					{
						if (coord.y == world.CellResolution && coord.x == world.CellResolution && UpRightValid)
						{
							point = GetPointBetweenPoints(
								Up.Pixels[coord.x, 0], GetPointFromIndex(coord.x, coord.y + 1),
								RightUp.Pixels[0, 0], GetPointFromIndex(coord.x + 1, coord.y + 1)
							);
							//no need to continue iterating, this edge is guaranteed to end here
							AddPoint(point);
							return;
						}
						else
							return; // a different chunk handles this cell or it is out of bounds
					}
					//if up connector cell
					else if (coord.y == world.CellResolution && UpValid)
					{
						point = GetPointBetweenPoints(
							Up.Pixels[coord.x, 0],			GetPointFromIndex(coord.x, coord.y + 1),
							Up.Pixels[coord.x + 1, 0],		GetPointFromIndex(coord.x + 1, coord.y + 1)
							);
						//no need to continue iterating, this edge is guaranteed to end here
						AddPoint(point);
						return;
					}
					else
						return; // a different chunk handles this cell or it is out of bounds
				}
				else if (GetIsCellOutOfXBounds(coord.x)) 
				{
					if (coord.x == -1 && LeftValid)
					{
						point = GetPointBetweenPoints(
							Left.Pixels[world.CellResolution, coord.y + 1], GetPointFromIndex(coord.x, coord.y + 1),
							pixels[coord.x + 1, coord.y + 1], GetPointFromIndex(coord.x + 1, coord.y + 1)
							);
					}
					else if (coord.x == world.CellResolution && RightValid)
					{
						point = GetPointBetweenPoints(
							pixels[coord.x, coord.y + 1], GetPointFromIndex(coord.x, coord.y + 1),
							Right.Pixels[0, coord.y + 1], GetPointFromIndex(coord.x + 1, coord.y + 1)
							);
					}
					else
						return;  // a different chunk handles this cell or it is out of bounds
				}
				else
					point = GetPointBetweenPoints(coord.x, coord.y + 1, coord.x + 1, coord.y + 1);

				AddPoint(point);

				coord.y++;
				EdgeNodeData data = new EdgeNodeData(coord.x, coord.y, Direction.up);
				if (CalculateNewEdge(ref data))
					CalculateEdge(coord, data.cellType, Direction.down, insertBefore);
			}
			void IterateRight()
			{
				Vector2 point;
				if (GetIsCellOutOfXBounds(coord.x))
				{
					if (GetIsCellOutOfYBounds(coord.y))
					{
						//if is iterating to a diagonal cell
						if (coord.y == world.CellResolution
							&& coord.x == world.CellResolution && UpRightValid) //check if out of bounds on x or y
						{
							point = GetPointBetweenPoints(
								Right.Pixels[0, coord.y], GetPointFromIndex(coord.x + 1, coord.y),
								RightUp.Pixels[0, 0], GetPointFromIndex(coord.x + 1, coord.y + 1)
							);

							//no need to continue iterating, this edge is guaranteed to end here
							AddPoint(point);
							return;
						}
						else
							return; // a different chunk handles this cell or it is out of bounds
					}
					else if (coord.x == world.CellResolution && RightValid)
					{
						point = GetPointBetweenPoints(
							Right.Pixels[0, coord.y], GetPointFromIndex(coord.x + 1, coord.y),
							Right.Pixels[0, coord.y + 1], GetPointFromIndex(coord.x + 1, coord.y + 1)
							);

						//no need to continue iterating, this edge is guaranteed to end here
						AddPoint(point);
						return;
					}
					else return; // a different chunk handles this cell or it is out of bounds
				}
				else if (GetIsCellOutOfYBounds(coord.y))
				{
					if (coord.y == -1 && DownValid)
					{
						point = GetPointBetweenPoints(
							Down.Pixels[coord.x + 1, world.CellResolution], GetPointFromIndex(coord.x + 1, coord.y),
							pixels[coord.x + 1, coord.y], GetPointFromIndex(coord.x + 1, coord.y + 1)
							);
					}
					else if (coord.y == world.CellResolution && UpValid)
					{
						point = GetPointBetweenPoints(
							pixels[coord.x + 1, coord.y], GetPointFromIndex(coord.x + 1, coord.y),
							Up.Pixels[coord.x + 1, 0], GetPointFromIndex(coord.x + 1, coord.y + 1)
							);
					}
					else
						return;  // a different chunk handles this cell or it is out of bounds
				}
				else
					point = GetPointBetweenPoints(coord.x + 1, coord.y, coord.x + 1, coord.y + 1);

				AddPoint(point);

				coord.x++;
				EdgeNodeData data = new EdgeNodeData(coord.x, coord.y, Direction.left);
				if (CalculateNewEdge(ref data))
					CalculateEdge(coord, data.cellType, Direction.left, insertBefore);
			}
			void IterateLeft()
			{
				Vector2 point;
				if (GetIsCellOutOfXBounds(coord.x))
				{	
					if (GetIsCellOutOfYBounds(coord.y))
					{
						//if diagonal connector cell going to vertical connector cell
						if (coord.y == world.CellResolution && UpValid)
						{
							point = GetPointBetweenPoints(
								pixels[coord.x, coord.y], GetPointFromIndex(coord.x, coord.y),
								Up.Pixels[coord.x, 0], GetPointFromIndex(coord.x, coord.y + 1)
							);
						}
						else
							return; //other chunk handles this cell or it is out of bounds
					}
					else if (coord.x == world.CellResolution) 
					{
						//is going from connector cell to regular cell
						point = GetPointBetweenPoints(coord.x, coord.y, coord.x, coord.y + 1);
					}
					else
						return; //other chunk handles this cell or it is out of bounds
				}
				else if (GetIsCellOutOfYBounds(coord.y)) //if is a chunk connector cell moving to another chunk connector cell point should still be found
														 //(since it is not going rightward or upward we don't need to worry about diagonal cell)
				{
					if (coord.y == -1 && DownValid) //connector cell to connector cell
					{
						point = GetPointBetweenPoints(
							Down.Pixels[coord.x, world.CellResolution], GetPointFromIndex(coord.x, coord.y),
							pixels[coord.x, coord.y], GetPointFromIndex(coord.x, coord.y + 1)
							);
					}
					else if (coord.y == world.CellResolution && UpValid)
					{
						point = GetPointBetweenPoints(
							pixels[coord.x, coord.y], GetPointFromIndex(coord.x, coord.y),
							Up.Pixels[coord.x, 0], GetPointFromIndex(coord.x, coord.y + 1));
					}
					else
						return; //other chunk handles this cell or it is out of bounds
				}
				else 
					point = GetPointBetweenPoints(coord.x, coord.y, coord.x, coord.y + 1);

				if (insertBefore)
					currentEdgeList.Insert(0, point);
				else
					currentEdgeList.Add(point);

				coord.x--;
				EdgeNodeData data = new EdgeNodeData(coord.x, coord.y, Direction.right);
				if (coord.x >= 0 && CalculateNewEdge(ref data))
					CalculateEdge(coord, data.cellType, Direction.right, insertBefore);
			}
			
			void AddPoint(Vector2 point)
			{
				if (insertBefore)
					currentEdgeList.Insert(0, point);
				else
					currentEdgeList.Add(point);
			}
		}

		bool FindEdge(ref EdgeNodeData edgeInfo)
		{
			if (edges.Count > 0)
			{
				edgeInfo = edges[edges.Count - 1];
				edges.RemoveAt(edges.Count - 1);
				return true;
			}
			return false;
		}

		//returns true if the edge is new, also sets edgeInfo celltype
		bool CalculateNewEdge(ref EdgeNodeData edgeInfo)
		{
			int edgeIndex = 0;
			bool isNewEdge = false;

			int size = edges.Count;
			for (int i = 0; i < size; i++)
			{
				if (edgeInfo.x == edges[i].x && edgeInfo.y == edges[i].y)
				{
					edgeIndex = i;
					switch (edges[i].cellType)
					{
						case 6:
							isNewEdge = (edgeInfo.lastDirection == Direction.left || edgeInfo.lastDirection == Direction.up)
								== (edges[i].lastDirection == Direction.left || edges[i].lastDirection == Direction.up);
							break;

						case 9:
							isNewEdge = (edgeInfo.lastDirection == Direction.left || edgeInfo.lastDirection == Direction.down)
								== (edges[i].lastDirection == Direction.left || edges[i].lastDirection == Direction.down);
							break;

						default:
							//edge is new
							isNewEdge = true;
							break;
					}
				}
			}

			//if new, remove edge from array and return true (aka yes, this is a new edge)
			if (isNewEdge)
			{
				edgeInfo.cellType = edges[edgeIndex].cellType;
				edges.RemoveAt(edgeIndex);
				return true;
			}
			else
				return false;
		}
	}

	void AddTriangle(Vector3 a, Vector3 b, Vector3 c)
	{
		triangles.Add(vertices.Count);
		triangles.Add(vertices.Count + 1);
		triangles.Add(vertices.Count + 2);
		vertices.Add(a);
		vertices.Add(b);
		vertices.Add(c);
	}

	void AddQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
	{
		triangles.Add(vertices.Count);
		triangles.Add(vertices.Count + 1);
		triangles.Add(vertices.Count + 2);
		triangles.Add(vertices.Count);
		triangles.Add(vertices.Count + 2);
		triangles.Add(vertices.Count + 3);
		vertices.Add(a);
		vertices.Add(b);
		vertices.Add(c);
		vertices.Add(d);

	}

	void AddPentagon(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 e)
	{
		triangles.Add(vertices.Count);
		triangles.Add(vertices.Count + 1);
		triangles.Add(vertices.Count + 2);
		triangles.Add(vertices.Count);
		triangles.Add(vertices.Count + 2);
		triangles.Add(vertices.Count + 3);
		triangles.Add(vertices.Count);
		triangles.Add(vertices.Count + 3);
		triangles.Add(vertices.Count + 4);
		vertices.Add(a);
		vertices.Add(b);
		vertices.Add(c);
		vertices.Add(d);
		vertices.Add(e);
	}

	void AddTriVertexColour(Color32 colour)
	{
		vertexColor.Add(colour);
		vertexColor.Add(colour);
		vertexColor.Add(colour);
	}


	Vector2 GetPointBetweenPoints(int x1, int y1, int x2, int y2)
	{
		float v1 = pixels[x1, y1].Value;
		float v2 = pixels[x2, y2].Value;
		float t = (world.ValueThreshold - v1) / (v2 - v1);

		return Vector3.Lerp(GetPointFromIndex(x1, y1), GetPointFromIndex(x2, y2), t);
	}
	Vector2 GetPointBetweenPoints(Pixel aPixel, Vector2 aPos, Pixel bPixel, Vector2 bPos)
	{
		float t = (world.ValueThreshold - aPixel.Value) / (bPixel.Value - aPixel.Value);
		return Vector3.Lerp(aPos, bPos, t);
	}

	Vector2 GetPointFromIndex(int x, int y)
	{
		return new Vector2(x * world.CellSize, y * world.CellSize);
	}

	private void GenerateConnectingPixels()
	{
		//if there is a chunk to the right add a connecting row
		if (ChunkCoord.x + 1 < world.ChunkResolution.x)
		{
			int x1 = world.CellResolution;
			int x2 = 0;

			for (int y = 0; y < world.CellResolution; y++)
			{
				MarchConnectingCell(new Vector2Int(x1, y), GetVertCellType(y), pixels[x1, y], Right.pixels[x2, y], pixels[x1, y + 1], Right.pixels[x2, y + 1]);
			}
		}

		//if there is a chunk above add a connecting row
		if (ChunkCoord.y + 1 < world.ChunkResolution.y)
		{
			int y1 = world.CellResolution;
			int y2 = 0;

			for (int x= 0; x < world.CellResolution; x++)
			{
				MarchConnectingCell(new Vector2Int(x, y1), GetHorCellType(x), pixels[x, y1], pixels[x + 1, y1], Up.pixels[x, y2], Up.pixels[x + 1, y2]);
			}
		}

		//if there is a chunk to the right, the top, and the top-right, add a single connecting diagonal cell
		if (ChunkCoord.x + 1 < world.ChunkResolution.x  && ChunkCoord.y + 1 < world.ChunkResolution.y)
		{
			MarchConnectingCell(new Vector2Int(world.CellResolution, world.CellResolution), 
				GetDiagCellType(), pixels[world.CellResolution, world.CellResolution], Right.pixels[0, world.CellResolution], 
				Up.pixels[world.CellResolution, 0], RightUp.pixels[0, 0]);
		}

		byte GetVertCellType(int y)
		{
			byte cellType = 0;

			if (pixels[world.CellResolution, y].Value > world.ValueThreshold)
				cellType = 0b0001;
			if (Right.pixels[0, y].Value > world.ValueThreshold)
				cellType |= 0b0010;
			if (pixels[world.CellResolution, y + 1].Value > world.ValueThreshold)
				cellType |= 0b0100;
			if (Right.pixels[0, y + 1].Value > world.ValueThreshold)
				cellType |= 0b1000;

			return cellType;
		}

		byte GetHorCellType(int x)
		{
			byte cellType = 0;

			if (pixels[x, world.CellResolution].Value > world.ValueThreshold)
				cellType = 0b0001;
			if (pixels[x + 1, world.CellResolution].Value > world.ValueThreshold)
				cellType |= 0b0010;
			if (Up.pixels[x, 0].Value > world.ValueThreshold)
				cellType |= 0b0100;
			if (Up.pixels[x + 1, 0].Value > world.ValueThreshold)
				cellType |= 0b1000;

			return cellType;
		}

		byte GetDiagCellType()
		{
			byte cellType = 0;

			if (pixels[world.CellResolution, world.CellResolution].Value > world.ValueThreshold)
				cellType = 0b0001;
			if (Right.pixels[0, world.CellResolution].Value > world.ValueThreshold)
				cellType |= 0b0010;
			if (Up.pixels[world.CellResolution, 0].Value > world.ValueThreshold)
				cellType |= 0b0100;
			if (RightUp.pixels[0, 0].Value > world.ValueThreshold)
				cellType |= 0b1000;

			return cellType;
		}
	}

	public void MarchConnectingCell(Vector2Int index, byte cellType, Pixel bL, Pixel bR, Pixel tL, Pixel tR)
	{
		Vector2 bottomLeft = GetPointFromIndex(index.x, index.y);
		Vector2 topRight = new Vector2(bottomLeft.x + world.CellSize, bottomLeft.y + world.CellSize);
		Vector2 bottomRight = new Vector2(topRight.x, bottomLeft.y);
		Vector2 topLeft = new Vector2(bottomLeft.x, topRight.y);

		//march but passing in more info 
		switch (cellType)
		{
			//case 0:
			case 1:
				AddTriangle(
					bottomLeft,
					GetPointBetweenPoints(bL, bottomLeft, tL, topLeft),
					GetPointBetweenPoints(bL, bottomLeft, bR, bottomRight)
					);
				AddTriVertexColour(bL.GetPixelInfo());

				AddCellEdge(index.x, index.y, cellType);
				break;
			case 2:
				AddTriangle(
					bottomRight,
					GetPointBetweenPoints(bL, bottomLeft, bR, bottomRight),
					GetPointBetweenPoints(bR, bottomRight, tR, topRight)
					);
				AddTriVertexColour(bR.GetPixelInfo());

				AddCellEdge(index.x, index.y, cellType);
				break;
			case 3:
				AddQuad(
					bottomLeft,
					GetPointBetweenPoints(bL, bottomLeft, tL, topLeft),
					GetPointBetweenPoints(bR, bottomRight, tR, topRight),
					bottomRight
					);
				vertexColor.Add(bL.GetPixelInfo());
				vertexColor.Add(bL.GetPixelInfo());
				vertexColor.Add(bR.GetPixelInfo());
				vertexColor.Add(bR.GetPixelInfo());

				AddCellEdge(index.x, index.y, cellType);
				break;
			case 4:
				AddTriangle(
					topLeft,
					GetPointBetweenPoints(tL, topLeft, tR, topRight),
					GetPointBetweenPoints(tL, topLeft, bL, bottomLeft)
					);
				AddTriVertexColour(tL.GetPixelInfo());

				AddCellEdge(index.x, index.y, cellType);
				break;
			case 5:
				AddQuad(
					bottomLeft,
					topLeft,
					GetPointBetweenPoints(tL, topLeft, tR, topRight),
					GetPointBetweenPoints(bL, bottomLeft, bR, bottomRight)
					);
				vertexColor.Add(bL.GetPixelInfo());
				vertexColor.Add(tL.GetPixelInfo());
				vertexColor.Add(tL.GetPixelInfo());
				vertexColor.Add(bL.GetPixelInfo());

				AddCellEdge(index.x, index.y, cellType);
				break;
			case 6:
				AddTriangle(
					topLeft,
					GetPointBetweenPoints(tL, topLeft, tR, topRight),
					GetPointBetweenPoints(tL, topLeft, bL, bottomLeft)
					);
				AddTriVertexColour(tL.GetPixelInfo());

				AddTriangle(
					bottomRight,
					GetPointBetweenPoints(bL, bottomLeft, bR, bottomRight),
					GetPointBetweenPoints(bR, bottomRight, tR, topRight)
					);
				AddTriVertexColour(bR.GetPixelInfo());

				AddDoubleCellEdge(index.x, index.y, cellType);
				break;
			case 7:
				AddPentagon(
					topLeft,
					GetPointBetweenPoints(tL, topLeft, tR, topRight),
					GetPointBetweenPoints(bR, bottomRight, tR, topRight),
					bottomRight,
					bottomLeft
					);
				vertexColor.Add(tL.GetPixelInfo());
				vertexColor.Add(tL.GetPixelInfo());
				vertexColor.Add(bR.GetPixelInfo());
				vertexColor.Add(bR.GetPixelInfo());
				vertexColor.Add(bL.GetPixelInfo());

				AddCellEdge(index.x, index.y, cellType);
				break;
			case 8:
				AddTriangle(
					topRight,
					GetPointBetweenPoints(tR, topRight, bR, bottomRight),
					GetPointBetweenPoints(tR, topRight, tL, topLeft)
					);
				AddTriVertexColour(bR.GetPixelInfo());


				AddCellEdge(index.x, index.y, cellType);
				break;
			case 9:
				AddTriangle(
					topRight,
					GetPointBetweenPoints(tR, topRight, bR, bottomRight),
					GetPointBetweenPoints(tR, topRight, tL, topLeft)
					);
				AddTriVertexColour(tR.GetPixelInfo());

				AddTriangle(
					bottomLeft,
					GetPointBetweenPoints(bL, bottomLeft, tL, topLeft),
					GetPointBetweenPoints(bL, bottomLeft, bR, bottomRight)
					);
				AddTriVertexColour(bL.GetPixelInfo());

				AddDoubleCellEdge(index.x, index.y, cellType);
				break;
			case 10:
				AddQuad(
					topRight,
					bottomRight,
					GetPointBetweenPoints(bR, bottomRight, bL, bottomLeft),
					GetPointBetweenPoints(tR, topRight, tL, topLeft)
					);
				vertexColor.Add(tR.GetPixelInfo());
				vertexColor.Add(bR.GetPixelInfo());
				vertexColor.Add(bR.GetPixelInfo());
				vertexColor.Add(tR.GetPixelInfo());

				AddCellEdge(index.x, index.y, cellType);
				break;
			case 11:
				AddPentagon(
					bottomLeft,
					GetPointBetweenPoints(bL, bottomLeft, tL, topLeft),
					GetPointBetweenPoints(tL, topLeft, tR, topRight),
					topRight,
					bottomRight
					);
				vertexColor.Add(bL.GetPixelInfo());
				vertexColor.Add(bL.GetPixelInfo());
				vertexColor.Add(tR.GetPixelInfo());
				vertexColor.Add(tR.GetPixelInfo());
				vertexColor.Add(bR.GetPixelInfo());

				AddCellEdge(index.x, index.y, cellType);
				break;
			case 12:
				AddQuad(
					topLeft,
					topRight,
					GetPointBetweenPoints(tR, topRight, bR, bottomRight),
					GetPointBetweenPoints(tL, topLeft, bL, bottomLeft)
					);
				vertexColor.Add(tL.GetPixelInfo());
				vertexColor.Add(tR.GetPixelInfo());
				vertexColor.Add(tR.GetPixelInfo());
				vertexColor.Add(tL.GetPixelInfo());

				AddCellEdge(index.x, index.y, cellType);
				break;
			case 13:
				AddPentagon(
					topRight,
					GetPointBetweenPoints(tR, topRight, bR, bottomRight),
					GetPointBetweenPoints(bL, bottomLeft, bR, bottomRight),
					bottomLeft,
					topLeft
					);
				vertexColor.Add(tR.GetPixelInfo());
				vertexColor.Add(tR.GetPixelInfo());
				vertexColor.Add(bL.GetPixelInfo());
				vertexColor.Add(bL.GetPixelInfo());
				vertexColor.Add(tL.GetPixelInfo());

				AddCellEdge(index.x, index.y, cellType);
				break;
			case 14:
				AddPentagon(
					bottomRight,
					GetPointBetweenPoints(bR, bottomRight, bL, bottomLeft),
					GetPointBetweenPoints(bL, bottomLeft, tL, topLeft),
					topLeft,
					topRight
					);
				vertexColor.Add(bR.GetPixelInfo());
				vertexColor.Add(bR.GetPixelInfo());
				vertexColor.Add(tL.GetPixelInfo());
				vertexColor.Add(tL.GetPixelInfo());
				vertexColor.Add(tR.GetPixelInfo());

				AddCellEdge(index.x, index.y, cellType);
				break;
			case 15: //full
				AddQuad(
					bottomLeft,
					topLeft,
					topRight,
					bottomRight
					);
				vertexColor.Add(bL.GetPixelInfo());
				vertexColor.Add(tL.GetPixelInfo());
				vertexColor.Add(tR.GetPixelInfo());
				vertexColor.Add(bR.GetPixelInfo());

				break;
		}
	}

	public class TreeNode
	{
		public float min;
		public float max;
		public TreeNode[] treeNodes = null;

		public enum NodeType
		{
			Root,
			Leef,
			Empty,
			Full
		}

		public NodeType GetNodeType(float threshold)
		{
			if (treeNodes != null) return NodeType.Root;
			else if (min > threshold) return NodeType.Full;
			else if (max < threshold) return NodeType.Empty;
			else return NodeType.Leef;
		}

		public void Collapse()
		{
			treeNodes = null;
		}
	}

	int TwoPow(int power)
	{
		if (power == 0) return 1;

		int val = 2;
		for (int i = 1; i < power; i++)
		{
			val *= 2;
		}
		return val;
	}

	//private void OnDrawGizmos()
	//{
	//	Gizmos.matrix = Matrix4x4.Translate(transform.position);
	//	for (int x = 0; x < pixels.GetLength(0); x++)
	//	{
	//		for (int y = 0; y < pixels.GetLength(1); y++)
	//		{
	//			Gizmos.color = pixels[x, y].Value > map.ValueThreshold ? Color.black : Color.white;
	//			Gizmos.DrawCube(GetPointFromIndex(x, y) * map.CellSize, new Vector3(0.15f,0.15f, 0.001f));
	//		}
	//	}
	//}

	bool LeftValid => ChunkCoord.x > 0;
	bool RightValid => ChunkCoord.x + 1 < world.ChunkResolution.x;
	bool DownValid => ChunkCoord.y > 0;
	bool UpValid => ChunkCoord.y + 1 < world.ChunkResolution.y;

	bool UpLeftValid => ChunkCoord.x > 0 && ChunkCoord.y + 1 < world.ChunkResolution.y;
	bool UpRightValid => ChunkCoord.x + 1 < world.ChunkResolution.x && ChunkCoord.y + 1 < world.ChunkResolution.y;
	bool DownLeftValid => ChunkCoord.x > 0 &&  ChunkCoord.y > 0;
	bool DownRightValid => ChunkCoord.x + 1 < world.ChunkResolution.x && ChunkCoord.y > 0;

	PixelChunk Left => world.Chunks[ChunkCoord.x - 1, ChunkCoord.y];
	PixelChunk Right => world.Chunks[ChunkCoord.x + 1, ChunkCoord.y];
	PixelChunk Up => world.Chunks[ChunkCoord.x, ChunkCoord.y + 1];
	PixelChunk Down => world.Chunks[ChunkCoord.x, ChunkCoord.y - 1];

	PixelChunk LeftDown => world.Chunks[ChunkCoord.x - 1, ChunkCoord.y - 1];
	PixelChunk LeftUp => world.Chunks[ChunkCoord.x - 1, ChunkCoord.y + 1];
	PixelChunk RightDown => world.Chunks[ChunkCoord.x + 1, ChunkCoord.y - 1];
	PixelChunk RightUp => world.Chunks[ChunkCoord.x + 1, ChunkCoord.y + 1];
}


