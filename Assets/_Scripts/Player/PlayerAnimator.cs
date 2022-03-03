using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAnimator : MonoBehaviour
{
	#region Editor
	//Feet stuff
	[Header("Feet Kinematics")]
	[SerializeField] FootInfo leftFoot;
	[SerializeField] FootInfo rightFoot;

	[SerializeField] float strideLength = 1;
	[SerializeField] float secondaryStrideLength = 1;
	[SerializeField] float footSpeed = 10.0f;
	[SerializeField] float kneeSpeed = 10.0f;
	[SerializeField] float strideHeight = 0.2f;
	[SerializeField] float bodySpeedStrideChange = 0.2f;
	[SerializeField] float bodySpeedStrideChangeInAir = 1.0f;
	[SerializeField] float legTopLength = 0.5f;
	[SerializeField] float legBottomLength = 0.5f;
	[SerializeField] float footScanExtraDist = 0.1f;
	[SerializeField] float footRadius = 0.1f;
	[SerializeField] LayerMask footAttachmentMask;
	bool leftTurnToStep = true;
	#endregion

	#region Private
	//References
	PlayerController controller;
	LineRenderer leftLeg;
	LineRenderer rightLeg;
	bool previousHit = false;
	#endregion

	private void Awake()
	{
		controller = GetComponent<PlayerController>();
		leftFoot.leg = leftFoot.foot.GetComponent<LineRenderer>();
		rightFoot.leg = rightFoot.foot.GetComponent<LineRenderer>();

		leftFoot.leg.SetPosition(1, 0.5f * (leftFoot.foot.position + leftFoot.footAttachment.position));
		rightFoot.leg.SetPosition(1, 0.5f * (rightFoot.foot.position + rightFoot.footAttachment.position));
	}

	private void Start()
	{
		leftFoot.target = leftFoot.footAttachment.position - (Vector3)controller.Motor.UpDirection * (legTopLength + legBottomLength);

	}

	private void LateUpdate()
	{
		MoveFoot(ref leftFoot, leftTurnToStep);
		MoveFoot(ref rightFoot, !leftTurnToStep);
		CalculateLeg(ref leftFoot);
		CalculateLeg(ref rightFoot);
	}

	void MoveFoot(ref FootInfo foot, bool canStep)
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
			//if in air and can not find point, should make sure target point is continuously updated
			if (!foot.attachedToGround)
			{
				Vector2 hitTangent = Vector3.Cross(Vector3.Cross(hitNormal, controller.RB.velocity), hitNormal).normalized;
				float strideVel = Time.deltaTime * bodySpeedStrideChangeInAir * Vector2.Dot(controller.RB.velocity, hitTangent);

				foot.target = hitPoint + hitTangent * (strideVel);
				foot.footT = 0.0f;
				foot.lastPoint = foot.point;
				foot.normal = hitNormal;
			}
		}

		if (foot.attachedToGround)
		{
			//if the foot has strode to far from the objective, reasses
			if ((canStep && (foot.target - hitPoint).SqrMagnitude() > strideLength * strideLength)
				|| (!canStep && (foot.target - hitPoint).SqrMagnitude() > secondaryStrideLength * secondaryStrideLength))
			{
				Vector2 hitTangent = Vector3.Cross(Vector3.Cross(hitNormal, controller.RB.velocity), hitNormal).normalized;
				float strideVel = Time.deltaTime * bodySpeedStrideChange * Vector2.Dot(controller.RB.velocity, hitTangent);
				foot.target = hitPoint + hitTangent * (strideLength + strideVel);
				foot.footT = 0;
				foot.lastPoint = foot.point;
				foot.normal = hitNormal;

				foot.attachedToGround = false;
			}
		}
		else
		{
			//should move in an arc toward position, but for now just do it linearly
			
			Vector2 footDelta = (foot.target - foot.point);
			float bodySpeed = Vector2.Dot(controller.RB.velocity, footDelta.normalized);

			//set footT
			foot.footT = Mathf.Clamp01(foot.footT + (footSpeed + bodySpeed) * Time.deltaTime);
			//add the arc
			float arcT = foot.footT - 0.5f;
			Vector2 arc = foot.normal * strideHeight * (-4 * arcT * arcT + 1);
			foot.point = Vector2.LerpUnclamped(foot.lastPoint, foot.target, foot.footT) + arc;
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

	//based on https://www.alanzucconi.com/2018/05/02/ik-2d-1/
	void CalculateLeg(ref FootInfo foot)
	{
		Vector2 startPos = foot.footAttachment.position;
		Vector2 targetMidPoint;
		float c = legTopLength;
		float b = (foot.point - startPos).magnitude;
		float a = legBottomLength;

		float aAngle;
		//float bAngle;

		float kneeSpeedModifier = 1;

		if (b > a + c)
		{
			Vector2 deltaNorm = (foot.point - startPos).normalized;

			foot.foot.position = startPos + deltaNorm * (legTopLength + legBottomLength);
			targetMidPoint = startPos + deltaNorm * legTopLength;
			kneeSpeedModifier = 0.5f;
		}
		else
		{
			aAngle = Mathf.Acos((b * b + c * c - a * a) / (2 * b * c)) + Mathf.Atan2((foot.point.y - startPos.y),
				(foot.point.x - startPos.x));
			//if (aAngle > Mathf.PI * 0.5f)
			//	aAngle -= Mathf.PI;
			//else if (aAngle < Mathf.PI * -0.5f)
			//	aAngle += Mathf.PI;

			//bAngle = Mathf.PI - Mathf.Acos((a * a + c * c - b * b) / (2 * a * c));

			foot.foot.position = foot.point;
			//Debug.Log(aAngle * Mathf.Rad2Deg);
			targetMidPoint = startPos + new Vector2(Mathf.Cos(aAngle) * legTopLength, Mathf.Sin(aAngle) * legTopLength);
			
		}

		foot.leg.SetPosition(0, foot.footAttachment.position);

		Vector2 kneeDelta = targetMidPoint - (Vector2)foot.leg.GetPosition(1);
		float bodySpeed = Vector2.Dot(controller.RB.velocity, kneeDelta.normalized);

		if (float.IsNaN(targetMidPoint.x) || float.IsNaN(targetMidPoint.y))
		{
			targetMidPoint = foot.leg.GetPosition(1);
		}
		Vector2 midPoint = Vector2.MoveTowards(foot.leg.GetPosition(1), targetMidPoint, kneeSpeedModifier * (bodySpeed + kneeSpeed) * Time.deltaTime);
		foot.leg.SetPosition(1, midPoint);
		foot.leg.SetPosition(2, foot.foot.position);


		


		

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
	}
}
