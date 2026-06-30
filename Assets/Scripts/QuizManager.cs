using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;

public class QuizManager : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI questionText;
    public TextMeshProUGUI[] optionTexts;

    [Header("Supabase")]
    public string supabaseUrl = "https://tjdfrhuwekdlrokkzamm.supabase.co/rest/v1/questions?select=*";
    public string apiKey = "YOUR_ANON_KEY";

    private List<Question> questions = new List<Question>();
    private int currentIndex = 0;

    public void StartQuiz()
    {
        StartCoroutine(LoadQuestions());
    }

    IEnumerator LoadQuestions()
    {
        UnityWebRequest request = UnityWebRequest.Get(supabaseUrl);

        request.SetRequestHeader("apikey", apiKey);
        request.SetRequestHeader("Authorization", "Bearer " + apiKey);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string json = request.downloadHandler.text;

            string wrappedJson = "{ \"items\": " + json + "}";

            QuestionListWrapper wrapper = JsonUtility.FromJson<QuestionListWrapper>(wrappedJson);

            questions = new List<Question>(wrapper.items);

            Debug.Log("Loaded Questions: " + questions.Count);

            currentIndex = 0;
            ShowQuestion();
        }
        else
        {
            Debug.LogError(request.error);
        }
    }

    void ShowQuestion()
    {
        if (questions == null || questions.Count == 0)
        {
            questionText.text = "No Questions Found!";
            return;
        }

        if (currentIndex >= questions.Count)
        {
            questionText.text = "Quiz Finished!";
            return;
        }

        Question q = questions[currentIndex];

        questionText.text = q.question;

        optionTexts[0].text = q.option_a;
        optionTexts[1].text = q.option_b;
        optionTexts[2].text = q.option_c;
        optionTexts[3].text = q.option_d;
    }

    // 🟢 اینو مستقیم از Button صدا می‌زنی
    public void AnswerA()
    {
        CheckAnswer(optionTexts[0].text);
    }

    public void AnswerB()
    {
        CheckAnswer(optionTexts[1].text);
    }

    public void AnswerC()
    {
        CheckAnswer(optionTexts[2].text);
    }

    public void AnswerD()
    {
        CheckAnswer(optionTexts[3].text);
    }

    void CheckAnswer(string selected)
    {
        if (currentIndex >= questions.Count) return;

        Question q = questions[currentIndex];

        if (selected == q.answer)
        {
            currentIndex++;
            ShowQuestion();
        }
        else
        {
            Debug.Log("Wrong Answer!");
        }
    }
}