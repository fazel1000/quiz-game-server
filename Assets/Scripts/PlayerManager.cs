using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using RTLTMPro;

public class PlayerManager : MonoBehaviour
{
    [Header("Server")]
    public string serverBaseUrl = "https://fazel1000.pythonanywhere.com";

    [Header("UI")]
    public TMP_InputField nameInput;
    public RTLTextMeshPro statusText;

    public string PlayerName { get; private set; }
    public string SessionId { get; private set; }

    private bool isRegistered = false;
    private bool isSearching = false;
    private bool hasLoggedOut = false;

    private Action<int, string> onMatchFound;
    private Coroutine heartbeatCoroutine;

    void Start()
    {
        SessionId = PlayerPrefs.GetString("session_id", "");

        if (string.IsNullOrEmpty(SessionId))
        {
            SessionId = Guid.NewGuid().ToString();
            PlayerPrefs.SetString("session_id", SessionId);
        }

        PlayerName = PlayerPrefs.GetString("player_name", "");

        if (nameInput != null && !string.IsNullOrEmpty(PlayerName))
        {
            nameInput.text = PlayerName;
        }
    }

    public void OnConfirmNameClicked()
    {
        string inputName = nameInput.text.Trim();

        if (string.IsNullOrEmpty(inputName))
        {
            SetStatus("اسم را وارد کن");
            return;
        }

        PlayerName = inputName;
        PlayerPrefs.SetString("player_name", PlayerName);

        hasLoggedOut = false;
        StartCoroutine(RegisterPlayer());
    }

    IEnumerator RegisterPlayer()
    {
        string url = serverBaseUrl + "/register_player";

        string json = "{"
            + "\"player_name\":\"" + EscapeJson(PlayerName) + "\","
            + "\"session_id\":\"" + EscapeJson(SessionId) + "\""
            + "}";

        UnityWebRequest request = new UnityWebRequest(url, "POST");
        request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        request.downloadHandler = new DownloadHandlerBuffer();

        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            RegisterResponse response = JsonUtility.FromJson<RegisterResponse>(request.downloadHandler.text);

            if (response.status == "ok")
            {
                isRegistered = true;
                hasLoggedOut = false;
                SetStatus("اسم ثبت شد");

                StartHeartbeat();

                Debug.Log("Player Registered: " + PlayerName);
            }
            else
            {
                SetStatus("خطا در ثبت اسم");
                Debug.LogError(request.downloadHandler.text);
            }
        }
        else
        {
            SetStatus("خطا در اتصال");
            Debug.LogError(request.error);
        }
    }

    void StartHeartbeat()
    {
        if (heartbeatCoroutine != null)
        {
            StopCoroutine(heartbeatCoroutine);
        }

        heartbeatCoroutine = StartCoroutine(HeartbeatLoop());
    }

    IEnumerator HeartbeatLoop()
    {
        while (isRegistered)
        {
            yield return SendHeartbeat();
            yield return new WaitForSeconds(10f);
        }
    }

    IEnumerator SendHeartbeat()
    {
        string url = serverBaseUrl + "/heartbeat_player";

        string json = "{"
            + "\"session_id\":\"" + EscapeJson(SessionId) + "\""
            + "}";

        UnityWebRequest request = new UnityWebRequest(url, "POST");
        request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        request.downloadHandler = new DownloadHandlerBuffer();

        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();
    }

    public void StartFindMatch(Action<int, string> callback)
    {
        if (isSearching)
            return;

        onMatchFound = callback;
        StartCoroutine(FindMatchProcess());
    }

    IEnumerator FindMatchProcess()
    {
        if (string.IsNullOrEmpty(PlayerName))
        {
            SetStatus("اول اسم را وارد کن");
            yield break;
        }

        if (!isRegistered)
        {
            yield return RegisterPlayer();
        }

        isSearching = true;
        SetStatus("در حال پیدا کردن حریف...");

        yield return SendFindMatchRequest();

        while (isSearching)
        {
            yield return new WaitForSeconds(2f);
            yield return SendCheckMatchRequest();
        }
    }

    IEnumerator SendFindMatchRequest()
    {
        string url = serverBaseUrl + "/find_match";

        string json = "{"
            + "\"player_name\":\"" + EscapeJson(PlayerName) + "\","
            + "\"session_id\":\"" + EscapeJson(SessionId) + "\""
            + "}";

        UnityWebRequest request = new UnityWebRequest(url, "POST");
        request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        request.downloadHandler = new DownloadHandlerBuffer();

        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            MatchResponse response = JsonUtility.FromJson<MatchResponse>(request.downloadHandler.text);
            HandleMatchResponse(response);
        }
        else
        {
            isSearching = false;
            SetStatus("خطا در جستجوی حریف");
            Debug.LogError(request.error);
        }
    }

    IEnumerator SendCheckMatchRequest()
    {
        string url = serverBaseUrl + "/check_match?session_id=" + UnityWebRequest.EscapeURL(SessionId);

        UnityWebRequest request = UnityWebRequest.Get(url);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            MatchResponse response = JsonUtility.FromJson<MatchResponse>(request.downloadHandler.text);
            HandleMatchResponse(response);
        }
        else
        {
            isSearching = false;
            SetStatus("خطا در بررسی مسابقه");
            Debug.LogError(request.error);
        }
    }

    void HandleMatchResponse(MatchResponse response)
    {
        if (response.status == "match_found")
        {
            isSearching = false;

            SetStatus("حریف پیدا شد: " + response.opponent_name);

            if (onMatchFound != null)
            {
                onMatchFound.Invoke(response.match_id, response.opponent_name);
            }
        }
        else if (response.status == "waiting")
        {
            SetStatus("منتظر حریف...");
        }
        else
        {
            SetStatus("خطا");
        }
    }

    void SendLogoutBlocking()
    {
        if (hasLoggedOut)
            return;

        if (string.IsNullOrEmpty(SessionId))
            return;

        hasLoggedOut = true;

        try
        {
            string url = serverBaseUrl + "/logout_player";

            string json = "{"
                + "\"session_id\":\"" + EscapeJson(SessionId) + "\""
                + "}";

            byte[] body = Encoding.UTF8.GetBytes(json);

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "POST";
            request.ContentType = "application/json";
            request.ContentLength = body.Length;
            request.Timeout = 3000;

            using (Stream stream = request.GetRequestStream())
            {
                stream.Write(body, 0, body.Length);
            }

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                Debug.Log("Player Logged Out");
            }
        }
        catch (Exception e)
        {
            Debug.Log("Logout failed: " + e.Message);
        }
    }

    void OnApplicationQuit()
    {
        isRegistered = false;
        SendLogoutBlocking();
    }

    void OnDestroy()
    {
        if (Application.isPlaying)
        {
            isRegistered = false;
            SendLogoutBlocking();
        }
    }

    void OnApplicationPause(bool pause)
    {
        if (pause)
        {
            isRegistered = false;
            SendLogoutBlocking();
        }
        else
        {
            if (!string.IsNullOrEmpty(PlayerName))
            {
                hasLoggedOut = false;
                StartCoroutine(RegisterPlayer());
            }
        }
    }

    void SetStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }

        Debug.Log(message);
    }

    string EscapeJson(string value)
    {
        return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }

    [Serializable]
    public class RegisterResponse
    {
        public string status;
        public string message;
        public int player_id;
    }

    [Serializable]
    public class MatchResponse
    {
        public string status;
        public string message;
        public int match_id;
        public string opponent_name;
    }
}