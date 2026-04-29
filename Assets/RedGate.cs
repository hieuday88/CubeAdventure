using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class RedGate : MonoBehaviour
{
    private Color originalColor = new Color(1f, 1f, 1f) * 90 / 255f;
    private Color targetColor = Color.white;

    void OnCollisionEnter2D(Collision2D collision)
    {
        this.GetComponent<SpriteRenderer>().color = targetColor;
        DOVirtual.DelayedCall(1f, () =>
        {
            this.GetComponent<SpriteRenderer>().color = originalColor;
        });
    }
}
