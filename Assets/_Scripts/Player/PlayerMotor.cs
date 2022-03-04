using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//this is based on the character controller from TurtleTown (my year 1 final project)
public class PlayerMotor : MonoBehaviour
{
	#region Editor
	//Player Movement
	[Header("Walking")]
	[SerializeField] float acceleration = 10; //How fast the player's movement velocity will reach it's target velocity
	[SerializeField] float targetSpeed = 10; //Target velocity of the player's movement velocity when horizontal input is more than 0
	[SerializeField] float maxVelocity = 100; //The overall maximum velocity of the player
	[SerializeField] float airControlModifier = 0.8f; //The percentage of acceleration used while in the air
	[SerializeField] float slopeLimit = 70.0f; //The maximum slope angle in degrees
	//Friction
	[SerializeField] float groundFriction = 0.4f; //The amount of velocity removed every second on the ground
	[SerializeField] float airFriction = 0.3f; //The amount of velocity removed every second in the air
	//Jumping
	[SerializeField] float jumpImpulse = 4; //The impulse applied when jumping
	[SerializeField] float jumpDuration = 0.3f; //How long jump impulse will be applied for
	[SerializeField] int airJumps = 0; //How many air jumps the player can take
	[SerializeField] float jumpBufferTime = 0.1f; //The length of time when not touching the ground that jump input will be saved
	[SerializeField] float jumpCoyoteTime = 0.1f; //The amount of time after leaving the ground that jump input will be acted on
	[SerializeField] float gravity = -10.0f; //The amount of gravitational force downwards
	[Header("Ground Detection")]
	//Ground Detection
	[SerializeField] float groundDetectionRadius = 0.8f; //The radius of the ground detection circle
	[SerializeField] float totalPlayerHeight = 1.8f; //The overal player height, including collider height
	[SerializeField] float groundDetectAddition = 0f; //The offset to total player height that the player is considered floored for
	[SerializeField] LayerMask groundLayerMask; //Layers that are considered ground
	[Header("Ground Avoidance")]
	//Ground Avoidance
	[SerializeField] float springHeight = 2.0f; //the hight of the ground avoidance spring
	[SerializeField] float springDamping = 0.3f; //the amount of dampening applied to the spring's force
	#endregion

	#region Private
	//References
	PlayerController controller; //The player controller (handles input and holds references)
	//Gravity
	GravityWell currentGravityWell;
	Vector2 upDirection = Vector2.up;
	//Velocity
	Vector2 targetVelocity; // the target velocity based on player input
	Vector2 movementAcceleration; //the part of velocity that tries to get to the target velocity
	Vector2 forcesAcceleration; //other parts of velocity (jumping, drag, spring force)
	//Jump
	float jumpBufferTimer = 0;
	float jumpCoyoteTimer = 0;
	float jumpTimer = 0;
	int airJumpsLeft = 0;
	//Ground Detection
	bool isGrounded = false; //whether the player can be considered grounded
	Vector2 groundPoint; //the point on the ground the player is on
	Vector2 groundNormal; // the normal of the ground under the player
	float groundDistance; // the distance to the ground from the player
	float groundAngle; // the distance to the ground from the player
	Collider2D groundCollider; //the ground collider
	//Ground Avoidance
	float springRestHeight = 0.5f; //the player's height minus the collider's height
	//Other
	MovementState state = MovementState.FALLING;
	//spring system
	float springConstant; //used to calculate the spring force
	Vector2 rayCastPoint;
	#endregion

	public enum MovementState
	{
		GROUNDED,
		FALLING,
		RISING,
		JUMPING 
	}

	void Awake()
    {
		controller = GetComponent<PlayerController>();
    }

	void Start()
	{
		CapsuleCollider2D collider = GetComponentInChildren<CapsuleCollider2D>();
		rayCastPoint = new Vector2(0, -0.5f * collider.size.y);

		springRestHeight = totalPlayerHeight - collider.size.y;
		springConstant = -controller.RB.mass * gravity / (springHeight - springRestHeight);
	}

	private void OnValidate()
	{
		if (controller && controller.RB)
		{
			CapsuleCollider2D collider = GetComponentInChildren<CapsuleCollider2D>();
			springRestHeight = totalPlayerHeight - collider.size.y;
			springConstant = -controller.RB.mass * gravity / (springHeight - springRestHeight);
		}
	}

	void FixedUpdate()
	{
		Run();
		//if (controller.EvaluateJumpPressed())
		//{
		//	controller.RB.AddForce(upDirection * jumpImpulse, ForceMode2D.Impulse);
		//}

		//if (controller.HorizontalInput != 0)
		//{
		//	controller.RB.AddForce(new Vector2(upDirection.y, -upDirection.x) * controller.HorizontalInput * targetSpeed);
		//}

		//Vector2 globalRayCastPoint = transform.TransformPoint(rayCastPoint);
		//RaycastHit2D hit = Physics2D.CircleCast(globalRayCastPoint, groundDetectionRadius, -upDirection, springHeight - groundDetectionRadius, groundLayerMask.value);
		//if (hit)
		//{
		//	float hitLength = Mathf.Abs(Vector2.Dot(hit.point, upDirection) - Vector2.Dot(globalRayCastPoint, upDirection));
		//	float overExtention = springHeight - hitLength;

		//	//spring force
		//	controller.RB.AddForce(upDirection * springConstant * overExtention);
		//	//damping
		//	controller.RB.AddForce(upDirection * Vector3.Dot(controller.RB.velocity, upDirection) * -springDamping);
		//}
	}

