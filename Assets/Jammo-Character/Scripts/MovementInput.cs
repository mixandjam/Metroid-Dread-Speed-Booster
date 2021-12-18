using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;

[RequireComponent(typeof(CharacterController))]
public class MovementInput : MonoBehaviour
{
    // Components
    private Animator anim;
    private CharacterController controller;
    private PlayerInput input;
    private SpeedBooster speedBooster;

    [Header("Settings")]
    [SerializeField] float movementSpeed;
    [SerializeField] float rotationSpeed = 0.1f;
    [SerializeField] float gravity = -12;
    [SerializeField] float jumpHeight = 9;
    [SerializeField] LayerMask groundLayerMask;

    [Header("Booleans")]
    [SerializeField] public bool isGrounded;
    [SerializeField] private bool isSliding = false;
    [SerializeField] bool chargeDash, isDashing = false;
    [SerializeField] private bool wallJumped;
    [SerializeField] private bool dashBreak;
    private bool desiredJump, desiredSlide, desiredDash = false;
    private bool tryingJump;

    private bool isBoosting => speedBooster.isActive();
    private bool canDash => speedBooster.isFullyCharged();

    [Header("Speed Break")]
    [SerializeField] private float breakSpeed;
    [SerializeField] private bool speedBreak = false;
    [SerializeField] private Ease breakEase;
    private float breakDirection;

    [Header("Input")]
    public Vector2 moveInput, dashVector;

    [Header("Character Stats")]
    [SerializeField] float characterVelocity;
    [SerializeField] float direction = 1;
    float storedDirection;
    [SerializeField] float verticalVel;

    private WaitForSeconds slideDelay = new WaitForSeconds(.5f);


