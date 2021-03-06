using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bone : MonoBehaviour
{
    public BoneSettings boneSettings = new BoneSettings();

    private BodyGenerator body;

    private bool isHovering;

    // Return the *Local* position
    public Vector3 BallCenter { get { return -transform.up * .34f * boneSettings.radius + transform.localPosition + body.spine.transform.localPosition; }}

    void OnDrawGizmosSelected()
    {
        Gizmos.DrawCube(-transform.up * .34f * boneSettings.radius + transform.position, new Vector3(0.05f, .05f, .05f));
    }

    void OnMouseDrag() {
        Plane plane = new Plane(Camera.main.transform.forward, transform.position);
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        float dist;
        if (!plane.Raycast(ray, out dist)) {
            return;
        }

        Vector3 point = ray.GetPoint(dist);
        point.x = transform.position.x;
        transform.position = point;

        body.UpdateBodyMesh();
    }

    void OnMouseEnter() {
        //if (Input.GetMouseButton(0) || Input.GetMouseButton(1)) {return;}
        body.cameraController.freezeZoom = true;
        isHovering = true;
        body.SetFreezeDrag(true);
    }

    void OnMouseExit() {
        //if (Input.GetMouseButton(0) || Input.GetMouseButton(1)) {return;}
        body.cameraController.freezeZoom = false;
        isHovering = false;
        body.SetFreezeDrag(false);
    }

    void OnScroll() {
        if (!isHovering || Input.mouseScrollDelta.y  == 0) { return; }
        boneSettings.radius += Input.mouseScrollDelta.y * body.spine.scrollSensitivity;
        boneSettings.radius = Mathf.Clamp(boneSettings.radius, body.spine.minRadius, body.spine.maxRadius);
        body.UpdateBodyMesh();
    }

    void Awake() {
        body = transform.parent.transform.parent.GetComponent<BodyGenerator>();
        isHovering = false;
    }

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        OnScroll();        
    }
}
