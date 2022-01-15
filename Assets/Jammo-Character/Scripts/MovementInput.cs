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
    [SerializeField] public bool canMove = true;
    [SerializeField] public bool isGrounded;
    [SerializeField] public bool isSliding = false;
    [SerializeField] public bool chargeDash, isDashing = false;
    [SerializeField] private bool wallJumped;
    [SerializeField] public bool dashBreak;
    private bool desiredJump, desiredSlide, desiredDash = false;
    private bool tryingJump;
    private bool continuingBoost;

    private bool isBoosting => speedBooster.isActive();
    private bool canDash => speedBooster.isEnergyStored();

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

        //Animations
        float velocity = CheckForwardContact() ? 0 : characterVelocity;
        anim.SetFloat("InputMagnitude", Mathf.Abs(moveInput.normalized.x * velocity) + (isBoosting ? 2 : 0), .02f, Time.deltaTime);
        anim.SetBool("isGrounded", isGrounded);
        anim.SetBool("canMove", canMove);

        if (chargeDash)
        {
            //Rotation
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(moveInput.x != 0 ? Vector3.right * moveInput.normalized.x : Vector3.right * direction), rotationSpeed/2);
        }

    }
    void CheckDirection()
    {

        if (continuingBoost)
            return;

        if (direction != storedDirection || characterVelocity == 0 || moveInput.x == 0 || CheckForwardContact())
        {
            if (!wallJumped && isGrounded)
            {
                if (isBoosting && !TouchingWall())
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

                speedBooster.StopAll(!TouchingWall());
            }
            storedDirection = direction;
        }
    }

    bool TouchingWall()
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

        if (isDashing)
        {
            controller.Move(dashVector * movementSpeed * 5 * Time.deltaTime);
            return;
        }

        if (!canMove)
        {
            return;
        }

        if (speedBreak)
        {
            controller.Move(Vector3.right * breakDirection * breakSpeed * Time.deltaTime);
            verticalVel = isGrounded ? gravity * Time.deltaTime : verticalVel + gravity * Time.deltaTime;
            controller.Move(Vector3.up * verticalVel * Time.deltaTime);
            return;
        }

        //Horizontal

        //Store temporaty direction
        float tempDirection = characterVelocity;

        //Set current direction
        bool isMoving = (wallJumped) ? true : moveInput.x != 0;

        //Direction Change
        if (isMoving && !isSliding)
        {
            if (!isGrounded && speedBooster.isActive())
                direction = direction;
            else
                direction = (moveInput.x > 0 ? 1 : -1);
        }
        float speed = movementSpeed * (isBoosting ? 2 : 1);

        //Disable speed if Sliding or Dashing
        if ((!isMoving && !isSliding) || chargeDash)
            speed = 0;

        //To move character automatically while in speed booster & jumping
        if(!isGrounded && speedBooster.isActive())
        {
            speed = movementSpeed * (isBoosting ? 2 : 1);
        }

        //Set final direction
        direction = wallJumped ? tempDirection : direction;

        controller.Move(Vector3.right * direction * speed * Time.deltaTime);

        characterVelocity = !CheckForwardContact() ? (wallJumped ? characterVelocity : controller.velocity.normalized.x) : characterVelocity;


        //Vertical
        verticalVel = isGrounded ? (gravity*20) * Time.deltaTime : verticalVel + gravity * Time.deltaTime;

        controller.Move(Vector3.up * verticalVel * Time.deltaTime);

        //Rotation
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(Vector3.right * direction), rotationSpeed);

    }
    void Jump()
    {
        bool wasGrounded;
        wasGrounded = isGrounded;

        if (!canMove || speedBreak)
            return;

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

            if (CheckForwardContact() && characterVelocity != 0 && !wasGrounded)
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
        if (moveInput.x == 0 || isSliding || !canMove)
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
            controller.height = slide ? 0 : 1.3f;
            controller.center = Vector3.up * (slide ? 0.38f : .72f);
            anim.SetBool("isSliding", slide);
        }
    }

    public void SetDashVector()
    {
        //InputDirection - Vector2 which you get from your typical joystick
        float angle = Mathf.Atan2(moveInput.x, moveInput.y) * Mathf.Rad2Deg;
        angle = Mathf.Round(angle / 45.0f) * 45.0f;

        int animationImpactSide = 0;
        float animationDashAngle = 0;

        direction = (moveInput.x > 0 ? 1 : -1);

        switch (angle)
        {
            default: dashVector = Vector2.up; animationDashAngle = 1; break; // UP
            case -180: dashVector = -Vector2.up;  storedDirection = direction ; animationImpactSide = 2; animationDashAngle = 2; break; // DOWN
            case 180: dashVector = -Vector2.up;  storedDirection = direction ; animationImpactSide = 2; animationDashAngle = 2; break; // DOWN
            case 90: dashVector = new Vector2(1,.001f); direction = 1; storedDirection = 1; animationImpactSide = 1; animationDashAngle = 0; break; //RIGHT
            case -90: dashVector = new Vector2(-1, .001f); direction = -1; storedDirection = -1; animationImpactSide = 1; animationDashAngle = 0; break; // LEFT
            case 135: dashVector = new Vector2(.7f, -.7f); direction = 1; storedDirection = 1; animationImpactSide = 2; animationDashAngle = 4; break; // DIAG RIGHT DOWN
            case -135: dashVector = new Vector2(-.7f, -.7f); direction = -1; storedDirection = -1; animationImpactSide = 2; animationDashAngle = 4; break; // DIAG DOWN LEFT
            case -45: dashVector = new Vector2(-.7f,.7f); direction = -1; storedDirection = -1; animationDashAngle = 3; break; // DIAG LEFT UP
            case 45: dashVector = new Vector2(.7f,.7f); ; direction = 1; storedDirection = 1; animationDashAngle = 3; break; // DIAG RIGHT UP
        }

        anim.SetFloat("DashAngle", animationDashAngle);
        anim.SetInteger("ImpactSide", animationImpactSide);
        //dashVector = moveInput == Vector2.zero ? Vector2.up : moveInput.normalized;
    }

    private void OnDrawGizmos()
    {
        //Upper Contact Check
        Gizmos.color = CheckUpperContact() ? Color.yellow : Color.red;
        Gizmos.DrawRay(transform.position + (transform.up * .5f), Vector3.up * 1.5f);

        //Extra Raycast Check
        Gizmos.color = Color.black;
        Gizmos.DrawRay(transform.position + (transform.up * .3f) + (transform.forward * .3f), Vector3.down * .5f);


        //Grounded Check
        Gizmos.color = isGrounded ? Color.yellow : Color.blue;
        Gizmos.DrawRay(transform.position + (transform.up * .05f), Vector3.down * .2f);

        //Forward Contact Check
        Gizmos.color = CheckForwardContact() ? Color.yellow : Color.green;
        Gizmos.DrawRay(transform.position + (transform.up * .7f), transform.forward * .5f);
    }

    //Character Controller collision
    void OnControllerColliderHit(ControllerColliderHit hit)
    {

        if (isDashing)
        {
            RaycastHit info;

            //Raycast to check the ground collision normal angle, if that angle is different than 180 the speedboster returns
            if (Physics.Raycast(transform.position + (transform.up * .3f) + (transform.forward *.3f), Vector3.down, out info, .5f, groundLayerMask))
            {
                if (Vector3.Angle(info.normal, Vector3.down) != 180)
                {
                    ContinueRunFromShinespark();
                    return;
                }
            }

            StartCoroutine(ImpactCooldownCoroutine());
            dashBreak = true;
            speedBooster.StoreEnergy(false, true, true);
            anim.SetBool("Impact",true);
        }

        IEnumerator ImpactCooldownCoroutine()
        {
            //reset gravity
            verticalVel = gravity * Time.deltaTime;
            yield return new WaitForSeconds(1f);
            canMove = true;
            anim.SetBool("Impact", false);
        }
    }

    void ContinueRunFromShinespark()
    {
        StartCoroutine(ContinueRunCoroutine());

        speedBooster.storedEnergy = false;
        speedBooster.activeShineSpark = false;
        anim.SetTrigger("ContinueRun");
        print("continue");
        isDashing = false;
        canMove = true;
        speedBooster.SpeedBoost(true);

        IEnumerator ContinueRunCoroutine()
        {
            continuingBoost = true;
            yield return new WaitForSeconds(.1f);
            continuingBoost = false;
        }
    }
}
