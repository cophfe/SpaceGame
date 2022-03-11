using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class GravityAffector : MonoBehaviour
{
	[SerializeField] float gravityModifier = 1.0f;
	Rigidbody2D rb;

	private void Awake()
	{
		rb = GetComponent<Rigidbody2D>();
	}

	private void FixedUpdate()
	{
		var well = GameManager.Instance.GetClosestWell(transform.position);
		if (well)
		{
			Vector2 upDirection = well.GetUpDirection(transform.position);
			rb.rotation = Vector2.SignedAngle(Vector2.up, upDirection);

			float force = well.GetGravityAcceleration(transform.position) * gravityModifier;
			rb.AddForce(force * rb.mass * -upDirection);
		}
	}
}
