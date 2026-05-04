using DG.Tweening;
using UnityEngine;

public class GreenArrow : MonoBehaviour
{
    private enum ClippingState
    {
        Center,
        Left,
        Right
    }

    private static readonly int ClipUvLeftId = Shader.PropertyToID("_ClipUvLeft");
    private static readonly int ClipUvRightId = Shader.PropertyToID("_ClipUvRight");
    [SerializeField] private Renderer targetRenderer;
    private Collider2D platformCollider;

    [Header("Clipping")]
    [SerializeField] private float centerClipValue = 0.5f;
    [SerializeField] private float sideClipValue = 0.27f;
    [SerializeField] private float clipTweenDuration = 0.2f;
    [SerializeField] private Ease clipTweenEase = Ease.OutQuad;

    private MaterialPropertyBlock propertyBlock;
    private Tween clipLeftTween;
    private Tween clipRightTween;
    private Sequence clipSequence;
    private float currentClipLeft;
    private float currentClipRight;
    private ClippingState currentState = ClippingState.Center;
    private Rigidbody2D platformRigidbody;
    private Rigidbody2D playerRigidbodyOnPlatform;
    private CubeController playerControllerOnPlatform;
    private Vector2 prevPlatformPosition;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2f;
    private bool isPlatformMoving;
    private float platformMoveDir;

    private RaycastHit2D[] castResults = new RaycastHit2D[5];

    private void FixedUpdate()
    {
        if (platformRigidbody == null)
        {
            return;
        }

        // store old position to compute displacement for carrying the player
        Vector2 oldPlatformPos = platformRigidbody.position;

        if (!isPlatformMoving)
        {
            // ensure velocity zeroed for non-kinematic
            if (platformRigidbody.bodyType != RigidbodyType2D.Kinematic)
                platformRigidbody.velocity = new Vector2(0f, platformRigidbody.velocity.y);
        }
        else
        {
            if (IsPathBlocked())
            {
                StopPlatformMovement();
                TweenToCenter();
            }
            else
            {
                Vector2 targetVelocity = new Vector2(platformMoveDir * moveSpeed, 0f);
                if (platformRigidbody.bodyType == RigidbodyType2D.Kinematic)
                {
                    platformRigidbody.MovePosition(platformRigidbody.position + targetVelocity * Time.fixedDeltaTime);
                }
                else
                {
                    platformRigidbody.velocity = new Vector2(targetVelocity.x, platformRigidbody.velocity.y);
                }
            }
        }

        // after moving platform, apply platform velocity to player controller so player doesn't slip
        float platformVelX = 0f;
        if (isPlatformMoving)
        {
            if (platformRigidbody.bodyType == RigidbodyType2D.Kinematic)
                platformVelX = platformMoveDir * moveSpeed;
            else
                platformVelX = platformRigidbody.velocity.x;
        }

        if (playerControllerOnPlatform != null)
        {
            if (isPlatformMoving)
                playerControllerOnPlatform.SetExternalVelocity(new Vector2(platformVelX, 0f));
            else
                playerControllerOnPlatform.ClearExternalVelocity();
        }
    }

