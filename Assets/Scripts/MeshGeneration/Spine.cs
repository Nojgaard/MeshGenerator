using System;
using System.Collections.Generic;
using UnityEngine;

public class Spine : MonoBehaviour
{
    [Range(0.05f, 1f)]
    public float boneGap = 0.18f;
    public float scrollSensitivity = 1;

    
    [Min(0.1f)]
    public float startRadius = 0.1f;
    [Min(0.1f)]
    public float minRadius = 0.1f;
    [Min(0.1f)]
    public float maxRadius = 1f;

    [Range(0.1f, 2f)]
    public float strength = 1f;
    public GameObject bonePrefab;

    private BodyGenerator body;

    List<Bone> bones;
    public List<Bone> Bones { get {return bones; }}

    public bool AddBone(Quaternion rotation, bool inFront = true) {
        GameObject boneObject = Instantiate(bonePrefab, transform, false);
        boneObject.name = "Bone."+bones.Count;
        HingeJoint joint = boneObject.GetComponent<HingeJoint>();
        if (bones.Count == 0) {
            Bone b = boneObject.GetComponent<Bone>();
            b.boneSettings.strength = strength;
            b.boneSettings.radius = minRadius;
            DestroyImmediate(joint);
            bones.Insert(0, b);
            return true;
        }

        Bone endBone = (inFront)?bones[0]:bones[bones.Count-1];
        boneObject.GetComponent<Bone>().boneSettings.radius = endBone.boneSettings.radius;
        boneObject.GetComponent<Bone>().boneSettings.strength = endBone.boneSettings.strength;

        /*float orgX = endBone.transform.rotation.eulerAngles.x;
        float deltaX = rotation.eulerAngles.x;
        if (deltaX > 180) {deltaX = -180 + (deltaX - 180);}
        if (orgX > 180) {orgX = -180 + (orgX - 180);}
        deltaX -= orgX;
        Vector3 rotationDelta = new Vector3(deltaX, 0, 0);
        
        Debug.Log(rotationDelta.x +", " + endBone.transform.rotation.eulerAngles.x + ", " + rotation.eulerAngles.x);
        rotationDelta.x = Mathf.Clamp(rotationDelta.x, -30, 30);
*/
        if (inFront) {
            boneObject.transform.position = bones[0].transform.position;
            boneObject.transform.rotation = bones[0].transform.rotation;
            if (bones.Count > 1) {
                bones[1].GetComponent<HingeJoint>().connectedBody = boneObject.GetComponent<Rigidbody>();
            }
            bones[0].transform.position += bones[0].transform.forward * boneGap;
            
            //bones[0].transform.rotation = Quaternion.Euler(endBone.transform.rotation.eulerAngles + rotationDelta);
            
            
            boneObject.GetComponent<HingeJoint>().connectedBody = bones[0].GetComponent<Rigidbody>();
            bones.Insert(1, boneObject.GetComponent<Bone>());
        } else {
            boneObject.transform.position = endBone.transform.position - endBone.transform.forward * boneGap;
            
            //boneObject.transform.rotation = rotation;
            boneObject.transform.rotation = endBone.transform.rotation;
            
            boneObject.GetComponent<HingeJoint>().connectedBody = endBone.GetComponent<Rigidbody>();
            bones.Insert(bones.Count, boneObject.GetComponent<Bone>());
        }
        body.UpdateBodyMesh();
        return true;
    }

    public bool RemoveBone(bool inFront) {
        Bone endBone = (inFront)?bones[0]:bones[bones.Count-1];
        bones.Remove(endBone);
        DestroyImmediate(endBone.gameObject);
        body.UpdateBodyMesh();
        return true;
    }

    public void Clear()
    {
        for (int i = transform.childCount-1; i >= 0; i--)
        {
            Destroy(transform.GetChild(i).gameObject);
        }
    }

    public void Move(Vector3 delta) {
        transform.localPosition += delta;
        body.UpdateBodyMesh();
    }

    public void Initialize() {
        bones = new List<Bone>();
        Debug.Log("Intializeing Body");
        for (int i = 0; i < 3; i++) {
            AddBone(Quaternion.identity, true);
        }
        foreach(Bone b in transform.GetComponentsInChildren<Bone>()) {
            b.boneSettings.strength = strength;
            b.boneSettings.radius = startRadius;
        }

        body.UpdateBodyMesh();
    }

    public void OnValidate()  {
        bool shouldUpdateMesh = false;
        foreach(Bone b in transform.GetComponentsInChildren<Bone>()) {
            if (b.boneSettings.strength != strength) {
                shouldUpdateMesh = true;
            }
            b.boneSettings.strength = strength;
        }
        if (shouldUpdateMesh) {
            body.UpdateBodyMesh();
        }
    }

    public void Awake() {
        body = GetComponentInParent<BodyGenerator>();
        Initialize();
    }

    public void Start() {
        
    }
}
