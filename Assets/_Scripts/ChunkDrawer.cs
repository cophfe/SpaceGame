using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkDrawer : MonoBehaviour
{
	public float startRadius = 5;
	public float radiusChangeSpeed = 0.1f;
	public float rotateSpeed = 0.1f;
	public Map map;
	float radius;
	bool negative = false;
	DrawType type = DrawType.Circle;

	enum DrawType
	{
		Circle,
		Square,
		Count
	}

	private void Start()
	{
		radius = startRadius;
	}

	private void Update()
	{
		if (Input.GetMouseButton(0))
		{
			//RaycastHit hitInfo;
			if (map && GetBounds().Intersects(map.GetBounds()))
			{
				
			}
		}

		if (Input.GetKeyDown(KeyCode.Tab))
		{
			type = (DrawType)(((int)type + 1) % (int)DrawType.Count);
		}
		if (Input.GetKey(KeyCode.LeftControl))
		{
			if (Input.mouseScrollDelta.y != 0)
			{
				transform.Rotate(new Vector3(0,0, rotateSpeed * Input.mouseScrollDelta.y));
			}
		}
		else if (Input.mouseScrollDelta.y != 0)
		{
			radius = Mathf.Max(radius + radiusChangeSpeed * Input.mouseScrollDelta.y, 0);
		}
		negative = Input.GetKey(KeyCode.LeftShift);
		transform.position = Camera.main.ScreenToWorldPoint(Input.mousePosition);
	}

	void DrawCircle(Map map)
	{

	}

	Bounds2D GetBounds()
	{
		return new Bounds2D(transform.position, radius * 2, radius * 2);
	}
	private void OnDrawGizmos()
	{
		Gizmos.color = negative ? Color.red : Color.green;
		Gizmos.matrix = Matrix4x4.TRS(transform.position + Vector3.forward * 15, transform.rotation, transform.lossyScale * Vector2.one);
		if (type == DrawType.Circle)
			Gizmos.DrawWireSphere(Vector3.zero, radius);
		else
			Gizmos.DrawWireCube(Vector3.zero, Vector3.one * (2*radius));
	}
}
