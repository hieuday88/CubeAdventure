using UnityEngine;

[DisallowMultipleComponent]
public class CubeCameraShake : MonoBehaviour
{
    [Header("Shake")]
    [SerializeField] private float defaultDuration = 0.12f;
    [SerializeField] private float defaultAmplitude = 0.08f;
    [SerializeField] private float defaultFrequency = 22f;
    [SerializeField] private float positionDamping = 18f;
    [SerializeField] private float rotationDamping = 18f;
    [SerializeField] private float maxPositionOffset = 0.18f;
    [SerializeField] private float maxRotationOffset = 2.5f;

    private Vector3 baseLocalPosition;
    private Quaternion baseLocalRotation;

    private float shakeTimeRemaining;
    private float shakeDuration;
    private float shakeAmplitude;
    private float shakeFrequency;
    private Vector3 currentPositionOffset;
    private Vector3 currentRotationOffset;

    private void Awake()
    {
        baseLocalPosition = transform.localPosition;
        baseLocalRotation = transform.localRotation;
    }

    private void OnEnable()
    {
        baseLocalPosition = transform.localPosition;
        baseLocalRotation = transform.localRotation;
    }

    private void LateUpdate()
    {
        float dt = Time.deltaTime;

        if (shakeTimeRemaining > 0f)
        {
            shakeTimeRemaining -= dt;

            float progress = 1f - Mathf.Clamp01(shakeTimeRemaining / Mathf.Max(0.0001f, shakeDuration));
            float envelope = 1f - progress;
            float time = Time.unscaledTime * shakeFrequency;

            float noiseX = (Mathf.PerlinNoise(time, 0.1f) - 0.5f) * 2f;
            float noiseY = (Mathf.PerlinNoise(0.3f, time + 3.7f) - 0.5f) * 2f;
            float noiseRot = (Mathf.PerlinNoise(time + 7.1f, 0.8f) - 0.5f) * 2f;

            Vector3 targetPositionOffset = new Vector3(noiseX, noiseY, 0f) * (shakeAmplitude * envelope);
            Vector3 targetRotationOffset = new Vector3(0f, 0f, noiseRot * shakeAmplitude * 12f * envelope);

            targetPositionOffset = Vector3.ClampMagnitude(targetPositionOffset, maxPositionOffset);
            targetRotationOffset = Vector3.ClampMagnitude(targetRotationOffset, maxRotationOffset);

            float positionT = 1f - Mathf.Exp(-positionDamping * dt);
            float rotationT = 1f - Mathf.Exp(-rotationDamping * dt);
            currentPositionOffset = Vector3.Lerp(currentPositionOffset, targetPositionOffset, positionT);
            currentRotationOffset = Vector3.Lerp(currentRotationOffset, targetRotationOffset, rotationT);
        }
        else
        {
            float positionT = 1f - Mathf.Exp(-positionDamping * dt);
            float rotationT = 1f - Mathf.Exp(-rotationDamping * dt);
            currentPositionOffset = Vector3.Lerp(currentPositionOffset, Vector3.zero, positionT);
            currentRotationOffset = Vector3.Lerp(currentRotationOffset, Vector3.zero, rotationT);
        }

        transform.localPosition = baseLocalPosition + currentPositionOffset;
        transform.localRotation = baseLocalRotation * Quaternion.Euler(currentRotationOffset);
    }

    public void Shake(float amplitude, float duration, float frequency)
    {
        shakeAmplitude = Mathf.Max(shakeAmplitude, amplitude > 0f ? amplitude : defaultAmplitude);
        shakeDuration = Mathf.Max(duration > 0f ? duration : defaultDuration, 0.01f);
        shakeFrequency = frequency > 0f ? frequency : defaultFrequency;
        shakeTimeRemaining = Mathf.Max(shakeTimeRemaining, shakeDuration);
    }

    public void ShakeGroundPound(float intensity)
    {
        float amplitude = defaultAmplitude * Mathf.Clamp(intensity, 0.5f, 2f) * 1.25f;
        float duration = defaultDuration * Mathf.Lerp(1f, 1.4f, Mathf.Clamp01(intensity));
        float frequency = defaultFrequency * Mathf.Lerp(0.9f, 1.15f, Mathf.Clamp01(intensity));
        Shake(amplitude, duration, frequency);
    }
}
