
using UnityEngine;

public class GreenInvisible : MonoBehaviour
{
    private CubeController playerController;
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        ObserverManager<CubeEventId>.AddListener(CubeEventId.PlayerLayerChanged, HandlePlayerLayerChanged);
        ResolvePlayerController();
        ApplyVisibility(playerController != null ? playerController.CurrentLayer : -1);
    }

    private void OnDisable()
    {
        ObserverManager<CubeEventId>.RemoveListener(CubeEventId.PlayerLayerChanged, HandlePlayerLayerChanged);
    }

    private void ResolvePlayerController()
    {
        if (playerController != null)
        {
            return;
        }

        if (GameManager.Instance == null)
        {
            return;
        }

        playerController = GameManager.Instance.cubeControllerRef;
    }

    private void HandlePlayerLayerChanged(object payload)
    {
        int layer = -1;
        if (payload is int intLayer)
        {
            layer = intLayer;
        }
        else if (payload is CubeController.CubeLayer cubeLayer)
        {
            layer = (int)cubeLayer;
        }
        else
        {
            ResolvePlayerController();
            layer = playerController != null ? playerController.CurrentLayer : -1;
        }

        ApplyVisibility(layer);
    }

    private void ApplyVisibility(int playerLayer)
    {
        if (spriteRenderer == null)
        {
            return;
        }

        spriteRenderer.enabled = playerLayer == (int)CubeController.CubeLayer.Green;
    }
}
