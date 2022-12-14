using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Unity.Netcode;

public class NetworkPlayerController : NetworkBehaviour, IDamageable
{
    //properties used to help check whether player can use certain mechanics. These are mostly to keep the code clean and organized
    //Kind of a rudimentary/crude state machine
    public float MaxHealth { get { return Maxhealth; } set { Maxhealth = value; } }
    public float Health { get { return health; } set { health = value; } }
    public bool PlayerCanMove { get; private set; } = true;
    public bool PlayerIsDashing { get; private set; }
    public bool PlayerCanDash => dashesRemaining > dashesAllowed - dashesAllowed;
    private bool DashShouldCooldown => dashesRemaining < dashesAllowed;
    //private bool PlayerIsSprinting => playerCanSprint && Input.GetKey(sprintKey) && !playerIsCrouching;
    //private bool PlayerShouldCrouch => Input.GetKeyDown(crouchKey) && !playerInCrouchAnimation && characterController.isGrounded;

    //Checks used to see if player is able to use mechanics.
    [Header("Functional Options")]
    [SerializeField]
    private float Maxhealth, health;
    [Tooltip("Is the player in the middle of a special movement, i.e. ladder climbing?")]
    [SerializeField]
    public bool playerOnSpecialMovement = false;
    //[SerializeField]
    //private bool playerCanSprint = true;
    [SerializeField]
    private bool playerCanJump = true;
    [SerializeField]
    private bool playerCanDash = true;
    //[SerializeField]
    //private bool playerCanCrouch = true;
    [SerializeField]
    private bool playerCanHeadbob = true;

    //parameters for different movement speeds
    [Header("Movement Parameters")]
    public float walkSpeed = 6; // Changed to public so powerups can affec this variable
    public float wallRunSpeed = 12f; // Changed to public so powerups can affec this variable
    public float owalkspeed = 6; // Changed to public so powerups can affec this variable
    public float owallspeed = 12f; // Changed to public so powerups can affec this variable
    //[SerializeField]
    //private float sprintSpeed = 6f;
    //[SerializeField]
    //private float crouchSpeed = 1.5f;
    [SerializeField]
    private float slopeSlideSpeed = 6f;
    [SerializeField]
    private float fovDefault = 60f;
    [SerializeField]
    private float fovSprint = 70f;
    [SerializeField]
    private float fovIncrement = 5f;

    //Parameters for looking around with mouse
    [Header("Look Parameters")]
    [SerializeField]
    private bool restrictHorizontal;
    [SerializeField, Range(1, 10)]
    private float lookSpeedX = 2f;
    [SerializeField, Range(1, 10)]
    private float lookSpeedY = 2f;
    [SerializeField, Range(1, 100)]
    private float upperLookLimit = 80f;
    [SerializeField, Range(1, 100)]
    private float lowerLookLimit = 80f;
    [SerializeField, Range(1, 100)]
    private float leftLookLimit = 80f;
    [SerializeField, Range(1, 100)]
    private float rightLookLimit = 80f;

    //Parameters for jump height and gravity
    [Header("Jumping Parameters")]
    [SerializeField]
    private int jumpsAllowed = 2;
    [SerializeField]
    private int remainingJumps;
    [SerializeField]
    private float jumpForce = 8f;
    [SerializeField]
    private float gravity = 30f;

    [Header("Wallrunning Parameters")]
    [SerializeField] private LayerMask whatIsWall;
    [SerializeField] private LayerMask whatIsGround;
    [SerializeField] private float wallRunForce;
    [SerializeField] private float maxWallRunTime;
    [SerializeField] private float wallCheckDistance;
    [SerializeField] private float minJumpHeight;
    [SerializeField] private Transform orientation;
    private RaycastHit leftWallHit;
    private RaycastHit rightWallHit;
    private bool wallLeft;
    private bool wallRight;
    private float wallRunTimer;
    private float verticalInput;

    //KEEPING THIS INCASE WE WANT TO ADD CROUCHING
    //Parameters for crouching. The height and center will directly affect the CharacterController height and center.
    //[Header("Crouch Parameters")]
    //[SerializeField]
    //private float crouchingHeight = 0.5f;
    //[SerializeField]
    //private float standingHeight = 2f;
    //[SerializeField]
    //private float timeToCrouch = 0.25f; //How long should the crouching animation take?
    //[SerializeField]
    //private Vector3 crouchingCenter = new Vector3(0, 0.5f, 0);
    //[SerializeField]
    //private Vector3 standingCenter = new Vector3(0, 0, 0); //Didn't use Vector3.Zero so that it would be customizable in inspector
    //private bool playerIsCrouching; //Is the player currently crouched?
    //private bool playerInCrouchAnimation; //Is the player currently in the middle of the crouching animation?

