using UnityEngine;
using System;

[Serializable]
public class BoneSettings {
    [HideInInspector]
    public Vector3 position;
    [HideInInspector]
    public Quaternion rotation;
    
    public float radius = 0.5f;
    public float strength = 1;

    public BoneSettings() {}

    public BoneSettings(Vector3 position, Quaternion rotation, float radius, float strength) {
        this.position = position;
        this.rotation = rotation;
        this.radius = radius;
        this.strength = strength;
    }
}
