using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//[ExecuteInEditMode]
public class BodyGenerator : MonoBehaviour
{
    private bool updateRequested = false;
    public bool selected = false;
    
    [Header("Cube Grid Settings")]
    public Vector3 gridSize;
    public float voxelSize = .5f;
    public float threshold = .5f;

    public ComputeShader computeShader;

    private CubeGrid cubeGrid;

    [HideInInspector]
    public Spine spine;

    [HideInInspector]
    public CameraOrbit cameraController;

    // input variables
    private bool mouseDown = false;
    private Vector3 mouseDownPosition = new Vector2();
    private Vector3 dragPosition = new Vector2();
    private bool  freezeDrag = false;

    BodyGenerator() {
        cubeGrid = new CubeGrid(gridSize, voxelSize, computeShader);
    }

    public void SetFreezeDrag(bool val) {
        freezeDrag = val;
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
        Gizmos.DrawWireCube(this.transform.position, gridSize);
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
        UpdateSelected();
    }

    void MakeMatOpaque() {
        Material mat = GetComponent<MeshRenderer>().material;
        mat.SetFloat("_Mode", 0);
        mat.renderQueue = 2000;
        mat.SetInt("_SrcBlend", 1);
        mat.SetInt("_DstBlend", 0);
        mat.SetInt("_ZWrite", 1);
        mat.EnableKeyword("_ALPHATEST_ON");
        mat.DisableKeyword("_ALPHABLEND_ON");
        mat.EnableKeyword("_ALPHAPREMULTIPLY_ON");
    }

    void MakeMatTransparent() {
        Material mat = GetComponent<MeshRenderer>().material;
        mat.SetFloat("_Mode", 3);
     
        mat.renderQueue = 3000;
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
    }

    void UpdateSelected() {
        if (selected) {
            MakeMatTransparent();
        } else {
            MakeMatOpaque();
        }
        foreach(Renderer r in GetComponentsInChildren<Renderer>()) {
            if(r.gameObject == gameObject) { continue; }
            r.enabled = selected;
        }
        foreach(Collider r in GetComponentsInChildren<Collider>()) {
            if (r.gameObject == gameObject) { continue; }
            r.enabled = selected;
        }
    }

    void HandleInput() {
        if (Input.GetMouseButtonDown(0)) { 
            mouseDown = true;
            mouseDownPosition = Input.mousePosition;
            dragPosition = mouseDownPosition;
            return; 
        } else if (Input.GetMouseButtonUp(0)) {
            mouseDown = false;
        }

        if (Input.GetMouseButtonUp(0) && (Input.mousePosition - mouseDownPosition).magnitude <= .1f){
            RaycastHit hit = new RaycastHit();
            Ray ray = Camera.main.ScreenPointToRay (Input.mousePosition);
            transform.gameObject.layer = 0;
            if (Physics.Raycast(ray, out hit))
            {           
                if (!selected && hit.collider.gameObject == this.gameObject)
                {
                    selected = true;
                    UpdateSelected();
                }
            } else if (selected) {
                selected = false;
                UpdateSelected();
            }
            transform.gameObject.layer = 2;
        } else if (!freezeDrag && mouseDown && (Input.mousePosition - mouseDownPosition).magnitude > .001) {
            Plane plane = new Plane(Camera.main.transform.forward, spine.transform.position);
            Ray ray1 = Camera.main.ScreenPointToRay(dragPosition);
            Ray ray2 = Camera.main.ScreenPointToRay(Input.mousePosition);
            float dist1, dist2;
            if (!plane.Raycast(ray1, out dist1) || !plane.Raycast(ray2, out dist2)) {
                return; 
            }
            Vector3 delta = ray2.GetPoint(dist2) - ray1.GetPoint(dist1);
            spine.Move(delta);
            dragPosition = Input.mousePosition;
        }

    }

    // Update is called once per frame
    void Update()
    {
        if (updateRequested) {
            updateRequested = false;
            //Debug.Log("Updating Body Generator " + computeShader.name + ".");
            cubeGrid.UpdateGrid(threshold, this.transform.position, gridSize, voxelSize, computeShader);
            Mesh mesh = this.GetComponent<MeshFilter>().sharedMesh;
            cubeGrid.March(spine.GetComponentsInChildren<Bone>(), mesh);

            GetComponent<MeshCollider>().sharedMesh  = mesh;
           // GetComponent<MeshCollider>().gameObject.layer = 2;
        }
        HandleInput();
    }
}