	void Run()
	{
		forcesAcceleration = Vector2.zero;
		movementAcceleration = Vector2.zero;

		CalculateUpDirection();
		UpdateRotation();
		ScanForGround();
		SetState();
		EvaluateJump();
		UpdateMovementVector();
		UpdateForcesVector();

		//move player
		controller.RB.velocity += movementAcceleration + forcesAcceleration; //might need to use a ground offset to keep player grounded when going down slopes
		if (controller.RB.velocity.sqrMagnitude > maxVelocity * maxVelocity)
		{
			controller.RB.velocity = controller.RB.velocity.normalized * maxVelocity;
		}

		//set timers
		jumpTimer -= Time.deltaTime;
		jumpCoyoteTimer -= Time.deltaTime;
		jumpBufferTimer -= Time.deltaTime;
	}
	
	void CalculateUpDirection()
	{
		//check attached gravity well direction
		if (currentGravityWell)
		{
			upDirection = currentGravityWell.GetGravityDirection();
		}
	}

	void UpdateRotation()
	{
		//update rotation based on upDirection (go through rigidbody.rotation)
	}

	void ScanForGround()
	{
		//scan goes down
		Vector2 scanDirection = -upDirection;
		//scan starts at bottom of collider
		Vector2 scanOrigin = transform.TransformPoint(rayCastPoint);
		float length = springHeight + groundDetectAddition - groundDetectionRadius;

		RaycastHit2D hit = Physics2D.CircleCast(scanOrigin, groundDetectionRadius, scanDirection, length, groundLayerMask.value);
		isGrounded = hit;
		
		if (isGrounded)
		{
			groundPoint = hit.point;
			groundDistance = hit.distance + groundDetectionRadius;
			groundNormal = hit.normal;
			groundAngle = Vector2.Angle(groundNormal, upDirection);
			groundCollider = hit.collider;
		}
	}

	void EvaluateJump()
	{
		if (controller.EvaluateJumpPressed())
		{
			if (state == MovementState.GROUNDED)
			{
				OnJump();
				OnLeaveGround();
			}
			else
			{
				if (airJumpsLeft > 0)
				{
					airJumpsLeft--;
					OnJump();
				}
				else if (jumpCoyoteTimer > 0)
				{
					jumpCoyoteTimer = 0;
					OnJump();
				}
				else
				{
					controller.EvaluateJumpCancelled();
					jumpBufferTimer = jumpBufferTime;
				}
			}
		}
		

		//if state was set to jumping, add jump force
		if (state == MovementState.JUMPING)
		{
			//set up component to jumpImpulse
 			controller.RB.velocity -= upDirection * Vector2.Dot(controller.RB.velocity, upDirection);
			forcesAcceleration += upDirection * jumpImpulse; 

			//check if jump should be cancelled
			if (controller.EvaluateJumpCancelled() || jumpTimer <= 0)
			{
				state = MovementState.RISING;
			}
		}
		else
		{
			controller.EvaluateJumpCancelled();

		}
	}

	void OnJump()
	{
		state = MovementState.JUMPING;
		jumpTimer = jumpDuration;
	}

	void OnLeaveGround()
	{
		jumpCoyoteTimer = jumpCoyoteTime;
		groundCollider = null;
	}

	void SetState()
	{
		bool movingUp = Vector3.Dot(upDirection, controller.RB.velocity) > 0.3f;
		bool groundTooSteep = groundAngle > slopeLimit;

		switch (state)
		{
			case MovementState.GROUNDED:
				if (movingUp)
				{
					state = MovementState.RISING;
					OnLeaveGround();
				}
				else if (!isGrounded)
				{
					state = MovementState.FALLING;
					OnLeaveGround();
				}
				else if (groundTooSteep)
				{
					state = MovementState.FALLING;
					OnLeaveGround();
				}
				break;

			case MovementState.FALLING:
				if (movingUp)
				{
					state = MovementState.RISING;
				}
				else if (isGrounded)
				{
					if (groundTooSteep)
					{
						state = MovementState.FALLING;
					}
					else
					{
						state = MovementState.GROUNDED;
						OnLand();
					}
				}
				break;

			case MovementState.RISING:
				if (!movingUp)
				{
					if (isGrounded)
					{
						if (groundTooSteep)
						{
							state = MovementState.FALLING;
						}
						else
						{
							state = MovementState.GROUNDED;
							OnLand();
						}
					}
					else
					{
						state = MovementState.FALLING;
					}
				}
				break;
		}

	}

