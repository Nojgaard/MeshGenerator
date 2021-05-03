using System;
using UnityEngine;

public class Spine : MonoBehaviour
{
    [Range(0.1f, 1f)]
    public float boneGap = 0.18f;
    public float scrollSensitivity = 1;
    public float minRadius = 0.1f;
    public float maxRadius = 2f;
    public GameObject bonePrefab;

    private BodyGenerator body;

    public void Clear()
    {
        for (int i = transform.childCount-1; i >= 0; i--)
        {
            Destroy(transform.GetChild(i).gameObject);
        }
    }

    public void Initialize() {
        Debug.Log("Intializeing Body");
        for (int i = 0; i < 3; i++) {
            GameObject boneObject = Instantiate(bonePrefab, transform, false);
            boneObject.name = "Bone." + i;
            boneObject.transform.position = new Vector3(boneGap*i,0,0);
            boneObject.GetComponent<Bone>().boneSettings.radius = minRadius;
            HingeJoint joint = boneObject.GetComponent<HingeJoint>();
            if (i == 0) {
                DestroyImmediate(joint);
                continue;
            }
            joint.connectedBody = transform.GetChild(i-1).GetComponent<Rigidbody>();
        }

        body.UpdateBodyMesh();
    }

    public void OnValidate()  {
    }

    public void Awake() {
        body = GetComponentInParent<BodyGenerator>();
    }

    public void Start() {
        Initialize();
    }
}
