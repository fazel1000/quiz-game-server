using UnityEngine;
using TMPro;

public class GameUIManager : MonoBehaviour
{
    [Header("UI")]
    public GameObject mainMenuUI;
    public GameObject gameUI;
    public GameObject endUI;

    [Header("Blink")]
    public BlinkUI blink;

    [Header("Player")]
    public TMP_InputField nameInput;
    public TextMeshProUGUI playerNameText;

    [Header("End Screen")]
    public TextMeshProUGUI endWinnerNameText;
    public TextMeshProUGUI endScoreText;
    public Button backToMenuButton;

    [Header("Systems")]
    public WebSocketClient ws;
    public SupabaseManager supabase;

    string playerName;

    void Start()
    {
        // اگر بازیکن نامی وارد نکند
        if (backToMenuButton != null)
        {
            backToMenuButton.onClick.AddListener(BackToMenu);
        }
    }

    // -------- نام --------
    public void ConfirmName()
    {
        playerName = string.IsNullOrEmpty(nameInput.text) ? "Player_" + Random.Range(1000, 9999) : nameInput.text;
        
        playerNameText.text = playerName;
        ws.playerName = playerName;

        // بازیکن را به Supabase اضافه کن
        supabase.AddPlayer(playerName, (success) =>
        {
            if (success)
            {
                Debug.Log("بازیکن با موفقیت اضافه شد");
            }
        });
    }

    // -------- آنلاین --------
    public void GoOnline()
    {
        blink.StartBlink();
        ws.JoinGame(); // منتظر یافتن بازیکن دوم
    }

    // -------- بازی شروع شد --------
    public void StartGame()
    {
        blink.StopBlink();

        mainMenuUI.SetActive(false);
        gameUI.SetActive(true);
        if (endUI != null) endUI.SetActive(false);
    }

    // -------- صفحه پایان --------
    public void ShowEndScreen(string winnerName, int score)
    {
        gameUI.SetActive(false);
        
        if (endUI != null)
        {
            endUI.SetActive(true);
            
            if (endWinnerNameText != null)
                endWinnerNameText.text = winnerName;
            
            if (endScoreText != null)
                endScoreText.text = "امتیاز: " + score.ToString();
        }
    }

    // -------- بازگشت به منو --------
    public void BackToMenu()
    {
        gameUI.SetActive(false);
        
        if (endUI != null) 
            endUI.SetActive(false);
        
        mainMenuUI.SetActive(true);

        // Input field را پاک کن
        if (nameInput != null)
            nameInput.text = "";
    }
}
