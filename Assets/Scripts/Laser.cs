using UnityEngine;

public class Laser : MonoBehaviour
{
    [Header("Laser Settings")]
    [SerializeField] private float maxConnectDistance = 5f;
    [SerializeField] private float beamWidth = 0.06f;
    [SerializeField] private Color beamColor = Color.red;

    private static readonly System.Collections.Generic.List<Laser> ActiveLasers = new System.Collections.Generic.List<Laser>();

    private Transform beamTransform;
    private LineRenderer lineRenderer;
    private EdgeCollider2D edgeCollider;

    private void Awake()
    {
        ResolveBeamComponents();
        SetupLineRenderer();
        SetupEdgeCollider();
    }

    private void OnEnable()
    {
        if (!ActiveLasers.Contains(this))
        {
            ActiveLasers.Add(this);
        }
    }

    private void OnDisable()
    {
        ActiveLasers.Remove(this);

        if (lineRenderer != null)
        {
            lineRenderer.enabled = false;
        }

        if (edgeCollider != null)
        {
            edgeCollider.enabled = false;
        }
    }

    private void OnValidate()
    {
        maxConnectDistance = Mathf.Max(0f, maxConnectDistance);
        beamWidth = Mathf.Max(0.001f, beamWidth);

        ResolveBeamComponents();

        if (lineRenderer != null)
        {
            SetupLineRenderer();
        }

        if (edgeCollider != null)
        {
            SetupEdgeCollider();
        }
    }

    private void Update()
    {
        if (lineRenderer == null)
        {
            ResolveBeamComponents();
            SetupLineRenderer();
            SetupEdgeCollider();
        }

        if (lineRenderer == null)
        {
            return;
        }

        Laser target = FindNearestLaser();

        if (target == null)
        {
            lineRenderer.enabled = false;

            if (edgeCollider != null)
            {
                edgeCollider.enabled = false;
            }
            return;
        }

        lineRenderer.enabled = true;
        Vector3 startWorld = transform.position;
        Vector3 endWorld = target.transform.position;
        lineRenderer.SetPosition(0, startWorld);
        lineRenderer.SetPosition(1, endWorld);

        if (edgeCollider != null && beamTransform != null)
        {
            edgeCollider.enabled = true;
            edgeCollider.points = new[]
            {
                (Vector2)beamTransform.InverseTransformPoint(startWorld),
                (Vector2)beamTransform.InverseTransformPoint(endWorld)
            };
        }
    }

    private void ResolveBeamComponents()
    {
        beamTransform = GetBeamTransformOrNull();
        lineRenderer = GetChildLineRendererOrNull();

        if (beamTransform == null)
        {
            edgeCollider = null;
            return;
        }

        edgeCollider = beamTransform.GetComponent<EdgeCollider2D>();
        if (edgeCollider == null)
        {
            edgeCollider = beamTransform.gameObject.AddComponent<EdgeCollider2D>();
        }
    }

    private Transform GetBeamTransformOrNull()
    {
        if (transform.childCount <= 0)
        {
            return null;
        }

        return transform.GetChild(0);
    }

    private LineRenderer GetChildLineRendererOrNull()
    {
        Transform child = GetBeamTransformOrNull();
        return child != null ? child.GetComponent<LineRenderer>() : null;
    }

    private Laser FindNearestLaser()
    {
        Laser nearest = null;
        float nearestSqrDistance = maxConnectDistance * maxConnectDistance;
        Vector3 currentPosition = transform.position;

        for (int i = 0; i < ActiveLasers.Count; i++)
        {
            Laser candidate = ActiveLasers[i];

            if (candidate == null || candidate == this || !candidate.isActiveAndEnabled)
            {
                continue;
            }

            float sqrDistance = (candidate.transform.position - currentPosition).sqrMagnitude;

            if (sqrDistance <= nearestSqrDistance)
            {
                nearestSqrDistance = sqrDistance;
                nearest = candidate;
            }
        }

        return nearest;
    }

    private void SetupLineRenderer()
    {
        if (lineRenderer == null)
        {
            return;
        }

        lineRenderer.positionCount = 2;
        lineRenderer.useWorldSpace = true;
        lineRenderer.startWidth = beamWidth;
        lineRenderer.endWidth = beamWidth;
        lineRenderer.startColor = beamColor;
        lineRenderer.endColor = beamColor;
        lineRenderer.numCapVertices = 4;
        lineRenderer.enabled = false;
    }

    private void SetupEdgeCollider()
    {
        if (edgeCollider == null)
        {
            return;
        }

        edgeCollider.enabled = false;
    }
}
