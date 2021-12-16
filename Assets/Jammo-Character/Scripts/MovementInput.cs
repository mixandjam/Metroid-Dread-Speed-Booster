using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class MovementInput : MonoBehaviour
{
	private Animator anim;
	private Camera cam;
	private CharacterController controller;
	private PlayerInput input;

	private Vector3 desiredMoveDirection;
	private Vector3 moveVector;

	public Vector2 moveAxis;
	private float verticalVel;

	[Header("Settings")]
	[SerializeField] float movementSpeed;
	float speedMultiplier = 1;
	[SerializeField] float rotationSpeed = 0.1f;
	[SerializeField] float fallSpeed = .2f;
	public float acceleration = 1;

	[Header("Booleans")]
	[SerializeField] bool blockRotationPlayer;
	[SerializeField] private bool isGrounded;
	[SerializeField] private bool isSliding;
	[SerializeField] private bool isJumpPressed;


	public float characterSpeed;
	private SpeedBooster speedBooster;
	public float gravity = -12;
	public float jumpHeight = 9;


	void Start()
	{
		anim = this.GetComponent<Animator>();
		cam = Camera.main;
		controller = this.GetComponent<CharacterController>();
		speedBooster = GetComponent<SpeedBooster>();
		input = GetComponent<PlayerInput>();


		input.actions["Jump"].started += Jumpcallback;
		input.actions["Jump"].canceled += Jumpcallback;
	}

	void Update()
	{

		if (isSliding)
		{
			controller.Move(new Vector3((movementSpeed * speedMultiplier) * (characterSpeed > 0 ? 1 : -1), 0, 0) * Time.deltaTime);
			return;
		}

		float inputMagnitude = new Vector2(moveAxis.x, 0).sqrMagnitude;

		if (inputMagnitude > 0.1f)
		{
			controller.Move(new Vector3((movementSpeed * speedMultiplier) * (moveAxis.x > 0 ? 1 : -1), 0, 0) * Time.deltaTime);
			transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(new Vector3(moveAxis.x, 0, 0)), rotationSpeed * acceleration);
			anim.SetFloat("InputMagnitude", inputMagnitude + (speedBooster.isActive() ? 2 : 0), .05f, Time.deltaTime);
		}
		else
		{
			anim.SetFloat("InputMagnitude", 0, .05f, Time.deltaTime);
		}

		characterSpeed = controller.velocity.normalized.x;
		isGrounded = controller.isGrounded;

		if (isGrounded)
		{
			verticalVel = gravity * Time.deltaTime;
			if (isJumpPressed)
			{
				verticalVel = Mathf.Sqrt(jumpHeight * -2f * gravity);
				anim.SetTrigger("Jump");
			}
		}
		else
		{
			verticalVel -= Math.Abs(gravity) * Time.deltaTime;

		}

		moveVector = new Vector3(0, verticalVel, 0);
		controller.Move(moveVector * Time.deltaTime);

		anim.SetBool("isGrounded", isGrounded);

	}

	#region Input

	public void OnMove(InputValue value)
	{
		moveAxis = value.Get<Vector2>();
	}

	void OnSlide()
	{

		if (isSliding)
			return;

		float inputMagnitude = new Vector2(moveAxis.x, 0).sqrMagnitude;

		if (inputMagnitude < 0.1f)
			return;

		anim.SetTrigger("Slide");
		StartCoroutine(SlideCoroutine());

		IEnumerator SlideCoroutine()
		{
			isSliding = true;
			controller.height = 0;
			controller.center = new Vector3(0, 0.38f, 0);
			yield return new WaitForSeconds(.5f);
			isSliding = false;
			controller.height = 1.8f;
			controller.center = new Vector3(0, 1, 0);
		}
	}

	private void Jumpcallback(InputAction.CallbackContext context)
	{
		isJumpPressed = context.ReadValueAsButton();
	}

	#endregion

	private void OnDisable()
	{
		anim.SetFloat("InputMagnitude", 0);
	}

	public void SetSpeedMultiplier(float value)
	{
		speedMultiplier = value;
	}
}
