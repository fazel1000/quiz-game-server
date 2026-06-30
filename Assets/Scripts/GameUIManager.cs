using UnityEngine;
using TMPro;

public class GameUIManager : MonoBehaviour
{
    [Header("UI")]
    public GameObject mainMenuUI;
    public GameObject gameUI;

    [Header("Blink")]
    public BlinkUI blink;

    [Header("Player")]
    public TMP_InputField nameInput;
    public TextMeshProUGUI playerNameText;

    [Header("Systems")]
    public WebSocketClient ws;
    public SupabaseManager supabase;

    string playerName;

    // ---------------- NAME ----------------
    public void ConfirmName()
    {
        if (string.IsNullOrEmpty(nameInput.text)) return;

        playerName = nameInput.text;
        playerNameText.text = playerName;

        supabase.AddPlayer(playerName);
        ws.playerName = playerName;
    }

    // ---------------- ONLINE ----------------
    public void GoOnline()
    {
        blink.StartBlink();
        ws.JoinGame(); // queue
    }

    // ---------------- MATCH FOUND ----------------
    public void StartGame()
    {
        blink.StopBlink();

        mainMenuUI.SetActive(false);
        gameUI.SetActive(true);
    }
}