#define TIMED

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class Chunk : MonoBehaviour
{
	//Private
	Voxel[,] voxels = null;
	Mesh mesh;
	List<Vector3> vertices = new List<Vector3>();
	List<int> triangles = new List<int>();
	TreeNode tree;
	Map map;

	//debug
#if TIMED
	Stopwatch timer;
#endif

	//collision
	//all colliders for this chunk
	List<EdgeCollider2D> colliders = null;
	//holds information about all edge nodes that haven't been used to make a collider yet
	List<EdgeNodeData> edges = null;

	public void Initialize(Map map, Voxel[,] values)
	{
		mesh = new Mesh();
		vertices = new List<Vector3>();
		triangles = new List<int>();
		GetComponent<MeshFilter>().mesh = mesh;
		voxels = values;
		this.map = map;

#if TIMED
		timer = new Stopwatch();
#endif

		Generate();
	}

	//temporarily public set
	public Voxel[,] Voxels { get => voxels; set => voxels = value; }

	public void ApplyStencil(Stencil stencil)
	{
		int xStart = (int)(stencil.XStart / map.CellSize);
		if (xStart < 0)
		{
			xStart = 0;
		}
		int xEnd = (int)(stencil.XEnd / map.CellSize);
		if (xEnd >= map.CellResolution + 1)
		{
			xEnd = map.CellResolution;
		}
		int yStart = (int)(stencil.YStart / map.CellSize);
		if (yStart < 0)
		{
			yStart = 0;
		}
		int yEnd = (int)(stencil.YEnd / map.CellSize);
		if (yEnd >= map.CellResolution + 1)
		{
			yEnd = map.CellResolution;
		}

		for (int y = yStart; y <= yEnd; y++)
		{
			for (int x = xStart; x <= xEnd; x++)
			{
				stencil.Apply(ref voxels[x,y], GetPointFromIndex(x,y));
			}
		}
		Generate();
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

	public void GenerateTree(int dim)
	{
		tree = BuildTree(0, 0, dim);

		//len is divisable by 2 always, except when it is one
		TreeNode BuildTree(int x, int y, int longestDimension)
		{
			if (x >= map.CellResolution || y >= map.CellResolution)
			{
				//if it is out of bounds, return a dummy node with 0 values

				TreeNode node = new TreeNode
				{
					max = 0,
					min = 0
				};

				return node;
			}
			else if (longestDimension == 1)
			{
				//if len == 1 this is the bottom of the tree.
				TreeNode node = new TreeNode();

				//find min and max values
				float min = voxels[x, y].value;
				float max = voxels[x, y].value;
				min = voxels[x + 1, y].value < min ? voxels[x + 1, y].value : min;
				min = voxels[x, y + 1].value < min ? voxels[x, y + 1].value : min;
				node.min = voxels[x + 1, y + 1].value < min ? voxels[x + 1, y + 1].value : min;
				max = voxels[x + 1, y].value > max ? voxels[x + 1, y].value : max;
				max = voxels[x, y + 1].value > max ? voxels[x, y + 1].value : max;
				node.max = voxels[x + 1, y + 1].value > max ? voxels[x + 1, y + 1].value : max;

				return node;
			}
			else
			{
				TreeNode node = new TreeNode
				{
					treeNodes = new TreeNode[4]
				};

				node.treeNodes[0] = BuildTree(x, y, longestDimension / 2);
				node.treeNodes[1] = BuildTree(x + longestDimension / 2, y, longestDimension / 2);
				node.treeNodes[2] = BuildTree(x, y + longestDimension / 2, longestDimension / 2);
				node.treeNodes[3] = BuildTree(x + longestDimension / 2, y + longestDimension / 2, longestDimension / 2);

				//find min and max values
				node.min = node.treeNodes[0].min;
				node.min = node.treeNodes[1].min < node.min ? node.treeNodes[1].min : node.min;
				node.min = node.treeNodes[2].min < node.min ? node.treeNodes[2].min : node.min;
				node.min = node.treeNodes[3].min < node.min ? node.treeNodes[3].min : node.min;

				node.max = node.treeNodes[0].max;
				node.max = node.treeNodes[1].max > node.max ? node.treeNodes[1].max : node.max;
				node.max = node.treeNodes[2].max > node.max ? node.treeNodes[2].max : node.max;
				node.max = node.treeNodes[3].max > node.max ? node.treeNodes[3].max : node.max;

				return node;
			}
		}
	}

	public void GenerateMesh()
	{
		int dim = map.CellResolution > map.CellResolution ? map.CellResolution : map.CellResolution;
		dim = TwoPow(Mathf.CeilToInt(Mathf.Log(dim, 2)));

		GenerateTree(dim);

		mesh.Clear();
		vertices.Clear();
		triangles.Clear();

		//if entire chunk is full just make one big quad
		if (map.ValueThreshold <= tree.min)
		{
			tree.Collapse();
			AddQuad(
				GetPointFromIndex(0, 0),
				GetPointFromIndex(0, map.CellResolution),
				GetPointFromIndex(map.CellResolution, map.CellResolution),
				GetPointFromIndex(map.CellResolution, 0)
				);

		}
		else
			ContourSubTree(tree, 0, 0, dim);

		mesh.indexFormat = vertices.Count >= 65535 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
		mesh.vertices = vertices.ToArray();
		mesh.triangles = triangles.ToArray();

		//x and y are the bottom edges of the node
		//length is the width, in cells, of the node
		void ContourSubTree(TreeNode node, int x, int y, int len)
		{
			if (map.ValueThreshold > node.min)
			{
				if (map.ValueThreshold < node.max)
				{
					if (len == 1)
					{
						//march
						March(x, y);
					}
					else
					{
						ContourSubTree(node.treeNodes[0], x, y, len / 2);
						ContourSubTree(node.treeNodes[1], x + len / 2, y, len / 2);
						ContourSubTree(node.treeNodes[2], x, y + len / 2, len / 2);
						ContourSubTree(node.treeNodes[3], x + len / 2, y + len / 2, len / 2);
					}
				}
				else
				{
					node.Collapse();
				}
			}
			else
			{
				//draw quad
				AddQuad(
				GetPointFromIndex(x, y),
				GetPointFromIndex(x, y + len),
				GetPointFromIndex(x + len, y + len),
				GetPointFromIndex(x + len, y)
				);
				node.Collapse();
			}
		}
	}

	public void March(int x, int y)
	{
		void AddCellEdge()
		{
			edges.Add(new EdgeNodeData(x, y));
		}

		void AddDoubleCellEdge()
		{
			edges.Add(new EdgeNodeData(x, y, Direction.up));
			edges.Add(new EdgeNodeData(x, y, Direction.down));
		}

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

				AddCellEdge();
				break;
			case 2:
				AddTriangle(
					GetPointFromIndex(x + 1, y),
					GetPointBetweenPoints(x, y, x + 1, y),
					GetPointBetweenPoints(x + 1, y, x + 1, y + 1)
					);

				AddCellEdge();
				break;
			case 3:
				AddQuad(
					GetPointFromIndex(x, y),
					GetPointBetweenPoints(x, y, x, y + 1),
					GetPointBetweenPoints(x + 1, y, x + 1, y + 1),
					GetPointFromIndex(x + 1, y)
					);

				AddCellEdge();
				break;
			case 4:
				AddTriangle(
					GetPointFromIndex(x, y + 1),
					GetPointBetweenPoints(x, y + 1, x + 1, y + 1),
					GetPointBetweenPoints(x, y + 1, x, y)
					);

				AddCellEdge();
				break;
			case 5:
				AddQuad(
					GetPointFromIndex(x, y),
					GetPointFromIndex(x, y + 1),
					GetPointBetweenPoints(x, y + 1, x + 1, y + 1),
					GetPointBetweenPoints(x, y, x + 1, y)
					);

				AddCellEdge();
				break;
			case 6:
				AddTriangle(
					GetPointFromIndex(x, y + 1),
					GetPointBetweenPoints(x, y + 1, x + 1, y + 1),
					GetPointBetweenPoints(x, y + 1, x, y)
					);
				AddTriangle(
					GetPointFromIndex(x + 1, y),
					GetPointBetweenPoints(x, y, x + 1, y),
					GetPointBetweenPoints(x + 1, y, x + 1, y + 1)
					);

				AddDoubleCellEdge();
				break;
			case 7:
				AddPentagon(
					GetPointFromIndex(x, y + 1),
					GetPointBetweenPoints(x, y + 1, x + 1, y + 1),
					GetPointBetweenPoints(x + 1, y, x + 1, y + 1),
					GetPointFromIndex(x + 1, y),
					GetPointFromIndex(x, y)
					);

				AddCellEdge();
				break;
			case 8:
				AddTriangle(
					GetPointFromIndex(x + 1, y + 1),
					GetPointBetweenPoints(x + 1, y + 1, x + 1, y),
					GetPointBetweenPoints(x + 1, y + 1, x, y + 1)
					);

				AddCellEdge();
				break;
			case 9:
				AddTriangle(
					GetPointFromIndex(x + 1, y + 1),
					GetPointBetweenPoints(x + 1, y + 1, x + 1, y),
					GetPointBetweenPoints(x + 1, y + 1, x, y + 1)
					);
				AddTriangle(
					GetPointFromIndex(x, y),
					GetPointBetweenPoints(x, y, x, y + 1),
					GetPointBetweenPoints(x, y, x + 1, y)
					);

				AddDoubleCellEdge();
				break;
			case 10:
				AddQuad(
					GetPointFromIndex(x + 1, y + 1),
					GetPointFromIndex(x + 1, y),
					GetPointBetweenPoints(x + 1, y, x, y),
					GetPointBetweenPoints(x + 1, y + 1, x, y + 1)
					);

				AddCellEdge();
				break;
			case 11:
				AddPentagon(
					GetPointFromIndex(x, y),
					GetPointBetweenPoints(x, y, x, y + 1),
					GetPointBetweenPoints(x, y + 1, x + 1, y + 1),
					GetPointFromIndex(x + 1, y + 1),
					GetPointFromIndex(x + 1, y)
					);

				AddCellEdge();
				break;
			case 12:
				AddQuad(
					GetPointFromIndex(x, y + 1),
					GetPointFromIndex(x + 1, y + 1),
					GetPointBetweenPoints(x + 1, y + 1, x + 1, y),
					GetPointBetweenPoints(x, y + 1, x, y)
					);

				AddCellEdge();
				break;
			case 13:
				AddPentagon(
					GetPointFromIndex(x + 1, y + 1),
					GetPointBetweenPoints(x + 1, y + 1, x + 1, y),
					GetPointBetweenPoints(x, y, x + 1, y),
					GetPointFromIndex(x, y),
					GetPointFromIndex(x, y + 1)
					);

				AddCellEdge();
				break;
			case 14:
				AddPentagon(
					GetPointFromIndex(x + 1, y),
					GetPointBetweenPoints(x + 1, y, x, y),
					GetPointBetweenPoints(x, y, x, y + 1),
					GetPointFromIndex(x, y + 1),
					GetPointFromIndex(x + 1, y + 1)
					);

				AddCellEdge();
				break;
			case 15: //full
				AddQuad(
					GetPointFromIndex(x, y),
					GetPointFromIndex(x, y + 1),
					GetPointFromIndex(x + 1, y + 1),
					GetPointFromIndex(x + 1, y)
					);
				break;
		}
	}

	byte GetCellType(int x, int y)
	{
		byte cellType = 0;

		if (voxels[x, y].value > map.ValueThreshold)
			cellType = 0b0001;
		if (voxels[x + 1, y].value > map.ValueThreshold)
			cellType |= 0b0010;
		if (voxels[x, y + 1].value > map.ValueThreshold)
			cellType |= 0b0100;
		if (voxels[x + 1, y + 1].value > map.ValueThreshold)
			cellType |= 0b1000;

		return cellType;
	}

	bool DoesCellHaveEdge(int x, int y)
	{

		return ! //not
			((voxels[x, y].value > map.ValueThreshold && voxels[x + 1, y].value > map.ValueThreshold && voxels[x, y + 1].value > map.ValueThreshold && voxels[x + 1, y + 1].value > map.ValueThreshold) // cell full
			|| (voxels[x, y].value < map.ValueThreshold && voxels[x + 1, y].value < map.ValueThreshold && voxels[x, y + 1].value < map.ValueThreshold && voxels[x + 1, y + 1].value < map.ValueThreshold)); // cell empty
	}

	//used to check if a physics edge has already been found
	private struct EdgeNodeData
	{

		public EdgeNodeData(int x, int y, Direction lastDirection = Direction.unset)
		{
			this.x = x;
			this.y = y;
			this.lastDirection = lastDirection;
		}

		public int x, y;
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
			colliders = new List<EdgeCollider2D>();
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

		EdgeNodeData edgeInfo = new EdgeNodeData(0,0);

		//find a node on an edge that isn't on the edgelist 
		while (FindEdge(ref edgeInfo)) 
		{
			//if this edge can be found there is a collider to be added here.
			currentEdgeList = new List<Vector2>();

			Vector2Int coord = new Vector2Int(edgeInfo.x, edgeInfo.y);
			//the first edge should have itself called twice, once per direction that has the potential to have an edge in it
			switch (GetCellType(coord.x, coord.y))
			{
				case 1:
					CalculateEdge(coord, Direction.left);
					CalculateEdge(coord, Direction.down, true);
					break;
				case 2:
					CalculateEdge(coord, Direction.right);
					CalculateEdge(coord, Direction.down, true);
					break;
				case 3:
					CalculateEdge(coord, Direction.left);
					CalculateEdge(coord, Direction.right, true);
					break;
				case 4:
					CalculateEdge(coord, Direction.up);
					CalculateEdge(coord, Direction.left, true);
					break;
				case 5:
					CalculateEdge(coord, Direction.up);
					CalculateEdge(coord, Direction.down, true);
					break;
				case 6:
					if (edgeInfo.lastDirection == Direction.up || edgeInfo.lastDirection == Direction.left)
					{
						CalculateEdge(coord, Direction.left);
						CalculateEdge(coord, Direction.up, true);
					}
					else
					{
						CalculateEdge(coord, Direction.right);
						CalculateEdge(coord, Direction.down, true);
					}
					
					break;
				case 7:
					CalculateEdge(coord, Direction.up);
					CalculateEdge(coord, Direction.right, true);
					break;
				case 8:
					CalculateEdge(coord, Direction.up);
					CalculateEdge(coord, Direction.right, true);
					break;
				case 9:
					if (edgeInfo.lastDirection == Direction.up || edgeInfo.lastDirection == Direction.right)
					{
						CalculateEdge(coord, Direction.right);
						CalculateEdge(coord, Direction.up, true);
					}
					else
					{
						CalculateEdge(coord, Direction.left);
						CalculateEdge(coord, Direction.down, true);
					}
					break;
				case 10:
					CalculateEdge(coord, Direction.up);
					CalculateEdge(coord, Direction.down, true);
					break;
				case 11:
					CalculateEdge(coord, Direction.left);
					CalculateEdge(coord, Direction.up, true);
					break;
				case 12:
					CalculateEdge(coord, Direction.left);
					CalculateEdge(coord, Direction.right, true);
					break;
				case 13:
					CalculateEdge(coord, Direction.right);
					CalculateEdge(coord, Direction.down, true);
					break;
				case 14:
					CalculateEdge(coord, Direction.left);
					CalculateEdge(coord, Direction.down, true);
					break;
			}

			//now add new collider
			EdgeCollider2D collider = gameObject.AddComponent<EdgeCollider2D>();
			collider.sharedMaterial = map.Material;

			collider.SetPoints(currentEdgeList);
			colliders.Add(collider);
		}

		//recursively calculate edge vertices and add them to array
		void CalculateEdge(Vector2Int coord, Direction previousCoordOffset, bool insertBefore = false)
		{
			//edge vertices are the ones that are interpolated
			//neighbours are based on the type of cell and the previous coordinate
			// some cells can have two edges running through them (cases 9 and 6)

			switch (GetCellType(coord.x, coord.y))
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
				Vector2 point = GetPointBetweenPoints(coord.x, coord.y, coord.x + 1, coord.y);
				if (insertBefore)
					currentEdgeList.Insert(0, point);
				else
					currentEdgeList.Add(point);

				//set coord to be true for the next cell coord (which is one down in this case)
				coord.y--;

				//calculate the next edge, unless the next edge does not exist
				EdgeNodeData data = new EdgeNodeData(coord.x, coord.y, Direction.up);

				if (coord.y >= 0 && CheckEdgeIsNew(ref data)) //if next edge exists and is not in edgeloop already (Should not have to use DoesCellHaveEdge(coord.x, coord.y) because if that false something is not being done properly and this whole thing will break anyway)
				{
					//iterate to next edge
					CalculateEdge(coord, Direction.up, insertBefore);
				}
				//else if (coord == edgeData[edgeDataStartIndex].Coord)
				//{
				//	//if final coord is the same as the first coord, it is a loop.
				//	currentEdgeList.Add(currentEdgeList[0]);
				//}
			}
			void IterateUp()
			{
				Vector2 point = GetPointBetweenPoints(coord.x, coord.y + 1, coord.x + 1, coord.y + 1);
				if (insertBefore)
					currentEdgeList.Insert(0, point);
				else
					currentEdgeList.Add(point);
				coord.y++;
				EdgeNodeData data = new EdgeNodeData(coord.x, coord.y, Direction.up);
				if (coord.y < map.CellResolution && CheckEdgeIsNew(ref data))
					CalculateEdge(coord, Direction.down, insertBefore);
			}
			void IterateRight()
			{
				Vector2 point = GetPointBetweenPoints(coord.x + 1, coord.y, coord.x + 1, coord.y + 1);
				if (insertBefore)
					currentEdgeList.Insert(0, point);
				else
					currentEdgeList.Add(point);

				coord.x++;
				EdgeNodeData data = new EdgeNodeData(coord.x, coord.y, Direction.left);
				if (coord.x < map.CellResolution && CheckEdgeIsNew(ref data))
					CalculateEdge(coord, Direction.left, insertBefore);
			}
			void IterateLeft()
			{
				Vector2 point = GetPointBetweenPoints(coord.x, coord.y, coord.x, coord.y + 1);
				if (insertBefore)
					currentEdgeList.Insert(0, point);
				else
					currentEdgeList.Add(point);

				coord.x--;
				EdgeNodeData data = new EdgeNodeData(coord.x, coord.y, Direction.right);
				if (coord.x >= 0 && CheckEdgeIsNew(ref data))
					CalculateEdge(coord, Direction.right, insertBefore);
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

		//returns true if the edge is new 
		bool CheckEdgeIsNew(ref EdgeNodeData edgeInfo)
		{
			int edgeIndex = 0;
			bool isNewEdge = false;

			int size = edges.Count;
			for (int i = 0; i < size; i++)
			{
				if (edgeInfo.x == edges[i].x && edgeInfo.y == edges[i].y)
				{
					edgeIndex = i;

					switch (GetCellType(edgeInfo.x, edgeInfo.y))
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

	Vector2 GetPointBetweenPoints(int x1, int y1, int x2, int y2)
	{
		float v1 = voxels[x1, y1].value;
		float v2 = voxels[x2, y2].value;
		float t = (map.ValueThreshold - v1) / (v2 - v1);

		return Vector3.Lerp(GetPointFromIndex(x1, y1), GetPointFromIndex(x2, y2), t);
	}

	Vector2 GetPointFromIndex(int x, int y)
	{
		return new Vector2(x * map.CellSize, y * map.CellSize);
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
	//	for (int x = 0; x < voxels.GetLength(0); x++)
	//	{
	//		for (int y = 0; y < voxels.GetLength(1); y++)
	//		{
	//			Gizmos.color = voxels[x, y].value > map.ValueThreshold ? Color.black : Color.white;
	//			Gizmos.DrawCube(GetPointFromIndex(x, y) * map.CellSize, new Vector3(0.15f,0.15f, 0.001f));
	//		}
	//	}
	//}
}


