﻿using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class Chunk2 : MonoBehaviour
{
	//Private
	float[,] voxels = null;
	Mesh mesh;
	List<Vector3> vertices = new List<Vector3>();
	List<int> triangles = new List<int>();
	TreeNode tree;

	Chunk2Map map;

	public void Initialize(Chunk2Map map, float[,] values)
	{
		mesh = new Mesh();
		vertices = new List<Vector3>();
		triangles = new List<int>();
		GetComponent<MeshFilter>().mesh = mesh;
		voxels = values;
		this.map = map;
		GenerateMesh();
	}

	public float[,] Voxels { get => voxels; set => voxels = value; }

	public void GenerateTree(int dim)
	{
		tree = BuildTree(0, 0, dim);

		//len is divisable by 2 always, except when it is one
		TreeNode BuildTree(int x, int y, int longestDimension)
		{
			if (x >= map.CellResolution.x || y >= map.CellResolution.y)
			{
				//if it is out of bounds, return a dummy node with infinite values

				TreeNode node = new TreeNode();
				node.max = 0;
				node.min = 0;

				return node;
			}
			else if (longestDimension == 1)
			{
				//if len == 1 this is the bottom of the tree.
				TreeNode node = new TreeNode();

				//find min and max values
				float min = voxels[x, y];
				float max = voxels[x, y];
				min = voxels[x + 1, y] < min ? voxels[x + 1, y] : min;
				min = voxels[x, y + 1] < min ? voxels[x, y + 1] : min;
				node.min = voxels[x + 1, y + 1] < min ? voxels[x + 1, y + 1] : min;
				max = voxels[x + 1, y] > max ? voxels[x + 1, y] : max;
				max = voxels[x, y + 1] > max ? voxels[x, y + 1] : max;
				node.max = voxels[x + 1, y + 1] > max ? voxels[x + 1, y + 1] : max;

				return node;
			}
			else
			{
				TreeNode node = new TreeNode();
				node.treeNodes = new TreeNode[4];

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
		int dim = map.CellResolution.x > map.CellResolution.y ? map.CellResolution.x : map.CellResolution.y;
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
				GetPointFromIndex(0, map.CellResolution.y),
				GetPointFromIndex(map.CellResolution.x, map.CellResolution.y),
				GetPointFromIndex(map.CellResolution.x, 0)
				);

		}
		else
			ContourSubTree(tree, 0, 0, dim);

		mesh.indexFormat = vertices.Count >= 65535 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
		mesh.vertices = vertices.ToArray();
		mesh.triangles = triangles.ToArray();

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
		byte cellType = 0;

		if (voxels[x, y] > map.ValueThreshold)
			cellType = 0b0001;
		if (voxels[x + 1, y] > map.ValueThreshold)
			cellType |= 0b0010;
		if (voxels[x, y + 1] > map.ValueThreshold)
			cellType |= 0b0100;
		if (voxels[x + 1, y + 1] > map.ValueThreshold)
			cellType |= 0b1000;

		switch (cellType)
		{
			//case 0:
			case 1:
				AddTriangle(
					GetPointFromIndex(x, y),
					GetPointBetweenPoints(x, y, x, y + 1),
					GetPointBetweenPoints(x, y, x + 1, y)
					);
				break;
			case 2:
				AddTriangle(
					GetPointFromIndex(x + 1, y),
					GetPointBetweenPoints(x, y, x + 1, y),
					GetPointBetweenPoints(x + 1, y, x + 1, y + 1)
					);
				break;
			case 3:
				AddQuad(
					GetPointFromIndex(x, y),
					GetPointBetweenPoints(x, y, x, y + 1),
					GetPointBetweenPoints(x + 1, y, x + 1, y + 1),
					GetPointFromIndex(x + 1, y)
					);
				break;
			case 4:
				AddTriangle(
					GetPointFromIndex(x, y + 1),
					GetPointBetweenPoints(x, y + 1, x + 1, y + 1),
					GetPointBetweenPoints(x, y + 1, x, y)
					);
				break;
			case 5:
				AddQuad(
					GetPointFromIndex(x, y),
					GetPointFromIndex(x, y + 1),
					GetPointBetweenPoints(x, y + 1, x + 1, y + 1),
					GetPointBetweenPoints(x, y, x + 1, y)
					);
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
				break;
			case 7:
				AddPentagon(
					GetPointFromIndex(x, y + 1),
					GetPointBetweenPoints(x, y + 1, x + 1, y + 1),
					GetPointBetweenPoints(x + 1, y, x + 1, y + 1),
					GetPointFromIndex(x + 1, y),
					GetPointFromIndex(x, y)
					);
				break;
			case 8:
				AddTriangle(
					GetPointFromIndex(x + 1, y + 1),
					GetPointBetweenPoints(x + 1, y + 1, x + 1, y),
					GetPointBetweenPoints(x + 1, y + 1, x, y + 1)
					);
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
				break;
			case 10:
				AddQuad(
					GetPointFromIndex(x + 1, y + 1),
					GetPointFromIndex(x + 1, y),
					GetPointBetweenPoints(x + 1, y, x, y),
					GetPointBetweenPoints(x + 1, y + 1, x, y + 1)
					);
				break;
			case 11:
				AddPentagon(
					GetPointFromIndex(x, y),
					GetPointBetweenPoints(x, y, x, y + 1),
					GetPointBetweenPoints(x, y + 1, x + 1, y + 1),
					GetPointFromIndex(x + 1, y + 1),
					GetPointFromIndex(x + 1, y)
					);
				break;
			case 12:
				AddQuad(
					GetPointFromIndex(x, y + 1),
					GetPointFromIndex(x + 1, y + 1),
					GetPointBetweenPoints(x + 1, y + 1, x + 1, y),
					GetPointBetweenPoints(x, y + 1, x, y)
					);
				break;
			case 13:
				AddPentagon(
					GetPointFromIndex(x + 1, y + 1),
					GetPointBetweenPoints(x + 1, y + 1, x + 1, y),
					GetPointBetweenPoints(x, y, x + 1, y),
					GetPointFromIndex(x, y),
					GetPointFromIndex(x, y + 1)
					);
				break;
			case 14:
				AddPentagon(
					GetPointFromIndex(x + 1, y),
					GetPointBetweenPoints(x + 1, y, x, y),
					GetPointBetweenPoints(x, y, x, y + 1),
					GetPointFromIndex(x, y + 1),
					GetPointFromIndex(x + 1, y + 1)
					);
				break;
			case 15:
				AddQuad(
					GetPointFromIndex(x, y),
					GetPointFromIndex(x, y + 1),
					GetPointFromIndex(x + 1, y + 1),
					GetPointFromIndex(x + 1, y)
					);
				break;
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
		float v1 = voxels[x1, y1];
		float v2 = voxels[x2, y2];
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
}

//public class Tree
//{
//	public Vector2Int min;
//	public Vector2Int max;
//	public int depth = 0;
//	public Tree[] trees;
//	public NodeType nodeType;

//	public enum NodeType
//	{
//		Root,
//		Leef,
//		Empty,
//		Full
//	}

//	public Tree(Vector2Int min, Vector2Int max, int lastDepth)
//	{
//		this.min = min;
//		this.max = max;
//		this.depth = lastDepth + 1;
//	}

//	public void Split()
//	{
//		Vector2Int mid = (min + max) / 2;
//		trees = new Tree[4];
//		trees[0] = new Tree(min, mid, depth);
//		trees[1] = new Tree(new Vector2Int(mid.x, min.y), new Vector2Int(max.x, mid.y), depth);
//		trees[1] = new Tree(new Vector2Int(min.x, mid.y), new Vector2Int(mid.x, max.y), depth);
//		trees[0] = new Tree(mid, max, depth);
//	}
//}
//class QuadNode
//{
//	//first child of node. If node is a leaf
//	int initialChild;
//	int count;
//}
