using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CubeController))]
public class CubeVisualJelly : MonoBehaviour
{
    [Header("Visual Root")]
    [SerializeField] private Transform visualRoot;

    [Header("Jelly Scale")]
    [SerializeField] private float visualLerpSpeed = 18f;
    [SerializeField] private float runSquashAmount = 0.12f;
    [SerializeField] private float jumpStretchAmount = 0.18f;
    [SerializeField] private float fallStretchAmount = 0.24f;
    [SerializeField] private float maxStretchVelocity = 22f;
    [SerializeField] private float landingSquashAmount = 0.2f;
    [SerializeField] private float landingRecoverySpeed = 14f;

    [Header("Run Tilt")]
    [SerializeField] private float maxTiltAngle = 8f;
    [SerializeField] private float tiltLerpSpeed = 16f;
    [SerializeField] private float tiltInAirMultiplier = 0.55f;

    [Header("Rotation Follow")]
    [SerializeField] private float angularTiltWeight = 0.12f;
    [SerializeField] private float maxAngularTilt = 14f;

    [Header("Special Move Visual")]
    [SerializeField] private float dashStretchAmount = 0.18f;
    [SerializeField] private float dashTiltBonus = 10f;
    [SerializeField] private float dashPulseRecoverySpeed = 18f;
    [SerializeField] private float groundPoundStretchAmount = 0.28f;
    [SerializeField] private float groundPoundSquashAmount = 0.16f;
    [SerializeField] private float groundPoundTiltBonus = 6f;
    [SerializeField] private float groundPoundPulseRecoverySpeed = 16f;

    [Header("Face (Optional)")]
    [SerializeField] private SpriteRenderer faceRenderer;
    [SerializeField] private Sprite idleFace;
    [SerializeField] private Sprite runFace;
    [SerializeField] private Sprite jumpFace;
    [SerializeField] private Sprite fallFace;
    [SerializeField] private Sprite landFace;

    [Header("Camera Shake")]
    [SerializeField] private CubeCameraShake cameraShake;
    [SerializeField] private float groundPoundShakeThreshold = 0.9f;
    [SerializeField] private float groundPoundShakeMultiplier = 1.35f;

    private Rigidbody2D body;

    private Vector3 visualBaseScale = Vector3.one;
    private Quaternion visualBaseRotation = Quaternion.identity;

    private bool wasGrounded;
    private float previousVerticalVelocity;
    private float landingSquashImpulse;
    private float dashPulseImpulse;
    private float groundPoundPulseImpulse;
    private float currentTiltAngle;
    private Vector3 scaleVelocity;
    private float tiltVelocity;
    private bool wasDashing;
    private bool wasGroundPounding;

    private CubeController Controller => GameManager.Instance.cubeControllerRef;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();

        if (visualRoot == null)
        {
            visualRoot = transform;
        }

        visualBaseScale = visualRoot.localScale;
        visualBaseRotation = visualRoot.localRotation;