    void Start()
    {
        anim = this.GetComponent<Animator>();
        controller = this.GetComponent<CharacterController>();
        speedBooster = GetComponent<SpeedBooster>();
        input = GetComponent<PlayerInput>();

        storedDirection = direction;
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
        CheckDirection();
        Move();
        if (desiredJump)
        {
            desiredJump = false;
            if(!isSliding)
            Jump();
        }
        if (desiredSlide)
        {
            desiredSlide = false;
            if(isGrounded)
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

        //print(Physics.SphereCast((transform.position - transform.forward *.2f) + Vector3.up, .5f, transform.forward, out RaycastHit info, 1, groundLayerMask));

    }
    void CheckDirection()
    {
        if (direction != storedDirection || characterVelocity == 0 || moveInput.x == 0)
        {
            if (!wallJumped)
            {
                if (isBoosting && !touchingWall())
                {
                    breakSpeed = movementSpeed * 2;
                    breakDirection = storedDirection;
                    speedBreak = true;
                    if (isGrounded)
                        anim.SetTrigger("BreakSlide");
                    DOVirtual.Float(movementSpeed * 2, 0, .5f, SetBreakSpeed).SetEase(breakEase);

                    StartCoroutine(BreakCoroutine());
                    IEnumerator BreakCoroutine()
                    {
                        yield return new WaitForSeconds(.9f);
                        speedBreak = false;
                    }
                }

                speedBooster.StopAll(!touchingWall());
            }
            storedDirection = direction;
        }
    }

    bool touchingWall()
    {
        return Physics.SphereCast((transform.position - transform.forward * .2f) + Vector3.up, .5f, transform.forward, out RaycastHit info, 1, groundLayerMask);
    }

    void SetBreakSpeed (float x)
    {
        breakSpeed = x;
    }

    void CheckGrounded()
    {
        isGrounded = tryingJump ? false : (Physics.Raycast(transform.position + (transform.up * .05f), Vector3.down, .2f, groundLayerMask));
    }
    bool CheckUpperContact()
    {
        return (Physics.Raycast(transform.position + (transform.up * .5f), Vector3.up, 1.5f,groundLayerMask));
    }

    bool CheckForwardContact()
    {
        return (Physics.Raycast(transform.position + (transform.up * .7f), transform.forward, .5f, groundLayerMask));
    }

    void Move()
    {

        if (speedBreak)
        {
            controller.Move(Vector3.right * breakDirection * breakSpeed * Time.deltaTime);
            verticalVel = isGrounded ? gravity * Time.deltaTime : verticalVel + gravity * Time.deltaTime;
            controller.Move(Vector3.up * verticalVel * Time.deltaTime);
            return;
        }

        if (isDashing)
        {
            controller.Move(dashVector * movementSpeed * 5 * Time.deltaTime);
            return;
        }

        //Horizontal

        //Store temporaty direction
        float tempDirection = characterVelocity;

        //Set current direction
        bool isMoving = (wallJumped) ? true : moveInput.x != 0;

        if (isMoving && !isSliding)
            direction = (moveInput.x > 0 ? 1 : -1);
        float speed = movementSpeed * (isBoosting ? 2 : 1);

        //Disable speed if Sliding or Dashing
        if ((!isMoving && !isSliding) || chargeDash)
            speed = 0;


        //Set final direction
        direction = wallJumped ? tempDirection : direction;

        controller.Move(Vector3.right * direction * speed * Time.deltaTime);

        characterVelocity = !CheckForwardContact() ? (wallJumped ? characterVelocity : controller.velocity.normalized.x) : characterVelocity;


        //Vertical
        verticalVel = isGrounded ? (gravity*10) * Time.deltaTime : verticalVel + gravity * Time.deltaTime;

        controller.Move(Vector3.up * verticalVel * Time.deltaTime);

        //Rotation
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(Vector3.right * direction), rotationSpeed);

    }
    void Jump()
    {
        if (!isGrounded)
        {
            StartCoroutine(JumpCoroutine());
            return;
        }

        if (!isBoosting)
            speedBooster.StopAll(false);

        isGrounded = false;
        verticalVel = Mathf.Sqrt(jumpHeight * -2f * gravity);
        anim.SetTrigger("Jump");

        StartCoroutine(JumpCoroutine());

        IEnumerator JumpCoroutine()
        {
            tryingJump = true;

            if (CheckForwardContact() && characterVelocity != 0)
            {
                wallJumped = true;
                direction *= -1;
                characterVelocity *= -1;
                verticalVel = Mathf.Sqrt((jumpHeight/2) * -2f * gravity);
            }

            yield return new WaitForSeconds(.05f);
            tryingJump = false;
            yield return new WaitForSeconds(1);
            wallJumped = false;
        }
    }
    void Slide()
    {
        if (moveInput.x == 0 || isSliding)
            return;

        if (!isBoosting)
            speedBooster.StopAll(false);
        
        StartCoroutine(SlideCoroutine());
        IEnumerator SlideCoroutine()
        {
            SetSlide(true);
            anim.SetTrigger("Slide");
            yield return slideDelay;
            yield return new WaitUntil(() => !CheckUpperContact());
            anim.SetBool("isSliding", false);
            yield return new WaitForSeconds(.05f);
            SetSlide(false);
        }
        void SetSlide(bool slide)
        {
            isSliding = slide;
            controller.height = slide ? 0 : 1.8f;
            controller.center = Vector3.up * (slide ? 0.38f : 1);
            anim.SetBool("isSliding", slide);
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
            while (t < .2f)
            {
                t += Time.deltaTime;
                yield return new WaitForEndOfFrame();
            }
            yield return new WaitUntil(() => dashBreak);

            dashBreak = false;
            isDashing = false;
            chargeDash = false;

        }
    }

    private void OnDisable()
    {
        anim.SetFloat("InputMagnitude", 0);
    }

    private void OnDrawGizmos()
    {
        //Upper Contact Check
        Gizmos.color = CheckUpperContact() ? Color.yellow : Color.red;
        Gizmos.DrawRay(transform.position + (transform.up * .5f), Vector3.up * 1.5f);

        //Grounded Check
        Gizmos.color = isGrounded ? Color.yellow : Color.blue;
        Gizmos.DrawRay(transform.position + (transform.up * .05f), Vector3.down * .2f);

        //Forward Contact Check
        Gizmos.color = CheckForwardContact() ? Color.yellow : Color.green;
        Gizmos.DrawRay(transform.position + (transform.up * .7f), transform.forward * .5f);
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (isDashing)
        {
            dashBreak = true;
        }
    }
}
