using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class SelectColor : MonoBehaviour
{
    private enum MenuColor
    {
        None,
        Red,
        Green,
        Blue
    }

    [Header("UI")]
    [SerializeField] private RectTransform menuRoot;
    [SerializeField] private Image redButton;
    [SerializeField] private Image greenButton;
    [SerializeField] private Image blueButton;

    [Header("Target")]
    [SerializeField] private SpriteRenderer targetRenderer;

    [Header("Tween")]
    [SerializeField] private float showDuration = 0.18f;
    [SerializeField] private float hideDuration = 0.14f;
    [SerializeField] private Ease showEase = Ease.OutBack;
    [SerializeField] private Ease hideEase = Ease.InBack;

    [Header("Colors")]
    [SerializeField] private Color redColor = Color.red;
    [SerializeField] private Color greenColor = Color.green;
    [SerializeField] private Color blueColor = Color.blue;

    private const float RestingAlpha = 0.2f;
    private const float HoveredAlpha = 1f;

    private Tween scaleTween;
    private bool isHoldingMouse;
    private MenuColor hoveredColor = MenuColor.None;
    private Color defaultTargetColor = Color.white;
    private bool cachedDefaultColor;
    private float previousTimeScale = 1f;
    private bool hasStoredTimeScale;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoAttachToMenu()
    {
        if (Object.FindFirstObjectByType<SelectColor>() != null)
        {
            return;
        }

        GameObject menuObject = GameObject.Find("Menu_Change");
        if (menuObject == null)
        {
            return;
        }

        menuObject.AddComponent<SelectColor>();
    }

    private void Awake()
    {
        if (menuRoot == null)
        {
            menuRoot = transform as RectTransform;
        }

        if (menuRoot != null)
        {
            menuRoot.localScale = Vector3.zero;
        }
        SetButtonsAlpha(RestingAlpha, MenuColor.None);
    }

    private void OnDisable()
    {
        RestoreTimeScale();
        scaleTween?.Kill();
        scaleTween = null;
    }

    private void Update()
    {
        if (!EnsureRuntimeReferences())
        {
            return;
        }

        bool mousePressed = Mouse.current != null && Mouse.current.leftButton.isPressed;

        if (mousePressed && !isHoldingMouse)
        {
            isHoldingMouse = true;
            SetButtonsAlpha(RestingAlpha, MenuColor.None);
            StoreAndSetTimeScale(0.1f);
            ShowMenu();
        }

        if (mousePressed)
        {
            hoveredColor = GetHoveredColor();
            SetButtonsAlpha(RestingAlpha, hoveredColor);
        }

        if (!mousePressed && isHoldingMouse)
        {
            ApplyHoveredColorOrDefault();
            HideMenu();
            RestoreTimeScale();
            isHoldingMouse = false;
            hoveredColor = MenuColor.None;
            SetButtonsAlpha(RestingAlpha, MenuColor.None);
        }
    }

    private bool EnsureRuntimeReferences()
    {
        if (menuRoot == null)
        {
            menuRoot = transform as RectTransform;
        }

        if (menuRoot == null || targetRenderer == null)
        {
            return false;
        }

        if (!cachedDefaultColor)
        {
            defaultTargetColor = targetRenderer.color;
            cachedDefaultColor = true;
        }

        return true;
    }

    private void TryResolveTargetRenderer()
    {
        if (GameManager.Instance != null && GameManager.Instance.cubeControllerRef != null)
        {
            Transform cubeTransform = GameManager.Instance.cubeControllerRef.transform;

            Transform skinTransform = cubeTransform.Find("Skin");
            if (skinTransform != null && skinTransform.TryGetComponent(out SpriteRenderer skinRenderer))
            {
                targetRenderer = skinRenderer;
                return;
            }

            targetRenderer = GameManager.Instance.cubeControllerRef.GetComponentInChildren<SpriteRenderer>(true);
        }
    }

    private Button FindButtonRecursive(Transform root, string targetName)
    {
        if (root == null)
        {
            return null;
        }

        foreach (Transform child in root)
        {
            if (child.name == targetName && child.TryGetComponent(out Button button))
            {
                return button;
            }

            Button nestedButton = FindButtonRecursive(child, targetName);
            if (nestedButton != null)
            {
                return nestedButton;
            }
        }

        return null;
    }

    private void ShowMenu()
    {
        if (menuRoot == null)
        {
            return;
        }

        scaleTween?.Kill();
        scaleTween = menuRoot.DOScale(Vector3.one, showDuration).SetEase(showEase).SetUpdate(true);
    }

    private void HideMenu()
    {
        if (menuRoot == null)
        {
            return;
        }

        scaleTween?.Kill();
        scaleTween = menuRoot.DOScale(Vector3.zero, hideDuration).SetEase(hideEase).SetUpdate(true);
    }

    private void ApplyHoveredColorOrDefault()
    {
        if (targetRenderer == null)
        {
            return;
        }

        CubeController cubeController = GameManager.Instance != null ? GameManager.Instance.cubeControllerRef : null;

        targetRenderer.color = hoveredColor switch
        {
            MenuColor.Red => ApplyLayerState(cubeController, CubeController.CubeLayer.Red, redColor),
            MenuColor.Green => ApplyLayerState(cubeController, CubeController.CubeLayer.Green, greenColor),
            MenuColor.Blue => ApplyLayerState(cubeController, CubeController.CubeLayer.Blue, blueColor),
            _ => ApplyLayerState(cubeController, CubeController.CubeLayer.White, defaultTargetColor)
        };
    }

    private Color ApplyLayerState(CubeController cubeController, CubeController.CubeLayer layer, Color color)
    {
        if (cubeController != null)
        {
            cubeController.SetCurrentLayer(layer);
        }

        return color;
    }

    private void StoreAndSetTimeScale(float targetTimeScale)
    {
        if (!hasStoredTimeScale)
        {
            previousTimeScale = Time.timeScale;
            hasStoredTimeScale = true;
        }

        Time.timeScale = targetTimeScale;
    }

    private void RestoreTimeScale()
    {
        if (!hasStoredTimeScale)
        {
            return;
        }

        Time.timeScale = previousTimeScale;
        hasStoredTimeScale = false;
    }

    private void SetButtonsAlpha(float alpha, MenuColor highlightedColor)
    {
        SetImageAlpha(redButton, highlightedColor == MenuColor.Red ? HoveredAlpha : alpha);
        SetImageAlpha(greenButton, highlightedColor == MenuColor.Green ? HoveredAlpha : alpha);
        SetImageAlpha(blueButton, highlightedColor == MenuColor.Blue ? HoveredAlpha : alpha);
    }

    private void SetImageAlpha(Image image, float alpha)
    {
        if (image == null)
        {
            return;
        }

        Color color = image.color;
        color.a = alpha;
        image.color = color;
    }

    private MenuColor GetHoveredColor()
    {
        if (EventSystem.current == null || menuRoot == null)
        {
            return MenuColor.None;
        }

        Vector2 pointerPosition = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
        PointerEventData pointerEventData = new PointerEventData(EventSystem.current)
        {
            position = pointerPosition
        };

        List<RaycastResult> raycastResults = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerEventData, raycastResults);

        for (int index = 0; index < raycastResults.Count; index++)
        {
            GameObject hitObject = raycastResults[index].gameObject;

            if (MatchesButton(hitObject, redButton))
            {
                return MenuColor.Red;
            }

            if (MatchesButton(hitObject, greenButton))
            {
                return MenuColor.Green;
            }

            if (MatchesButton(hitObject, blueButton))
            {
                return MenuColor.Blue;
            }
        }

        return MenuColor.None;
    }

    private bool MatchesButton(GameObject hitObject, Image button)
    {
        if (hitObject == null || button == null)
        {
            return false;
        }

        return hitObject == button.gameObject || hitObject.transform.IsChildOf(button.transform);
    }
}
