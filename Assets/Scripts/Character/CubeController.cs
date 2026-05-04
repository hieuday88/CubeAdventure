using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(BoxCollider2D), typeof(Rigidbody2D))]
public class CubeController : MonoBehaviour
{
    public enum CubeLayer
    {
        White = 9,
        Red = 6,
        Green = 7,
        Blue = 8
    }

    [Header("Input Actions")]
    [SerializeField] private InputAction moveAction;
    [SerializeField] private InputAction jumpAction;
    [SerializeField] private InputAction dashAction;

    [Header("Collision")]
    [SerializeField] private LayerMask groundMask = ~0;
    [SerializeField] private float skinWidth = 0.02f;
    [SerializeField] private float groundProbeDistance = 0.12f;
    [SerializeField] private float maxSlopeAngle = 50f;

    [Header("Horizontal")]
    [SerializeField] private float maxMoveSpeed = 9f;
    [SerializeField] private float groundAcceleration = 90f;
    [SerializeField] private float groundDeceleration = 110f;
    [SerializeField] private float airAcceleration = 50f;
    [SerializeField] private float airDeceleration = 40f;

    [Header("Vertical")]
    [SerializeField] private float jumpVelocity = 13f;
    [SerializeField] private float gravityUp = 34f;
    [SerializeField] private float gravityDown = 48f;
    [SerializeField] private float apexGravityMultiplier = 0.65f;
    [SerializeField] private float apexVelocityThreshold = 2f;
    [SerializeField] private float terminalFallSpeed = 26f;
    [SerializeField] private float jumpCutMultiplier = 0.45f;

    [Header("Jump Assist")]
    [SerializeField] private float coyoteTime = 0.12f;
    [SerializeField] private float jumpBufferTime = 0.12f;

    [Header("Special Moves")]
    [SerializeField] private float doubleTapWindow = 0.22f;
    [SerializeField] private float groundPoundSpeed = 24f;
    [SerializeField] private float groundPoundGravityMultiplier = 2.2f;
    [SerializeField] private float dashSpeed = 16f;
    [SerializeField] private float dashDuration = 0.14f;
    [SerializeField] private float dashCooldown = 0.35f;

    [Header("Debug")]
    [SerializeField] private bool drawDebugGizmos;

    [Header("Layer State")]
    [Tooltip("If set to -1, keep the GameObject's current layer.")]
    [SerializeField] private int startLayer = -1;

    private const int MaxGroundHits = 8;

    private readonly RaycastHit2D[] groundHits = new RaycastHit2D[MaxGroundHits];

    private Rigidbody2D body;
    private BoxCollider2D boxCollider;
    private ContactFilter2D contactFilter;

    private Vector2 velocity;
    private Vector2 groundNormal = Vector2.up;
    // External velocity applied each physics step (e.g., moving platforms)
    private Vector2 externalVelocity = Vector2.zero;

    private float moveInput;
    private Vector2 moveInputVector;
    private bool jumpHeld;
    private bool jumpReleasedThisFrame;

    private float coyoteCounter;
    private float jumpBufferCounter;
    private bool isGrounded;

    private float lastJumpPressTime = -999f;
    private bool isGroundPounding;

    private bool isDashing;
    private float dashCounter;
    private float dashCooldownCounter;
    private Vector2 dashDirection = Vector2.right;
    private float facingDirection = 1f;

    private bool isForceMoving;
    private Vector2 forceMovePosition;
    private Tween forceMoveTween;

    protected void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        boxCollider = GetComponent<BoxCollider2D>();

        body.bodyType = RigidbodyType2D.Dynamic;
        body.gravityScale = 0f;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        //body.freezeRotation = true;

        RebuildContactFilter();

