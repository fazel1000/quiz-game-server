using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class SupabaseManager : MonoBehaviour
{
    [Header("Supabase")]
    public string url = "https://tjdfrhuwekdlrokkzamm.supabase.co";
    public string apiKey = "sb_publishable_ISp17xws8PunBbTkO6P5yQ_-Wru_y4s";

    [Header("UI Leaderboard")]
    public Transform contentParent;   // Scroll Content
    public GameObject rowPrefab;       // یک Row (Name + Score)

    // ---------------- SEND SCORE ----------------
    public void AddScore(string name, int score)
    {
        StartCoroutine(SendScore(name, score));
    }

    IEnumerator SendScore(string name, int score)
    {
        string json = "{\"name\":\"" + name + "\",\"score\":" + score + "}";

        UnityWebRequest request = new UnityWebRequest(url + "/rest/v1/leaderboard", "POST");

        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();

        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("apikey", apiKey);
        request.SetRequestHeader("Authorization", "Bearer " + apiKey);
        request.SetRequestHeader("Prefer", "return=representation");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Score saved");
        }
        else
        {
            Debug.Log("Error: " + request.error);
        }
    }

    // ---------------- GET LEADERBOARD ----------------
    public void GetLeaderboard()
    {
        StartCoroutine(FetchLeaderboard());
    }

    IEnumerator FetchLeaderboard()
    {
        UnityWebRequest request = UnityWebRequest.Get(
            url + "/rest/v1/leaderboard?select=*&order=score.desc"
        );

        request.SetRequestHeader("apikey", apiKey);
        request.SetRequestHeader("Authorization", "Bearer " + apiKey);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("LB: " + request.downloadHandler.text);
            BuildLeaderboard(request.downloadHandler.text);
        }
        else
        {
            Debug.Log("Error: " + request.error);
        }
    }

    // ---------------- BUILD UI ----------------
    void BuildLeaderboard(string json)
    {
        // پاک کردن قبلی‌ها
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }

        // ساده‌ترین parse دستی (برای MVP)
        string[] rows = json.Split('{');

        foreach (string row in rows)
        {
            if (row.Contains("name") && row.Contains("score"))
            {
                string name = Extract(row, "name\":\"", "\"");
                string score = Extract(row, "score\":", "}");

                GameObject obj = Instantiate(rowPrefab, contentParent);

                obj.transform.Find("Name").GetComponent<TextMeshProUGUI>().text = name;
                obj.transform.Find("Score").GetComponent<TextMeshProUGUI>().text = score;
            }
        }
    }

    string Extract(string text, string start, string end)
    {
        int s = text.IndexOf(start) + start.Length;
        int e = text.IndexOf(end, s);
        return text.Substring(s, e - s);
    }
}