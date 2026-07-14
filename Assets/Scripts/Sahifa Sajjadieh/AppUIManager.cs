using UnityEngine;

public class AppUIManager : MonoBehaviour
{
    public GameObject prayerListPanel;
    public GameObject prayerPagePanel;
    public PrayerPageUI prayerPageUI;

    private void Start()
    {
        ShowPrayerList();
    }

    public void OpenPrayer(int prayerId)
    {
        prayerListPanel.SetActive(false);
        prayerPagePanel.SetActive(true);

        prayerPageUI.LoadPrayer(prayerId);
    }

    public void ShowPrayerList()
    {
        prayerPagePanel.SetActive(false);
        prayerListPanel.SetActive(true);
    }
}