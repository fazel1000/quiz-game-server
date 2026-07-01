using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;

public class PlayerManager : MonoBehaviour
{
    [Header("Supabase")]
    public string supabaseUrl = "https://tjdfrhuwekdlrokkzamm.supabase.co/rest/v1/online_players";
    public string apiKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6InRqZGZyaHV3ZWtkbHJva2t6YW1tIiwicm9sZSI6ImFub24iLCJpYXQiOjE3ODI4MDYzMzEsImV4cCI6MjA5ODM4MjMzMX0.G-DqziwuxhBYG-mmPRVqzq3hn-DcoUx9c4hdwdfpx3E";  // ⚠️ اینو صحیح کن

    private string playerName;
    private string sessionId;

    void Start()
    {
        // ID منحصر برفرد برای این دستگاه
        sessionId = System.Guid.NewGuid().ToString();
        
        // نام بازیکن تصادفی
        GenerateRandomPlayerName();
        
        // ثبت بازیکن در Database
        RegisterPlayer();
    }

    void GenerateRandomPlayerName()
    {
        int randomNumber = Random.Range(1, 10000);
        playerName = $"Player {randomNumber:D2}";
        Debug.Log($"✅ Generated Player Name: {playerName}");
    }

    void RegisterPlayer()
    {
        StartCoroutine(AddPlayerToDatabase());
    }

    IEnumerator AddPlayerToDatabase()
    {
        // ✅ JSON صحیح (snake_case)
        string jsonData = $@"{{
            ""player_name"": ""{playerName}"",
            ""session_id"": ""{sessionId}"",
            ""is_online"": true
        }}";

        Debug.Log($"📤 Sending JSON: {jsonData}");

        UnityWebRequest request = new UnityWebRequest(supabaseUrl, "POST");
        request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(jsonData));
        request.downloadHandler = new DownloadHandlerBuffer();

        // Headers
        request.SetRequestHeader("apikey", apiKey);
        request.SetRequestHeader("Authorization", "Bearer " + apiKey);
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Prefer", "return=representation");

        yield return request.SendWebRequest();

        // ✅ بهتر Debug
        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log($"✅ Player '{playerName}' registered successfully!");
            Debug.Log($"Response: {request.downloadHandler.text}");
        }
        else
        {
            Debug.LogError($"❌ Error Code: {request.responseCode}");
            Debug.LogError($"❌ Error Message: {request.error}");
            Debug.LogError($"❌ Response: {request.downloadHandler.text}");
        }
    }

    // وقتی بازیکن از برنامه خارج شد
    void OnApplicationQuit()
    {
        RemovePlayer();
    }

    void RemovePlayer()
    {
        StartCoroutine(DeletePlayerFromDatabase());
    }

    IEnumerator DeletePlayerFromDatabase()
    {
        string deleteUrl = $"{supabaseUrl}?session_id=eq.{sessionId}";

        UnityWebRequest request = UnityWebRequest.Delete(deleteUrl);

        request.SetRequestHeader("apikey", apiKey);
        request.SetRequestHeader("Authorization", "Bearer " + apiKey);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log($"✅ Player '{playerName}' removed from online list!");
        }
    }

    // برای بروزرسانی آخرین فعالیت
    public void UpdatePlayerActivity()
    {
        StartCoroutine(UpdateActivityInDatabase());
    }

    IEnumerator UpdateActivityInDatabase()
    {
        string updateUrl = $"{supabaseUrl}?session_id=eq.{sessionId}";

        string jsonData = $@"{{
            ""last_activity"": ""{System.DateTime.UtcNow:O}""
        }}";

        UnityWebRequest request = new UnityWebRequest(updateUrl, "PATCH");
        request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(jsonData));
        request.downloadHandler = new DownloadHandlerBuffer();

        request.SetRequestHeader("apikey", apiKey);
        request.SetRequestHeader("Authorization", "Bearer " + apiKey);
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log($"✅ Activity updated!");
        }
    }
}