    private bool IsPathBlocked()
    {
        if (platformCollider == null) return false;

        float checkDistance = moveSpeed * Time.fixedDeltaTime + 0.02f;
        Vector2 direction = new Vector2(platformMoveDir, 0);
        int hitCount = platformCollider.Cast(direction, castResults, checkDistance);

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hitCollider = castResults[i].collider;

            if (hitCollider == platformCollider || hitCollider.transform.IsChildOf(platformRigidbody.transform))
                continue;

            if (hitCollider.isTrigger)
                continue;

            if (IsPlayerCollider(hitCollider))
                continue;

            return true;
        }
        return false;
    }

    private bool IsPlayerCollider(Collider2D collider)
    {
        return collider != null && collider.GetComponentInParent<CubeController>() != null;
    }

    private void Awake()
    {
        propertyBlock = new MaterialPropertyBlock();

        if (targetRenderer == null)
        {
            targetRenderer = GetComponentInChildren<Renderer>(true);
        }

        if (platformCollider == null)
        {
            platformCollider = GetComponentInParent<Collider2D>();
            if (platformCollider == null)
            {
                platformCollider = GetComponent<Collider2D>();
            }
        }

        // find platform Rigidbody2D (parent object expected)
        if (platformRigidbody == null)
        {
            platformRigidbody = GetComponentInParent<Rigidbody2D>();
            if (platformRigidbody == null)
            {
                platformRigidbody = GetComponent<Rigidbody2D>();
            }
        }

        // default: not moving
        isPlatformMoving = false;
        platformMoveDir = 0f;

        currentClipLeft = centerClipValue;
        currentClipRight = centerClipValue;
        ApplyClipValues();
    }

    private void OnDisable()
    {
        clipLeftTween?.Kill();
        clipRightTween?.Kill();
        clipSequence?.Kill();
        clipLeftTween = null;
        clipRightTween = null;
        clipSequence = null;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        HandleCollision(collision);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        HandleCollision(collision);
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (!IsPlayerCollision(collision))
        {
            return;
        }

        // if the exiting collider was the stored player, clear references
        Collider2D exiting = collision.collider;
        if (exiting != null)
        {
            Rigidbody2D rb = exiting.attachedRigidbody ?? exiting.GetComponentInParent<Rigidbody2D>();
            if (playerRigidbodyOnPlatform == rb)
            {
                playerRigidbodyOnPlatform = null;
            }
            CubeController cc = exiting.GetComponentInParent<CubeController>();
            if (playerControllerOnPlatform == cc)
            {
                if (playerControllerOnPlatform != null)
                    playerControllerOnPlatform.ClearExternalVelocity();
                playerControllerOnPlatform = null;
            }
        }

        StopPlatformMovement();
        TweenToCenter();
    }

    private void HandleCollision(Collision2D collision)
    {
        if (!IsPlayerCollision(collision))
        {
            return;
        }

        // store player references
        Collider2D col = collision.collider;
        if (col != null)
        {
            playerRigidbodyOnPlatform = col.attachedRigidbody ?? col.GetComponentInParent<Rigidbody2D>();
            playerControllerOnPlatform = col.GetComponentInParent<CubeController>();
        }

        ClippingState nextState = GetStateFromPlayerPosition(collision);
        SetTargetState(nextState);
    }

    private bool IsPlayerCollision(Collision2D collision)
    {
        if (collision == null)
        {
            return false;
        }

        return collision.collider != null && collision.collider.GetComponentInParent<CubeController>() != null;
    }

    private ClippingState GetStateFromPlayerPosition(Collision2D collision)
    {
        if (platformCollider == null)
        {
            return ClippingState.Center;
        }
        float contactX;
        if (collision.contactCount == 0)
        {
            // fallback to the transform position of the other collider (player)
            contactX = collision.transform.position.x;
        }
        else
        {
            contactX = collision.GetContact(0).point.x;
        }
        Bounds bounds = platformCollider.bounds;
        float leftEdge = bounds.min.x;
        float rightEdge = bounds.max.x;
        float centerX = bounds.center.x;
        if (contactX < centerX)
        {
            return ClippingState.Left;
        }

        return ClippingState.Right;
    }

    private void SetTargetState(ClippingState nextState)
    {
        if (currentState == nextState)
        {
            return;
        }
        currentState = nextState;

        float targetLeft = centerClipValue;
        float targetRight = centerClipValue;

        if (nextState == ClippingState.Right)
        {
            targetLeft = sideClipValue;
            targetRight = 0f;
        }
        else if (nextState == ClippingState.Left)
        {
            targetLeft = 0f;
            targetRight = sideClipValue;
        }

        // First tween both values to center, then tween to the side targets
        clipLeftTween?.Kill();
        clipRightTween?.Kill();
        clipSequence?.Kill();

        clipSequence = DOTween.Sequence();
        // Move to center
        clipSequence.Append(DOTween.To(() => currentClipLeft, v => { currentClipLeft = v; ApplyClipValues(); }, centerClipValue, clipTweenDuration).SetEase(clipTweenEase));
        clipSequence.Join(DOTween.To(() => currentClipRight, v => { currentClipRight = v; ApplyClipValues(); }, centerClipValue, clipTweenDuration).SetEase(clipTweenEase));
        // Start movement when center phase completes
        clipSequence.AppendCallback(() => StartPlatformMovement(nextState));
        // Then move to side
        clipSequence.Append(DOTween.To(() => currentClipLeft, v => { currentClipLeft = v; ApplyClipValues(); }, targetLeft, clipTweenDuration).SetEase(clipTweenEase));
        clipSequence.Join(DOTween.To(() => currentClipRight, v => { currentClipRight = v; ApplyClipValues(); }, targetRight, clipTweenDuration).SetEase(clipTweenEase));
    }

    private void TweenClipValues(float targetLeft, float targetRight)
    {
        // kept for compatibility but now we prefer sequences; still provide instant tween
        clipLeftTween?.Kill();
        clipRightTween?.Kill();

        clipLeftTween = DOTween.To(() => currentClipLeft, value => { currentClipLeft = value; ApplyClipValues(); }, targetLeft, clipTweenDuration).SetEase(clipTweenEase);
        clipRightTween = DOTween.To(() => currentClipRight, value => { currentClipRight = value; ApplyClipValues(); }, targetRight, clipTweenDuration).SetEase(clipTweenEase);
    }

    private void TweenToCenter()
    {
        clipLeftTween?.Kill();
        clipRightTween?.Kill();
        clipSequence?.Kill();

        clipSequence = DOTween.Sequence();
        clipSequence.Append(DOTween.To(() => currentClipLeft, v => { currentClipLeft = v; ApplyClipValues(); }, centerClipValue, clipTweenDuration).SetEase(clipTweenEase));
        clipSequence.Join(DOTween.To(() => currentClipRight, v => { currentClipRight = v; ApplyClipValues(); }, centerClipValue, clipTweenDuration).SetEase(clipTweenEase));
    }

    private void StartPlatformMovement(ClippingState state)
    {
        if (platformRigidbody == null)
        {
            return;
        }
        float dir = state == ClippingState.Left ? -1f : 1f;
        platformMoveDir = dir;
        isPlatformMoving = true;
    }

    private void StopPlatformMovement()
    {
        if (platformRigidbody == null)
        {
            return;
        }
        isPlatformMoving = false;
        platformMoveDir = 0f;

        if (platformRigidbody.bodyType != RigidbodyType2D.Kinematic)
        {
            platformRigidbody.velocity = new Vector2(0f, platformRigidbody.velocity.y);
        }
    }

    private void ApplyClipValues()
    {
        if (targetRenderer == null || propertyBlock == null)
        {
            return;
        }

        targetRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetFloat(ClipUvLeftId, currentClipLeft);
        propertyBlock.SetFloat(ClipUvRightId, currentClipRight);
        targetRenderer.SetPropertyBlock(propertyBlock);
    }
}
