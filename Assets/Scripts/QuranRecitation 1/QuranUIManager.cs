using System.Collections;
using System.Threading.Tasks;
using RTLTMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuranUIManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject quranPanel;
    [SerializeField] private GameObject versesListPanel;
    [SerializeField] private GameObject versePanel;

    [Header("Surah List")]
    [SerializeField] private Transform surahContent;
    [SerializeField] private Button surahButtonPrefab;

    [Header("Verse List")]
    [SerializeField] private Transform verseContent;
    [SerializeField] private Button verseFramePrefab;

    [Header("Verse Panel")]
    [SerializeField] private RTLTextMeshPro verseText;
    [SerializeField] private RTLTextMeshPro verseTitle;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private Button playButton;

    [Header("Data")]
    [SerializeField] private QuranDatabase database;
    [SerializeField] private QuranJsonRepository quranRepository;

    [Header("Recording & Assessment")]
    [SerializeField] private QuranRecorder recorder;
    [SerializeField] private QuranAssessmentEngine assessmentEngine;
    [SerializeField] private RTLTextMeshPro percentText;

    [Header("Transition")]
    [SerializeField, Min(0.05f)] private float transitionDuration = 0.25f;

    private CanvasGroup mainGroup;
    private CanvasGroup quranGroup;
    private CanvasGroup versesGroup;
    private CanvasGroup verseGroup;

    private int currentSurahIndex = -1;
    private int currentVerseIndex = -1;
    private AudioClip currentVerseAudio;
    private bool isPlayingVerse;
    private bool isAssessing;

    private void Awake()
    {
        mainGroup = PrepareGroup(mainMenuPanel);
        quranGroup = PrepareGroup(quranPanel);
        versesGroup = PrepareGroup(versesListPanel);
        verseGroup = PrepareGroup(versePanel);

        ShowInstant(mainMenuPanel, mainGroup);
        HideInstant(quranPanel, quranGroup);
        HideInstant(versesListPanel, versesGroup);
        HideInstant(versePanel, verseGroup);

        if (quranRepository != null && !quranRepository.IsReady)
            quranRepository.Initialize();

        if (recorder != null)
            recorder.RecordingFinished += OnRecordingFinished;

        BuildSurahList();

        if (playButton != null)
        {
            playButton.onClick.RemoveAllListeners();
            playButton.onClick.AddListener(ReplayCurrentVerse);
        }
    }

    private void OnDestroy()
    {
        if (recorder != null)
            recorder.RecordingFinished -= OnRecordingFinished;
    }

    public void OpenQuranPanel()
    {
        StartCoroutine(
            SwitchPanel(
                mainMenuPanel,
                mainGroup,
                quranPanel,
                quranGroup));
    }

    public void BackToMainMenu()
    {
        StopAudio();

        StartCoroutine(
            SwitchPanel(
                quranPanel,
                quranGroup,
                mainMenuPanel,
                mainGroup));
    }

    public void BackToQuran()
    {
        StopAudio();

        StartCoroutine(
            SwitchPanel(
                versesListPanel,
                versesGroup,
                quranPanel,
                quranGroup));
    }

    public void BackToVerseList()
    {
        StopAudio();

        StartCoroutine(
            SwitchPanel(
                versePanel,
                verseGroup,
                versesListPanel,
                versesGroup));
    }

    private void BuildSurahList()
    {
        ClearChildren(surahContent);

        if (database == null ||
            database.surahs == null ||
            surahButtonPrefab == null ||
            surahContent == null)
        {
            return;
        }

        for (int i = 0; i < database.surahs.Length; i++)
        {
            if (database.surahs[i] == null)
                continue;

            int capturedIndex = i;

            Button button = Instantiate(
                surahButtonPrefab,
                surahContent);

            RTLTextMeshPro text =
                button.GetComponentInChildren<RTLTextMeshPro>(true);

            if (text != null)
                text.text = database.surahs[i].namePersian;

            button.onClick.RemoveAllListeners();

            button.onClick.AddListener(
                () => OpenSurah(capturedIndex));
        }
    }

    public void OpenSurah(int index)
    {
        if (database == null ||
            database.surahs == null ||
            index < 0 ||
            index >= database.surahs.Length)
        {
            return;
        }

        currentSurahIndex = index;
        currentVerseIndex = -1;

        BuildVerseList(database.surahs[index]);

        StartCoroutine(
            SwitchPanel(
                quranPanel,
                quranGroup,
                versesListPanel,
                versesGroup));
    }

    private void BuildVerseList(SurahData surah)
    {
        ClearChildren(verseContent);

        if (surah == null ||
            surah.verses == null ||
            verseFramePrefab == null ||
            verseContent == null)
        {
            return;
        }

        for (int i = 0; i < surah.verses.Length; i++)
        {
            if (surah.verses[i] == null)
                continue;

            int capturedIndex = i;
            VerseData verse = surah.verses[i];

            Button frame = Instantiate(
                verseFramePrefab,
                verseContent);

            RTLTextMeshPro text =
                frame.GetComponentInChildren<RTLTextMeshPro>(true);

            if (text != null)
            {
                text.text = GetVerseText(
                    surah.number,
                    verse.number);
            }

            frame.onClick.RemoveAllListeners();

            frame.onClick.AddListener(
                () => OpenVerse(
                    currentSurahIndex,
                    capturedIndex));
        }
    }

    public void OpenVerse(int surahIndex, int verseIndex)
    {
        if (database == null ||
            database.surahs == null ||
            surahIndex < 0 ||
            surahIndex >= database.surahs.Length)
        {
            return;
        }

        SurahData surah =
            database.surahs[surahIndex];

        if (surah == null ||
            surah.verses == null ||
            verseIndex < 0 ||
            verseIndex >= surah.verses.Length)
        {
            return;
        }

        VerseData verse =
            surah.verses[verseIndex];

        if (verse == null)
            return;

        currentSurahIndex = surahIndex;
        currentVerseIndex = verseIndex;
        currentVerseAudio = verse.audio;

        if (verseTitle != null)
        {
            verseTitle.text =
                surah.namePersian +
                " - آیه " +
                verse.number;
        }

        if (verseText != null)
        {
            verseText.text = GetVerseText(
                surah.number,
                verse.number);
        }

        ResetPercent();
        PlayVerseAudio(verse.audio);

        StartCoroutine(
            SwitchPanel(
                versesListPanel,
                versesGroup,
                versePanel,
                verseGroup));
    }

    private string GetVerseText(
        int surahNumber,
        int verseNumber)
    {
        if (quranRepository == null ||
            !quranRepository.IsReady)
        {
            return string.Empty;
        }

        return quranRepository.GetVerseText(
            surahNumber,
            verseNumber);
    }

    private void PlayVerseAudio(AudioClip clip)
    {
        if (audioSource == null)
            return;

        audioSource.Stop();
        audioSource.clip = clip;

        if (clip != null)
            audioSource.Play();
    }

    public void ReplayCurrentVerse()
    {
        if (isPlayingVerse ||
            currentVerseAudio == null ||
            audioSource == null)
        {
            return;
        }

        audioSource.Stop();
        audioSource.clip = currentVerseAudio;
        audioSource.Play();

        StartCoroutine(WaitForAudioFinish());
    }

    private IEnumerator WaitForAudioFinish()
    {
        isPlayingVerse = true;

        if (playButton != null)
            playButton.interactable = false;

        while (audioSource != null &&
               audioSource.isPlaying)
        {
            yield return null;
        }

        isPlayingVerse = false;

        if (playButton != null)
            playButton.interactable = true;
    }

    private void StopAudio()
    {
        if (audioSource != null)
            audioSource.Stop();
    }

    public void ToggleRecording()
    {
        if (recorder == null ||
            isAssessing)
        {
            return;
        }

        if (!recorder.IsRecording)
        {
            StopAudio();
            recorder.StartRecording();
            ResetPercent();
            return;
        }

        recorder.StopRecording();
    }

    private void OnRecordingFinished(AudioClip recording)
    {
        if (recording == null || isAssessing)
            return;

        _ = EvaluateRecordingAsync(recording);
    }

    private async Task EvaluateRecordingAsync(
        AudioClip recording)
    {
        if (recording == null ||
            assessmentEngine == null ||
            database == null ||
            database.surahs == null)
        {
            return;
        }

        if (currentSurahIndex < 0 ||
            currentSurahIndex >= database.surahs.Length)
        {
            return;
        }

        SurahData surah =
            database.surahs[currentSurahIndex];

        if (surah == null ||
            surah.verses == null ||
            currentVerseIndex < 0 ||
            currentVerseIndex >= surah.verses.Length)
        {
            return;
        }

        VerseData verse =
            surah.verses[currentVerseIndex];

        if (verse == null)
            return;

        isAssessing = true;
        ResetPercent();

        QuranAssessmentResult result =
            await assessmentEngine.AssessAsync(
                surah.number,
                verse.number,
                recording);

        isAssessing = false;

        if (!result.success)
        {
            Debug.LogWarning(
                "Quran assessment failed: " +
                result.error);

            SetPercent(0f);
            return;
        }

        SetPercent(result.score);

        Debug.Log(
            "Quran assessment | " +
            "Verse=" + surah.number + ":" + verse.number +
            " | Score=" + result.score.ToString("0.0") + "%" +
            " | Matches=" + result.matches +
            " | Sub=" + result.substitutions +
            " | Del=" + result.deletions +
            " | Ins=" + result.insertions);
    }

    private void SetPercent(float value)
    {
        if (percentText == null)
            return;

        percentText.text =
            Mathf.RoundToInt(
                Mathf.Clamp(value, 0f, 100f)) +
            "%";
    }

    private void ResetPercent()
    {
        SetPercent(0f);
    }

    private IEnumerator SwitchPanel(
        GameObject fromObject,
        CanvasGroup fromGroup,
        GameObject toObject,
        CanvasGroup toGroup)
    {
        if (fromObject == null ||
            toObject == null ||
            fromGroup == null ||
            toGroup == null)
        {
            yield break;
        }

        toObject.SetActive(true);

        toGroup.alpha = 0f;
        toGroup.interactable = false;
        toGroup.blocksRaycasts = false;

        float t = 0f;

        while (t < transitionDuration)
        {
            t += Time.unscaledDeltaTime;

            float p =
                Mathf.Clamp01(
                    t / transitionDuration);

            p = p * p * (3f - 2f * p);

            fromGroup.alpha = 1f - p;
            toGroup.alpha = p;

            yield return null;
        }

        fromGroup.alpha = 0f;
        fromObject.SetActive(false);

        toGroup.alpha = 1f;
        toGroup.interactable = true;
        toGroup.blocksRaycasts = true;
    }

    private CanvasGroup PrepareGroup(
        GameObject panel)
    {
        if (panel == null)
            return null;

        CanvasGroup group =
            panel.GetComponent<CanvasGroup>();

        if (group == null)
            group = panel.AddComponent<CanvasGroup>();

        return group;
    }

    private void ShowInstant(
        GameObject obj,
        CanvasGroup group)
    {
        if (obj == null ||
            group == null)
            return;

        obj.SetActive(true);
        group.alpha = 1f;
        group.interactable = true;
        group.blocksRaycasts = true;
    }

    private void HideInstant(
        GameObject obj,
        CanvasGroup group)
    {
        if (obj == null ||
            group == null)
            return;

        obj.SetActive(false);
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;
    }

    private void ClearChildren(
        Transform parent)
    {
        if (parent == null)
            return;

        for (int i = parent.childCount - 1;
             i >= 0;
             i--)
        {
            Destroy(parent.GetChild(i).gameObject);
        }
    }
}