    [Header("Headbob Parameters")]
    [SerializeField]
    private float walkBobSpeed = 14f;
    [SerializeField]
    private float walkBobAmount = 0.05f;
    //[SerializeField]
    //private float sprintBobSpeed = 18f;
    //[SerializeField]
    //private float sprintBobAmount = 0.1f;
    //[SerializeField]
    //private float crouchBobSpeed = 8f;
    //[SerializeField]
    //private float crouchBobAmount = 0.025f;
    private float defaultYPosCamera = 0;
    private float timer;

    [Header("Dash Parameters")]
    [SerializeField]
    private int dashesAllowed = 2;
    [SerializeField]
    private int dashesRemaining;
    [SerializeField]
    private float dashSpeed = 0f;
    [Tooltip("How long the player moves at dash speed for after they press the button")]
    [SerializeField]
    private float dashTime;
    [Tooltip("Length in seconds of the dash cooldown")]
    [SerializeField]
    private float dashCooldownTime;
    [Tooltip("If player is holding dash and there are dashes remaining, how much time should there be between the dashes?")]
    [SerializeField]
    private float dashBetweenTime;
    private float dashCooldownStartTime;

    [Header("State bools")]
    public bool basicMovement;
    public bool wallRunning;


    [System.NonSerialized] public Collider playerCol;

    private Camera playerCamera;
    private AudioListener audioListener;
    private Canvas canvas;
    private CharacterController characterController;
    //private Rigidbody playerRB;
    public GunHandler playerGun;
    public GunHandler playerGun2;

    private Vector3 moveDirection;
    private Vector2 currentInput; //Whether player is moving vertically or horizontally along x and z planes
    private Vector2 dashInput;
    public Vector2 MoveInput { get; private set; }

    private float rotationX = 0f; //Camera rotation for clamping
    private float rotationY = 0f;

    //private bool playerIsSprinting;
    private bool playerDashing;
    private bool dashOnCooldown;
    private bool playerShouldDash;

    private float groundRayDistance = 1;
    private RaycastHit slopeHit;
    private WaitForSeconds dashCooldownWait;
    private WaitForSeconds dashBetweenWait;

    public static InputActions _input;

    private MovementState state;

    ulong networkID;

    public enum MovementState
    {
        basic,
        wallrunning
    }

    public override void OnNetworkSpawn()
    {
        playerCamera.enabled = IsLocalPlayer ? true : false;
        audioListener.enabled = IsLocalPlayer ? true : false;
        playerGun.enabled = IsLocalPlayer ? true : false;
        canvas.enabled = IsLocalPlayer ? true : false;
        TargetManager.Instance.FindTargets();
    }

    public override void OnNetworkDespawn()
    {
        TargetManager.Instance.FindTargets();
    }

    void Awake()
    {
        walkSpeed = 6;
        wallRunSpeed = 12;
        health = Maxhealth;
        dashCooldownWait = new WaitForSeconds(dashCooldownTime);
        dashBetweenWait = new WaitForSeconds(dashBetweenTime);

        playerCamera = GetComponentInChildren<Camera>();
        audioListener = GetComponentInChildren<AudioListener>();
        canvas = GetComponentInChildren<Canvas>();
        characterController = GetComponent<CharacterController>();
        playerCol = GetComponent<Collider>();
        //playerRB = GetComponent<Rigidbody>();
        playerGun = GetComponentInChildren<GunHandler>();

        defaultYPosCamera = playerCamera.transform.localPosition.y;

        //Lock and hide cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        playerCamera.fieldOfView = fovDefault;

        _input = new InputActions();

        state = MovementState.basic;

        remainingJumps = jumpsAllowed;
        dashesRemaining = dashesAllowed;
    }

    // Update is called once per frame
    void Update()
    {
        if (!IsLocalPlayer)
            return;
        if (!playerOnSpecialMovement)
        {
            if (PlayerCanMove)
            {
                if (DashShouldCooldown)
                {
                    DashCooldown();
                }
                CheckForWall();
                StateHandler();
            }
        }

        HandleMouseLook();
    }

