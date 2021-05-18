using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Movement")]
    // Button Movement Input Vars
    public float panSpeed = 20;
    public float scrollSpeed = 20;

    // Mouse Movement Input Vars
    Vector3 lastMousePosition;
    public float dragSpeed = 0.01f;

    Vector3 lastMouseRightPosition;
    public float dragRotateSpeed = 0.01f;
    Vector3 rotateAroundPoint;

    [Header("Rotation")]
    // Button Rotation Vars
    public float rotateSpeed = 40;
    public float lookAhead = 2f;

    [Header("Camera Bounds")]
    public float minY = 5f;
    public float maxY = 30f;

    [HideInInspector]
    public bool movementOn = true;

    void Update()
    {
        if (!movementOn) return;
        ButtonMovementInputs();
        ButtonRotationInputs();
        MouseInputs();
        ZoomCamera();
    }

    // Take x and z inputs and translate based on camera rotation
    private void MoveCamera(float xInput, float zInput)
    {
        float zMove = Mathf.Cos(transform.eulerAngles.y * Mathf.PI / 180) * zInput - Mathf.Sin(transform.eulerAngles.y * Mathf.PI / 180) * xInput;
        float xMove = Mathf.Sin(transform.eulerAngles.y * Mathf.PI / 180) * zInput + Mathf.Cos(transform.eulerAngles.y * Mathf.PI / 180) * xInput;

        transform.position = transform.position + new Vector3(xMove, 0, zMove);
    }

    // Get mouse drag inputs
    void MouseInputs()
    {
        if (Input.GetMouseButtonDown(0))
        {
            lastMousePosition = Input.mousePosition;
        }
        else if (Input.GetMouseButton(0))
        {
            Vector3 delta = Input.mousePosition - lastMousePosition;

            MoveCamera(-1 * dragSpeed * Mathf.Clamp(transform.position.y,25f,Mathf.Infinity) * delta.x, -1 * dragSpeed * Mathf.Clamp(transform.position.y, 25f, Mathf.Infinity) * delta.y);

            lastMousePosition = Input.mousePosition;
        }

        if (Input.GetMouseButtonDown(1))
        {
            lastMouseRightPosition = Input.mousePosition;
            Ray inputRay = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            RaycastHit hit;
            if (Physics.Raycast(inputRay, out hit))
            {
                rotateAroundPoint = hit.point;
            } else
            {
                rotateAroundPoint = new Vector3(Camera.main.transform.position.x, 0, Camera.main.transform.position.z);
            }
        }
        else if (Input.GetMouseButton(1))
        {
            Vector3 delta = Input.mousePosition - lastMouseRightPosition;

            float xlook = Mathf.Sin(transform.eulerAngles.y * Mathf.PI / 180) * lookAhead;
            float zlook = Mathf.Cos(transform.eulerAngles.y * Mathf.PI / 180) * lookAhead;
            transform.RotateAround(rotateAroundPoint, Vector3.up, dragRotateSpeed * delta.x * Time.deltaTime);

            lastMouseRightPosition = Input.mousePosition;
        }
    }

    // get keyboard inputs
    void ButtonMovementInputs()
    {
        float inputZ = 0f;
        float inputX = 0f;

        if (Input.GetKey("w"))
        {
            inputZ -= panSpeed * Mathf.Clamp(transform.position.y, 25f, Mathf.Infinity) * Time.deltaTime;
        }
        if (Input.GetKey("s"))
        {
            inputZ += panSpeed * Mathf.Clamp(transform.position.y, 25f, Mathf.Infinity) * Time.deltaTime;
        }
        if (Input.GetKey("d"))
        {
            inputX -= panSpeed * Mathf.Clamp(transform.position.y, 25f, Mathf.Infinity) * Time.deltaTime;
        }
        if (Input.GetKey("a"))
        {
            inputX += panSpeed * Mathf.Clamp(transform.position.y, 25f, Mathf.Infinity) * Time.deltaTime;
        }

        MoveCamera(inputX, inputZ);
    }

    void ButtonRotationInputs()
    {
        if (Input.GetKeyDown("q") || Input.GetKeyDown("e"))
        {
            Ray inputRay = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            RaycastHit hit;
            if (Physics.Raycast(inputRay, out hit))
            {
                rotateAroundPoint = hit.point;
            }
            else
            {
                rotateAroundPoint = new Vector3(Camera.main.transform.position.x, 0, Camera.main.transform.position.z);
            }
        }

        if (Input.GetKey("q"))
        {
            transform.RotateAround(rotateAroundPoint, Vector3.up, rotateSpeed * Time.deltaTime);
        }
        if (Input.GetKey("e"))
        {
            transform.RotateAround(rotateAroundPoint, Vector3.up, -rotateSpeed * Time.deltaTime);
        }
    }

    // zoom via scrollwheel
    void ZoomCamera()
    {
        Vector3 pos = transform.position;
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        pos.y -= scroll * scrollSpeed * Time.deltaTime * 300f;
        pos.y = Mathf.Clamp(pos.y, minY, maxY);
        transform.position = pos;
    }
}
