using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spring : MonoBehaviour
{
	[SerializeField] float springConstant = 10;
	[SerializeField] float dampening = 10;
	[SerializeField] float maxDistance = 0.4f;
	[SerializeField] Vector2 localAxis = Vector2.up;

	Rigidbody2D rb;
	Vector2 localFixedPosition;
	Vector2 lastPosition;
	Vector2 velocity = Vector2.zero;
	const float springHeight = 0;

	private void Awake()
	{
		rb = GetComponentInParent<Rigidbody2D>();
		localFixedPosition = transform.localPosition;
		lastPosition = rb.transform.position;
	}
	private void Update()
	{
		Vector2 direction = transform.TransformDirection(localAxis);
		Vector2 perpendicular = Vector2.Perpendicular(direction);

		//keep object in same position
		Vector2 delta = (Vector2)rb.transform.position - lastPosition;
		delta -= perpendicular * Vector2.Dot(perpendicular, delta);
		transform.position -= (Vector3)delta;
		lastPosition = rb.transform.position;

		//clamp distance
		Vector2 targetDelta = (Vector2)transform.localPosition - localFixedPosition;
		if (targetDelta.sqrMagnitude > maxDistance *maxDistance)
		{
			transform.localPosition = localFixedPosition + targetDelta.normalized * maxDistance;
		}

		//apply spring stuff
		float extention = Vector2.Dot(direction, localFixedPosition - (Vector2)transform.localPosition);
		velocity += direction * -springConstant * (springHeight - extention) * Time.deltaTime;
		Vector2 dampForce = direction * Vector3.Dot(velocity, direction) * dampening * Time.deltaTime;
		if (dampForce.sqrMagnitude > velocity.sqrMagnitude)
			velocity = Vector2.zero;
		else
			velocity -= dampForce;

		transform.localPosition += Time.deltaTime * (Vector3)velocity;
	}
}
