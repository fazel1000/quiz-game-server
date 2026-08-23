using UnityEngine;
using RTLTMPro;

public class SurahItemUI : MonoBehaviour
{
    [SerializeField] private RTLTextMeshPro surahNameText;

    public void Setup(string surahName)
    {
        surahNameText.text = surahName;
    }
}