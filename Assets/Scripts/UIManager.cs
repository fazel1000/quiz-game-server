using UnityEngine;

public class UIManager : MonoBehaviour
{
    public GameObject mainBackground;
    public GameObject quizBackground;
    public QuizManager quizManager;

    public void OnOnlineClicked()
    {
        mainBackground.SetActive(false);
        quizBackground.SetActive(true);

        quizManager.StartQuiz();
    }
}