using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//[ExecuteInEditMode]
public class BodyGenerator : MonoBehaviour
{
    private bool updateRequested = false;
    
    [Header("Cube Grid Settings")]
    public int width = 2;
    public int resolution = 10;
    public float threshold = .5f;

    public ComputeShader computeShader;

    private CubeGrid cubeGrid;

    [HideInInspector]
    public Spine spine;

    [HideInInspector]
    public CameraOrbit cameraController;

    BodyGenerator() {
        cubeGrid = new CubeGrid(resolution, width, computeShader);
    }

    public void UpdateBodyMesh()  {
        updateRequested = true;
    }

    void OnValidate() {
        //Debug.Log("OnValidate");
        updateRequested = true;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireCube(this.transform.position, new Vector3(width, width, width));
        //cubeGrid.DrawGrid();
    }

    void OnDisable() {
        cubeGrid.ReleaseBuffers();
    }

    void Awake() {
        spine = transform.GetComponentInChildren<Spine>();
        cameraController = transform.GetComponentInChildren<CameraOrbit>();
        //Initialize();
    }

    // Start is called before the first frame update
    void Start()
    {
    }

    void UpdateVisibility() {
        if (!Input.GetMouseButtonDown(0)) { return; }
        RaycastHit hit = new RaycastHit();
        Ray ray = Camera.main.ScreenPointToRay (Input.mousePosition);
        if (Physics.Raycast(ray, out hit))
        {           
            if (hit.collider.gameObject == this.gameObject)
            {
                Debug.Log("Click!!!");
            }
            else
            {
                Debug.Log("Click outside");
            
            }
        }

    }

    // Update is called once per frame
    void Update()
    {
        if (updateRequested) {
            updateRequested = false;
            Debug.Log("Updating Body Generator " + computeShader.name + ".");
            cubeGrid.UpdateGrid(threshold, this.transform.position, resolution, width, computeShader);
            Mesh mesh = this.GetComponent<MeshFilter>().sharedMesh;
            cubeGrid.March(spine.GetComponentsInChildren<Bone>(), mesh);

            GetComponent<MeshCollider>().sharedMesh  = mesh;
            GetComponent<MeshCollider>().gameObject.layer = 2;
        }
        UpdateVisibility();
    }
}
