using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class Question
{
    public int id;
    public string question;
    public string option_a;
    public string option_b;
    public string option_c;
    public string option_d;
    public string answer;
}

[System.Serializable]
public class QuestionList
{
    public Question[] questions;
}

[System.Serializable]
public class LeaderboardEntry
{
    public int id;
    public string name;
    public int score;
}

[System.Serializable]
public class LeaderboardList
{
    public LeaderboardEntry[] leaderboard;
}

public class SupabaseManager : MonoBehaviour
{
    public string url = "https://tjdfrhuwekdlrokkzamm.supabase.co";
    public string apiKey = "YOUR_ANON_KEY"; // ⚠️ publishable key رو اینجا بزار

    // برای دریافت سوالات
    public void GetRandomQuestions(System.Action<List<Question>> callback)
    {
        StartCoroutine(FetchRandomQuestions(callback));
    }

    IEnumerator FetchRandomQuestions(System.Action<List<Question>> callback)
    {
        // تمام سوالات رو دریافت کن
        string endpoint = url + "/rest/v1/questions?select=*";

        UnityWebRequest req = UnityWebRequest.Get(endpoint);
        req.SetRequestHeader("apikey", apiKey);
        req.SetRequestHeader("Authorization", "Bearer " + apiKey);

        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            string json = req.downloadHandler.text;
            
            // JSON را Deserialize کن
            QuestionList questionList = new QuestionList();
            questionList.questions = JsonUtility.FromJson<Question[]>(json);

            // 10 سوال تصادفی انتخاب کن
            List<Question> randomQuestions = new List<Question>();
            List<int> usedIndexes = new List<int>();

            for (int i = 0; i < 10 && i < questionList.questions.Length; i++)
            {
                int randomIndex;
                do
                {
                    randomIndex = Random.Range(0, questionList.questions.Length);
                } while (usedIndexes.Contains(randomIndex));

                usedIndexes.Add(randomIndex);
                randomQuestions.Add(questionList.questions[randomIndex]);
            }

            callback?.Invoke(randomQuestions);
        }
        else
        {
            Debug.Log("خطا در دریافت سوالات: " + req.error);
            callback?.Invoke(new List<Question>());
        }
    }

    // برای اضافه کردن بازیکن
    public void AddPlayer(string name, System.Action<bool> callback = null)
    {
        StartCoroutine(SendPlayer(name, callback));
    }

    IEnumerator SendPlayer(string name, System.Action<bool> callback)
    {
        string playerName = string.IsNullOrEmpty(name) ? "Player_" + Random.Range(1000, 9999) : name;

        string json = "{\"name\":\"" + playerName + "\",\"score\":0}";

        UnityWebRequest req = new UnityWebRequest(url + "/rest/v1/leaderboard", "POST");

        byte[] body = System.Text.Encoding.UTF8.GetBytes(json);

        req.uploadHandler = new UploadHandlerRaw(body);
        req.downloadHandler = new DownloadHandlerBuffer();

        req.SetRequestHeader("Content-Type", "application/json");
        req.SetRequestHeader("apikey", apiKey);
        req.SetRequestHeader("Authorization", "Bearer " + apiKey);

        yield return req.SendWebRequest();

        bool success = req.result == UnityWebRequest.Result.Success;
        
        if (success)
        {
            Debug.Log("بازیکن ذخیره شد: " + playerName);
        }
        else
        {
            Debug.Log("خطا: " + req.error);
        }

        callback?.Invoke(success);
    }

    // برای بروزرسانی امتیاز بازیکن
    public void UpdatePlayerScore(string playerName, int points, System.Action<bool> callback = null)
    {
        StartCoroutine(FetchAndUpdateScore(playerName, points, callback));
    }

    IEnumerator FetchAndUpdateScore(string playerName, int points, System.Action<bool> callback)
    {
        // ابتدا بازیکن را پیدا کن
        string searchEndpoint = url + "/rest/v1/leaderboard?name=eq." + playerName;

        UnityWebRequest getReq = UnityWebRequest.Get(searchEndpoint);
        getReq.SetRequestHeader("apikey", apiKey);
        getReq.SetRequestHeader("Authorization", "Bearer " + apiKey);

        yield return getReq.SendWebRequest();

        if (getReq.result == UnityWebRequest.Result.Success)
        {
            string responseText = getReq.downloadHandler.text;
            
            // JSON array رو parse کن
            if (responseText.StartsWith("[") && responseText.EndsWith("]"))
            {
                responseText = responseText.Substring(1, responseText.Length - 2);
            }

            LeaderboardEntry entry = JsonUtility.FromJson<LeaderboardEntry>(responseText);
            
            int newScore = entry.score + points;

            // حالا score رو بروزرسانی کن
            string updateJson = "{\"score\":" + newScore + "}";

            UnityWebRequest updateReq = new UnityWebRequest(
                url + "/rest/v1/leaderboard?name=eq." + playerName, "PATCH");

            byte[] body = System.Text.Encoding.UTF8.GetBytes(updateJson);
            updateReq.uploadHandler = new UploadHandlerRaw(body);
            updateReq.downloadHandler = new DownloadHandlerBuffer();

            updateReq.SetRequestHeader("Content-Type", "application/json");
            updateReq.SetRequestHeader("apikey", apiKey);
            updateReq.SetRequestHeader("Authorization", "Bearer " + apiKey);

            yield return updateReq.SendWebRequest();

            callback?.Invoke(updateReq.result == UnityWebRequest.Result.Success);
        }
        else
        {
            Debug.Log("خطا در پیدا کردن بازیکن: " + getReq.error);
            callback?.Invoke(false);
        }
    }

    // برای دریافت لیدربورد
    public void GetLeaderboard(System.Action<List<LeaderboardEntry>> callback)
    {
        StartCoroutine(FetchLeaderboard(callback));
    }

    IEnumerator FetchLeaderboard(System.Action<List<LeaderboardEntry>> callback)
    {
        string endpoint = url + "/rest/v1/leaderboard?order=score.desc";

        UnityWebRequest req = UnityWebRequest.Get(endpoint);
        req.SetRequestHeader("apikey", apiKey);
        req.SetRequestHeader("Authorization", "Bearer " + apiKey);

        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            string json = req.downloadHandler.text;
            
            // JSON array رو handle کن
            if (!json.StartsWith("["))
                json = "[" + json + "]";

            // هر entry رو parse کن
            List<LeaderboardEntry> leaderboard = new List<LeaderboardEntry>();
            
            // سادہ JSON parsing (because JsonUtility doesn't support arrays directly)
            string[] entries = json.Split(new string[] { "},{" }, System.StringSplitOptions.None);
            
            foreach (string entry in entries)
            {
                string cleanEntry = entry.Replace("[", "").Replace("]", "").Replace("{", "").Replace("}", "");
                LeaderboardEntry le = JsonUtility.FromJson<LeaderboardEntry>("{" + cleanEntry + "}");
                leaderboard.Add(le);
            }

            callback?.Invoke(leaderboard);
        }
        else
        {
            Debug.Log("خطا در دریافت لیدربورد: " + req.error);
            callback?.Invoke(new List<LeaderboardEntry>());
        }
    }
}
