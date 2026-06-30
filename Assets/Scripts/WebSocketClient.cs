using UnityEngine;
using NativeWebSocket;
using System.Text;
using TMPro;

public class WebSocketClient : MonoBehaviour
{
    public WebSocket ws;

    public GameUIManager ui;

    [Header("Player")]
    public string playerName;

    [Header("Game UI")]
    public TextMeshProUGUI questionText;
    public TextMeshProUGUI timerText;

    public TextMeshProUGUI optA, optB, optC, optD;

    float timer = 10;
    bool timerRunning;

    bool answeredThisQuestion = false;

    async void Start()
    {
        ws = new WebSocket("ws://localhost:8081/ws");

        ws.OnOpen += () => Debug.Log("Connected");

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

    // ---------------- JOIN ----------------
    public async void JoinGame()
    {
        string json = "{\"type\":\"join\",\"name\":\"" + playerName + "\"}";
        await ws.SendText(json);
    }

    // ---------------- MESSAGE ----------------
    void HandleMessage(string msg)
    {
        if (msg.Contains("match"))
        {
            ui.StartGame();
        }

        if (msg.Contains("question"))
        {
            answeredThisQuestion = false;

            timer = 10;
            timerRunning = true;

            questionText.text = msg;
        }

        if (msg.Contains("correct"))
        {
            Debug.Log("Correct Answer");
        }

        if (msg.Contains("wrong"))
        {
            Debug.Log("Wrong Answer");
        }

        if (msg.Contains("end"))
        {
            Debug.Log("Game End");
        }
    }

    // ---------------- ANSWER ----------------
    public async void SendAnswer(string answer)
    {
        if (answeredThisQuestion) return; // ⭐ فقط یک جواب

        answeredThisQuestion = true;

        string json = "{\"answer\":\"" + answer + "\"}";
        await ws.SendText(json);
    }

    // ---------------- TIMEOUT ----------------
    void SendTimeout()
    {
        answeredThisQuestion = true;

        Debug.Log("No answer → next question");
    }
}