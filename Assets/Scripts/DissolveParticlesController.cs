using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

[DisallowMultipleComponent]
public class DissolveParticlesController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SpriteRenderer targetRenderer;
    [SerializeField] private ShadowCaster2D shadowCaster2DRef;
    [SerializeField] private ParticleSystem particleSystemRef;
    [SerializeField] private ParticleSystem regenParticleSystemRef;

    [Header("Dissolve")]
    [SerializeField] private string dissolveEnabledPropertyName = "_DissolveEnabled";
    [SerializeField] private string weightPropertyName = "_DissolveWeight";
    [SerializeField] private string glowPropertyName = "_DissolveGlow";
    [SerializeField] private string dissolveColorPropertyName = "_DissolveColor";
    [SerializeField] private string disintegrationColorPropertyName = "_DisintegrationColor";
    [SerializeField] private string directionPropertyName = "_DissolveDirection";
    [SerializeField] private float maxGlow = 5f;
    [SerializeField] private float dissolveDuration = 0.8f;
    [SerializeField] private bool disableRendererWhenDone = true;

    [Header("Particle Emission")]
    [SerializeField] private int particlesPerSecond = 80;
    [SerializeField] private int endBurstCount = 18;
    [SerializeField] private Vector2 velocityXRange = new Vector2(-0.8f, 0.8f);
    [SerializeField] private Vector2 velocityYRange = new Vector2(1.4f, 3.2f);

    private MaterialPropertyBlock propertyBlock;
    private int dissolveEnabledPropertyId;
    private int weightPropertyId;
    private int glowPropertyId;
    private int dissolveColorPropertyId;
    private int disintegrationColorPropertyId;
    private int directionPropertyId;
    private Coroutine playRoutine;
    private const string DissolveKeyword = "DISSOLVE_ON";

    private void Awake()
    {
        if (targetRenderer == null)
        {
            targetRenderer = GetComponent<SpriteRenderer>();
        }

        if (shadowCaster2DRef == null)
        {
            shadowCaster2DRef = GetComponent<ShadowCaster2D>();
        }
        propertyBlock = new MaterialPropertyBlock();
        dissolveEnabledPropertyId = Shader.PropertyToID(dissolveEnabledPropertyName);
        weightPropertyId = Shader.PropertyToID(weightPropertyName);
        glowPropertyId = Shader.PropertyToID(glowPropertyName);
        dissolveColorPropertyId = Shader.PropertyToID(dissolveColorPropertyName);
        disintegrationColorPropertyId = Shader.PropertyToID(disintegrationColorPropertyName);
        directionPropertyId = Shader.PropertyToID(directionPropertyName);
        SetDissolveKeywordEnabled(true);
        SetDissolveEnabled(true);
        SetWeight(0f);
        SetGlow(0f);
    }

    private static string ResolvePropertyName(Material mat, string currentValue, params string[] fallbacks)
    {
        if (!string.IsNullOrEmpty(currentValue) && mat.HasProperty(currentValue))
        {
            return currentValue;
        }

        for (int i = 0; i < fallbacks.Length; i++)
        {
            string candidate = fallbacks[i];
            if (mat.HasProperty(candidate))
            {
                return candidate;
            }
        }

        return currentValue;
    }

    private void OnValidate()
    {
        dissolveDuration = Mathf.Max(0.01f, dissolveDuration);
        maxGlow = Mathf.Max(0f, maxGlow);
        particlesPerSecond = Mathf.Max(0, particlesPerSecond);
        endBurstCount = Mathf.Max(0, endBurstCount);

        velocityXRange.x = Mathf.Min(velocityXRange.x, velocityXRange.y);
        velocityYRange.x = Mathf.Min(velocityYRange.x, velocityYRange.y);
    }

    [ContextMenu("Play Dissolve")]
    public void PlayDissolve()
    {
        if (!isActiveAndEnabled)
        {
            return;
        }

        if (playRoutine != null)
        {
            StopCoroutine(playRoutine);
        }

        playRoutine = StartCoroutine(PlayDissolveRoutine());
    }

    [ContextMenu("Play Regen")]
    public void PlayRegen()
    {
        if (!isActiveAndEnabled)
        {
            return;
        }

        if (playRoutine != null)
        {
            StopCoroutine(playRoutine);
            playRoutine = null;
        }

        playRoutine = StartCoroutine(PlayRegenRoutine());
    }

    private IEnumerator PlayDissolveRoutine()
    {
        SetDissolveEnabled(true);
        SetRotationZZero();
        SetWeight(0f);
        SetGlow(0f);

        float elapsed = 0f;
        float emitAccumulator = 0f;

        while (elapsed < dissolveDuration)
        {
            float t = elapsed / dissolveDuration;
            float weightT = RemapWeightProgress(t);
            SetWeight(weightT);
            SetGlow(Mathf.Lerp(0f, maxGlow, weightT));

            float dt = Time.deltaTime;
            emitAccumulator += particlesPerSecond * dt;
            int emitCount = Mathf.FloorToInt(emitAccumulator);
            emitAccumulator -= emitCount;

            EmitParticles(particleSystemRef, emitCount);

            elapsed += dt;
            yield return null;
        }

        SetWeight(1f);
        SetGlow(maxGlow);
        EmitParticles(particleSystemRef, endBurstCount);

        if (disableRendererWhenDone)
        {
            SetShadowCasterEnabled(false);
        }

        playRoutine = null;
    }

    private IEnumerator PlayRegenRoutine()
    {
        SetDissolveEnabled(true);
        SetRotationZZero();
        SetDissolveColor(Color.green);
        SetDisintegrationColor(Color.green);
        SetWeight(1f);
        SetGlow(maxGlow);

        float elapsed = 0f;
        float emitAccumulator = 0f;
        while (elapsed < dissolveDuration)
        {
            float t = elapsed / dissolveDuration;
            float weightT = 1f - RemapWeightProgress(t);
            SetWeight(weightT);
            SetGlow(Mathf.Lerp(0f, maxGlow, weightT));

            float dt = Time.deltaTime;
            emitAccumulator += particlesPerSecond * dt;
            int emitCount = Mathf.FloorToInt(emitAccumulator);
            emitAccumulator -= emitCount;

            EmitParticles(regenParticleSystemRef, emitCount);

            elapsed += dt;
            yield return null;
        }

        SetWeight(0f);
        SetGlow(0f);
        EmitParticles(regenParticleSystemRef, endBurstCount);
        SetDissolveColor(Color.red);
        SetDisintegrationColor(Color.red);
        SetDissolveEnabled(true);
        SetShadowCasterEnabled(true);
        playRoutine = null;
    }

    private void SetDissolveEnabled(bool enabled)
    {
        if (targetRenderer == null)
        {
            return;
        }

        targetRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetFloat(dissolveEnabledPropertyId, enabled ? 1f : 0f);
        targetRenderer.SetPropertyBlock(propertyBlock);
    }

    private void SetDissolveKeywordEnabled(bool enabled)
    {
        if (targetRenderer == null)
        {
            return;
        }

        Material material = targetRenderer.material;
        if (enabled)
        {
            material.EnableKeyword(DissolveKeyword);
        }
        else
        {
            material.DisableKeyword(DissolveKeyword);
        }
    }

    private void EmitParticles(ParticleSystem targetParticleSystem, int count)
    {
        if (targetParticleSystem == null || count <= 0 || targetRenderer == null)
        {
            return;
        }

        Bounds bounds = targetRenderer.bounds;

        for (int i = 0; i < count; i++)
        {
            ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams
            {
                position = new Vector3(
                    Random.Range(bounds.min.x, bounds.max.x),
                    Random.Range(bounds.min.y, bounds.max.y),
                    bounds.center.z),
                velocity = new Vector3(
                    Random.Range(velocityXRange.x, velocityXRange.y),
                    Random.Range(velocityYRange.x, velocityYRange.y),
                    0f)
            };

            targetParticleSystem.Emit(emitParams, 1);
        }
    }

    private void SetWeight(float weight)
    {
        if (targetRenderer == null)
        {
            return;
        }

        targetRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetFloat(weightPropertyId, Mathf.Clamp01(weight));
        targetRenderer.SetPropertyBlock(propertyBlock);
    }

    private float RemapWeightProgress(float normalizedTime)
    {
        normalizedTime = Mathf.Clamp01(normalizedTime);

        const float slowPhaseEndTime = 3f / 4f;
        if (normalizedTime <= slowPhaseEndTime)
        {
            return normalizedTime * (2f / 3f);
        }

        float fastPhaseT = (normalizedTime - slowPhaseEndTime) / (1f - slowPhaseEndTime);
        return 0.5f + fastPhaseT * 0.5f;
    }

    private void SetGlow(float glow)
    {
        if (targetRenderer == null)
        {
            return;
        }

        targetRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetFloat(glowPropertyId, Mathf.Max(0f, glow));
        targetRenderer.SetPropertyBlock(propertyBlock);
    }

    private void SetDissolveColor(Color color)
    {
        if (targetRenderer == null)
        {
            return;
        }

        targetRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor(dissolveColorPropertyId, color);
        targetRenderer.SetPropertyBlock(propertyBlock);
    }

    private void SetDisintegrationColor(Color color)
    {
        if (targetRenderer == null)
        {
            return;
        }

        targetRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor(disintegrationColorPropertyId, color);
        targetRenderer.SetPropertyBlock(propertyBlock);
    }

    private void SetRotationZZero()
    {
        Vector3 eulerAngles = GameManager.Instance.cubeControllerRef.transform.rotation.eulerAngles;
        eulerAngles.z = 0f;
        GameManager.Instance.cubeControllerRef.transform.rotation = Quaternion.Euler(eulerAngles);
    }

    private void SetShadowCasterEnabled(bool enabled)
    {
        if (shadowCaster2DRef != null)
        {
            shadowCaster2DRef.enabled = enabled;
        }
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.G))
        {
            PlayDissolve();
        }
        if (Input.GetKeyDown(KeyCode.H))
        {
            PlayRegen();
        }
    }
}
