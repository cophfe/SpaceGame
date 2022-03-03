using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
	//

	//Input 
	public float HorizontalInput {  get; private set; }
	bool jumpPressed;

	//Properties
	public PlayerMotor Motor { get; private set; }
	public PlayerAnimator Animator { get; private set; }
	public Rigidbody2D RB { get; private set; }

	private void Awake()
	{
		Motor = GetComponentInChildren<PlayerMotor>();
		Animator = GetComponentInChildren<PlayerAnimator>();
		RB = GetComponentInChildren<Rigidbody2D>();
	}



	#region Input

	//Input Callback
	public void OnMove(InputAction.CallbackContext ctx)
	{
		if (ctx.performed)
		{
			HorizontalInput = ctx.ReadValue<float>();
		}
		else if (ctx.canceled)
		{
			HorizontalInput = 0;
		}
	}

	public void OnJump(InputAction.CallbackContext ctx)
	{
		if (ctx.performed)
		{
			jumpPressed = true;
		}
	}

	//Evaluate Input
	public bool EvaluateJumpPressed()
	{
		bool cache = jumpPressed;
		jumpPressed = false;

		return cache;
	}
	#endregion
}
