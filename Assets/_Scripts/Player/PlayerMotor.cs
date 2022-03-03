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
	//Friction
	[SerializeField] float groundFriction = 0.4f; //The amount of velocity removed every second on the ground
	[SerializeField] float airFriction = 0.3f; //The amount of velocity removed every second in the air
	//Jumping
	[SerializeField] float jumpImpulse = 4; //The impulse applied when jumping
	[SerializeField] float jumpDuration = 0.3f; //How long jump impulse will be applied for
	[SerializeField] int airJumps = 0; //How many air jumps the player can take
	[SerializeField] float jumpBufferTime = 0.1f; //The length of time when not touching the ground that jump input will be saved
	[SerializeField] float jumpCoyoteTime = 0.1f; //The amount of time after leaving the ground that jump input will be acted on
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
	//GravityWell currentGravityWell;
	Vector2 upDirection = Vector2.up;
	//Velocity
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
		BoxCollider2D collider = GetComponentInChildren<BoxCollider2D>();
		rayCastPoint = new Vector2(0, -0.5f * collider.size.y);

		springRestHeight = totalPlayerHeight - collider.size.y;
		springConstant = -controller.RB.mass * Vector2.Dot(upDirection, Physics2D.gravity) / (springHeight - springRestHeight);
	}

	private void OnValidate()
	{
		if (controller && controller.RB)
			springConstant = -controller.RB.mass * Vector2.Dot(upDirection, Physics2D.gravity) / (springHeight - springRestHeight);
	}

	void FixedUpdate()
	{
		if (controller.EvaluateJumpPressed())
		{
			controller.RB.AddForce(upDirection * jumpImpulse, ForceMode2D.Impulse);
		}

		if (controller.HorizontalInput != 0)
		{
			controller.RB.AddForce(new Vector2(upDirection.y, -upDirection.x) * controller.HorizontalInput * targetSpeed);
		}

		Vector2 globalRayCastPoint = transform.TransformPoint(rayCastPoint);
		RaycastHit2D hit = Physics2D.CircleCast(globalRayCastPoint, groundDetectionRadius, -upDirection, springHeight - groundDetectionRadius, groundLayerMask.value);
		if (hit)
		{
			float hitLength = Mathf.Abs(Vector2.Dot(hit.point, upDirection) - Vector2.Dot(globalRayCastPoint, upDirection));
			float overExtention = springHeight - hitLength;

			//spring force
			controller.RB.AddForce(upDirection * springConstant * overExtention);
			//damping
			controller.RB.AddForce(upDirection * Vector3.Dot(controller.RB.velocity, upDirection) * -springDamping);
		}
	}

	void Run()
	{
		ScanForGround();
		EvaluateJump();
		UpdateMovementVector();
		UpdateForcesVector();

		//move player
		controller.RB.velocity += movementVelocity + forcesVelocity; //might need to use a ground offset to keep player grounded when going down slopes

		//set timers
		jumpTimer -= Time.deltaTime;
		jumpCoyoteTimer -= Time.deltaTime;
		jumpBufferTimer -= Time.deltaTime;
	}
	
	void ScanForGround()
	{
		//scan starts at bottom of collider
		Vector2 scanOrigin = transform.TransformPoint(rayCastPoint);

	}

	void EvaluateJump()
	{

	}

	void UpdateMovementVector()
	{

	}

	void UpdateForcesVector()
	{
		//spring stuff
		//drag
	}


	#region Properties
	public Vector2 UpDirection { get { return upDirection; } }
	public MovementState State { get { return state; } }
	public Vector2 TotalVelocity { get { return forcesVelocity + movementVelocity; } }
	public Vector2 TargetVelocity { get { return targetVelocity; } }
	public Vector2 MovementVelocity { get { return movementVelocity; } }
	public Vector2 ForcesVelocity { get { return forcesVelocity; } }
	public Vector2 GroundNormal { get { return groundNormal; } }
	#endregion
}
