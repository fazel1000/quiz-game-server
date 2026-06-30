using UnityEngine;
using TMPro;

public class GameUIManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject mainMenuUI;
    public GameObject gameUI;

    [Header("Player Name")]
    public TMP_InputField nameInput;
    public TextMeshProUGUI playerNameText;

    string playerName;

    // ---------------- NAME SAVE ----------------
    public void ConfirmName()
    {
        if (string.IsNullOrEmpty(nameInput.text)) return;

        playerName = nameInput.text;

        playerNameText.text = playerName;

        Debug.Log("Player Name: " + playerName);
    }

    // ---------------- UI SWITCH ----------------
    public void GoOnline()
    {
        mainMenuUI.SetActive(false);
        gameUI.SetActive(true);
    }

    public void GoOffline()
    {
        mainMenuUI.SetActive(false);
        gameUI.SetActive(true);
    }

    public void BackToMenu()
    {
        gameUI.SetActive(false);
        mainMenuUI.SetActive(true);
    }
}