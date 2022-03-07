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
	[SerializeField] float maxWalkAngle = 40f; //The percentage of acceleration used while in the air
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
	[SerializeField] float slopeLimit = 40.0f; //The maximum slope angle in degrees
	[SerializeField] float groundDetectionRadius = 0.8f; //The radius of the ground detection circle
	[SerializeField] float groundDetectAddition = 0f; //The offset to total player height that the player is considered floored for
	[SerializeField] LayerMask groundLayerMask; //Layers that are considered ground
	[Header("Ground Avoidance")]
	//Ground Avoidance
	[SerializeField] float springRestHeight = 0.575f; //The overal player height, including collider height
	[SerializeField] float springHeight = 2.0f; //the hight of the ground avoidance spring
	[SerializeField] float springDamping = 0.3f; //the amount of dampening applied to the spring's force
	[SerializeField] float groundMagnetDistanceModifer = 0.5f; //Affects the distance that will be added to the ground detection distance when the ground magnet is enabled
	[SerializeField] float groundMagnetHeight = 0.5f; //The height that the ground magnet will kick in at
	[SerializeField] float maxAttachAngle = 40;
	[Header("Ground Slope")]
	[SerializeField, Range(0,3)] float slopeGroundFriction = 0.6f;
	[SerializeField, Range(0,3)] float slopeFriction = 0.4f;
	[SerializeField] float slopeGravityMod = 1.0f;
	[Header("Rotation")]
	[SerializeField] float maxRotationSpeed = 1000f;
	[SerializeField] float rotationSmoothSpeed = 0.1f;
	#endregion

	#region Private
	//References
	PlayerController controller; //The player controller (handles input and holds references)
	//Gravity
	GravityWell currentGravityWell;
	Vector2 upDirection = Vector2.up;
	Vector2 rightDirection = Vector2.right; //I use it so much, might as well just cache it
	//Velocity
	Vector2 inputHorizontalDirection = Vector2.right;
	Vector2 targetVelocity; // the target velocity based on player input
	Vector2 movementVelocity; //the part of velocity that tries to get to the target velocity
	Vector2 forcesVelocity; //other parts of velocity (jumping, drag, spring force)
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
	bool enableGroundMagnet = false;
	float groundMagnetOffset;
	Vector2 colliderSize;
	//Other
	MovementState state = MovementState.FALLING;
	//spring system
	float springConstant; //used to calculate the spring force
	Vector2 rayCastPoint;
	//rotation
	float rotationVelocity;
	#endregion

	public enum MovementState
	{
		GROUNDED,
		FALLING,
		RISING,
		JUMPING,
		SLIDING
	}

	void Awake()
    {
		controller = GetComponent<PlayerController>();
    }

	void Start()
	{
		CapsuleCollider2D collider = GetComponentInChildren<CapsuleCollider2D>();
		rayCastPoint = collider.offset + new Vector2(0, (-0.5f * collider.size.y));
		colliderSize = collider.size;
		
		springConstant = -controller.RB.mass * gravity / (springHeight - springRestHeight);
	}

	private void OnValidate()
	{
		if (controller && controller.RB)
		{
			CapsuleCollider2D collider = GetComponentInChildren<CapsuleCollider2D>();
			springConstant = -controller.RB.mass * gravity / (springHeight - springRestHeight);
		}
	}

	void FixedUpdate()
	{
		Run();
	}

	void Run()
	{
		CalculateUpDirection();
		UpdateRotation();
		ScanForGround();
		SetState();
		EvaluateJump();
		UpdateMovementVector();
		UpdateForcesVector();

		//change player velocity
		controller.RB.velocity = movementVelocity + forcesVelocity; //might need to use a ground offset to keep player grounded when going down slopes
		//clamp speed
		controller.RB.velocity = Vector2.ClampMagnitude(controller.RB.velocity, maxVelocity);
		//move player
		controller.RB.position += groundMagnetOffset * UpDirection;
		
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
			upDirection = currentGravityWell.GetUpDirection();
			rightDirection = -Vector2.Perpendicular(upDirection);
		}
	}

	void UpdateRotation()
	{
		//update rotation based on upDirection(go through rigidbody.rotation)
		float targetRotation = Vector2.SignedAngle(Vector2.up, upDirection);
		controller.RB.rotation = Mathf.SmoothDampAngle( controller.RB.rotation, targetRotation, ref rotationVelocity, rotationSmoothSpeed, maxRotationSpeed, Time.deltaTime);
	}

	void ScanForGround()
	{
		//scan goes down
		Vector2 scanDirection = -upDirection;
		//scan starts at bottom of collider
		Vector2 scanOrigin = transform.TransformPoint(rayCastPoint);
		float length = springHeight + groundDetectAddition - groundDetectionRadius;
		
		if (isGrounded) //if player was grounded last frame
		{
			enableGroundMagnet = true;
			length *= (1 + groundMagnetDistanceModifer);
		}	
		
		RaycastHit2D hit = Physics2D.CircleCast(scanOrigin, groundDetectionRadius, scanDirection, length, groundLayerMask.value);
		isGrounded = hit;
		
		if (isGrounded)
		{
			groundPoint = hit.point;
			groundDistance = Vector2.Dot(scanOrigin - groundPoint, UpDirection);
			groundNormal = hit.normal;
			groundAngle = Vector2.Angle(groundNormal, upDirection);
			groundCollider = hit.collider;

			if (enableGroundMagnet && groundAngle < maxAttachAngle) 
			{
				float offset = (groundMagnetHeight - springRestHeight - groundDistance);
				//if the offset would move the player downward
				if (offset < 0)
				{
					//investigate into whether the player would also move downward at their rest hight
					offset = (springRestHeight - groundDistance);
					//if so, the player should attach to a slope
					if (offset < 0)
						groundMagnetOffset = offset;

					//this is effectively two different ground offsets, one for keeping the player above the ground and one for attaching the player to a slope
				}
				else
					groundMagnetOffset = offset;
			}
		}
		else
		{
			groundMagnetOffset = 0;
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
			groundMagnetOffset = 0;
			//set up component to jumpImpulse
			forcesVelocity -= upDirection * Vector2.Dot(controller.RB.velocity, upDirection);
			forcesVelocity += upDirection * jumpImpulse; 

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
		groundMagnetOffset = 0;
		state = MovementState.JUMPING;
		jumpTimer = jumpDuration;
	}

	void OnLeaveGround()
	{
		groundMagnetOffset = 0;
		jumpCoyoteTimer = jumpCoyoteTime;
		groundCollider = null;
	}

	void SetState()
	{
		bool movingUp = !isGrounded && Vector3.Dot(upDirection, forcesVelocity) > 0.01f;
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
					state = MovementState.SLIDING;
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
						state = MovementState.SLIDING;
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
							state = MovementState.SLIDING;
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
			case MovementState.SLIDING:
				if (movingUp)
				{
					state = MovementState.RISING;
				}
				else if (isGrounded)
				{
					if (!groundTooSteep)
					{
						state = MovementState.GROUNDED;
						OnLand();
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
			int inputDir = GetInputDir();

			//the maximum up angle is maxWalkAngle
			if (Vector2.SignedAngle(System.Math.Sign(Vector2.Dot(rightDirection, movementVelocity)) * groundNormal, upDirection) <0)
			{
				tangent = Vector3.RotateTowards(-rightDirection, tangent, maxWalkAngle * Mathf.Deg2Rad, 0);
			}
			inputHorizontalDirection = tangent;

			targetVelocity = inputDir * tangent * -targetSpeed;

			float currentAcceleration = acceleration;

			//make it turn around fast
			if (Vector2.Dot(targetVelocity, controller.RB.velocity) < 0)
			{
				currentAcceleration *= 2;
			}

			movementVelocity = Vector2.MoveTowards(movementVelocity, targetVelocity, currentAcceleration * Time.deltaTime);
			//friction applied 
			movementVelocity += GetMoveStep(movementVelocity, Vector3.zero, groundFriction * Time.deltaTime);
		}
		else
		{
			if (state == MovementState.SLIDING)
			{
				movementVelocity = Vector2.MoveTowards(movementVelocity, Vector2.zero, acceleration * Time.deltaTime);
				//friction applied 
				movementVelocity += GetMoveStep(movementVelocity, Vector3.zero, groundFriction * Time.deltaTime);
			}
			else
			{
				if (controller.HorizontalInput != 0)
				{
					Vector2 tangent = Vector2.Perpendicular(upDirection);
					targetVelocity = GetInputDir() * tangent * -targetSpeed;

					movementVelocity = Vector2.MoveTowards(movementVelocity, targetVelocity, acceleration * airControlModifier * Time.deltaTime);
				}

				//air friction applied
				movementVelocity += GetMoveStep(movementVelocity, Vector3.zero, airFriction * Time.deltaTime);
			}
		}

		int GetInputDir()
		{
			return controller.HorizontalInput == 0 ? 0 : System.Math.Sign(controller.HorizontalInput);
		}

		movementVelocity = Vector2.ClampMagnitude(movementVelocity, maxVelocity);
	}

	void UpdateForcesVector()
	{
		Vector2 groundTangent = Vector2.Perpendicular(groundNormal);
		//gravity
		if (state == MovementState.SLIDING)
		{
			forcesVelocity += slopeGravityMod * Vector2.Dot(groundTangent, upDirection * (currentGravityWell ? currentGravityWell.GetGravity(gravity) : gravity) * Time.deltaTime) * groundTangent;
		}
		else
		{
			forcesVelocity += upDirection * (currentGravityWell ? currentGravityWell.GetGravity(gravity) : gravity) * Time.deltaTime;
		}

		//this will only affect forces from sliding, so it should be strong
		float frictionConst = state == MovementState.GROUNDED ? slopeGroundFriction : slopeFriction;
		Vector2 friction = -(rightDirection * Vector2.Dot(rightDirection, forcesVelocity)) * frictionConst * Time.deltaTime;
		forcesVelocity += friction;

		//spring stuff
		//ground distance along spring normal
		float springDist = Vector2.Dot(UpDirection, transform.TransformPoint(rayCastPoint)) - Vector2.Dot(UpDirection, groundPoint);

		if (state == MovementState.SLIDING)//&& Vector2.Dot(TotalVelocity, upDirection) < 0) //if sliding and going downwards, ground magnet replaces spring (spring is too unstable)
		{
			//groundMagnetOffset = (springRestHeight - groundDistance);
		}
		else if (isGrounded && springDist < springHeight)
		{
			float overExtention = springHeight - springDist;

			float currentSpringConst = springConstant;
			//if (state == MovementState.SLIDING)
			//	currentSpringConst *= Mathf.Max(0, Vector2.Dot(upDirection, groundNormal));

			//spring force
			forcesVelocity += Time.deltaTime * (upDirection * currentSpringConst * overExtention);
			//damping
			forcesVelocity += Time.deltaTime * (upDirection * Vector3.Dot(forcesVelocity, upDirection) * -springDamping);
		}

		forcesVelocity = Vector2.ClampMagnitude(forcesVelocity, maxVelocity);
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

	//gravity wells try to catch the players attention if they are close enough
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

	private void OnCollisionStay2D(Collision2D collision)
	{
		OnCollision(collision);
	}
	private void OnCollisionEnter2D(Collision2D collision)
	{
		OnCollision(collision);
	}
	private void OnCollisionExit2D(Collision2D collision)
	{
		OnCollision(collision);
	}

	void OnCollision (Collision2D collision)
	{
		//this is a terrible solution

		//first take velocity from ground movement and put it into movement velocity 
		if (isGrounded)
		{
			movementVelocity = Vector2.Dot(controller.RB.velocity, inputHorizontalDirection) * inputHorizontalDirection;
		}
		else
		{
			movementVelocity = Vector2.zero;
		}

		//than put gravity forces to forcesVelocity
		forcesVelocity = Vector2.Dot(controller.RB.velocity, UpDirection) * UpDirection;

		//then put the leftovers in movement velocity
		movementVelocity += controller.RB.velocity - forcesVelocity - movementVelocity;

		//this is what happens when the two velocities are mushed together but need to be treated differently :(
	}

	#region Properties
	public bool IsGrounded{ get { return isGrounded; } }
	public Vector2 UpDirection { get { return upDirection; } }
	public Vector2 RightDirection { get { return rightDirection; } }
	public MovementState State { get { return state; } }
	public Vector2 TotalVelocity { get { return movementVelocity + forcesVelocity; } }
	public Vector2 TargetVelocity { get { return targetVelocity; } }
	public Vector2 MovementVelocity { get { return movementVelocity; } }
	public Vector2 ForcesVelocity { get { return forcesVelocity; } }
	public Vector2 GroundNormal { get { return groundNormal; } }
	#endregion

	private void OnDrawGizmos()
	{
		Gizmos.color = Color.blue;
		Gizmos.DrawRay(groundPoint, groundNormal);
		Gizmos.color = new Color(1,0,0, 0.5f);
		GizmosDrawSphere(groundPoint + groundNormal * groundDetectionRadius, groundDetectionRadius);
		Gizmos.color = Color.red;
		Gizmos.DrawRay(groundPoint, Vector2.Perpendicular(Vector2.Perpendicular(groundNormal)));

	}

	void GizmosDrawSphere(Vector2 pos, float radius, int pointCount = 32)
	{
		Vector2 lastRotation = Vector2.up * radius;
		Quaternion rotation = Quaternion.Euler(0, 0, 360 / (float)pointCount);

		for (int i = 0; i < pointCount; i++)
		{
			Vector2 newRotation = rotation * lastRotation;
			Gizmos.DrawLine(pos + lastRotation, pos + newRotation);
			lastRotation = newRotation;

		}
	}
}
