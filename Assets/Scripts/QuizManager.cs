using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using RTLTMPro;

public class QuizManager : MonoBehaviour
{
    [Header("UI")]
    public RTLTextMeshPro questionText;
    public RTLTextMeshPro[] optionTexts;
    public RTLTextMeshPro scoreText;

    [Header("Server")]
    public string serverBaseUrl = "https://fazel1000.pythonanywhere.com";

    private List<Question> questions = new List<Question>();
    private int currentIndex = 0;
    private int playerScore = 0;
    private int currentMatchId = 0;

    public void StartQuiz(int matchId)
    {
        currentMatchId = matchId;
        currentIndex = 0;
        playerScore = 0;

        UpdateScoreText();

        StartCoroutine(LoadQuestions());
    }

    IEnumerator LoadQuestions()
    {
        string url = serverBaseUrl + "/match_questions/" + currentMatchId;

        UnityWebRequest request = UnityWebRequest.Get(url);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string json = request.downloadHandler.text;

            Question[] loadedQuestions = JsonHelper.FromJson<Question>(json);
            questions = new List<Question>(loadedQuestions);

            Debug.Log("Questions Loaded: " + questions.Count);

            ShowQuestion();
        }
        else
        {
            Debug.LogError("Server Error: " + request.error);
            questionText.text = "خطا در اتصال به سرور";
        }
    }

    void ShowQuestion()
    {
        if (questions == null || questions.Count == 0)
        {
            questionText.text = "سوالی پیدا نشد";
            return;
        }

        if (currentIndex >= questions.Count)
        {
            questionText.text = "پایان مسابقه\nامتیاز شما: " + playerScore;

            optionTexts[0].text = "";
            optionTexts[1].text = "";
            optionTexts[2].text = "";
            optionTexts[3].text = "";

            return;
        }

        Question q = questions[currentIndex];

        questionText.text = q.question;

        optionTexts[0].text = q.A;
        optionTexts[1].text = q.B;
        optionTexts[2].text = q.C;
        optionTexts[3].text = q.D;
    }

    public void AnswerA()
    {
        CheckAnswer("A");
    }

    public void AnswerB()
    {
        CheckAnswer("B");
    }

    public void AnswerC()
    {
        CheckAnswer("C");
    }

    public void AnswerD()
    {
        CheckAnswer("D");
    }

    void CheckAnswer(string selectedAnswer)
    {
        if (currentIndex >= questions.Count)
            return;

        Question q = questions[currentIndex];

        if (selectedAnswer == q.correct)
        {
            playerScore += q.score;
            Debug.Log("Correct +" + q.score);
        }
        else
        {
            playerScore -= 10;
            Debug.Log("Wrong -10");
        }

        currentIndex++;
        UpdateScoreText();
        ShowQuestion();
    }

    void UpdateScoreText()
    {
        if (scoreText != null)
        {
            scoreText.text = "امتیاز: " + playerScore;
        }
    }
}