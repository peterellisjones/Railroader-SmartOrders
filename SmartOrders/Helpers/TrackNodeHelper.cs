using System;
using System.Collections;
using Track;
using UnityEngine;

namespace SmartOrders.Helpers;

public sealed class TrackNodeHelper : MonoBehaviour
{
    private const float CenterZ = 2;
    private const float HalfHeight = 0.5f;
    private const float HalfWidth = 0.2f;
    //private const float ArrowSize = 2f;
    //private const float ArrowHeight = 0.5f;
    //private const float ArrowWidth = 0.2f;

    private static readonly Material _LineMaterial = new(Shader.Find("Universal Render Pipeline/Lit")!);
    //private LineRenderer? _LineRenderer;
    //private LineRenderer CreateLineRenderer() {
    //    var lineRenderer = gameObject!.AddComponent<LineRenderer>()!;
    //    lineRenderer.material = _LineMaterial;
    //    lineRenderer.material.color = Color.yellow;
    //    lineRenderer.startWidth = 0.05f;
    //    lineRenderer.positionCount = 5;
    //    lineRenderer.useWorldSpace = false;
    //    lineRenderer.SetPosition(0, new Vector3(-ArrowWidth, ArrowHeight, 0));
    //    lineRenderer.SetPosition(1, new Vector3(0, 0, 0));
    //    lineRenderer.SetPosition(2, new Vector3(0, ArrowSize, 0));
    //    lineRenderer.SetPosition(3, new Vector3(0, 0, 0));
    //    lineRenderer.SetPosition(4, new Vector3(ArrowWidth, ArrowHeight, 0));
    //    lineRenderer.enabled = true;
    //    return lineRenderer;
    //}

    private MeshFilter? _MeshFilter;
    private MeshRenderer? _MeshRenderer;

    private Mesh CreateMesh(float k) {
        // Create a new Mesh
        Mesh mesh = new Mesh();

        var k2 = k / 2;

        // Define vertices of an octahedron
        Vector3[] vertices = new Vector3[] {
            new Vector3(0, k2 * (CenterZ + HalfHeight), 0), // Top vertex
            new Vector3(0, k2 * (CenterZ - HalfHeight), 0), // Bottom vertex
            new Vector3(k * HalfWidth, k2 * CenterZ, 0),    // Front vertex
            new Vector3(k * -HalfWidth, k2 * CenterZ, 0),   // Back vertex
            new Vector3(0, k2 * CenterZ, k * HalfWidth),     // Right vertex
            new Vector3(0, k2 * CenterZ, k * -HalfWidth)     // Left vertex
        };

        // Define the triangles of the octahedron
        int[] triangles = new int[]
        {
            // Top pyramid
            0, 4, 2,
            0, 3, 4,
            0, 5, 3,
            0, 2, 5,

            // Bottom pyramid
            1, 2, 4,
            1, 4, 3,
            1, 3, 5,
            1, 5, 2,
        };

        mesh.vertices = vertices;
        mesh.triangles = triangles;

        mesh.RecalculateNormals();
        mesh.uv = new Vector2[vertices.Length];

        return mesh;
    }
    
    public void Show(TrackNode node, float distanceInMeters) {
        gameObject!.transform!.SetParent(node.transform!);
        gameObject.transform.localPosition = Vector3.zero;
        gameObject.transform.localRotation = Quaternion.identity;
        gameObject.transform.localScale = new Vector3(2, 5, 2);

        //_LineRenderer ??= CreateLineRenderer();
        //_LineRenderer.enabled = true;

        var k = distanceInMeters < 100 ? 1 : (distanceInMeters - 100f) / 50f;
        
        _MeshFilter ??= gameObject!.AddComponent<MeshFilter>()!;
        _MeshFilter.mesh = CreateMesh(k);

        if (_MeshRenderer == null) {
            _MeshRenderer = gameObject.AddComponent<MeshRenderer>()!;
            _MeshRenderer.material =  _LineMaterial;
            _MeshRenderer.material.color = Color.magenta;
        }

        _MeshRenderer.enabled = true;

        StartCoroutine(Routine());
    }

    private IEnumerator Routine() {
        yield return new WaitForSecondsRealtime(2f);
        _MeshRenderer!.enabled = false;
        //_LineRenderer!.enabled = false;
    }

}
