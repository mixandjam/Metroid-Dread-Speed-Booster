using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class MovementInput : MonoBehaviour
{
	private Animator anim;
	private Camera cam;
	private CharacterController controller;

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
	private bool isGrounded;

	public float characterSpeed;
	private SpeedBooster speedBooster;

	void Start()
	{
		anim = this.GetComponent<Animator>();
		cam = Camera.main;
		controller = this.GetComponent<CharacterController>();
		speedBooster = GetComponent<SpeedBooster>();
	}

	void Update()
	{
		InputMagnitude();

		characterSpeed = controller.velocity.normalized.x;


		isGrounded = controller.isGrounded;

		if (isGrounded)
			verticalVel -= 0;
		else
			verticalVel -= 1;

		moveVector = new Vector3(0, verticalVel * fallSpeed * Time.deltaTime, 0);
		controller.Move(moveVector);
	}

	void PlayerMoveAndRotation()
	{

		transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(new Vector3(moveAxis.x, 0, 0)), rotationSpeed * acceleration);
		controller.Move(new Vector3((movementSpeed * speedMultiplier) * (moveAxis.x > 0 ? 1 : -1), 0,0) * Time.deltaTime * (acceleration));

	}

	public void LookAt(Vector3 pos)
	{
		transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(pos), rotationSpeed);
	}

	public void RotateToCamera(Transform t)
	{
		var forward = cam.transform.forward;

		desiredMoveDirection = forward;
		Quaternion lookAtRotation = Quaternion.LookRotation(desiredMoveDirection);
		Quaternion lookAtRotationOnly_Y = Quaternion.Euler(transform.rotation.eulerAngles.x, lookAtRotation.eulerAngles.y, transform.rotation.eulerAngles.z);

		t.rotation = Quaternion.Slerp(transform.rotation, lookAtRotationOnly_Y, rotationSpeed);
	}

	void InputMagnitude()
	{
		//Calculate the Input Magnitude
		float inputMagnitude = new Vector2(moveAxis.x,0).sqrMagnitude;

		//Physically move player
		if (inputMagnitude > 0.1f)
		{
			anim.SetFloat("InputMagnitude", (inputMagnitude * acceleration) + (speedBooster.isActive() ? 1 : 0), .05f, Time.deltaTime);
			PlayerMoveAndRotation();
		}
		else
		{
			anim.SetFloat("InputMagnitude", 0, .05f,Time.deltaTime);
		}
	}

	#region Input

	public void OnMove(InputValue value)
	{
		moveAxis.x = value.Get<Vector2>().x;
		moveAxis.y = value.Get<Vector2>().y;
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
