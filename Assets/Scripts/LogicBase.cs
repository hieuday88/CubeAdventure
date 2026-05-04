using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class LogicBase : MonoBehaviour
{
    private Color offColor = new Color(90f / 255f, 90f / 255f, 90f / 255f);
    private Color onColor = new Color(1f, 1f, 1f);

    [SerializeField] private LineRenderer line;

    [Header("Glow")]
    [SerializeField] private float glowOff = 0f;
    [SerializeField] private float glowOn = 5f;

    private static readonly int GlowIntensityPropertyId = Shader.PropertyToID("_Glow");
    private const string GlowKeyword = "GLOW_ON";

    private SpriteRenderer spriteRenderer;
    private Material lineMaterial;
    private bool lineMaterialHasGlow;

    [Header("Events")]
    [SerializeField] private UnityEvent onAllInputsOn;

    private bool lastAllOn;

    public List<LogicInput> inputs = new List<LogicInput>();

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (line == null)
        {
            line = GetComponentInChildren<LineRenderer>();
        }

        if (line != null)
        {
            lineMaterial = line.material;
            lineMaterialHasGlow = lineMaterial != null && lineMaterial.HasProperty(GlowIntensityPropertyId);
        }
    }

    private void OnEnable()
    {
        ObserverManager<LogicEventId>.AddListener(LogicEventId.LogicInputChanged, HandleInputChanged);
        lastAllOn = false;
        CheckInput();
    }

    private void OnDisable()
    {
        ObserverManager<LogicEventId>.RemoveListener(LogicEventId.LogicInputChanged, HandleInputChanged);
    }

    private void HandleInputChanged(object payload)
    {
        LogicInput changedInput = payload as LogicInput;
        if (changedInput == null)
        {
            return;
        }

        if (!inputs.Contains(changedInput))
        {
            return;
        }

        CheckInput();
    }

    private void CheckInput()
    {
        bool allOn = true;
        foreach (LogicInput input in inputs)
        {
            if (input == null || !input.IsOn)
            {
                allOn = false;
                break;
            }
        }

        if (allOn && !lastAllOn)
        {
            onAllInputsOn?.Invoke();
        }
        lastAllOn = allOn;

        if (spriteRenderer != null)
        {
            spriteRenderer.color = allOn ? onColor : offColor;
        }

        if (line != null)
        {
            Material mat = lineMaterial != null ? lineMaterial : line.material;
            if (mat != null && (lineMaterialHasGlow || mat.HasProperty(GlowIntensityPropertyId)))
            {
                if (allOn)
                {
                    mat.EnableKeyword(GlowKeyword);
                    mat.SetFloat(GlowIntensityPropertyId, glowOn);
                }
                else
                {
                    mat.SetFloat(GlowIntensityPropertyId, glowOff);
                    if (glowOff <= 0f)
                    {
                        mat.DisableKeyword(GlowKeyword);
                    }
                }
            }
        }
    }
}
