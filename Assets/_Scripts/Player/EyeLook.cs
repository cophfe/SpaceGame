using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;


//makes eyes look in direction of player movement and slightly towards cursor
public class EyeLook : MonoBehaviour
{
	[SerializeField] Transform lookPoint;
	[SerializeField] float directionOffset = 0.05f;
	[SerializeField] float maxHorizontalDistance = 0.1f;
	[SerializeField] float maxVerticalDistance = 0.04f;
	[SerializeField] float distanceMaxed = 15.0f;

	[SerializeField] Vector2 localRight = Vector2.right;

	Vector2 centrePosition;
	PlayerController controller;

	private void Start()
	{
		controller = GetComponentInParent<PlayerController>();
		centrePosition = transform.localPosition;
	}

	void Update()
    {
		Vector2 targetPosition = centrePosition;
		Vector2 up = Vector2.Perpendicular(localRight);
		
		//calculated mouse offset
		Vector2 mousePos = GameManager.Instance.MainCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());

		Vector2 mouseDelta = (mousePos - (Vector2)lookPoint.position);
		mouseDelta = Vector2.ClampMagnitude(mouseDelta, distanceMaxed) / distanceMaxed;
		mouseDelta = transform.InverseTransformDirection(mouseDelta);

		Vector2 mouseOffsetX = localRight * Vector2.Dot(localRight, mouseDelta) * maxHorizontalDistance;
		Vector2 mouseOffsetY = up * Vector2.Dot(up, mouseDelta) * maxVerticalDistance;

		//add mouse offset to target position
		targetPosition += mouseOffsetX + mouseOffsetY;

		if (controller.Animator.PlayerHorizontalSpeed * controller.Animator.PlayerHorizontalSpeed > 1)
		{
			if (controller.Animator.PlayerPointingLeft)
				targetPosition -= localRight * directionOffset;
			else
				targetPosition += localRight * directionOffset;
		}

		float t = 1.0f - Mathf.Pow(0.001f, Time.deltaTime);
		transform.localPosition = Vector3.Lerp(transform.localPosition, targetPosition, t);
	}
}
