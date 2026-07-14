using UnityEngine;
using RTLTMPro;

public class UIManager : MonoBehaviour
{
    [Header("Pages")]
    public GameObject mainBackground;
    public GameObject quizBackground;

    [Header("Managers")]
    public PlayerManager playerManager;
    public QuizManager quizManager;

    [Header("Optional UI")]
    public RTLTextMeshPro opponentNameText;

    void Start()
    {
        mainBackground.SetActive(true);
        quizBackground.SetActive(false);
    }

    public void OnConfirmClicked()
    {
        playerManager.OnConfirmNameClicked();
    }

    public void OnOnlineClicked()
    {
        playerManager.StartFindMatch(OnMatchFound);
    }

    void OnMatchFound(int matchId, string opponentName)
    {
        mainBackground.SetActive(false);
        quizBackground.SetActive(true);

        if (opponentNameText != null)
        {
            opponentNameText.text = "حریف: " + opponentName;
        }

        quizManager.StartQuiz(matchId);
    }
}