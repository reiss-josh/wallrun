using UnityEngine;

public class PlayerMovement : MonoBehaviour
{

    //Assingables
    public Transform playerCam;
    public Transform orientation; //do we even need this?
    public Transform gunHand;
    public Transform head;

    //Other
    private Rigidbody rb;
    private CapsuleCollider playerCollider;

    //for resizing in slide
    private Vector3 playerScale;
    private Vector3 crouchScale;
    private float headOffset = 0.666f;
    private Vector3 headDest;

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

        CrouchCollider();
        HeadUpdate();
        GunRotate();
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

    private void Movement()
    {

    }

    //rotates gun to face same angle as playerCam
    //can take a Vector3 as an offset angle
    //eg (0,0,45) would rotate gun 45 degrees on z axis
    private void GunRotate()
    {
        Quaternion rotation = playerCam.rotation;
        Vector3 Angle = (crouchInput ? new Vector3(0,0,45) : default(Vector3));
        if (Angle != default(Vector3)) rotation *= Quaternion.Euler(Angle);
        gunHand.rotation = Quaternion.Slerp(gunHand.rotation, rotation, Time.deltaTime * 10f);
    }

    private void CrouchCollider()
    {
        Vector3 crouchDest = (crouchInput ? new Vector3(0, -0.5f, 0) : Vector3.zero);
        float newHeight = (crouchInput ? 1f : 2f);
        playerCollider.height = Mathf.Lerp(playerCollider.height, newHeight, Time.deltaTime * 10f);
        playerCollider.center = Vector3.Lerp(playerCollider.center, crouchDest, Time.deltaTime * 10f);
    }

    private void HeadUpdate()
    {
        headDest.y = headOffset - (crouchInput ? 0.5f : 0);
        head.localPosition = Vector3.Lerp(head.localPosition, headDest, Time.deltaTime * 10f);
    }
}