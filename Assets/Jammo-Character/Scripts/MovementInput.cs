using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class MovementInput : MonoBehaviour
{
    // Components
    private Animator anim;
    private Camera cam;
    private CharacterController controller;
    private PlayerInput input;
    private SpeedBooster speedBooster;

    [Header("Settings")]
    [SerializeField] float movementSpeed;
    float speedMultiplier = 1;
    [SerializeField] float rotationSpeed = 0.1f;
    [SerializeField] float fallSpeed = .2f;

    [Header("Booleans")]
    [SerializeField] bool blockRotationPlayer;
    [SerializeField] private bool isGrounded, isSliding = false;
    bool chargeDash, isDashing = false;
    [SerializeField] private bool desiredJump, desiredSlide, desiredDash = false;
    private bool isBoosting => speedBooster.isActive();
    private bool canDash => speedBooster.isFullyCharged();


    public Vector2 moveInput, dashVector;
    public float characterVelocity;
    public float gravity = -12;
    public float jumpHeight = 9;
    private float direction = 1;
    private float verticalVel;
    private WaitForSeconds slideDelay = new WaitForSeconds(.5f);



    void Start()
    {
        cam = Camera.main;
        anim = this.GetComponent<Animator>();
        controller = this.GetComponent<CharacterController>();
        speedBooster = GetComponent<SpeedBooster>();
        input = GetComponent<PlayerInput>();
    }

    void Update()
    {
        //Input
        moveInput = input.actions["Move"].ReadValue<Vector2>();
        desiredJump |= input.actions["Jump"].WasPressedThisFrame();
        desiredSlide |= input.actions["Slide"].WasPressedThisFrame();
        desiredDash |= input.actions["Dash"].WasPressedThisFrame();

        //Movement
        CheckGrounded();
        Move();
        if (desiredJump)
        {
            desiredJump = false;
            Jump();
        }
        if (desiredSlide)
        {
            desiredSlide = false;
            Slide();
        }
        if (desiredDash)
        {
            desiredDash = false;
            Dash();
        }

        //Animations
        anim.SetFloat("InputMagnitude", Mathf.Abs(moveInput.normalized.x * characterVelocity) + (isBoosting ? 1 : 0), .05f, Time.deltaTime);
        anim.SetBool("isGrounded", isGrounded);

    }

    void CheckGrounded()
    {
        isGrounded = controller.isGrounded;
    }
    bool CheckUpperContact()
    {
        return false;
        return (Physics.Raycast(transform.position, Vector3.up, 3));
    }

    void Move()
    {
        if (isDashing)
        {
            controller.Move(dashVector * movementSpeed * 5 * Time.deltaTime);
            return;
        }
        //Horizontal
        bool isMoving = moveInput.x != 0;
        if (isMoving && !isSliding)
            direction = moveInput.x > 0 ? 1 : -1;
        float speed = movementSpeed * (isBoosting ? 2 : 1);

        if ((!isMoving && !isSliding) || chargeDash)
            speed = 0;

        controller.Move(Vector3.right * direction * speed * Time.deltaTime);

        characterVelocity = controller.velocity.normalized.x;

        //Vertical
        verticalVel = isGrounded ? gravity * Time.deltaTime : verticalVel + gravity * Time.deltaTime;
        controller.Move(Vector3.up * verticalVel * Time.deltaTime);

        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(Vector3.right * direction), rotationSpeed);

    }
    void Jump()
    {
        if (!isGrounded)
            return;

        verticalVel = Mathf.Sqrt(jumpHeight * -2f * gravity);
        anim.SetTrigger("Jump");
    }
    void Slide()
    {
        if (moveInput.x == 0 || isSliding)
            return;

        anim.SetTrigger("Slide");
        StartCoroutine(SlideCoroutine());
        IEnumerator SlideCoroutine()
        {
            SetSlide(true);
            yield return slideDelay;
            yield return new WaitUntil(() => CheckUpperContact());
            SetSlide(false);
        }
        void SetSlide(bool slide)
        {
            isSliding = slide;
            controller.height = slide ? 0 : 1.8f;
            controller.center = Vector3.up * (slide ? 0.38f : 1);
        }
    }

    void Dash()
    {
        if (!canDash || isDashing)
            return;

        float dashTime = 1;
        StartCoroutine(DashCoroutine());
        IEnumerator DashCoroutine()
        {
            chargeDash = true;
            yield return new WaitForSeconds(1f);
            dashVector = moveInput.normalized;

            isDashing = true;
            float t = 0;
            while (t < dashTime)
            {
                t += Time.deltaTime;
                yield return new WaitForEndOfFrame();
            }
            isDashing = false;
            chargeDash = false;

        }
    }

    private void OnDisable()
    {
        anim.SetFloat("InputMagnitude", 0);
    }
}
