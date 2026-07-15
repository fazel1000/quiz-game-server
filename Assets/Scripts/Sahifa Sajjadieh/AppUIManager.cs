using UnityEngine;
using UnityEngine.InputSystem;

public class AppUIManager : MonoBehaviour
{
    public GameObject prayerListPanel;
    public GameObject prayerPagePanel;
    public GameObject settingsPanel;
    public PrayerPageUI prayerPageUI;

    private void Start()
    {
        settingsPanel.SetActive(false);
        ShowPrayerList();
    }

    private void Update()
    {
        if (Keyboard.current == null ||
            !Keyboard.current.escapeKey.wasPressedThisFrame)
            return;

        if (settingsPanel.activeSelf)
        {
            CloseSettings();
        }
        else if (prayerPagePanel.activeSelf)
        {
            ShowPrayerList();
        }
    }

    public void OpenPrayer(int prayerId)
    {
        prayerListPanel.SetActive(false);
        prayerPagePanel.SetActive(true);
        settingsPanel.SetActive(false);

        prayerPageUI.LoadPrayer(prayerId);
    }

    public void ShowPrayerList()
    {
        settingsPanel.SetActive(false);
        prayerPagePanel.SetActive(false);
        prayerListPanel.SetActive(true);
    }

    public void OpenSettings()
    {
        settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        settingsPanel.SetActive(false);
    }
}