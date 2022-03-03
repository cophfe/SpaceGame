using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
	//Player Movement
	[Header("Movement")]
	[SerializeField] float springHeight = 2.0f;
	[SerializeField] float springRestHeight = 0.7f;
	[SerializeField] float springDamping = 0.3f;
	[SerializeField] float springDetectionRadius = 0.2f;
	[SerializeField] float moveStrength = 30;
	[SerializeField] float jumpStrength = 30;

	[SerializeField] LayerMask groundLayerMask;

	Rigidbody2D rb = null;

	//input
	float horizontalMovement = 0;
	bool jumped = false;
	
	Vector2 upDirection = Vector2.up;
	
	//anti ground system
	float springConstant;
	Vector2 rayCastPoint;


    void Start()
    {
		rb = GetComponentInChildren<Rigidbody2D>();

		BoxCollider2D collider = GetComponentInChildren<BoxCollider2D>();
		rayCastPoint = new Vector2(0, -0.5f * collider.size.y);

		springConstant = -rb.mass * Vector2.Dot(upDirection, Physics2D.gravity)/(springHeight - springRestHeight);
    }

	private void OnValidate()
	{
		if (rb != null)
			springConstant = -rb.mass * Vector2.Dot(upDirection, Physics2D.gravity)/(springHeight - springRestHeight);
	}

	void FixedUpdate()
    {
		if (jumped)
		{
			jumped = false;
			rb.AddForce(upDirection * jumpStrength, ForceMode2D.Impulse);
		}

		if (horizontalMovement != 0)
		{
			rb.AddForce(new Vector2(upDirection.y, -upDirection.x) * horizontalMovement * moveStrength);
		}
		else
		{
		}

		Vector2 globalRayCastPoint = transform.TransformPoint(rayCastPoint);
		RaycastHit2D hit = Physics2D.CircleCast(globalRayCastPoint, springDetectionRadius, - upDirection, springHeight - springDetectionRadius, groundLayerMask.value);
		if (hit)
		{
			float hitLength = Mathf.Abs(Vector2.Dot(hit.point, upDirection) - Vector2.Dot(globalRayCastPoint, upDirection));
			float overExtention = springHeight - hitLength;

			//spring force
			rb.AddForce(upDirection * springConstant * overExtention);
			//damping
			rb.AddForce(upDirection * Vector3.Dot(rb.velocity, upDirection) * -springDamping);
		}
    }

	
}
