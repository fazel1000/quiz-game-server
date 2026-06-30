using UnityEngine;

public class BlinkUI : MonoBehaviour
{
    public GameObject blinkObject;
    bool state = true;

    void Start()
    {
        InvokeRepeating("Blink", 0.5f, 0.5f);
    }

    void Blink()
    {
        state = !state;
        blinkObject.SetActive(state);
    }

    public void StopBlink()
    {
        CancelInvoke();
        blinkObject.SetActive(false);
    }
}