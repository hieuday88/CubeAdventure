using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class Laser : MonoBehaviour
{
    [Header("Laser Settings")]
    [SerializeField] private float maxConnectDistance = 5f;
    [SerializeField] private float beamWidth = 0.06f;
    [SerializeField] private Color beamColor = Color.red;

    private static readonly System.Collections.Generic.List<Laser> ActiveLasers = new System.Collections.Generic.List<Laser>();

    private LineRenderer lineRenderer;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        SetupLineRenderer();
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
    }

    private void OnValidate()
    {
        maxConnectDistance = Mathf.Max(0f, maxConnectDistance);
        beamWidth = Mathf.Max(0.001f, beamWidth);

        if (lineRenderer == null)
        {
            lineRenderer = GetComponent<LineRenderer>();
        }

        if (lineRenderer != null)
        {
            SetupLineRenderer();
        }
    }

    private void Update()
    {
        Laser target = FindNearestLaser();

        if (target == null)
        {
            lineRenderer.enabled = false;
            return;
        }

        lineRenderer.enabled = true;
        lineRenderer.SetPosition(0, transform.position);
        lineRenderer.SetPosition(1, target.transform.position);
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
        lineRenderer.positionCount = 2;
        lineRenderer.useWorldSpace = true;
        lineRenderer.startWidth = beamWidth;
        lineRenderer.endWidth = beamWidth;
        lineRenderer.startColor = beamColor;
        lineRenderer.endColor = beamColor;
        lineRenderer.numCapVertices = 4;
        lineRenderer.enabled = false;
    }
}
