using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoneAdder : MonoBehaviour
{
    public bool appendFront = true;
    public float offset = 0.2f;

    Spine spine;


    void OnMouseDrag() {
        Plane plane = new Plane(Camera.main.transform.forward, transform.position);
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        float dist;
        if (!plane.Raycast(ray, out dist)) {
            return;
        }

        Vector3 point = ray.GetPoint(dist);
        Vector3 displacement = point - transform.position;
        if  (displacement.magnitude < spine.boneGap) {
            return;
        }

        float relDirection = Vector3.Dot(displacement.normalized, transform.forward);
        Debug.Log(relDirection);
        if (relDirection > 0.1f) {
            Vector3 direction = displacement.normalized;
            
            Quaternion rotation = Quaternion.LookRotation(direction, transform.up);
            spine.AddBone(rotation, appendFront);
        } else if (relDirection < -0.1f) {
            spine.RemoveBone(appendFront);
        }
    }

    void UpdatePosition() {
        Bone b = (appendFront) ? spine.Bones[0] : spine.Bones[spine.Bones.Count - 1];
        /*transform.localPosition = b.transform.forward * (b.boneSettings.radius + offset);
        transform.rotation = Quaternion.LookRotation(b.transform.forward, b.transform.up);*/


        transform.position = b.transform.position + ((appendFront)?-1:1) * -b.transform.forward * (b.boneSettings.radius + offset);
        Vector3 front = (appendFront)?b.transform.forward : -b.transform.forward;
        transform.rotation = Quaternion.LookRotation(front, -b.transform.up);
    }

    void Awake() {
        spine = transform.parent.GetComponentInChildren<Spine>();
    }

    // Start is called before the first frame update
    void Start()
    {
        UpdatePosition();
    }

    // Update is called once per frame
    void Update()
    {
        UpdatePosition();
    }
}
