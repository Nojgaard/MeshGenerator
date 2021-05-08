using System.Collections;
using System.Collections.Generic;
using UnityEngine;


struct GPUVertex {
    public int idx;
    public Vector3 pos;
    public Vector3 normal;
}

struct GPUTriangle {
    public int a, b, c;
    //public Vector3 a, b, c, na, nb, nc;
    //public int edgeA, edgeB, edgeC;
}

struct GPUBall {
    public float strength;
    public float radius;
    public Vector3 position;
}

public class CubeGrid
{
    private Vector3 gridSize;
    private float voxelSize;
    private Vector3Int numVoxels;


    //private int gridWidth;
    //private float cubeWidth;
    //private int numCubesPerAxis;
    private float threshold;
    private Vector3[] positions;
    private int[] vertexIds;
    private Vector3 center;
    private ComputeBuffer metaballsBuffer;
    private ComputeBuffer positionsBuffer;
    private ComputeBuffer vertexBuffer;
    private ComputeBuffer normalBuffer;
    private ComputeBuffer triangleBuffer;

    private int[] countArray = {0};
    private ComputeBuffer countBuffer;

    private ComputeShader shader;

    public CubeGrid(Vector3  gridSize, float voxelSize, ComputeShader shader) {
        this.shader = shader;
        this.gridSize = gridSize;
        this.voxelSize = voxelSize;
        numVoxels = new Vector3Int((int)Mathf.Ceil(gridSize.x/voxelSize), (int)Mathf.Ceil(gridSize.y/voxelSize), (int)Mathf.Ceil(gridSize.z/voxelSize));

        InitializePositions();
    }
	void GenerateTables () {
        int[] cubeEdges = new int[24];
		int k = 0;
		for( int i=0; i<8; ++i) {
			for( int j=1; j<=4; j<<=1) {
				int p = i^j;
				if(i <= p) {
					cubeEdges[k++] = i;
					cubeEdges[k++] = p;
				}
			}
		}
        string stredgeTable = "[";
        for (int i = 0; i < 24; i++) {
            stredgeTable  += cubeEdges[i] + ", ";
        }

        string table = "";
        int[] edgeTable = new int[256];
        Debug.Log(stredgeTable);

        		for(int i=0; i<256; ++i) {
			int em = 0;
			for(int j=0; j<24; j+=2) {
				bool a = (i & (1 << cubeEdges [j])) != 0;
				var b = (i & (1 << cubeEdges[j+1])) != 0;
				em |= a != b ? (1 << (j >> 1)) : 0;
			}
			edgeTable[i] = em;
            table += "0x" + em.ToString("X").ToLower() + ", ";
		}
        Debug.Log(table);
	}

    public void UpdateGrid(float threshold, Vector3 center, Vector3 gridSize, float voxelSize, ComputeShader shader) {
        //GenerateTables();
        this.center = center;
        this.threshold = threshold;
        if (shader != null || shader.name != this.shader.name) {
            ReleaseBuffers();
        }
        this.shader = shader;
        if (voxelSize != this.voxelSize || this.gridSize != gridSize) {    
            this.gridSize = gridSize;
            this.voxelSize = voxelSize;
            numVoxels = new Vector3Int((int)Mathf.Ceil(gridSize.x/voxelSize), (int)Mathf.Ceil(gridSize.y/voxelSize), (int)Mathf.Ceil(gridSize.z/voxelSize));
            InitializePositions();
        }

    }

    void InitializePositions() {
        float d = voxelSize / 2;
        positions = new Vector3[numVoxels.x * numVoxels.y * numVoxels.z];
        vertexIds = new int[numVoxels.x * numVoxels.y * numVoxels.z];
        Debug.Log(numVoxels);
        for (int z = 0; z < numVoxels.z; ++z) {
            for (int y = 0; y < numVoxels.y; ++y) {
                for (int x = 0; x < numVoxels.x; ++x) {
                    Vector3 pos = new Vector3(d + voxelSize * x, d + voxelSize * y, d + voxelSize * z);
                    pos = pos - new Vector3(gridSize.x/2f,gridSize.y/2f,gridSize.z/2f);
                    int idx = x + y * numVoxels.x + z * numVoxels.x * numVoxels.y;
                    positions[idx] = pos;
                }
            }
        } 
    }