        if (startLayer >= 0)
        {
            SetCurrentLayer(startLayer);
        }
    }

    private void OnEnable()
    {
        if (moveAction != null)
        {
            moveAction.Enable();
        }

        if (jumpAction != null)
        {
            jumpAction.Enable();
        }

        if (dashAction != null)
        {
            dashAction.Enable();
        }
    }

    private void OnDisable()
    {
        if (moveAction != null)
        {
            moveAction.Disable();
        }

        if (jumpAction != null)
        {
            jumpAction.Disable();
        }

        if (dashAction != null)
        {
            dashAction.Disable();
        }
    }

    private void OnValidate()
    {
        skinWidth = Mathf.Max(0.001f, skinWidth);
        groundProbeDistance = Mathf.Max(0.01f, groundProbeDistance);
        maxSlopeAngle = Mathf.Clamp(maxSlopeAngle, 0f, 89f);

        maxMoveSpeed = Mathf.Max(0f, maxMoveSpeed);
        groundAcceleration = Mathf.Max(0f, groundAcceleration);
        groundDeceleration = Mathf.Max(0f, groundDeceleration);
        airAcceleration = Mathf.Max(0f, airAcceleration);
        airDeceleration = Mathf.Max(0f, airDeceleration);

        jumpVelocity = Mathf.Max(0f, jumpVelocity);
        gravityUp = Mathf.Max(0f, gravityUp);
        gravityDown = Mathf.Max(0f, gravityDown);
        apexGravityMultiplier = Mathf.Clamp(apexGravityMultiplier, 0.1f, 2f);
        apexVelocityThreshold = Mathf.Max(0.01f, apexVelocityThreshold);
        terminalFallSpeed = Mathf.Max(0.1f, terminalFallSpeed);
        jumpCutMultiplier = Mathf.Clamp(jumpCutMultiplier, 0.1f, 1f);

        coyoteTime = Mathf.Max(0f, coyoteTime);
        jumpBufferTime = Mathf.Max(0f, jumpBufferTime);

        doubleTapWindow = Mathf.Max(0f, doubleTapWindow);
        groundPoundSpeed = Mathf.Max(0.1f, groundPoundSpeed);
        groundPoundGravityMultiplier = Mathf.Max(0.1f, groundPoundGravityMultiplier);
        dashSpeed = Mathf.Max(0.1f, dashSpeed);
        dashDuration = Mathf.Max(0.01f, dashDuration);
        dashCooldown = Mathf.Max(0f, dashCooldown);

        startLayer = Mathf.Clamp(startLayer, -1, 31);

        RebuildContactFilter();
    }

    private void Update()
    {
        moveInputVector = ReadMoveVector();
        moveInput = moveInputVector.x;

        if (Mathf.Abs(moveInput) > 0.01f)
        {
            facingDirection = Mathf.Sign(moveInput);
        }

        if (jumpAction != null && jumpAction.enabled && jumpAction.WasPressedThisFrame())
        {
            HandleJumpPressed();
        }

        jumpHeld = jumpAction != null && jumpAction.enabled && jumpAction.IsPressed();
        jumpReleasedThisFrame = jumpAction != null && jumpAction.enabled && jumpAction.WasReleasedThisFrame();

        if (dashAction != null && dashAction.enabled && dashAction.WasPressedThisFrame())
        {
            TryStartDash();
        }
    }

    private void FixedUpdate()
    {
        if (isForceMoving)
        {
            body.velocity = Vector2.zero;
            velocity = Vector2.zero;
            body.MovePosition(forceMovePosition);
            return;
        }

        float dt = Time.fixedDeltaTime;

        TickDashTimers(dt);
        UpdateGroundState();
        TickJumpTimers(dt);
        TickHorizontal(dt);
        TickJumpLogic();
        TickGravity(dt);

        body.velocity = velocity + externalVelocity;
        velocity = body.velocity - externalVelocity;
        UpdateGroundState();

        jumpReleasedThisFrame = false;

    }

    private void HandleJumpPressed()
    {
        float now = Time.time;

        if (!isGrounded && now - lastJumpPressTime <= doubleTapWindow)
        {
            TryStartGroundPound();
            lastJumpPressTime = now;
            return;
        }

        jumpBufferCounter = jumpBufferTime;
        lastJumpPressTime = now;
    }

    private Vector2 ReadMoveVector()
    {
        if (moveAction == null || !moveAction.enabled)
        {
            return Vector2.zero;
        }

        object actionValue = moveAction.ReadValueAsObject();
        if (actionValue is Vector2 vector)
        {
            return Vector2.ClampMagnitude(vector, 1f);
        }

        return new Vector2(Mathf.Clamp(moveAction.ReadValue<float>(), -1f, 1f), 0f);
    }

    private void TickJumpTimers(float dt)
    {
        jumpBufferCounter = Mathf.Max(0f, jumpBufferCounter - dt);

        if (isGrounded)
        {
            coyoteCounter = coyoteTime;
        }
        else
        {
            coyoteCounter = Mathf.Max(0f, coyoteCounter - dt);
        }
    }

    private void TickHorizontal(float dt)
    {
        if (isDashing)
        {
            velocity = dashDirection * dashSpeed;
            return;
        }

        if (isGroundPounding)
        {
            velocity.x = Mathf.MoveTowards(velocity.x, 0f, groundAcceleration * dt * 0.35f);
            velocity.y = Mathf.Min(velocity.y, -groundPoundSpeed);
            return;
        }

        float targetSpeed = moveInput * maxMoveSpeed;
        bool accelerating = Mathf.Abs(targetSpeed) > 0.01f;

        float accelRate;
        if (isGrounded)
        {
            accelRate = accelerating ? groundAcceleration : groundDeceleration;
        }
        else
        {
            accelRate = accelerating ? airAcceleration : airDeceleration;
        }

        velocity.x = Mathf.MoveTowards(velocity.x, targetSpeed, accelRate * dt);
    }

    private void TickJumpLogic()
    {
        if (isDashing || isGroundPounding)
        {
            return;
        }

        if (jumpBufferCounter > 0f && coyoteCounter > 0f)
        {
            velocity.y = jumpVelocity;
            isGrounded = false;
            coyoteCounter = 0f;
            jumpBufferCounter = 0f;
        }

        if (jumpReleasedThisFrame && velocity.y > 0f)
        {
            velocity.y *= jumpCutMultiplier;
        }

        if (!jumpHeld && velocity.y > 0f)
        {
            velocity.y *= Mathf.Lerp(1f, jumpCutMultiplier, 0.6f);
        }
    }

    private void TickGravity(float dt)
    {
        if (isDashing)
        {
            return;
        }

        if (isGrounded && velocity.y <= 0f)
        {
            velocity.y = -2f;
            return;
        }

        float gravity = velocity.y > 0f ? gravityUp : gravityDown;
        if (isGroundPounding)
        {
            gravity *= groundPoundGravityMultiplier;
        }

        bool inApexWindow = Mathf.Abs(velocity.y) < apexVelocityThreshold;
        if (inApexWindow)
        {
            gravity *= apexGravityMultiplier;
        }

        velocity.y -= gravity * dt;
        velocity.y = Mathf.Max(velocity.y, -terminalFallSpeed);
    }

    private void UpdateGroundState()
    {
        isGrounded = false;
        groundNormal = Vector2.up;

        int hitCount = boxCollider.Cast(Vector2.down, contactFilter, groundHits, groundProbeDistance + skinWidth);
        float bestDistance = float.MaxValue;

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit2D hit = groundHits[i];
            if (hit.collider == null || hit.distance <= 0f)
            {
                continue;
            }

            float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
            if (slopeAngle > maxSlopeAngle)
            {
                continue;
            }

            if (hit.distance < bestDistance)
            {
                bestDistance = hit.distance;
                isGrounded = true;
                groundNormal = hit.normal;
            }
        }

        if (isGrounded && velocity.y < 0f)
        {
            velocity.y = -2f;
        }

        if (isGrounded)
        {
            isGroundPounding = false;
        }
    }

    private void TickDashTimers(float dt)
    {
        dashCooldownCounter = Mathf.Max(0f, dashCooldownCounter - dt);

        if (!isDashing)
        {
            return;
        }

        dashCounter -= dt;
        if (dashCounter <= 0f)
        {
            isDashing = false;
        }
    }

    private void TryStartGroundPound()
    {
        if (isGrounded || isDashing)
        {
            return;
        }

        isGroundPounding = true;
        jumpBufferCounter = 0f;
        coyoteCounter = 0f;
        velocity.y = -groundPoundSpeed;
    }

    private void TryStartDash()
    {
        if (dashCooldownCounter > 0f)
        {
            return;
        }

        Vector2 direction = moveInputVector;
        if (direction.sqrMagnitude < 0.01f)
        {
            direction = new Vector2(facingDirection, 0f);
        }

        direction.Normalize();

        isDashing = true;
        isGroundPounding = false;
        dashCounter = dashDuration;
        dashCooldownCounter = dashCooldown;
        dashDirection = direction;
        velocity = dashDirection * dashSpeed;
        jumpBufferCounter = 0f;
        coyoteCounter = 0f;
    }

    private void RebuildContactFilter()
    {
        contactFilter = new ContactFilter2D
        {
            useLayerMask = true,
            layerMask = groundMask,
            useTriggers = false
        };
    }

    public Vector2 Velocity => velocity;
    public bool IsGrounded => isGrounded;
    public float MaxMoveSpeed => maxMoveSpeed;
    public float TerminalFallSpeed => terminalFallSpeed;
    public bool IsDashing => isDashing;
    public bool IsGroundPounding => isGroundPounding;
    public int CurrentLayer => gameObject.layer;

    public void SetCurrentLayer(CubeLayer layer)
    {
        SetCurrentLayer((int)layer);
    }

    public void SetCurrentLayer(int layer)
    {
        layer = Mathf.Clamp(layer, 0, 31);
        if (gameObject.layer == layer)
        {
            return;
        }

        gameObject.layer = layer;
        ObserverManager<CubeEventId>.Post(CubeEventId.PlayerLayerChanged, layer);
    }

    // Called by external systems (e.g., moving platforms) to apply a persistent velocity for this physics frame
    public void SetExternalVelocity(Vector2 vel)
    {
        externalVelocity = vel;
    }

    public void ClearExternalVelocity()
    {
        externalVelocity = Vector2.zero;
    }

    public void SetCurrentLayerByName(string layerName)
    {
        if (string.IsNullOrWhiteSpace(layerName))
        {
            return;
        }

        int layer = LayerMask.NameToLayer(layerName);
        if (layer < 0)
        {
            return;
        }

        SetCurrentLayer(layer);
    }

    public void AddVelocity(Vector2 deltaVelocity)
    {
        velocity += deltaVelocity;
        if (body != null)
        {
            body.velocity = velocity;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawDebugGizmos)
        {
            return;
        }

        BoxCollider2D localBox = boxCollider != null ? boxCollider : GetComponent<BoxCollider2D>();
        if (localBox == null)
        {
            return;
        }

        Bounds bounds = localBox.bounds;

        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireCube(bounds.center + Vector3.down * (groundProbeDistance + skinWidth), bounds.size);

        Gizmos.color = Color.yellow;
        Vector3 origin = bounds.center;
        Vector3 normal = new Vector3(groundNormal.x, groundNormal.y, 0f);
        Gizmos.DrawLine(origin, origin + normal * 0.8f);
    }

    private void OnDestroy()
    {
        forceMoveTween?.Kill();
        forceMoveTween = null;
    }

    public void ForceMove(Vector2 worldDirection, float distance, float duration, Ease ease = Ease.OutQuad)
    {
        if (body == null)
        {
            return;
        }

        if (duration <= 0f || distance == 0f || worldDirection.sqrMagnitude < 0.0001f)
        {
            return;
        }

        Vector2 direction = worldDirection.normalized;
        Vector2 startPosition = body.position;
        Vector2 endPosition = startPosition + direction * distance;

        forceMoveTween?.Kill();

        isForceMoving = true;
        forceMovePosition = startPosition;
        body.velocity = Vector2.zero;
        velocity = Vector2.zero;

        forceMoveTween = DOTween
            .To(() => forceMovePosition, value => forceMovePosition = value, endPosition, duration)
            .SetEase(ease)
            .SetUpdate(UpdateType.Fixed)
            .OnComplete(() =>
            {
                isForceMoving = false;
                forceMoveTween = null;
            });
    }
}
