using UnityEngine;
using UnityEngine.UI;

public class BlinkUI : MonoBehaviour
{
    [Header("Drag UI Image Here")]
    public Image blinkImage;

    public float speed = 0.5f;
    bool state;

    void Start()
    {
        if (blinkImage != null)
            blinkImage.enabled = false;
    }

    public void StartBlink()
    {
        CancelInvoke();
        InvokeRepeating(nameof(Toggle), speed, speed);
    }

    void Toggle()
    {
        if (blinkImage == null) return;

        state = !state;
        blinkImage.enabled = state;
    }

    public void StopBlink()
    {
        CancelInvoke();

        if (blinkImage != null)
            blinkImage.enabled = false;
    }
}