    private int GetCount(ComputeBuffer buffer) {
        ComputeBuffer.CopyCount (buffer, countBuffer, 0);
        countBuffer.GetData (countArray);
        return countArray[0];
    }

    public void March(Bone[] metaballs, Mesh mesh) {
        Debug.Log("Marching with " + metaballs.Length + " balls");
        CreateBuffers();
        //positionsBuffer.SetData(positions);

        GPUBall[] gpuBalls = new GPUBall[metaballs.Length];
        for(int i = 0; i < metaballs.Length; i++) {
            Bone metaball = metaballs[i];
            //gpuBalls[i].position = metaball.transform.position;
            gpuBalls[i].position = metaball.BallCenter;
            gpuBalls[i].radius = metaball.boneSettings.radius;
            gpuBalls[i].strength = metaball.boneSettings.strength;
        }
        metaballsBuffer.SetData(gpuBalls);
        shader.SetBuffer(0, "metaballs", metaballsBuffer);
        shader.SetInt("numBalls", metaballs.Length);

        //shader.SetInt("numCubesPerAxis", numCubesPerAxis);
        shader.SetInts("numVoxels", new int[] {numVoxels.x, numVoxels.y, numVoxels.z});
        shader.SetFloat("threshold", threshold);
        shader.SetFloat("cubeLength", voxelSize/2);
        shader.SetVector("gridCenter", center);

        triangleBuffer.SetCounterValue(0);
        vertexBuffer.SetCounterValue(0);
        normalBuffer.SetCounterValue(0);


        //int numThreadsPerAxis = Mathf.CeilToInt (numCubesPerAxis / (float) 8);

        shader.Dispatch(0,  Mathf.CeilToInt (numVoxels.x / 8f), Mathf.CeilToInt (numVoxels.y / 8f), Mathf.CeilToInt (numVoxels.z / 8f));

         // Get number of triangles in the triangle buffer
        int numTris = GetCount(triangleBuffer);

        int vertexCount = GetCount(vertexBuffer);
        Debug.Log("Vertex Count " + vertexCount);

        // Get triangle data from shader
        GPUTriangle[] tris = new GPUTriangle[numTris];
        triangleBuffer.GetData(tris, 0, 0, numTris);

        mesh.Clear ();

        if (shader.name == "MarchingCubes"){
            CreateMarchingCubesMesh(mesh);
        } else {
            CreateSurfaceNetMesh(mesh);
        }
    }

    public void CreateSurfaceNetMesh(Mesh mesh)  {

        int vertexCount = GetCount(vertexBuffer);
        int triangleCount = GetCount(triangleBuffer);

        var gpuVertices = new GPUVertex[vertexCount];
        vertexBuffer.GetData(gpuVertices, 0, 0, vertexCount);
/*
        foreach (GPUVertex v in gpuVertices) {
            vertexIds[v.idx] = -1;
        }
  */      
        var vertices = new Vector3[vertexCount];
        var normals = new Vector3[vertexCount];
        for (int i = 0; i < vertexCount; i++) {
            GPUVertex v = gpuVertices[i];
            vertexIds[v.idx] = i;
            vertices[i] = v.pos;
            normals[i] = v.normal;
            //Debug.Log("Vertex: " + v.idx + ", " + v.pos + ", " + v.normal.x);
        }

        GPUTriangle[] tris = new GPUTriangle[triangleCount];
        triangleBuffer.GetData(tris, 0, 0, triangleCount);
        
        var meshTriangles = new int[triangleCount * 3];
        for (int i = 0; i < triangleCount; i++) {
            //Debug.Log(vertexIds[tris[i].a] + ", " + vertexIds[tris[i].b] + ", " + vertexIds[tris[i].c]);
            meshTriangles[i * 3] =  vertexIds[tris[i].a];
            meshTriangles[i * 3 + 1] = vertexIds[tris[i].b];
            meshTriangles[i * 3 + 2] = vertexIds[tris[i].c];
        }        

        mesh.vertices = vertices;
        mesh.triangles = meshTriangles;
        mesh.normals = normals;

        Debug.Log("Mesh Created: " + vertices.Length + ", " + meshTriangles.Length);

        //mesh.RecalculateNormals();
    }