	void OnLand()
	{
		//refresh air jumps
		airJumpsLeft = airJumps;

		//if there is a jump buffered
		if (jumpBufferTimer > 0)
		{
			jumpBufferTimer = 0;
			OnJump();
			OnLeaveGround();
			//cancel jump immediatly if player let go of button
			if (controller.EvaluateJumpCancelled())
			{
				state = MovementState.RISING;
			}
		}
	}

	void UpdateMovementVector()
	{
		if (state == MovementState.GROUNDED)
		{
			Vector2 tangent = Vector2.Perpendicular(groundNormal);
			targetVelocity = GetInputDir() * tangent * -targetSpeed;
			float currentAcceleration = acceleration;

			//make it turn around fast
			if (Vector2.Dot(targetVelocity, controller.RB.velocity) < 0)
			{
				currentAcceleration *= 2;
			}

			Vector2 vel = Vector2.Dot(tangent, controller.RB.velocity) * tangent;
			if ((targetVelocity - vel).sqrMagnitude < currentAcceleration * Time.deltaTime)
			{
				movementAcceleration += targetVelocity - vel;
			}
			else
			{
				movementAcceleration += (targetVelocity-vel).normalized * currentAcceleration * Time.deltaTime;
			}
		}
		else
		{
			if (controller.HorizontalInput != 0)
			{
				Vector2 tangent = Vector2.Perpendicular(groundNormal);
				targetVelocity = GetInputDir() * tangent * -targetSpeed;

				Vector2 vel = Vector2.Dot(tangent, controller.RB.velocity) * tangent;
				if ((targetVelocity - vel).sqrMagnitude < airControlModifier * acceleration * Time.deltaTime)
				{
					movementAcceleration += targetVelocity - vel;
				}
				else
				{
					movementAcceleration += (targetVelocity - vel).normalized * airControlModifier * acceleration * Time.deltaTime;
				}
			}
		}

		int GetInputDir()
		{
			return controller.HorizontalInput == 0 ? 0 : System.Math.Sign(controller.HorizontalInput);
		}
	}

	void UpdateForcesVector()
	{

		//gravity
		forcesAcceleration += upDirection * (currentGravityWell ? currentGravityWell.GetGravity(gravity) : gravity) * Time.deltaTime;


		////Friction
		if (state == MovementState.GROUNDED)
		{
			forcesAcceleration += GetMoveStep(controller.RB.velocity, Vector3.zero, groundFriction * Time.deltaTime);
		}
		else
		{
			forcesAcceleration += GetMoveStep(controller.RB.velocity, Vector3.zero, airFriction * Time.deltaTime);
		}


		//Vector2 globalRayCastPoint = transform.TransformPoint(rayCastPoint);
		//RaycastHit2D hit = Physics2D.CircleCast(globalRayCastPoint, groundDetectionRadius, -upDirection, springHeight - groundDetectionRadius, groundLayerMask.value);
		//if (hit)
		//{
		//	float hitLength = Mathf.Abs(Vector2.Dot(hit.point, upDirection) - Vector2.Dot(globalRayCastPoint, upDirection));
		//	float overExtention = springHeight - hitLength;

		//	//spring force
		//	controller.RB.AddForce(upDirection * springConstant * overExtention);
		//	//damping
		//	controller.RB.AddForce(upDirection * Vector3.Dot(controller.RB.velocity, upDirection) * -springDamping);
		//}

		//spring stuff
		if (isGrounded && groundDistance < springHeight && state != MovementState.JUMPING)
		{
			float overExtention = springHeight - groundDistance;

			//spring force
			forcesAcceleration += Time.deltaTime * (upDirection * springConstant * overExtention);
			//damping
			forcesAcceleration += Time.deltaTime * (upDirection * Vector3.Dot(controller.RB.velocity, upDirection) * -springDamping);
		}
	}

	Vector2 GetMoveStep(Vector2 current, Vector2 target, float stepMagnitude)
	{
		if ((target - current).sqrMagnitude < stepMagnitude)
		{
			return target - current;
		}
		else
		{
			return (target - current).normalized * stepMagnitude;
		}
	}

	//gravity wells try to catch the players attention
	public void TrySetWell(GravityWell well)
	{
		if (well)
		{
			if (!currentGravityWell
				|| ((currentGravityWell.transform.position - transform.position).sqrMagnitude <
				(well.transform.position - transform.position).sqrMagnitude))
			{
				currentGravityWell = well;
			}
		}
	}

	#region Properties
	public bool IsGrounded{ get { return isGrounded; } }
	public Vector2 UpDirection { get { return upDirection; } }
	public MovementState State { get { return state; } }
	public Vector2 TotalVelocity { get { return controller.RB.velocity; } }
	public Vector2 TargetVelocity { get { return targetVelocity; } }
	public Vector2 MovementAcceleration { get { return movementAcceleration; } }
	public Vector2 ForcesAcceleration { get { return forcesAcceleration; } }
	public Vector2 GroundNormal { get { return groundNormal; } }
	#endregion
}
