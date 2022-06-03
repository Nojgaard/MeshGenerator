using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraOrbit : MonoBehaviour
{
    [Header("Rotation")]
    public float mouseSensitivity = 1;
    public float rotationSmoothing = 1;

    [Header("Zoom")]
    public float zoomSensitivity = 1;
    public float zoomSmoothing = 1;
    public float minZoom = -1;
    public float maxZoom = -5;

    private new Camera camera;
    private Vector2 velocity;
    private Vector2 targetRotation;
    private float targetZoom;
    public bool freezeZoom;

    public static float ClampAngle(float angle, float min, float max)
    {
        if (angle < -360f) { angle += 360f; }
        if (angle > 360f) { angle -= 360f; }

        return Mathf.Clamp(angle, min, max);
    }

    public void Awake() {
        camera = GetComponentInChildren<Camera>();
        Vector3 rot = transform.rotation.eulerAngles;
        targetRotation = new Vector2(rot.x, rot.y);
        targetZoom = camera.transform.localPosition.z;
        freezeZoom = false;
    }

    private void OnRotate() {
        if (Input.GetMouseButton(1)) {
            velocity.x += mouseSensitivity * Input.GetAxis("Mouse X");
            velocity.y += mouseSensitivity * Input.GetAxis("Mouse Y");
        }

        targetRotation.y += velocity.x % 360;
        targetRotation.x -= velocity.y % 360;
        //targetRotation.x = ClampAngle(targetRotation.x, minMaxRotation.x, minMaxRotation.y);

        transform.rotation = Quaternion.Euler(targetRotation.x, targetRotation.y, 0);

        velocity.x = Mathf.Lerp(velocity.x, 0, Time.deltaTime * rotationSmoothing);
        velocity.y = Mathf.Lerp(velocity.y, 0, Time.deltaTime * rotationSmoothing);
    }

    private void OnZoom() {
        if (freezeZoom) { return; }
        targetZoom = Mathf.Clamp(targetZoom + Input.mouseScrollDelta.y * zoomSensitivity, maxZoom, minZoom);
        Vector3 pos = camera.transform.localPosition;
        pos.z = Vector3.Lerp(camera.transform.localPosition, new Vector3(0,0,targetZoom), Time.deltaTime * zoomSmoothing).z;
        camera.transform.localPosition = pos;
    }
    
    // Update is called once per frame
    void LateUpdate()
    {
        OnRotate();
        OnZoom();
    }
}