    private void OnEnable()
    {
        //Subscribe methods to the input actions
        _input.Enable();

        //HumanoidLand
        _input.HumanoidLand.Walk.performed += HandleWalkInput;
        _input.HumanoidLand.Walk.canceled += HandleWalkInput;
        _input.HumanoidLand.Dash.performed += HandleDashInput;
        _input.HumanoidLand.Dash.canceled += HandleDashInput;
        _input.HumanoidLand.Jump.performed += HandleJump;
        _input.HumanoidLand.Restart.performed += ReloadScene;

        //HumanoidWall
        _input.HumanoidWall.Forward.performed += HandleWallrunInput;
        _input.HumanoidWall.Forward.canceled += HandleWallrunInput;
        _input.HumanoidWall.Jump.performed += HandleJump;

        //GunHandler
        _input.Gun.Shoot.performed += playerGun.Shoot;
        _input.Gun.Shoot.canceled += playerGun.Shoot;
        //_input.Gun.Shoot.performed += playerGun2.Shoot;
        //_input.Gun.Shoot.canceled += playerGun2.Shoot;
        //_input.Gun.Shoot.performed += playerGun.ShootOther;
        //_input.Gun.Shoot.canceled += playerGun.ShootOther;
        _input.Gun.SwitchWeapon.performed += playerGun.SwitchWeapon;
        _input.Gun.Reload.performed += playerGun.Reload;
    }

    private void OnDisable()
    {
        //Unsubscribe methods from the input actions
        _input.Disable();

        //HumanoidLand
        _input.HumanoidLand.Walk.performed -= HandleWalkInput;
        _input.HumanoidLand.Walk.canceled -= HandleWalkInput;
        _input.HumanoidLand.Dash.performed -= HandleDashInput;
        _input.HumanoidLand.Dash.canceled -= HandleDashInput;
        _input.HumanoidLand.Jump.performed -= HandleJump;
        _input.HumanoidLand.Restart.performed -= ReloadScene;

        //HumanoidWall
        _input.HumanoidWall.Forward.performed -= HandleWallrunInput;
        _input.HumanoidWall.Forward.canceled -= HandleWallrunInput;
        _input.HumanoidWall.Jump.performed -= HandleJump;

        //GunHandler
        _input.Gun.Shoot.performed -= playerGun.Shoot;
        _input.Gun.Shoot.canceled -= playerGun.Shoot;
        //_input.Gun.Shoot.performed -= playerGun.ShootOther;
        //_input.Gun.Shoot.canceled -= playerGun.ShootOther;
        _input.Gun.SwitchWeapon.performed -= playerGun.SwitchWeapon;
        _input.Gun.Reload.performed -= playerGun.Reload;
    }

    private void StateHandler()
    {
        // Mode - Basic Movement
        if (state == MovementState.basic)
        {
            //state = MovementState.basic;
            _input.HumanoidWall.Disable();
            _input.HumanoidLand.Enable();
            ApplyFinalBasicMovements();
        }

        // Mode - Wallrunning
        if (state == MovementState.wallrunning)
        {
            //state = MovementState.wallrunning;
            _input.HumanoidLand.Disable();
            _input.HumanoidWall.Enable();
            ApplyFinalWallrunMovements();
        }
    }

    private void ReloadScene(InputAction.CallbackContext context)
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void AdaptFOV()
    {
        if (playerDashing && playerCamera.fieldOfView < fovSprint)
        {
            playerCamera.fieldOfView += fovIncrement * Time.deltaTime;
        }
        else if (!playerDashing && playerCamera.fieldOfView > fovDefault)
        {
            playerCamera.fieldOfView -= fovIncrement * Time.deltaTime;
        }
    }

    private void HandleWalkInput(InputAction.CallbackContext context)
    {
        //when the player presses W and S or A and D
        currentInput = (context.ReadValue<Vector2>());
        MoveInput = new Vector2(currentInput.x * walkSpeed, currentInput.y * walkSpeed);

    }

    private void HandleDashInput(InputAction.CallbackContext context)
    {
        //KEEPING THIS IN CASE WE WANT TO ADD SPRINTING
        //playerIsSprinting = !playerIsSprinting;

        //if (currentInput != null)
        //{
        //    if (playerIsSprinting)
        //    {
        //        MoveInput = new Vector2(currentInput.x * sprintSpeed, currentInput.y * sprintSpeed);
        //    }
        //    else if (!playerIsSprinting)
        //    {
        //        MoveInput = new Vector2(currentInput.x * walkSpeed, currentInput.y * walkSpeed);
        //    }
        //}
        if (context.performed)
        {
            playerShouldDash = true;

            if ((characterController.velocity.z != 0 || characterController.velocity.x != 0) && PlayerCanDash)
            {
                StartCoroutine(Dash());
            }
        }
        else if (context.canceled)
        {
            playerShouldDash = false;
            playerDashing = false;
        }
    }

