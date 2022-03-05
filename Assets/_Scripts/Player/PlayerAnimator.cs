using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAnimator : MonoBehaviour
{
	#region Editor
	[Header("Body Bob")]
	[SerializeField] float bobSpeed = 1; 
	[SerializeField] float bobMagnitude = 0.4f; 
	[SerializeField] float bobCancelSpeed = 0.4f; 
	[SerializeField] Transform bobObject;

	[Header("Hand Kinematics")]
	[SerializeField] ArmInfo leftArm;
	[SerializeField] ArmInfo rightArm;
	[SerializeField] float armTopLength = 0.57f;
	[SerializeField] float armBottomLength = 0.46f;
	[SerializeField] float handSpeed = 4;
	[SerializeField] float handSpeedAirMod = 2.0f;
	[SerializeField] float handSpeedRunMod = 1.0f;
	[SerializeField] float elbowSpeed = 4;
	[SerializeField] float inAirHandOffset = 0.3f;
	[SerializeField] float runHandDistance = 0.6f; //the distance between the attach point and the hand while running
	[SerializeField] float runLowerAngle = 0; //angle addition to the rest angle while running (hand is rotated low)
	[SerializeField] float runUpperAngle = 30f; //angle addition to the rest angle while running (hand rotated high)
	[SerializeField] float runArmSpeed = 0.1f; //angle addition to the rest angle while running (hand rotated high)
	[SerializeField] float runHorizontalMaxSpeed = 5; // the horizontal speed at which the run arm distance reaches the hand distance

	[Header("Feet Kinematics")]
	[SerializeField] FootInfo leftFoot;
	[SerializeField] FootInfo rightFoot;

	[SerializeField] float strideLength = 1; 
	[SerializeField] float footSpeed = 10.0f; //the speed of the feet
	[SerializeField] float kneeSpeed = 10.0f; //the speed of the feet
	[SerializeField] float footRotateSpeed = 10.0f; //the speed of the feet
	[SerializeField] float strideHeight = 0.2f; //the height of the arc of a stride
	[SerializeField] float bodySpeedStrideChange = 0.2f; //the amount body speed changes stride length
	[SerializeField] float bodySpeedStrideChangeInAir = 1.0f; //the amount body speed changes stride length in air
	[SerializeField] float legTopLength = 0.5f; //the leg length above the knee
	[SerializeField] float legBottomLength = 0.5f; //the leg length below the knee
	[SerializeField] float footScanExtraDist = 0.1f; // the extra distance, on top of the leg length, that the foot scans foor
	[SerializeField] float footRadius = 0.1f; //the radius of the foot raycast
	[SerializeField] float minDistance = 0.1f; //the minimum distance of the feet under the body
	[SerializeField] float bodySpeedFootModifier = 0.1f; //how much the body speed changes the foot speed
	[SerializeField] float speedArcModifier = 0.5f; // how much the body speed shrinks the arc size
	[SerializeField] float maxArcSpeed = 5.0f; // if the player is going this speed, the arc will be speedArcModifier * strideHeight tall
	[SerializeField] float maxHorizontalDistance = 1.0f; // if a foot is this far away from its attachement point it will be forced to step no matter what

	[SerializeField] LayerMask footAttachmentMask;
	#endregion
	public bool PlayerPointingLeft { get; private set; } = false;
	public float PlayerHorizontalSpeed { get; private set; } = 0;
	public bool PlayerIsWalking { get { return PlayerHorizontalSpeed * PlayerHorizontalSpeed > 0.1f * 0.1f; } }

	#region Private
	//References
	PlayerController controller;
	bool leftTurnToStep = true;
	bool previousHit = false;
	Vector2 lastPos;
	Vector2 bobPosition;
	float bobX = 0;

	Vector2 lastRBPosition;
	Vector2 rBDelta;
	#endregion

	private void Awake()
	{
		controller = GetComponent<PlayerController>();

		//bob
		lastPos = transform.position;
		bobPosition = bobObject.localPosition;

		//arms
		leftArm.Set();
		rightArm.Set();
		leftArm.isLeftArm = true;
		rightArm.isLeftArm = false;

		//legs
		leftFoot.leg = leftFoot.foot.GetComponent<LineRenderer>();
		rightFoot.leg = rightFoot.foot.GetComponent<LineRenderer>();

		leftFoot.leg.SetPosition(1, 0.5f * leftFoot.foot.InverseTransformPoint(leftFoot.footAttachment.position));
		rightFoot.leg.SetPosition(1, 0.5f * rightFoot.foot.InverseTransformPoint(rightFoot.footAttachment.position));
		leftFoot.leg.useWorldSpace = false;
		rightFoot.leg.useWorldSpace = false;

		
	}

	private void Start()
	{
		leftFoot.target = leftFoot.footAttachment.position - (Vector3)controller.Motor.UpDirection * (legTopLength + legBottomLength);
		lastRBPosition = controller.transform.position;
	}

	private void LateUpdate()
	{
		Vector2 tangent = Vector2.Perpendicular(controller.Motor.UpDirection);
		PlayerHorizontalSpeed = -Vector2.Dot(tangent, controller.RB.velocity);

		//switch if velocity is above minimum
		if (PlayerHorizontalSpeed * PlayerHorizontalSpeed > 2 * 2)
		{
			PlayerPointingLeft = PlayerHorizontalSpeed < 0;
		}

		MoveFoot(ref leftFoot, leftTurnToStep, ref rightFoot);
		MoveFoot(ref rightFoot, !leftTurnToStep, ref leftFoot);
		CalculateLeg(ref leftFoot);
		CalculateLeg(ref rightFoot);

		BobBody();

		Vector2 newPos = controller.transform.position;
		rBDelta = newPos - lastRBPosition;
		CalculateHand(ref leftArm);
		CalculateHand(ref rightArm);
		lastRBPosition = newPos;

		CalculateArm(ref leftArm);
		CalculateArm(ref rightArm);
	}

	void BobBody()
	{
		Vector2 tangent = Vector2.Perpendicular(controller.Motor.UpDirection);

		//Bob
		Vector2 newPos = transform.position;
		if (controller.Motor.IsGrounded)
		{
			float speed = Vector2.Dot(newPos - lastPos, tangent);
			//bob sine
			bobX = (bobX + speed * bobSpeed) % (2 * Mathf.PI);
			if (bobX < 0)
				bobX += (2 * Mathf.PI);
			//cancel bob overtime if going slow (move it towards a value that gives a sin of zero)
			if (speed * speed < 1)
			{
				if (bobX < Mathf.PI / 2.0f)
				{
					bobX = Mathf.MoveTowards(bobX, 0, bobCancelSpeed * Time.deltaTime);
				}
				else if (bobX > Mathf.PI * 1.5f)
				{
					bobX = Mathf.MoveTowards(bobX, (2 * Mathf.PI), bobCancelSpeed * Time.deltaTime);
				}
				else
				{
					bobX = Mathf.MoveTowards(bobX, Mathf.PI, bobCancelSpeed * Time.deltaTime);
				}
			}

			bobObject.localPosition = bobPosition + new Vector2(0, Mathf.Sin(bobX) * bobMagnitude);

			//bob bouncy (this one makes more sense but looks worse imo)
			//bobX = (bobX + speed * bobSpeed) % 1.0f;
			//if (bobX < 0)
			//	bobX += 1;

			//if (speed * speed < 1)
			//{
			//	if (bobX < 0.5f)
			//	{
			//		bobX = Mathf.MoveTowards(bobX, 0, bobCancelSpeed * Time.deltaTime);
			//	}
			//	else
			//		bobX = Mathf.MoveTowards(bobX, 1, bobCancelSpeed * Time.deltaTime);
			//}

			//float t = bobX - 0.5f;
			//bobObject.localPosition = bobPosition + new Vector2(0, (0.5f - 2* t*t) * bobMagnitude);

		}
		lastPos = newPos;
	}

	void MoveFoot(ref FootInfo foot, bool canStep, ref FootInfo otherFoot)
	{
		RaycastHit2D hit = Physics2D.CircleCast(foot.footAttachment.position, footRadius, - controller.Motor.UpDirection, legTopLength + legBottomLength - footRadius + footScanExtraDist, footAttachmentMask.value);

		Vector2 hitPoint;
		Vector2 hitNormal;
		if (hit)
		{
			hitNormal = hit.normal;
			hitPoint = hit.point; //+ (Vector2.Dot(controller.RB.velocity, hitTangent) * hitTangent * 0.1f);

			if (!previousHit)
			{
				foot.target = hitPoint;
				foot.footT = 0;
				foot.lastPoint = foot.point;
				foot.normal = hitNormal;
			}
		}
		else
		{
			hitPoint = foot.footAttachment.position - (Vector3)controller.Motor.UpDirection * (legTopLength + legBottomLength);
			hitNormal = controller.Motor.UpDirection;

			//the legs do a wierd jiggle in this case. Don't know how to fix it so will instead avoid it
			if (PlayerIsWalking || controller.Motor.State == PlayerMotor.MovementState.FALLING)
			{
				//if in air and can not find point, should make sure target point is continuously updated
				foot.attachedToGround = false;
				Vector2 hitTangent = Vector3.Cross(Vector3.Cross(hitNormal, controller.RB.velocity), hitNormal).normalized;
				float strideVel = Time.fixedDeltaTime * bodySpeedStrideChangeInAir * Vector2.Dot(controller.RB.velocity, hitTangent);

				foot.target = hitPoint + hitTangent * (strideVel);
				foot.footT = 0.0f;
				foot.lastPoint = foot.point;
				foot.normal = hitNormal;
			}
			else
				return;
			
			
			
		}

		if (foot.attachedToGround)
		{
			float horizontalDistance = Mathf.Abs(Vector2.Dot(controller.Motor.RightDirection, foot.target - hitPoint));

			//if standing still, make sure feet are under player
			if (canStep && !PlayerIsWalking && (foot.target - hitPoint).SqrMagnitude() > 0.01f)
			{
				foot.target = hitPoint;
				foot.footT = 0;
				foot.lastPoint = foot.point;
				foot.normal = hitNormal;

				foot.attachedToGround = false;

				foot.bendDirectionIsLeft = PlayerPointingLeft;

			}
			//if the foot has strode to far from the objective, reasses
			else if ((canStep && (foot.target - hitPoint).SqrMagnitude() > strideLength * strideLength)
				|| horizontalDistance > maxHorizontalDistance)
			{
				foot.target = GetHitTarget(hitPoint, hitNormal);
				foot.footT = 0;
				foot.lastPoint = foot.point;
				foot.normal = hitNormal;

				foot.attachedToGround = false;

				foot.bendDirectionIsLeft = PlayerPointingLeft;
			}
			else if (horizontalDistance < 0.02f)
			{
				//if the player falls, sometimes the foot gets caught in the air. this should prevent that by moving the foot down if it is in the air
				RaycastHit2D hit2 = Physics2D.Raycast(foot.foot.position, -controller.Motor.UpDirection, legTopLength + legBottomLength + footScanExtraDist, footAttachmentMask.value);
				if (hit2 && hit2.distance > bobMagnitude) //bob magnitude is a random value, so is the raycast length^. it used to have reason but now after a bunch of edits it doesn't. buut it works so whatever
				{
					foot.target = hitPoint;
					foot.point = hitPoint;
				}
			}
			//if, while standing still, this foot is in the right position but the other foot isn't, allow the other foot to step
			else if (canStep && !PlayerIsWalking && (otherFoot.target - hitPoint).SqrMagnitude() > 0.01f)
			{
				leftTurnToStep = !leftTurnToStep;
			}
		}
		else
		{
			
			//should move in an arc toward position, but for now just do it linearly

			Vector2 footDelta = (foot.target - foot.point);
			float bodySpeed = Vector2.Dot(controller.RB.velocity, footDelta.normalized) * bodySpeedFootModifier * Time.fixedDeltaTime;

			//if target point has fallen too far behind player
			Vector2 tangent = Vector3.Cross(Vector3.Cross(hitNormal, controller.RB.velocity), hitNormal).normalized;
			if (Vector2.Dot(tangent, (Vector2)foot.footAttachment.position - foot.target) > maxHorizontalDistance)
			{
				foot.footT = 0;
				foot.lastPoint = foot.point;
				foot.target = GetHitTarget(hitPoint, hitNormal);
			}

			//set footT
			foot.footT = Mathf.Clamp01(foot.footT + footSpeed * Time.deltaTime + bodySpeed);
			//add the arc
			float arcT = foot.footT - 0.5f;
			Vector2 arc = foot.normal * strideHeight * (-4 * arcT * arcT + 1);
			float arcHeightT = 1 - Mathf.Clamp01(bodySpeed / maxArcSpeed) * speedArcModifier;
			foot.point = Vector2.LerpUnclamped(foot.lastPoint, foot.target, foot.footT) + arcHeightT * arc;
			if (foot.footT >= 1)
			{
				foot.attachedToGround = true && hit;
				leftTurnToStep = !leftTurnToStep;
			}

			//foot.point = Vector2.MoveTowards(foot.point, foot.target, (footSpeed + bodySpeed) * Time.deltaTime);
			//foot.attachedToGround = (foot.point - foot.target).SqrMagnitude() < 0.00001f && hit;

		}


		previousHit = hit;
	}

	Vector2 GetHitTarget(Vector2 hitPoint, Vector2 hitNormal)
	{
		Vector2 hitTangent = Vector3.Cross(Vector3.Cross(hitNormal, controller.RB.velocity), hitNormal).normalized;
		float strideVel = Time.fixedDeltaTime * bodySpeedStrideChange * Vector2.Dot(controller.RB.velocity, hitTangent);
		return hitPoint + hitTangent * strideLength + hitTangent * (strideVel);
	}

	//based on https://www.alanzucconi.com/2018/05/02/ik-2d-1/
	void CalculateLeg(ref FootInfo foot)
	{
		Vector2 startPos = foot.footAttachment.position;
		Vector2 targetMidPoint;
		float c = legTopLength;
		float b = (foot.point - startPos).magnitude;
		float a = legBottomLength;

		if (b >= a + c)
		{
			Vector2 deltaNorm = (foot.point - startPos).normalized;

			foot.point = startPos + deltaNorm * (legTopLength + legBottomLength);
			ClampFootPoint(ref foot);

			foot.foot.position = foot.point;
			targetMidPoint = startPos + deltaNorm * legTopLength;

			int rotMod = 1;
			if (foot.bendDirectionIsLeft)
			{
				foot.foot.localScale = new Vector3(-1, 1, 1);
				rotMod = -1;
			}
			else
			{
				foot.foot.localScale = new Vector3(1, 1, 1);

			}

			foot.foot.rotation = Quaternion.RotateTowards(foot.foot.rotation, Quaternion.Euler(0, 0, rotMod * -Vector2.Angle(Vector2.up, controller.Motor.UpDirection)), footRotateSpeed * Time.deltaTime);

		}
		else
		{
			float aAngle;
			int rotMod = 1;

			if (foot.bendDirectionIsLeft)
			{
				foot.foot.localScale = new Vector3(-1, 1, 1);
				aAngle = Mathf.Atan2((foot.point.y - startPos.y),
				(foot.point.x - startPos.x)) - Mathf.Acos((b * b + c * c - a * a) / (2 * b * c));
				rotMod = -1;
			}
			else
			{
				foot.foot.localScale = new Vector3(1, 1, 1);
				aAngle = Mathf.Acos((b * b + c * c - a * a) / (2 * b * c)) + Mathf.Atan2((foot.point.y - startPos.y),
					(foot.point.x - startPos.x));
			}
			ClampFootPoint(ref foot);
			foot.foot.position = foot.point;
			//Debug.Log(aAngle * Mathf.Rad2Deg);
			targetMidPoint = startPos + new Vector2(Mathf.Cos(aAngle) * legTopLength, Mathf.Sin(aAngle) * legTopLength);

			if (!foot.attachedToGround)
				foot.foot.rotation = Quaternion.RotateTowards(foot.foot.rotation, Quaternion.Euler(0, 0, rotMod * - Vector2.Angle(Vector2.up, (targetMidPoint - foot.point).normalized)), footRotateSpeed * Time.deltaTime);
			else
				foot.foot.rotation = Quaternion.RotateTowards(foot.foot.rotation, Quaternion.Euler(0, 0, rotMod  *- Vector2.Angle(Vector2.up, controller.Motor.UpDirection)), footRotateSpeed * Time.deltaTime);
		}

		//positions are local to foot
		foot.leg.SetPosition(0, foot.foot.InverseTransformPoint(foot.footAttachment.position));
		foot.leg.SetPosition(2, Vector2.zero);

		if (float.IsNaN(targetMidPoint.x) || float.IsNaN(targetMidPoint.y))
			targetMidPoint = 0.5f * foot.leg.GetPosition(0);

		Vector2 midPoint = Vector2.MoveTowards(foot.leg.GetPosition(1), foot.foot.InverseTransformPoint(targetMidPoint), kneeSpeed * Time.deltaTime);
		foot.leg.SetPosition(1, midPoint);
	}

	void ClampFootPoint(ref FootInfo foot)
	{
		Vector2 start = foot.footAttachment.position;
		Vector2 up = controller.Motor.UpDirection;
		float footUpDist = Vector2.Dot(start - foot.point, up);

		if (footUpDist < minDistance)
		{
			foot.point -= Vector2.Dot(foot.point, up) * up;
			foot.point += Vector2.Dot(start, up) * up - up * minDistance;
		}
	}
	float GetVelocityInDirection(Vector2 direction, Vector2 velocity)
	{
		return Vector2.Dot(direction, velocity);
	}

	void CalculateHand(ref ArmInfo arm)
	{
		if (!controller.Motor.IsGrounded)
		{
			Vector2 newTargetPoint = arm.restPoint + controller.Motor.UpDirection * inAirHandOffset;

			arm.rotateT = 0;
			//keep object in same position as previous frame
			arm.hand.position -= (Vector3)rBDelta;
			//smoothly move toward new position
			arm.hand.position = Vector2.SmoothDamp(arm.hand.position, arm.armAttachPoint.TransformPoint(newTargetPoint), ref arm.handVelocity, handSpeed * handSpeedAirMod);
		}
		else if (PlayerIsWalking)
		{
			arm.bendDirectionIsLeft = PlayerHorizontalSpeed > 0;
			arm.SetOrdered(arm.bendDirectionIsLeft == !arm.isLeftArm); //this seems like a slow thing to set every frame but what do I know

			Vector2 armDelta = arm.armAttachPoint.TransformPoint(arm.restPoint) - arm.armAttachPoint.position;
			float distanceToHand = Mathf.Lerp(armDelta.magnitude, runHandDistance, Mathf.Abs(PlayerHorizontalSpeed) / runHorizontalMaxSpeed);
			armDelta = (arm.armAttachPoint.TransformPoint(arm.restPoint) - arm.armAttachPoint.position).normalized * distanceToHand;
			
			arm.rotateT += PlayerHorizontalSpeed * runArmSpeed * Time.deltaTime;

			int sign = System.Math.Sign(PlayerHorizontalSpeed);

			float t = arm.isLeftArm ? 0.5f * Mathf.Sin(arm.rotateT) + 0.5f : 0.5f * Mathf.Cos(arm.rotateT) + 0.5f;
			Quaternion rotation = Quaternion.Slerp(Quaternion.Euler(0, 0, sign * runLowerAngle), Quaternion.Euler(0, 0, sign * runUpperAngle), t);
			armDelta = rotation * armDelta;

			arm.hand.position = Vector2.SmoothDamp(arm.hand.position, (Vector2)arm.armAttachPoint.position + armDelta, ref arm.handVelocity, handSpeed * handSpeedRunMod);
		}
		else
		{
			arm.rotateT = 0;
		
			arm.bendDirectionIsLeft = !arm.isLeftArm;

			//keep object in same position as previous frame
			arm.hand.position -= (Vector3)rBDelta;
			//smoothly move toward new position
			arm.hand.position = (Vector3)Vector2.SmoothDamp(arm.hand.position, arm.armAttachPoint.TransformPoint(arm.restPoint), ref arm.handVelocity, handSpeed);
		}		
	}
	void CalculateArm(ref ArmInfo arm)
	{
		Vector2 startPos = arm.armAttachPoint.position;
		Vector2 endPos = arm.hand.position;
		Vector2 targetMidPoint;

		float c = armTopLength;
		float b = (endPos - startPos).magnitude;
		float a = armBottomLength;

		if (b >= a + c)
		{
			Vector2 deltaNorm = (endPos - startPos).normalized;

			Vector2 newHandPosition = startPos + deltaNorm * (armTopLength + armBottomLength);
			arm.hand.position = newHandPosition;
			targetMidPoint = startPos + deltaNorm * armTopLength;

			int rotMod = 1;
			if (arm.bendDirectionIsLeft)
			{
				arm.hand.localScale = new Vector3(-1, 1, 1);
				rotMod = -1;
			}
			else
			{
				arm.hand.localScale = new Vector3(1, 1, 1);

			}

			arm.hand.rotation = Quaternion.RotateTowards(arm.hand.rotation, Quaternion.Euler(0, 0, rotMod * -Vector2.Angle(Vector2.up, controller.Motor.UpDirection)), footRotateSpeed * Time.deltaTime);

		}
		else
		{
			float aAngle;
			int rotMod = 1;

			if (arm.bendDirectionIsLeft)
			{
				arm.hand.localScale = new Vector3(-1, 1, 1);
				aAngle = Mathf.Atan2((endPos.y - startPos.y),
				(endPos.x - startPos.x)) - Mathf.Acos((b * b + c * c - a * a) / (2 * b * c));
				rotMod = -1;
			}
			else
			{
				arm.hand.localScale = new Vector3(1, 1, 1);
				aAngle = Mathf.Acos((b * b + c * c - a * a) / (2 * b * c)) + Mathf.Atan2((endPos.y - startPos.y),
					(endPos.x - startPos.x));
			}

			targetMidPoint = startPos + new Vector2(Mathf.Cos(aAngle) * armTopLength, Mathf.Sin(aAngle) * armTopLength);

			arm.hand.rotation = Quaternion.RotateTowards(arm.hand.rotation, Quaternion.Euler(0, 0, rotMod * -Vector2.Angle(Vector2.up, (targetMidPoint - endPos).normalized)), footRotateSpeed * Time.deltaTime);
		}
		
		//positions are local to hand
		arm.arm.SetPosition(0, arm.hand.InverseTransformPoint(arm.armAttachPoint.position));
		arm.arm.SetPosition(2, Vector2.zero);

		if (float.IsNaN(targetMidPoint.x) || float.IsNaN(targetMidPoint.y))
			targetMidPoint = 0.5f * arm.arm.GetPosition(0);

		//if currentMidPoint is on the oppoisite side of the start position than target midpoint, clamp it to the correct side
		Vector2 tangent = -TripleCross(targetMidPoint - startPos, controller.Motor.UpDirection, controller.Motor.UpDirection).normalized;
		Vector2 currentMidPoint = arm.hand.TransformPoint(arm.arm.GetPosition(1));
		Plane p = new Plane(tangent, arm.armAttachPoint.position);
		if (!p.GetSide(currentMidPoint))
		{
			currentMidPoint = p.ClosestPointOnPlane(currentMidPoint);
		}

		Vector2 midPoint = arm.hand.InverseTransformPoint(Vector2.MoveTowards(currentMidPoint, targetMidPoint, elbowSpeed * Time.deltaTime));
		arm.arm.SetPosition(1, midPoint);

	}

	//Perpendicular in direction is (direction x line) x line
	Vector2 TripleCross(Vector2 a, Vector2 b, Vector2 c)
	{
		float crossValue = a.x * b.y - b.x * a.y;
		return new Vector2(-crossValue * c.y, crossValue * c.x);
	}

	[System.Serializable]
	struct FootInfo
	{
		public Transform foot;
		public Transform footAttachment;
		[System.NonSerialized]
		public bool attachedToGround;
		[System.NonSerialized]
		public Vector2 point; //current foot position
		[System.NonSerialized]
		public Vector2 target; //target foot position
		[System.NonSerialized]
		public LineRenderer leg;
		[System.NonSerialized]
		public Vector2 lastPoint; //foot position when target was set
		[System.NonSerialized]
		public Vector2 normal; 
		[System.NonSerialized]
		public float footT;
		[System.NonSerialized]
		public bool bendDirectionIsLeft;
	}

	[System.Serializable]
	struct ArmInfo
	{
		public void Set()
		{
			restPoint = armAttachPoint.InverseTransformPoint(hand.position);
			arm = hand.GetComponent<LineRenderer>();
			arm.useWorldSpace = false;
			velocity = Vector2.zero;
			rotateT = 0;

			handSprite = hand.GetComponentInChildren<SpriteRenderer>();
		}

		public void SetOrdered(bool inFront)
		{
			if (inFront)
			{
				handSprite.sortingOrder = 5;
				arm.sortingOrder = 5;
			}
			else
			{
				handSprite.sortingOrder = -5;
				arm.sortingOrder = -5;
			}
		}

		public Transform hand;
		public Transform armAttachPoint;
		[System.NonSerialized]
		public LineRenderer arm;
		[System.NonSerialized]
		public Vector2 restPoint; //points are relative to arm attach 
		[System.NonSerialized]
		public float rotateT;
		[System.NonSerialized]
		public Vector2 velocity;
		[System.NonSerialized]
		public bool bendDirectionIsLeft;
		[System.NonSerialized]
		public Vector2 handVelocity;
		[System.NonSerialized]
		public bool isLeftArm;

		[System.NonSerialized]
		SpriteRenderer handSprite;
	}
}