        if (cameraShake == null && Camera.main != null)
        {
            cameraShake = Camera.main.GetComponent<CubeCameraShake>();
        }
    }

    private void OnEnable()
    {
        wasGrounded = Controller.IsGrounded;
        previousVerticalVelocity = Controller.Velocity.y;
        landingSquashImpulse = 0f;
        dashPulseImpulse = 0f;
        groundPoundPulseImpulse = 0f;
        currentTiltAngle = 0f;
        scaleVelocity = Vector3.zero;
        tiltVelocity = 0f;
        wasDashing = Controller.IsDashing;
        wasGroundPounding = Controller.IsGroundPounding;
    }

    private void OnValidate()
    {
        visualLerpSpeed = Mathf.Max(0.1f, visualLerpSpeed);
        runSquashAmount = Mathf.Clamp(runSquashAmount, 0f, 0.5f);
        jumpStretchAmount = Mathf.Clamp(jumpStretchAmount, 0f, 0.7f);
        fallStretchAmount = Mathf.Clamp(fallStretchAmount, 0f, 0.9f);
        maxStretchVelocity = Mathf.Max(1f, maxStretchVelocity);
        landingSquashAmount = Mathf.Clamp(landingSquashAmount, 0f, 0.6f);
        landingRecoverySpeed = Mathf.Max(0.1f, landingRecoverySpeed);

        maxTiltAngle = Mathf.Clamp(maxTiltAngle, 0f, 20f);
        tiltLerpSpeed = Mathf.Max(0.1f, tiltLerpSpeed);
        tiltInAirMultiplier = Mathf.Clamp(tiltInAirMultiplier, 0f, 1f);

        angularTiltWeight = Mathf.Clamp(angularTiltWeight, 0f, 1f);
        maxAngularTilt = Mathf.Clamp(maxAngularTilt, 0f, 40f);

        dashStretchAmount = Mathf.Clamp(dashStretchAmount, 0f, 0.6f);
        dashTiltBonus = Mathf.Clamp(dashTiltBonus, 0f, 30f);
        dashPulseRecoverySpeed = Mathf.Max(0.1f, dashPulseRecoverySpeed);
        groundPoundStretchAmount = Mathf.Clamp(groundPoundStretchAmount, 0f, 0.8f);
        groundPoundSquashAmount = Mathf.Clamp(groundPoundSquashAmount, 0f, 0.6f);
        groundPoundTiltBonus = Mathf.Clamp(groundPoundTiltBonus, 0f, 20f);
        groundPoundPulseRecoverySpeed = Mathf.Max(0.1f, groundPoundPulseRecoverySpeed);

        groundPoundShakeThreshold = Mathf.Max(0f, groundPoundShakeThreshold);
        groundPoundShakeMultiplier = Mathf.Max(0f, groundPoundShakeMultiplier);
    }

    private void Update()
    {
        if (visualRoot == null)
        {
            return;
        }

        Vector2 velocity = Controller.Velocity;
        Vector2 localVelocity = GetLocalVelocity(velocity);
        bool isGrounded = Controller.IsGrounded;
        bool isDashing = Controller.IsDashing;
        bool isGroundPounding = Controller.IsGroundPounding;

        ApplyLandingImpulseIfNeeded(isGrounded);
        ApplySpecialMoveTransitions(isDashing, isGroundPounding);
        ApplyGroundPoundCameraShake(isGrounded, isGroundPounding);
        TickScaleVisual(Time.deltaTime, localVelocity, velocity, isGrounded, isDashing, isGroundPounding);
        TickTiltVisual(Time.deltaTime, localVelocity, isGrounded, isDashing, isGroundPounding);
        UpdateFaceState(velocity, isGrounded);

        wasGrounded = isGrounded;
        previousVerticalVelocity = velocity.y;
        wasDashing = isDashing;
        wasGroundPounding = isGroundPounding;
    }

    private void ApplyLandingImpulseIfNeeded(bool isGrounded)
    {
        if (!wasGrounded && isGrounded && previousVerticalVelocity < -0.5f)
        {
            float normalizedImpact = Mathf.InverseLerp(0.5f, Controller.TerminalFallSpeed, Mathf.Abs(previousVerticalVelocity));
            landingSquashImpulse = Mathf.Max(landingSquashImpulse, normalizedImpact * landingSquashAmount);
        }
    }

    private Vector2 GetLocalVelocity(Vector2 worldVelocity)
    {
        Vector3 localVelocity3 = transform.InverseTransformDirection(new Vector3(worldVelocity.x, worldVelocity.y, 0f));
        return new Vector2(localVelocity3.x, localVelocity3.y);
    }

    private void ApplySpecialMoveTransitions(bool isDashing, bool isGroundPounding)
    {
        if (isDashing && !wasDashing)
        {
            dashPulseImpulse = Mathf.Max(dashPulseImpulse, 0.16f);
        }

        if (isGroundPounding && !wasGroundPounding)
        {
            groundPoundPulseImpulse = Mathf.Max(groundPoundPulseImpulse, 0.14f);
        }

        if (!isGroundPounding && wasGroundPounding && previousVerticalVelocity < -0.5f)
        {
            groundPoundPulseImpulse = Mathf.Max(groundPoundPulseImpulse, 0.22f);
        }
    }

    private void ApplyGroundPoundCameraShake(bool isGrounded, bool isGroundPounding)
    {
        if (cameraShake == null)
        {
            return;
        }

        if (isGroundPounding || !wasGroundPounding || !isGrounded || previousVerticalVelocity >= -groundPoundShakeThreshold)
        {
            return;
        }

        float impact = Mathf.InverseLerp(groundPoundShakeThreshold, Controller.TerminalFallSpeed, Mathf.Abs(previousVerticalVelocity));
        float intensity = Mathf.Clamp01(impact * groundPoundShakeMultiplier);
        cameraShake.ShakeGroundPound(intensity);
    }

    private void TickScaleVisual(float dt, Vector2 localVelocity, Vector2 worldVelocity, bool isGrounded, bool isDashing, bool isGroundPounding)
    {
        float horizontalRatio = Controller.MaxMoveSpeed > 0.001f
            ? Mathf.Clamp01(Mathf.Abs(localVelocity.x) / Controller.MaxMoveSpeed)
            : 0f;

        float verticalRatio = Mathf.Clamp(localVelocity.y / maxStretchVelocity, -1f, 1f);

        float xScale = 1f;
        float yScale = 1f;

        if (isDashing)
        {
            float dashDirection = Mathf.Sign(localVelocity.x);
            if (dashDirection == 0f)
            {
                dashDirection = Mathf.Sign(worldVelocity.x);
            }

            xScale += dashStretchAmount;
            yScale -= dashStretchAmount * 0.65f;
            xScale += dashPulseImpulse;

            dashPulseImpulse = Mathf.MoveTowards(dashPulseImpulse, 0f, dashPulseRecoverySpeed * dt);
        }
        else if (isGroundPounding)
        {
            xScale -= groundPoundSquashAmount;
            yScale += groundPoundStretchAmount;
            yScale += groundPoundPulseImpulse;
            xScale -= groundPoundPulseImpulse * 0.65f;

            groundPoundPulseImpulse = Mathf.MoveTowards(groundPoundPulseImpulse, 0f, groundPoundPulseRecoverySpeed * dt);
        }
        else if (isGrounded)
        {
            xScale += runSquashAmount * horizontalRatio;
            yScale -= runSquashAmount * horizontalRatio;
        }
        else if (verticalRatio > 0f)
        {
            yScale += jumpStretchAmount * verticalRatio;
            xScale -= jumpStretchAmount * verticalRatio;
        }
        else
        {
            float fallRatio = -verticalRatio;
            yScale += fallStretchAmount * fallRatio;
            xScale -= fallStretchAmount * fallRatio;
        }

        if (landingSquashImpulse > 0.0001f)
        {
            xScale += landingSquashImpulse;
            yScale -= landingSquashImpulse;
            landingSquashImpulse = Mathf.MoveTowards(landingSquashImpulse, 0f, landingRecoverySpeed * dt);
        }

        xScale = Mathf.Max(0.25f, xScale);
        yScale = Mathf.Max(0.25f, yScale);

        Vector3 targetScale = new Vector3(
            visualBaseScale.x * xScale,
            visualBaseScale.y * yScale,
            visualBaseScale.z);

        float smoothTime = 1f / Mathf.Max(0.1f, visualLerpSpeed);
        visualRoot.localScale = Vector3.SmoothDamp(visualRoot.localScale, targetScale, ref scaleVelocity, smoothTime, Mathf.Infinity, dt);
    }

    private void TickTiltVisual(float dt, Vector2 localVelocity, bool isGrounded, bool isDashing, bool isGroundPounding)
    {
        float horizontalRatio = Controller.MaxMoveSpeed > 0.001f
            ? Mathf.Clamp(Mathf.Abs(localVelocity.x) / Controller.MaxMoveSpeed, 0f, 1f)
            : 0f;

        float targetTilt = -Mathf.Sign(localVelocity.x) * maxTiltAngle * horizontalRatio;

        if (isDashing)
        {
            targetTilt += Mathf.Sign(localVelocity.x) * dashTiltBonus;
        }

        if (isGroundPounding)
        {
            targetTilt += groundPoundTiltBonus;
        }

        if (body != null)
        {
            float angularComponent = Mathf.Clamp(-body.angularVelocity * angularTiltWeight, -maxAngularTilt, maxAngularTilt);
            targetTilt += angularComponent;
        }

        if (!isGrounded)
        {
            targetTilt *= tiltInAirMultiplier;
        }

        float tiltSmoothTime = 1f / Mathf.Max(0.1f, tiltLerpSpeed);
        currentTiltAngle = Mathf.SmoothDampAngle(currentTiltAngle, targetTilt, ref tiltVelocity, tiltSmoothTime, Mathf.Infinity, dt);

        visualRoot.localRotation = visualBaseRotation * Quaternion.Euler(0f, 0f, currentTiltAngle);
    }

    private void UpdateFaceState(Vector2 velocity, bool isGrounded)
    {
        if (faceRenderer == null)
        {
            return;
        }

        Sprite targetFace = idleFace;

        if (!isGrounded)
        {
            targetFace = localVelocityForFace(velocity).y >= 0f ? jumpFace : fallFace;
        }
        else if (landingSquashImpulse > 0.02f)
        {
            targetFace = landFace;
        }
        else if (Mathf.Abs(localVelocityForFace(velocity).x) > 0.1f)
        {
            targetFace = runFace;
        }

        if (targetFace != null)
        {
            faceRenderer.sprite = targetFace;
        }
    }

    private Vector2 localVelocityForFace(Vector2 worldVelocity)
    {
        return GetLocalVelocity(worldVelocity);
    }
}