    public void CreateMarchingCubesMesh(Mesh mesh)  {
        int numTris = GetCount(triangleBuffer);

        int vertexCount = GetCount(vertexBuffer);
        Debug.Log("Vertex Count " + vertexCount);

        // Get triangle data from shader
        GPUTriangle[] tris = new GPUTriangle[numTris];
        triangleBuffer.GetData(tris, 0, 0, numTris);
        Debug.Log(numTris);
        var vertices = new Vector3[vertexCount];
        vertexBuffer.GetData(vertices, 0, 0, vertexCount);
        var normals = new Vector3[vertexCount];
        normalBuffer.GetData(normals, 0, 0, vertexCount);
        var meshTriangles = new int[numTris * 3];

        for (int i = 0; i < numTris; i++) {
      //      Debug.Log(tris[i].a + ", " + tris[i].b + ", " + tris[i].c);
            meshTriangles[i * 3] =  tris[i].c;
            meshTriangles[i * 3 + 1] = tris[i].b;
            meshTriangles[i * 3 + 2] = tris[i].a;
        }
        Debug.Log("Mesh Created: " + vertices.Length + ", " + meshTriangles.Length);
        mesh.vertices = vertices;
        mesh.triangles = meshTriangles;
        mesh.normals = normals;
    }

    public void DrawGrid() {
        //Gizmos.color = Color.red;
        foreach(Vector3 pos in positions) {
            Gizmos.DrawWireCube(pos, new Vector3(voxelSize, voxelSize, voxelSize));
        }
    }

    public void CreateSurfaceNetBuffers(int numCubes, int maxTriangleCount) {
        vertexBuffer = new ComputeBuffer(numCubes, sizeof(int) + sizeof(float) * 3 * 2, ComputeBufferType.Counter);
        triangleBuffer = new ComputeBuffer(maxTriangleCount, sizeof(int) * 3, ComputeBufferType.Append);        
        normalBuffer = new ComputeBuffer(1, sizeof(float), ComputeBufferType.Counter);
    }

    public void CreateMarchingCubesBuffers(int numCubes, int maxTriangleCount) {
        vertexBuffer = new ComputeBuffer(maxTriangleCount, sizeof(float) * 3 * 3, ComputeBufferType.Counter);
        normalBuffer = new ComputeBuffer(maxTriangleCount, sizeof(float) * 3 * 3, ComputeBufferType.Counter);
        triangleBuffer = new ComputeBuffer(maxTriangleCount, sizeof(int) * 3, ComputeBufferType.Append);
    }

    void PrepareShader() {
        positionsBuffer.SetData(positions);
        shader.SetBuffer(0, "cubePositions", positionsBuffer);
        shader.SetBuffer(0, "vertexBuffer", vertexBuffer);
        shader.SetBuffer(0, "normalBuffer", normalBuffer);
        shader.SetBuffer(0, "triangles", triangleBuffer);
    }

    void CreateBuffers() {
        int numCubes = numVoxels.x * numVoxels.y * numVoxels.z;
        int maxTriangleCount = 5 * numCubes;
        if (positionsBuffer == null || positionsBuffer.count != numCubes) {
            ReleaseBuffers();
            positionsBuffer = new ComputeBuffer(numCubes, sizeof(float) * 3);
            metaballsBuffer = new ComputeBuffer(20, sizeof(float) * 5);
            countBuffer = new ComputeBuffer (1, sizeof (int), ComputeBufferType.Raw);
            if (shader.name == "MarchingCubes") {
                CreateMarchingCubesBuffers(numCubes, maxTriangleCount);
            } else {
                CreateSurfaceNetBuffers(numCubes, maxTriangleCount);
            }
            PrepareShader();
        }
    }

    public void ReleaseBuffers() {
        if (positionsBuffer == null) {
            return;
        }
        positionsBuffer.Release();
        vertexBuffer.Release();
        normalBuffer.Release();
        triangleBuffer.Release();
        countBuffer.Release();
        metaballsBuffer.Release();
        metaballsBuffer = null;
        positionsBuffer = null;
        vertexBuffer = null;
        normalBuffer = null;
        triangleBuffer = null;
        countBuffer = null;
    }
}
