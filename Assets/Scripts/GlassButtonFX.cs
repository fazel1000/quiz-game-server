using UnityEngine;
using UnityEngine.EventSystems;

public class GlassButtonFX : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    public Material mat;

    void Start()
    {
        Debug.Log("START: Script is running");

        if (mat == null)
        {
            Debug.LogError("MATERIAL IS NULL!");
            return;
        }

        mat.SetFloat("GlowIntensity", 0);
        Debug.Log("START: Glow set to 0");
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Debug.Log("HOVER ENTER");

        if (mat == null)
        {
            Debug.LogError("MAT NULL on Hover");
            return;
        }

        mat.SetFloat("GlowIntensity", 1.5f);
        Debug.Log("Glow = 1.5");
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Debug.Log("HOVER EXIT");

        if (mat == null)
        {
            Debug.LogError("MAT NULL on Exit");
            return;
        }

        mat.SetFloat("GlowIntensity", 0);
        Debug.Log("Glow = 0");
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log("CLICKED");

        if (mat == null)
        {
            Debug.LogError("MAT NULL on Click");
            return;
        }

        mat.SetFloat("GlowIntensity", 3f);
        Debug.Log("Glow = 3");
    }
}