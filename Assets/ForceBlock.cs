using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;

public class ForceBlock : MonoBehaviour
{
    [Header("Move")]
    [SerializeField] private float moveDistance = 1f;
    [SerializeField] private float moveDuration = 0.2f;
    [SerializeField] private Ease moveEase = Ease.OutQuad;

    [Header("Target Filter")]
    [SerializeField] private bool onlyAffectPlayer = true;

    [Header("Glow")]
    [SerializeField] private SpriteRenderer targetRenderer;
    [SerializeField] private float glowFrom = 0f;
    [SerializeField] private float glowTo = 5f;
    [SerializeField] private float glowDuration = 0.2f;
    [SerializeField] private Ease glowEase = Ease.OutQuad;

    private static readonly int GlowIntensityPropertyId = Shader.PropertyToID("_Glow");

    private Material runtimeMaterial;
    private bool hasGlowIntensityProperty;
    private Tween glowTween;
    private float currentGlow;
    private int insideCount;
    private readonly Dictionary<CubeController, int> cubeOverlapCounts = new Dictionary<CubeController, int>();

    private void Awake()
    {
        Collider2D col = GetComponent<Collider2D>();
        col.isTrigger = true;

        if (targetRenderer == null)
        {
            targetRenderer = GetComponent<SpriteRenderer>();
        }

        if (targetRenderer != null)
        {
            runtimeMaterial = targetRenderer.material;
            hasGlowIntensityProperty = runtimeMaterial != null && runtimeMaterial.HasProperty(GlowIntensityPropertyId);

            if (hasGlowIntensityProperty)
            {
                currentGlow = glowFrom;
                runtimeMaterial.SetFloat(GlowIntensityPropertyId, currentGlow);
            }
        }
    }

    private void OnDisable()
    {
        glowTween?.Kill();
        glowTween = null;

        insideCount = 0;
        if (runtimeMaterial != null && hasGlowIntensityProperty)
        {
            currentGlow = glowFrom;
            runtimeMaterial.SetFloat(GlowIntensityPropertyId, currentGlow);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!TryGetTarget(other, out Rigidbody2D body, out CubeController cube))
        {
            return;
        }

        insideCount++;
        if (cube != null)
        {
            cubeOverlapCounts.TryGetValue(cube, out int overlapCount);
            cubeOverlapCounts[cube] = overlapCount + 1;

            if (overlapCount == 0)
            {
                cube.ForceMove(transform.up, moveDistance, moveDuration, moveEase);
            }
        }

        PlayGlow();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!TryGetTarget(other, out _, out CubeController cube))
        {
            return;
        }

        if (cube != null && cubeOverlapCounts.TryGetValue(cube, out int overlapCount))
        {
            overlapCount = Mathf.Max(0, overlapCount - 1);
            if (overlapCount == 0)
            {
                cubeOverlapCounts.Remove(cube);
            }
            else
            {
                cubeOverlapCounts[cube] = overlapCount;
            }
        }

        insideCount = Mathf.Max(0, insideCount - 1);
        if (insideCount == 0)
        {
            StopGlow();
        }
    }

    private bool TryGetTarget(Collider2D other, out Rigidbody2D body, out CubeController cube)
    {
        body = null;
        cube = null;

        if (other == null)
        {
            return false;
        }

        cube = other.GetComponentInParent<CubeController>();
        if (cube == null)
        {
            return false;
        }

        if (cube.CurrentColor != CubeController.CubeColor.Blue)
        {
            return false;
        }

        body = other.attachedRigidbody;
        if (body == null)
        {
            body = other.GetComponentInParent<Rigidbody2D>();
        }

        return body != null;
    }

    private void PlayGlow()
    {
        if (runtimeMaterial == null || !hasGlowIntensityProperty)
        {
            return;
        }

        glowTween?.Kill();
        currentGlow = glowFrom;
        runtimeMaterial.SetFloat(GlowIntensityPropertyId, currentGlow);
        glowTween = DOTween
            .To(() => currentGlow, value =>
            {
                currentGlow = value;
                runtimeMaterial.SetFloat(GlowIntensityPropertyId, value);
            }, glowTo, glowDuration)
            .SetEase(glowEase);
    }

    private void StopGlow()
    {
        if (runtimeMaterial == null || !hasGlowIntensityProperty)
        {
            return;
        }

        glowTween?.Kill();
        glowTween = DOTween
            .To(() => currentGlow, value =>
            {
                currentGlow = value;
                runtimeMaterial.SetFloat(GlowIntensityPropertyId, value);
            }, glowFrom, glowDuration)
            .SetEase(glowEase);
    }
}
