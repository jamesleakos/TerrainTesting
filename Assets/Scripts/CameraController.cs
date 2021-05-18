﻿using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Movement")]
    // Button Movement Input Vars
    public float panSpeed = 20;
    public float scrollSpeed = 20;

    // Mouse Movement Input Vars
    Vector3 lastMousePosition;
    public float dragSpeed = 0.01f;

    [Header("Rotation")]
    // Button Rotation Vars
    public float rotateSpeed = 40;
    public float lookAhead = 2f;

    [Header("Camera Bounds")]
    public float minY = 5f;
    public float maxY = 30f;

    void Update()
    {
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

            MoveCamera(-1 * dragSpeed * delta.x, -1 * dragSpeed * delta.y);

            lastMousePosition = Input.mousePosition;
        }
    }

    // get keyboard inputs
    void ButtonMovementInputs()
    {
        float inputZ = 0f;
        float inputX = 0f;

        if (Input.GetKey("w"))
        {
            inputZ += panSpeed * Time.deltaTime;
        }
        if (Input.GetKey("s"))
        {
            inputZ -= panSpeed * Time.deltaTime;
        }
        if (Input.GetKey("d"))
        {
            inputX += panSpeed * Time.deltaTime;
        }
        if (Input.GetKey("a"))
        {
            inputX -= panSpeed * Time.deltaTime;
        }

        MoveCamera(inputX, inputZ);
    }

    void ButtonRotationInputs()
    {
        if (Input.GetKey("q") || Input.GetKey("e"))
        {
            float xlook = Mathf.Sin(transform.eulerAngles.y * Mathf.PI / 180) * lookAhead;
            float zlook = Mathf.Cos(transform.eulerAngles.y * Mathf.PI / 180) * lookAhead;

            float finalRotateSpeed = 0;

            if (Input.GetKey("q"))
            {
                finalRotateSpeed = rotateSpeed * Time.deltaTime;
            }
            if (Input.GetKey("e"))
            {
                finalRotateSpeed = -rotateSpeed * Time.deltaTime;
            }

            transform.RotateAround(transform.position + new Vector3(xlook, 0, zlook), Vector3.up, finalRotateSpeed * Time.deltaTime);
        }

        if (Input.GetKey("q"))
        {
            float lookAhead = 2f;
            float xlook = Mathf.Sin(transform.eulerAngles.y * Mathf.PI / 180) * lookAhead;
            float zlook = Mathf.Cos(transform.eulerAngles.y * Mathf.PI / 180) * lookAhead;
            transform.RotateAround(transform.position + new Vector3 (xlook, 0, zlook), Vector3.up, rotateSpeed * Time.deltaTime);
        }
        if (Input.GetKey("e"))
        {
            float lookAhead = 2f;
            float xlook = Mathf.Sin(transform.eulerAngles.y * Mathf.PI / 180) * lookAhead;
            float zlook = Mathf.Cos(transform.eulerAngles.y * Mathf.PI / 180) * lookAhead;
            transform.RotateAround(transform.position + new Vector3(xlook, 0, zlook), Vector3.up, -rotateSpeed * Time.deltaTime);
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
