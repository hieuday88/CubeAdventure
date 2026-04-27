using UnityEngine;

public class LogicInput : MonoBehaviour
{
    private Color offColor = new Color(90f / 255f, 90f / 255f, 90f / 255f);
    private Color onColor = new Color(1f, 1f, 1f);
    [SerializeField] private Sprite offRenderer;
    [SerializeField] private Sprite onRenderer;

    private SpriteRenderer spriteRenderer;

    [SerializeField] private bool isOn;
    public bool IsOn => isOn;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        if (spriteRenderer == null)
        {
            return;
        }

        if (isOn)
        {
            spriteRenderer.color = onColor;
            spriteRenderer.sprite = onRenderer;
        }
        else
        {
            spriteRenderer.color = offColor;
            spriteRenderer.sprite = offRenderer;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        SetIsOn(!isOn);
    }

    public void SetIsOn(bool value)
    {
        if (isOn == value)
        {
            return;
        }

        isOn = value;
        UpdateVisuals();
        ObserverManager<LogicEventId>.Post(LogicEventId.LogicInputChanged, this);
    }

}

