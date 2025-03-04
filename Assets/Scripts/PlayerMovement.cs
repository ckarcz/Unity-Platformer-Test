using System;
using UnityEditor.MPE;
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
    [SerializeField, Range(0, 1)] private float jumpCoyoteTime = 0.1f;
    [SerializeField, Range(0, 1)] private float jumpBufferTime = 0.1f;

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
    [HideInInspector] public bool isTouchingGroundSide;
    [HideInInspector] public bool isTouchingGroundSideLeft;
    [HideInInspector] public bool isTouchingGroundSideRight;
    [HideInInspector] public bool isTouchingWall;
    [HideInInspector] public bool isTouchingWallLeft;
    [HideInInspector] public bool isTouchingWallRight;

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
    [HideInInspector] public bool isFalling;
    [HideInInspector] public uint jumpsTaken;
    [HideInInspector] public float jumpAirTimeCounter;
    [HideInInspector] public float jumpCoyoteTimeCounter;
    [HideInInspector] public float jumpBufferTimeCounter;

    [HideInInspector] public bool isWallGrabbing;

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
        isTouchingGround = BoxCastDrawer.BoxCastAndDraw(playerCollider.bounds.center, playerCollider.bounds.size, 0, Vector2.down, 0.1f, groundLayer, colorNormal: Color.cyan, colorHit: Color.magenta, draw: true);

        isTouchingGroundSide = BoxCastDrawer.BoxCastAndDraw(playerCollider.bounds.center, playerCollider.bounds.size, 0, isFacingRight ? Vector2.right : Vector2.left, 0.1f, groundLayer, colorNormal: Color.yellow, colorHit: Color.red, draw: false);
        isTouchingGroundSideLeft = isTouchingGroundSide && isFacingLeft;
        isTouchingGroundSideRight = isTouchingGroundSide && isFacingRight;

        isTouchingWall = BoxCastDrawer.BoxCastAndDraw(playerCollider.bounds.center, playerCollider.bounds.size, 0, isFacingRight ? Vector2.right : Vector2.left, 0.1f, wallLayer, colorNormal: Color.yellow, colorHit: Color.red, draw: true);
        isTouchingWallLeft = isTouchingWall && isFacingLeft;
        isTouchingWallRight = isTouchingWall && isFacingRight;

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

        isWallGrabbing = allowWallGrabbing && isTouchingWall && isFalling;

        runTime = isRunning ? runTime + Time.deltaTime : 0;
        jumpAirTimeCounter = isJumping ? jumpAirTimeCounter + Time.deltaTime : 0;
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
            var isMovingIntoWall = (isTouchingWallLeft && inputHorizontalLeft) || (isTouchingWallRight && inputHorizontalRight);
            var isMovingIntoGroundSide = (isTouchingGroundSideLeft && inputHorizontalLeft) || (isTouchingGroundSideRight && inputHorizontalRight);
            if (!isMovingIntoWall && !isMovingIntoGroundSide)
            {
                MoveHorizontally();
            }
        }

        if (isCrouching && !isCrouchSliding)
        {
            // below prevents crouch jumping
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
        if (isTouchingGround && !isJumping && !isFalling)
        {
            isJumping = false;
            isFalling = false;

            jumpsTaken = 0;

            jumpCoyoteTimeCounter = jumpCoyoteTime;
        }
        else
        {
            jumpCoyoteTimeCounter -= Time.deltaTime;
            jumpBufferTimeCounter -= Time.deltaTime;
        }

        if ((inputJumpPressed && (isTouchingGround || jumpCoyoteTimeCounter > 0f || (jumpsTaken >= 1 && jumpsTaken <= extraJumps))) || (isTouchingGround && jumpBufferTimeCounter > 0f))
        {
            isJumping = true;
            isFalling = false;

            jumpsTaken++;

            jumpAirTimeCounter = 0;
            jumpBufferTimeCounter = 0;
        }
        else if (inputJumpPressed && isFalling)
        {
            jumpBufferTimeCounter = jumpBufferTime;
        }

        var maxJumpAirTime = isRunning ? runningMaxJumpAirTime : walkingMaxJumpAirTime;
        if ((!inputJumpHeldDown && isJumping && jumpAirTimeCounter < minJumpAirTime) || (inputJumpHeldDown && isJumping && jumpAirTimeCounter < maxJumpAirTime))
        {
            var verticalVelocity = isRunning && jumpsTaken == 1 ? runningJumpVelocity : walkingJumpVelocity;
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

    void OnDrawGizmos()
    {
        //Gizmos.DrawLine(new Vector2(peakJump.x - playerCollider.bounds.extents.x, peakJump.y), new Vector2(peakJump.x + playerCollider.bounds.extents.x, peakJump.y));
    }

    #endregion Helper Methods
}
