using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class SupabaseManager : MonoBehaviour
{
    public string url = "https://tjdfrhuwekdlrokkzamm.supabase.co";

    // ⚠️ اینو درست کن:
    public string apiKey = "YOUR_ANON_KEY";

    // ---------------- ADD PLAYER ----------------
    public void AddPlayer(string name)
    {
        StartCoroutine(SendPlayer(name));
    }

    IEnumerator SendPlayer(string name)
    {
        string json = "{\"name\":\"" + name + "\",\"score\":0}";

        UnityWebRequest req = new UnityWebRequest(url + "/rest/v1/leaderboard", "POST");

        byte[] body = System.Text.Encoding.UTF8.GetBytes(json);

        req.uploadHandler = new UploadHandlerRaw(body);
        req.downloadHandler = new DownloadHandlerBuffer();

        req.SetRequestHeader("Content-Type", "application/json");
        req.SetRequestHeader("apikey", apiKey);
        req.SetRequestHeader("Authorization", "Bearer " + apiKey);

        yield return req.SendWebRequest();

        Debug.Log(req.result == UnityWebRequest.Result.Success
            ? "Player saved"
            : req.error);
    }
}