using UnityEngine;
using NativeWebSocket;
using System.Text;
using TMPro;
using System.Collections.Generic;

public class WebSocketClient : MonoBehaviour
{
    public WebSocket ws;

    public GameUIManager ui;
    public SupabaseManager supabase;

    [Header("Player")]
    public string playerName;

    [Header("Game UI")]
    public TextMeshProUGUI questionText;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI optA, optB, optC, optD;

    float timer = 10;
    bool timerRunning;
    bool answeredThisQuestion = false;

    // سوالات این بازی
    private List<Question> currentGameQuestions;
    private int currentQuestionIndex = 0;
    private int playerScore = 0;
    private int opponentScore = 0;
    private string opponentName = "";

    async void Start()
    {
        ws = new WebSocket("ws://localhost:8081/ws");

        ws.OnOpen += () => Debug.Log("وصل شد");

        ws.OnMessage += (bytes) =>
        {
            string msg = Encoding.UTF8.GetString(bytes);
            HandleMessage(msg);
        };

        await ws.Connect();
    }

    void Update()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        ws?.DispatchMessageQueue();
#endif

        if (timerRunning)
        {
            timer -= Time.deltaTime;
            timerText.text = Mathf.Ceil(timer).ToString();

            if (timer <= 0)
            {
                timerRunning = false;
                SendTimeout();
            }
        }
    }

    // برای شروع بازی
    public async void JoinGame()
    {
        string json = "{\"type\":\"join\",\"name\":\"" + playerName + "\"}";
        await ws.SendText(json);
        Debug.Log("درخواست پیوستن به بازی فرستاده شد");
    }

    // پیام‌های سرور
    void HandleMessage(string msg)
    {
        Debug.Log("پیام دریافت شد: " + msg);

        if (msg.Contains("\"type\":\"match\""))
        {
            // دو بازیکن پیدا شد
            HandleMatchFound(msg);
        }

        if (msg.Contains("\"type\":\"question\""))
        {
            HandleNewQuestion(msg);
        }

        if (msg.Contains("\"type\":\"correct\""))
        {
            HandleCorrectAnswer(msg);
        }

        if (msg.Contains("\"type\":\"wrong\""))
        {
            HandleWrongAnswer(msg);
        }

        if (msg.Contains("\"type\":\"end\""))
        {
            HandleGameEnd(msg);
        }
    }

    void HandleMatchFound(string msg)
    {
        // سرور پیام می‌فرستد که match شروع شد
        Debug.Log("بازی پیدا شد!");
        
        // سوالات را دریافت کن
        supabase.GetRandomQuestions((questions) =>
        {
            if (questions.Count > 0)
            {
                currentGameQuestions = questions;
                currentQuestionIndex = 0;
                playerScore = 0;
                opponentScore = 0;
                
                // به GameUI برو
                ui.StartGame();
                
                // سوال اول را نشان بده
                ShowQuestion();
            }
            else
            {
                Debug.Log("خطا: سوالات دریافت نشد");
            }
        });
    }

    void ShowQuestion()
    {
        if (currentQuestionIndex >= currentGameQuestions.Count)
        {
            EndGame();
            return;
        }

        answeredThisQuestion = false;
        timer = 10;
        timerRunning = true;

        Question q = currentGameQuestions[currentQuestionIndex];

        questionText.text = q.question;
        optA.text = q.option_a;
        optB.text = q.option_b;
        optC.text = q.option_c;
        optD.text = q.option_d;
    }

    void HandleNewQuestion(string msg)
    {
        // این پیام از سرور می‌آید (ممکن است برای sync استفاده شود)
        ShowQuestion();
    }

    void HandleCorrectAnswer(string msg)
    {
        Debug.Log("جواب درست!");
        playerScore += 10;
        
        // بعد از یک تاخیر، سوال بعدی را نشان بده
        Invoke(nameof(MoveToNextQuestion), 1f);
    }

    void HandleWrongAnswer(string msg)
    {
        Debug.Log("جواب غلط!");
        playerScore -= 10;
        
        Invoke(nameof(MoveToNextQuestion), 1f);
    }

    void HandleGameEnd(string msg)
    {
        Debug.Log("بازی تمام شد!");
        EndGame();
    }

    void MoveToNextQuestion()
    {
        currentQuestionIndex++;
        ShowQuestion();
    }

    void EndGame()
    {
        timerRunning = false;
        
        // بروزرسانی امتیاز در Supabase
        supabase.UpdatePlayerScore(playerName, playerScore, (success) =>
        {
            ui.ShowEndScreen(playerName, playerScore);
        });
    }

    // پاسخ دادن
    public async void SendAnswer(string answer)
    {
        if (answeredThisQuestion) return;
        answeredThisQuestion = true;

        // جواب را check کن
        Question q = currentGameQuestions[currentQuestionIndex];
        
        if (answer == q.answer)
        {
            HandleCorrectAnswer("");
        }
        else
        {
            HandleWrongAnswer("");
        }

        // سرور رو اطلاع بده
        string json = "{\"answer\":\"" + answer + "\"}";
        await ws.SendText(json);
    }

    void SendTimeout()
    {
        answeredThisQuestion = true;
        Debug.Log("بدون پاسخ → سوال بعدی");
        MoveToNextQuestion();
    }
}
