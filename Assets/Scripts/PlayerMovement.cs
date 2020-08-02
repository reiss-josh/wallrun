using UnityEngine;
using System;

public class PlayerMovement : MonoBehaviour
{

    //Assingables
    public Transform playerCam;
    public Transform orientation; //do we even need this?
    public Transform gunHand;
    public Transform head;

    //Movement Vars
    public float moveSpeed = 4500;
    public float walkSpeedCap = 20;
    public float sprintSpeedCap = 40;
    public float jumpForce = 200f;

    public float friction = 0.175f;
    private float threshold = 0.01f;
    public float maxSlopeAngle = 35f;
    private float jumpTimer = 0f;
    public float jumpTime = 1f;

    //Other
    private Rigidbody rb;
    private CapsuleCollider playerCollider;
    private bool isGrounded = true;
    private bool canJump = false;

    //Crouch and Slide
    private Vector3 playerScale; //how tall is player when standing?
    private Vector3 crouchScale; //how tall is player when crouching?
    private Vector3 headDest; //where should the camera be?
    private float headOffset = 0.666f; //offset btwn camera and body origin
    private float slideTimer; //how long since last slide?
    public float slideTime = 1.0f; //how long should slide continue?
    public float slideForce = 50f;
    private bool isCrouching;

    //input
    float xInput, yInput, mouseX, mouseY;
    bool jumpInput, sprintInput, crouchInput, shootInput;

    //Options
    public float mouseSensitivity = 1, mouseSensitivityScale = 1;

    //get the rigidbody component
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        playerCollider = GetComponent<CapsuleCollider>();
        headDest = new Vector3(0, headOffset, 0);
    }

    //lock the cursor to the camera
    void Start()
    {
        playerScale = transform.localScale;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void FixedUpdate()
    {
        Movement();
    }

    private void Update()
    {
        GetInput();
        Look();
    }

    //should probably make this its own class
    private void GetInput()
    {
        xInput = Input.GetAxis("Horizontal");
        yInput = Input.GetAxis("Vertical");
        shootInput = Input.GetButton("Shoot");
        crouchInput = Input.GetButton("Crouch");
        jumpInput = Input.GetButton("Jump");
        mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * mouseSensitivityScale * Time.fixedDeltaTime;
        mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * mouseSensitivityScale * Time.fixedDeltaTime;
        sprintInput = Input.GetButton("Sprint");
    }

    //modeled off look code from Dani
    private float xRotation, yRotation;
    private void Look()
    {
        yRotation += mouseX;
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);
        playerCam.transform.localRotation = Quaternion.Euler(xRotation, yRotation, 0);
        orientation.transform.localRotation = Quaternion.Euler(0, yRotation, 0);
    }

    //taken from fps controller from Dani
    //gets the x/z velocity of orientation in local space
    public Vector2 FindVelRelativeToOrientation()
    {
        float orientationAngle = orientation.transform.eulerAngles.y;
        float moveAngle = Mathf.Atan2(rb.velocity.x, rb.velocity.z) * Mathf.Rad2Deg;

        float u = Mathf.DeltaAngle(orientationAngle, moveAngle);
        float v = 90 - u;

        float magnitue = rb.velocity.magnitude;
        float yMag = magnitue * Mathf.Cos(u * Mathf.Deg2Rad);
        float xMag = magnitue * Mathf.Cos(v * Mathf.Deg2Rad);

        return new Vector2(xMag, yMag);
    }

    private void Movement()
    {
        //Debug.Log(isGrounded);
        //Extra gravity
        rb.AddForce(Vector3.down * Time.deltaTime * 10);
        Vector2 xyVelocity = FindVelRelativeToOrientation();

        //speed limiting
        if ((xInput > 0 && xyVelocity.x > walkSpeedCap) ||
            (xInput < 0 && xyVelocity.x < -walkSpeedCap)) xInput = 0;
        if ((yInput > 0 && xyVelocity.y > walkSpeedCap) ||
            (yInput < 0 && xyVelocity.y < -walkSpeedCap)) yInput = 0;

        if (jumpTimer > 0) jumpTimer -= Time.deltaTime;
        else if (jumpTimer == 0) canJump = true;
        else { jumpTimer = 0; canJump = true; }
        if (canJump && jumpInput && isGrounded) HandleJumping();

        //might want to replace slideTimer with something else
        //maybe only trigger it if you're jumping or sprinting for a minimum amnt of time?
        if (crouchInput && slideTimer <= 0 && isGrounded) HandleSliding(xyVelocity);

        HandleCrouching();
        float multiplier = 1f;
        rb.AddForce(orientation.transform.forward * yInput * moveSpeed * Time.deltaTime * multiplier);
        rb.AddForce(orientation.transform.right * xInput * moveSpeed * Time.deltaTime * multiplier);
    }

    private void HandleCrouching()
    {
        if (crouchInput && isGrounded) slideTimer = 1.0f;
        else if (slideTimer > 0) slideTimer -= Time.deltaTime;
        else slideTimer = 0;
        isCrouching = (crouchInput || HeadOccluded()) ? true : false;
        CrouchCollider();
        HeadUpdate();
        GunRotate();
    }

    private void HandleSliding(Vector2 xyVelocity)
    {
        rb.AddForce(orientation.transform.forward * xyVelocity.y * slideForce);
        rb.AddForce(orientation.transform.right * xyVelocity.x * slideForce);
    }

    private Vector3 normalVector = Vector3.up;
    private void HandleJumping()
    {
        jumpTimer = jumpTime;
        canJump = false;
        //Add jump forces
        rb.AddForce(Vector2.up * jumpForce * 1.5f);
        rb.AddForce(normalVector * jumpForce * 0.5f);

        //If jumping while falling, reset y velocity.
        Vector3 vel = rb.velocity;
        if (rb.velocity.y < 0.5f)
            rb.velocity = new Vector3(vel.x, 0, vel.z);
        else if (rb.velocity.y > 0)
            rb.velocity = new Vector3(vel.x, vel.y / 2, vel.z);
    }


    //rotates gun to face same angle as playerCam
    //can take a Vector3 as an offset angle
    //eg (0,0,45) would rotate gun 45 degrees on z axis
    private void GunRotate()
    {
        Quaternion rotation = playerCam.rotation;
        Vector3 Angle = (isCrouching ?
                            new Vector3(0,0,45) : default(Vector3));
        if (Angle != default(Vector3)) rotation *= Quaternion.Euler(Angle);
        gunHand.rotation = Quaternion.Slerp(gunHand.rotation, rotation, Time.deltaTime * 10f);
    }

    private void CrouchCollider()
    {
        Vector3 crouchDest = (isCrouching ?
                                new Vector3(0, -0.5f, 0) : Vector3.zero);
        float newHeight = (isCrouching ?
                                1f : 2f);
        playerCollider.height = Mathf.Lerp(playerCollider.height, newHeight, Time.deltaTime * 10f);
        playerCollider.center = Vector3.Lerp(playerCollider.center, crouchDest, Time.deltaTime * 10f);
    }

    private void HeadUpdate()
    {
        headDest.y = headOffset - (isCrouching ? 0.5f : 0);
        head.localPosition = Vector3.Lerp(head.localPosition, headDest, Time.deltaTime * 10f);
    }

    //returns true if standing up would get player stuck in geometry
    private bool HeadOccluded()
    {
        return false;
    }
}