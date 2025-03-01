using System;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    #region Component Variables 

    private Rigidbody2D rigidBody;
    private BoxCollider2D boxCollider;
    private Animator animator;

    #endregion Component Variables

    #region Edtior Variables

    [Header("World")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask wallLayer;

    [Header("Movement")]
    [SerializeField, Range(0, 20)] private float walkingVelocity = 5;
    [SerializeField, Range(0, 20)] private float runningVelocity = 10;
    [SerializeField] private bool allowJumpRunning = true;
    [SerializeField] private bool allowFallRunning = true;
    [SerializeField] private bool mustJumpRunToFallRun = true;


    [Header("Jumping")]
    [SerializeField, Range(0, 20)] private float jumpVelocity = 10;
    [SerializeField, Range(0, 20)] private float runningJumpVelocity = 12;
    [SerializeField, Range(0, 2)] private float minJumpAirTime = 0.1f;
    [SerializeField, Range(0, 2)] private float maxJumpAirTime = 0.3f;
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

    [HideInInspector] public bool isCrouching;

    [HideInInspector] public bool isMoving;
    [HideInInspector] public bool isWalking;
    [HideInInspector] public bool isRunning;
    [HideInInspector] public bool isJumpRunning;
    [HideInInspector] public bool wasJumpRunning;
    [HideInInspector] public bool isFallRunning;

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
        rigidBody = GetComponent<Rigidbody2D>();
        boxCollider = GetComponent<BoxCollider2D>();
        animator = GetComponent<Animator>();
    }

    private void Start()
    {
        FaceRight();
    }

    private void FixedUpdate()
    {
        if (isJumping)
        {
            rigidBody.gravityScale = gravityJumping;
        }
        else if (isFalling)
        {
            if (isWallGrabbing)
            {
                rigidBody.gravityScale = gravityWallGrabbing;
            }
            else
            {
                rigidBody.gravityScale = gravityFalling;
            }
        }
        else
        {
            rigidBody.gravityScale = gravityNormal;
        }
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

    private void UpdateState()
    {
        isTouchingGround = Physics2D.BoxCast(boxCollider.bounds.center, boxCollider.bounds.size, 0, Vector2.down, 0.1f, groundLayer);
        isTouchingWall = Physics2D.BoxCast(boxCollider.bounds.center, boxCollider.bounds.size, 0, new Vector2(transform.localScale.x, 0), 0.1f, wallLayer);

        isCrouching = inputVerticalValue < 0;

        isMoving = Math.Abs(rigidBody.linearVelocityX) > 0.01f;
        isWalking = isMoving && Math.Abs(inputHorizontalValue) > 0.01f;
        isRunning = isWalking && inputRunHeldDown;

        isJumping = rigidBody.linearVelocityY > 0.01f;
        isFalling = rigidBody.linearVelocityY < -0.01f;

        wasJumpRunning = wasJumpRunning || isJumpRunning;
        isJumpRunning = inputRunHeldDown && isJumping && isJumpRunning;
        isFallRunning = inputRunHeldDown && isFalling && isFallRunning;

        isWallGrabbing = allowWallGrabbing && !isTouchingGround && isTouchingWall && isFalling;
        isWallGrabbingLeft = isWallGrabbing && isFacingLeft;
        isWallGrabbingRight = isWallGrabbing && isFacingRight;
    }

    private void UpdateAnimation()
    {
        animator.SetBool("Grounded", isTouchingGround);

        animator.SetBool("Crouching", isCrouching);

        animator.SetBool("Walking", isTouchingGround && !isJumping && !isFalling && isWalking);
        animator.SetBool("Running", isTouchingGround && !isJumping && !isFalling && isRunning);

        animator.SetBool("Jumping", isJumping);
        animator.SetBool("Falling", isFalling);
        animator.SetBool("WallGrabbing", isWallGrabbing);
    }

    private void UpdateMovement()
    {
        UpdateMovementHorizontal();
        UpdateMovementVertical1();
    }

    private void UpdateMovementHorizontal()
    {
        if (inputHorizontalRight)
        {
            FaceRight();
        }
        else if (inputHorizontalLeft)
        {
            FaceLeft();
        }

        if (!isCrouching || isJumping)
        {
            var isMovingIntoWall = isTouchingWall && ((isFacingRight && inputHorizontalRight) || (isFacingLeft && inputHorizontalLeft));
            if (!isMovingIntoWall)
            {
                MoveHorizontally();
            }
        }
    }

    private void MoveHorizontally()
    {
        var horizontalVelovity = inputRunHeldDown && (isTouchingGround || isJumpRunning || isFallRunning) ? runningVelocity : walkingVelocity;
        rigidBody.linearVelocity = new Vector2(horizontalVelovity * inputHorizontalValue, rigidBody.linearVelocity.y);
    }

    private void UpdateMovementVertical1()
    {
        if (isTouchingGround)
        {
            jumpsTaken = 0;
            jumpAirTime = 0;
        }

        if (inputJumpPressed && (isTouchingGround || jumpsTaken < extraJumps))
        {
            isJumping = true;
            jumpAirTime = 0;
            jumpsTaken++;

            if (inputRunHeldDown && allowJumpRunning)
            {
                isJumpRunning = true;
            }
            else
            {
                isJumpRunning = false;
            }

            isFallRunning = false;
            wasJumpRunning = false;
        }

        if ((!inputJumpHeldDown && isJumping && jumpAirTime < minJumpAirTime) || (inputJumpHeldDown && isJumping && jumpAirTime < maxJumpAirTime))
        {
            var verticalVelocity = isRunning && jumpsTaken == 0 ? runningJumpVelocity : jumpVelocity;
            rigidBody.linearVelocity = new Vector2(rigidBody.linearVelocity.x, verticalVelocity);
        }
        
        if (isFalling)
        {
            var verticalVelocity = isWallGrabbing ? maxWallGrabbingFallingVelocity : maxFallingVelocity;
            rigidBody.linearVelocity = new Vector2(rigidBody.linearVelocity.x, Math.Clamp(rigidBody.linearVelocity.y, -verticalVelocity, 0));

            if (inputRunHeldDown && allowFallRunning)
            {
                isFallRunning = isFallRunning || (mustJumpRunToFallRun ? wasJumpRunning : true);
            }
            else
            {
                isFallRunning = false;
            }

            isJumpRunning = false;
            wasJumpRunning = false;
        }

        if (isJumping)
        {
            jumpAirTime += Time.deltaTime;
        }
    }

    private void FaceRight()
    {
        transform.localScale = new Vector3(1, 1, 1);
        isFacingLeft = !(isFacingRight = true);
    }

    private void FaceLeft()
    {
        transform.localScale = new Vector3(-1, 1, 1);
        isFacingLeft = !(isFacingRight = false);
    }

    #endregion Helper Methods
}
