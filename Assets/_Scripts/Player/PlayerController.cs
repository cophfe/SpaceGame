using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
	//Input 
	public float HorizontalInput {  get; private set; }
	public Vector2 LookPosition {  get; private set; }
	bool jumpPressed;
	bool jumpCancelled;
	bool actionButtonPressed;
	bool actionButtonReleased;

	//Properties
	public PlayerMotor Motor { get; private set; }
	public PlayerAnimator Animator { get; private set; }
	public PlayerAction Action { get; private set; }
	public Rigidbody2D RB { get; private set; }
	public Vector2 PlayerPosition { get { return transform.position; } }

	private void Awake()
	{
		Motor = GetComponentInChildren<PlayerMotor>();
		Animator = GetComponentInChildren<PlayerAnimator>();
		Action = GetComponentInChildren<PlayerAction>();
		RB = GetComponentInChildren<Rigidbody2D>();

		if (GameManager.Instance)
			GameManager.Instance.RegisterPlayerController(this);
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

	public void OnLook(InputAction.CallbackContext ctx)
	{
		if (ctx.performed)
		{
			LookPosition = ctx.ReadValue<Vector2>();
		}
	}

	public void OnJump(InputAction.CallbackContext ctx)
	{
		if (ctx.performed)
		{
			jumpPressed = true;
		}
		else if (ctx.canceled)
		{
			jumpCancelled = true;
		}
	}

	public void OnActionPressed(InputAction.CallbackContext ctx)
	{
		if (ctx.performed)
		{
			actionButtonPressed = true;
		}
		else if (ctx.canceled)
		{
			actionButtonReleased = true;
		}
	}

	public void OnScroll(InputAction.CallbackContext ctx)
	{
		if (ctx.performed)
		{
			if (ctx.ReadValue<float>() > 0)
			{
				Action.OnSwitchInventorySlot(-1);
			}
			else
			{
				Action.OnSwitchInventorySlot(1);
			}
		}
	}

	//Evaluate Input
	public bool EvaluateJumpPressed()
	{
		bool cache = jumpPressed;
		jumpPressed = false;

		return cache;
	}

	public bool EvaluateJumpCancelled()
	{
		bool cache = jumpCancelled;
		jumpCancelled = false;

		return cache;
	}

	public bool EvaluateActionPressed()
	{
		bool cache = actionButtonPressed;
		actionButtonPressed = false;

		return cache;
	}
	public bool EvaluateActionReleased()
	{
		bool cache = actionButtonReleased;
		actionButtonReleased = false;

		return cache;
	}
	#endregion
}
