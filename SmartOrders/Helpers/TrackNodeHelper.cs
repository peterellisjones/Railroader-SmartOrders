using System;
using System.Collections;
using Track;
using UnityEngine;

namespace SmartOrders.Helpers;

public sealed class TrackNodeHelper : MonoBehaviour
{
    private static readonly Material _LineMaterial = new(Shader.Find("Universal Render Pipeline/Lit")!);

    private LineRenderer? _LineRenderer;

    private LineRenderer CreateLineRenderer() {
        var lineRenderer = gameObject!.AddComponent<LineRenderer>()!;
        lineRenderer.material = _LineMaterial;
        lineRenderer.material.color = Color.yellow;
        lineRenderer.startWidth = 0.05f;
        lineRenderer.positionCount = 5;
        lineRenderer.useWorldSpace = false;
        lineRenderer.SetPosition(0, new Vector3(-0.2f, 0.5f, 0));
        lineRenderer.SetPosition(1, new Vector3(0, 0, 0));
        lineRenderer.SetPosition(2, new Vector3(0, 4, 0));
        lineRenderer.SetPosition(3, new Vector3(0, 0, 0));
        lineRenderer.SetPosition(4, new Vector3(0.2f, 0.5f, 0));
        lineRenderer.enabled = true;
        return lineRenderer;
    }

    public void Show(TrackNode node) {
        gameObject!.transform!.SetParent(node.transform!);
        gameObject.transform.localPosition = Vector3.zero;
        gameObject.transform.localRotation = Quaternion.identity;

        _LineRenderer ??= CreateLineRenderer();
        _LineRenderer.enabled = true;
        StartCoroutine(Routine());
    }

    private IEnumerator Routine() {
        yield return new WaitForSecondsRealtime(2f);
        _LineRenderer!.enabled = false;
        OnHidden?.Invoke();
    }

    public Action? OnHidden;
}