    //TODO: Figure out why there's a weird stutter when falling after cancelling before second dash in midair
    private IEnumerator Dash()
    {
        dashesRemaining--;
        float startTime = Time.time;
        dashCooldownStartTime = startTime;

        //The direction in which the player moves based on input
        float moveDirectionY = moveDirection.y;
        moveDirection = (transform.TransformDirection(Vector3.right) * MoveInput.x) + (transform.TransformDirection(Vector3.forward) * MoveInput.y);
        moveDirection.y = 0;//moveDirectionY;

        while (Time.time < startTime + dashTime && playerShouldDash)
        {
            playerDashing = true;
            characterController.Move(moveDirection * dashSpeed * Time.deltaTime);

            yield return null;
        }

        if (playerShouldDash && PlayerCanDash)
        {
            yield return dashBetweenWait;
            StartCoroutine(Dash());
        }
        else
        {
            playerDashing = false;
        }
    }

    private void DashCooldown()
    {
        if (dashCooldownStartTime + dashCooldownTime < Time.time && DashShouldCooldown)
        {
            dashesRemaining++;
            dashCooldownStartTime = Time.time;
            Debug.Log("Cooldown Complete!");
        }
    }

    private void HandleMouseLook()
    {
        rotationX -= Input.GetAxis("Mouse Y") * lookSpeedY;
        rotationX = Mathf.Clamp(rotationX, -upperLookLimit, lowerLookLimit);

        if (restrictHorizontal)
        {
            //rotate camera around X and Y axis, and rotate player around x axis
            rotationY += Input.GetAxis("Mouse X") * lookSpeedX;
            rotationY = Mathf.Clamp(rotationY, -leftLookLimit, rightLookLimit);//clamp camera
            playerCamera.transform.localRotation = Quaternion.Euler(rotationX, rotationY, 0);
        }
        else
        {
            playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);
            transform.rotation *= Quaternion.Euler(0, Input.GetAxis("Mouse X") * lookSpeedX, 0);
        }
    }

    private void HandleJump(InputAction.CallbackContext context)
    {
        remainingJumps--;
        //only jump if property conditions are met
        if (remainingJumps > 0)
        {
            moveDirection.y = jumpForce;
        }
    }

    //KEEPING THIS INCASE WE WANT TO ADD CROUCHING
    //private void HandleCrouch()
    //{
    //    //only crouch if property conditions are met
    //    if (PlayerShouldCrouch)
    //    {
    //        StartCoroutine(CrouchStand());
    //    }
    //}

    private void HandleHeadbob()
    {
        if (!characterController.isGrounded && state == MovementState.basic)
        {
            return;
        }

        if (Mathf.Abs(moveDirection.x) > 0.1f || Mathf.Abs(moveDirection.z) > 0.1f)
        {
            timer += Time.deltaTime * (walkBobSpeed);
            playerCamera.transform.localPosition = new Vector3(
                playerCamera.transform.localPosition.x,
                defaultYPosCamera + Mathf.Sin(timer) * (walkBobAmount),
                playerCamera.transform.localPosition.z);
        }
    }

    private void ApplyFinalBasicMovements()
    {
        HandleHeadbob();
        AdaptFOV();

        //make sure the player is on the ground if applying gravity (after pressing Jump)
        if (!characterController.isGrounded && !playerDashing)
        {
            moveDirection.y -= gravity * Time.deltaTime;
        }

        if (remainingJumps < jumpsAllowed && characterController.isGrounded)
        {
            remainingJumps = jumpsAllowed;
        }

        //The direction in which the player moves based on input
        float moveDirectionY = moveDirection.y;
        moveDirection = (transform.TransformDirection(Vector3.right) * MoveInput.x) + (transform.TransformDirection(Vector3.forward) * MoveInput.y);
        moveDirection.y = moveDirectionY;

        if (OnSteepSlope()) SteepSlopeMovement();

        //move the player based on the parameters gathered in the "Handle-" functions
        characterController.Move(moveDirection * Time.deltaTime);
    }

    private bool OnSteepSlope()
    {
        if (!characterController.isGrounded) return false;

        if (Physics.Raycast(transform.position, Vector3.down, out slopeHit, (characterController.height / 2) + groundRayDistance))
        {
            float slopeAngle = Vector3.Angle(slopeHit.normal, Vector3.up);
            if (slopeAngle > characterController.slopeLimit) return true;
        }

        return false;
    }

    private void SteepSlopeMovement()
    {
        Vector3 slopeDirection = Vector3.up - slopeHit.normal * Vector3.Dot(Vector3.up, slopeHit.normal);
        float slideSpeed = walkSpeed + slopeSlideSpeed + Time.deltaTime;

        moveDirection = slopeDirection * -slideSpeed;
        moveDirection.y = moveDirection.y - slopeHit.point.y;

        characterController.Move(moveDirection * Time.deltaTime);
    }

    //TODO: Consider using OverlapSpheres instead of raycasts as this might help with stutter
    private void CheckForWall()
    {
        //Parameters in order: start point, direction, store hit info, distance, layermask
        wallRight = Physics.Raycast(transform.position, orientation.right, out rightWallHit, wallCheckDistance, whatIsWall);
        wallLeft = Physics.Raycast(transform.position, -orientation.right, out leftWallHit, wallCheckDistance, whatIsWall);

        verticalInput = Input.GetAxisRaw("Vertical");

        //State 1 - Wallrunning
        if ((wallLeft || wallRight) && verticalInput > 0 && !characterController.isGrounded)
        {
            if (state != MovementState.wallrunning)
            {
                state = MovementState.wallrunning;
            }
        }
        else
        {
            if (state == MovementState.wallrunning)
            {
                remainingJumps = jumpsAllowed;
                state = MovementState.basic;
            }
        }
    }

    private void HandleWallrunInput(InputAction.CallbackContext context)
    {
        currentInput = (context.ReadValue<Vector2>());
        MoveInput = new Vector2(currentInput.x * wallRunSpeed, currentInput.y * wallRunSpeed);
    }

    private void ApplyFinalWallrunMovements()
    {
        HandleHeadbob();
        AdaptFOV();

        Vector3 wallNormal = wallRight ? rightWallHit.normal : leftWallHit.normal;

        Vector3 wallForward = Vector3.Cross(wallNormal, transform.up);

        //The direction in which the player moves based on input
        //float moveDirectionY = moveDirection.y;
        moveDirection = (transform.TransformDirection(wallForward) * MoveInput.x) + (transform.TransformDirection(Vector3.forward) * MoveInput.y);
        moveDirection.y = 0;

        //move the player based on the parameters gathered in the "Handle-" functions
        characterController.Move(moveDirection * Time.deltaTime);
    }

    public void TakeDamage(float damageTaken)
    {
        Health -= damageTaken;
        Debug.Log($"Player Health: {health}");
        CheckForDeath();
    }
    public void HealthRegen(float heal)
    {
        health += heal;
        if (health > MaxHealth)
        {
            health = MaxHealth;
        }
        Debug.Log($"Player healed. Current health is {health}");
    }

    public void CheckForDeath()
    {
        if (Health <= 0)
        {
            Debug.Log("Player died!");
            //SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    

    //KEEPING THIS INCASE WE WANT TO ADD CROUCHING
    //Coroutine that handles crouching/standing
    //private IEnumerator CrouchStand()
    //{
    //    //make sure there is nothing above player's head that should prevent them from standing, if there is, do not allow them to stand
    //    if (playerIsCrouching && Physics.Raycast(playerCamera.transform.position, Vector3.up, 1f))
    //    {
    //        yield break;
    //    }

    //    //player is now in crouching animation
    //    playerInCrouchAnimation = true;

    //    float timeElapsed = 0; //amount of time elapsed during animation
    //    float targetHeight = playerIsCrouching ? standingHeight : crouchingHeight; //target height based on the state the player is in when they press crouch button
    //    float currentHeight = characterController.height; //the player's height when they press the crouch button
    //    Vector3 targetCenter = playerIsCrouching ? standingCenter : crouchingCenter; //target center based on the state the player is in when they press crouch button
    //    Vector3 currentCenter = characterController.center; //the player's center when they press the crouch button

    //    //while the animation is still going
    //    while(timeElapsed < timeToCrouch)
    //    {
    //        characterController.height = Mathf.Lerp(currentHeight, targetHeight, timeElapsed/timeToCrouch); //change the current height to the target height
    //        characterController.center = Vector3.Lerp(currentCenter, targetCenter, timeElapsed/timeToCrouch); //change the current center to the target center

    //        timeElapsed += Time.deltaTime; //increment the time elapsed based on the time it took between frames

    //        yield return null;
    //    }

    //    //Sanity check :P
    //    characterController.height = targetHeight;
    //    characterController.center = targetCenter;

    //    playerIsCrouching = !playerIsCrouching; //update whether or not the player is crouching

    //    playerInCrouchAnimation = false; //the crouching animation has ended
    //}
}
