using System;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    #region Component Variables 

    private Rigidbody2D playerRigidBody;
    private Collider2D playerCollider;
    private Animator playerAnimator;

    #endregion Component Variables

    #region Edtior Variables

    [Header("World")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask wallLayer;

    [Header("Character")]
    [SerializeField] private bool spriteFacesRight = true;

    [Header("Movement")]
    [SerializeField, Range(0, 20)] private float walkingVelocity = 5;
    [SerializeField, Range(0, 20)] private float runningVelocity = 10;
    [SerializeField] private bool allowJumpRunning = true;
    [SerializeField] private bool allowFallRunning = true;
    [SerializeField] private bool mustJumpRunToFallRun = true;
    [SerializeField] private bool allowCrouchSliding = true;
    [SerializeField, Range(0, 5)] private float minRunTimeForSlide = 0.5f;

    [Header("Jumping")]
    [SerializeField, Range(0, 20)] private float walkingJumpVelocity = 10;
    [SerializeField, Range(0, 20)] private float runningJumpVelocity = 10;
    [SerializeField, Range(0, 2)] private float minJumpAirTime = 0.1f;
    [SerializeField, Range(0, 2)] private float walkingMaxJumpAirTime = 0.3f;
    [SerializeField, Range(0, 2)] private float runningMaxJumpAirTime = 0.4f;
    [SerializeField, Range(0, 20)] private float maxFallingVelocity = 12;
    [SerializeField] private uint extraJumps = 0;

    [Header("Wall Grabbing")]
    [SerializeField] private bool allowWallGrabbing = true;
    [SerializeField, Range(0, 20)] private float maxWallGrabbingFallingVelocity = 1;

    [Header("Gravity")]
    [SerializeField, Range(0, 20)] private float gravityNormal = 4;
    [SerializeField, Range(0, 20)] private float gravityJumping = 4;
    [SerializeField, Range(0, 20)] private float gravityFalling = 10;
    [SerializeField, Range(0, 20)] private float gravityWallGrabbing = 10;

    #endregion Edtior Variables

    #region Input Variables

    [HideInInspector] private float inputHorizontalValue;
    [HideInInspector] private bool inputHorizontalLeft;
    [HideInInspector] private bool inputHorizontalRight;
    [HideInInspector] private float inputVerticalValue;
    [HideInInspector] private bool inputVerticalUp;
    [HideInInspector] private bool inputVerticalDown;
    [HideInInspector] private bool inputRunHeldDown;
    [HideInInspector] private bool inputJumpPressed;
    [HideInInspector] private bool inputJumpHeldDown;

    #endregion Input Variables

    #region State Variables

    [HideInInspector] public bool isTouchingGround;
    [HideInInspector] public bool isTouchingWall;

    [HideInInspector] public bool isFacingLeft;
    [HideInInspector] public bool isFacingRight;

    [HideInInspector] public bool isMoving;
    [HideInInspector] public bool isWalking;
    [HideInInspector] public bool isRunning;
    [HideInInspector] public float runTime;
    [HideInInspector] public bool isJumpRunning;
    [HideInInspector] public bool wasJumpRunning;
    [HideInInspector] public bool isFallRunning;

    [HideInInspector] public bool isCrouching;
    [HideInInspector] public bool isCrouchSliding;

    [HideInInspector] public bool isDashing;

    [HideInInspector] public bool isJumping;
    [HideInInspector] public uint jumpsTaken;
    [HideInInspector] public float jumpAirTime;
    [HideInInspector] public bool isFalling;

    [HideInInspector] public bool isWallGrabbing;
    [HideInInspector] public bool isWallGrabbingLeft;
    [HideInInspector] public bool isWallGrabbingRight;

    #endregion State Variables

    #region Unity Events

    private void Awake()
    {
        playerRigidBody = GetComponent<Rigidbody2D>();
        playerCollider = GetComponent<BoxCollider2D>();
        playerAnimator = GetComponent<Animator>();

        isFacingRight = spriteFacesRight;
        isFacingLeft = !isFacingRight;
    }

    private void Start()
    {
        FaceRight();
    }

    private void FixedUpdate()
    {
        UpdateGravity();
    }

    private void Update()
    {
        ReadInputs();

        UpdateState();

        UpdateMovement();
        UpdateAnimation();
    }

    #endregion Unity Events

    #region Helper Methods

    private void ReadInputs()
    {
        inputHorizontalValue = Input.GetAxisRaw("Horizontal");
        inputHorizontalLeft = inputHorizontalValue < -0.01f;
        inputHorizontalRight = inputHorizontalValue > 0.01f;

        inputVerticalValue = Input.GetAxisRaw("Vertical");
        inputVerticalDown = inputVerticalValue < -0.01f;
        inputVerticalUp = inputVerticalValue > 0.01f;

        inputRunHeldDown = Input.GetButton("Run");

        inputJumpPressed = Input.GetButtonDown("Jump");
        inputJumpHeldDown = Input.GetButton("Jump");
    }

    private void UpdateGravity()
    {
        if (isWallGrabbing)
        {
            playerRigidBody.gravityScale = gravityWallGrabbing;
        }
        else if (isJumping)
        {
            playerRigidBody.gravityScale = gravityJumping;
        }
        else if (isFalling)
        {
            playerRigidBody.gravityScale = gravityFalling;
        }
        else
        {
            playerRigidBody.gravityScale = gravityNormal;
        }
    }

    private void UpdateState()
    {
        var groundCollisionDown = Physics2D.BoxCast(playerCollider.bounds.center, playerCollider.bounds.size, 0, Vector2.down, 0.1f, groundLayer);
        isTouchingGround = groundCollisionDown;

        isTouchingWall = Physics2D.BoxCast(playerCollider.bounds.center, playerCollider.bounds.size, 0, isFacingRight ? Vector2.right : Vector2.left, 0.1f, wallLayer)
                         ||
                         Physics2D.BoxCast(playerCollider.bounds.center, playerCollider.bounds.size, 0, isFacingRight ? Vector2.right : Vector2.left, 0.1f, groundLayer);

        isMoving = Math.Abs(playerRigidBody.linearVelocityX) > 0.01f;
        isWalking = isMoving && !isCrouching && Math.Abs(inputHorizontalValue) > 0.01f;
        isRunning = isWalking && !isCrouching && inputRunHeldDown;

        isCrouching = inputVerticalValue < 0;
        isCrouchSliding = allowCrouchSliding && isCrouching && isMoving && (isCrouchSliding || runTime >= minRunTimeForSlide);

        isJumping = playerRigidBody.linearVelocityY > 0.01f;
        isFalling = playerRigidBody.linearVelocityY < -0.01f;

        wasJumpRunning = wasJumpRunning || isJumpRunning;
        isJumpRunning = inputRunHeldDown && isJumping && isJumpRunning;
        isFallRunning = inputRunHeldDown && isFalling && isFallRunning;

        isWallGrabbing = allowWallGrabbing && !isTouchingGround && isTouchingWall;
        isWallGrabbingLeft = isWallGrabbing && isFacingLeft;
        isWallGrabbingRight = isWallGrabbing && isFacingRight;

        runTime = isRunning ? runTime + Time.deltaTime : 0;
        jumpAirTime = isJumping ? jumpAirTime + Time.deltaTime : 0;
    }

    private void UpdateAnimation()
    {
        playerAnimator.SetBool("Grounded", isTouchingGround);

        playerAnimator.SetBool("Crouching", isCrouching);

        playerAnimator.SetBool("Walking", isTouchingGround && !isJumping && !isFalling && isWalking);
        playerAnimator.SetBool("Running", isTouchingGround && !isJumping && !isFalling && isRunning);

        playerAnimator.SetBool("Jumping", isJumping);
        playerAnimator.SetBool("Falling", isFalling);

        playerAnimator.SetBool("WallGrabbing", isWallGrabbing);
    }

    private void UpdateMovement()
    {
        UpdateMovementHorizontal();
        UpdateMovementVertical();
    }

    private void UpdateMovementHorizontal()
    {

        if (!isCrouching || isJumping)
        {
            var isMovingIntoWall = isTouchingWall && ((isFacingRight && inputHorizontalRight) || (isFacingLeft && inputHorizontalLeft));
            if (!isMovingIntoWall)
            {
                MoveHorizontally();
            }
        }

        if (isCrouching && !isCrouchSliding)
        {
            playerRigidBody.linearVelocity = new Vector2(0, playerRigidBody.linearVelocity.y);
        }

        if (inputHorizontalRight)
        {
            FaceRight();
        }
        else if (inputHorizontalLeft)
        {
            FaceLeft();
        }
    }

    private void MoveHorizontally()
    {
        var horizontalVelovity = inputRunHeldDown && (isTouchingGround || isJumpRunning || isFallRunning) ? runningVelocity : walkingVelocity;
        playerRigidBody.linearVelocity = new Vector2(horizontalVelovity * (isCrouchSliding ? Math.Sign(playerRigidBody.linearVelocity.x) : inputHorizontalValue), playerRigidBody.linearVelocity.y);
    }

    private void UpdateMovementVertical()
    {
        if (isTouchingGround)
        {
            isJumping = false;
            isFalling = false;

            jumpsTaken = 0;
        }

        if (inputJumpPressed && (isTouchingGround || jumpsTaken < extraJumps))
        {
            isJumping = true;
            isFalling = false;

            jumpsTaken++;
            jumpAirTime = 0;
        }

        var maxJumpAirTime = isRunning ? runningMaxJumpAirTime : walkingMaxJumpAirTime;
        if ((!inputJumpHeldDown && isJumping && jumpAirTime < minJumpAirTime) || (inputJumpHeldDown && isJumping && jumpAirTime < maxJumpAirTime))
        {
            var verticalVelocity = isRunning && jumpsTaken == 0 ? runningJumpVelocity : walkingJumpVelocity;
            playerRigidBody.linearVelocity = new Vector2(playerRigidBody.linearVelocity.x, verticalVelocity);
        }

        if (isJumping)
        {
            if (inputJumpPressed && inputRunHeldDown && allowJumpRunning)
            {
                isJumpRunning = true;
            }

            isFallRunning = false;
        }

        if (isFalling)
        {
            if (isWallGrabbing)
            {
                playerRigidBody.linearVelocity = new Vector2(playerRigidBody.linearVelocity.x, Math.Clamp(playerRigidBody.linearVelocity.y, -maxWallGrabbingFallingVelocity, 0));
            }
            else
            {
                playerRigidBody.linearVelocity = new Vector2(playerRigidBody.linearVelocity.x, Math.Clamp(playerRigidBody.linearVelocity.y, -maxFallingVelocity, 0));
            }

            if (inputRunHeldDown && allowFallRunning)
            {
                isFallRunning = isFallRunning || (mustJumpRunToFallRun ? wasJumpRunning : true);
            }

            isJumpRunning = false;
        }

        wasJumpRunning = isJumpRunning;
    }

    private void FaceRight()
    {
        if (!isFacingRight)
        {
            transform.Rotate(Vector3.up, 180);

            isFacingRight = true;
            isFacingLeft = false;
        }
    }

    private void FaceLeft()
    {
        if (!isFacingLeft)
        {
            transform.Rotate(Vector3.up, 180);

            isFacingRight = false;
            isFacingLeft = true;
        }
    }

    #endregion Helper Methods
}
