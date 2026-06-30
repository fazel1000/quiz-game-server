using UnityEngine;
using TMPro;
using NativeWebSocket;
using System.Text;
using System;

public class WebSocketClient : MonoBehaviour
{
    public TextMeshProUGUI questionText;
    public TextMeshProUGUI timerText;

    public TextMeshProUGUI optA;
    public TextMeshProUGUI optB;
    public TextMeshProUGUI optC;
    public TextMeshProUGUI optD;

    public TextMeshProUGUI player1Name;
    public TextMeshProUGUI player2Name;

    public TextMeshProUGUI player1Score;
    public TextMeshProUGUI player2Score;

    public GameObject matchmakingUI;
    public GameObject questionUI;

    WebSocket ws;
    float timer = 10;
    bool isTimerRunning = false;

    async void Start()
    {
        ws = new WebSocket("ws://localhost:8081/ws");

        ws.OnOpen += () =>
        {
            Debug.Log("Connected");
        };

        ws.OnMessage += (bytes) =>
        {
            string msg = Encoding.UTF8.GetString(bytes);
            Debug.Log(msg);

            HandleMessage(msg);
        };

        await ws.Connect();
    }

    void Update()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        ws?.DispatchMessageQueue();
#endif

        if (isTimerRunning)
        {
            timer -= Time.deltaTime;
            timerText.text = Mathf.Ceil(timer).ToString();

            if (timer <= 0)
            {
                isTimerRunning = false;
            }
        }
    }

    void HandleMessage(string msg)
    {
        if (msg.Contains("match"))
        {
            matchmakingUI.SetActive(false);
            questionUI.SetActive(true);
        }

        if (msg.Contains("question"))
        {
            // ساده (فعلاً بدون JSON حرفه‌ای)
            questionText.text = msg;

            timer = 10;
            isTimerRunning = true;
        }

        if (msg.Contains("correct") || msg.Contains("wrong"))
        {
            Debug.Log("Score updated");
        }

        if (msg.Contains("end"))
        {
            Debug.Log("Game Ended");
        }
    }

    public async void SendAnswer(string answer)
    {
        if (ws.State == WebSocketState.Open)
        {
            string json = "{\"answer\":\"" + answer + "\"}";
            await ws.SendText(json);
        }
    }
}