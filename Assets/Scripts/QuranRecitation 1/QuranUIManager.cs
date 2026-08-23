using System;
using System.Collections;
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

    [Header("Data")]
    [SerializeField] private QuranDatabase database;

    [Header("Recording")]
    [SerializeField] private QuranRecorder recorder;
    [SerializeField] private PronunciationScore pronunciationScore;
    [SerializeField] private RTLTextMeshPro percentText;

    [Header("Transition")]
    [SerializeField, Min(0.05f)]
    private float transitionDuration = 0.25f;

    private CanvasGroup mainGroup;
    private CanvasGroup quranGroup;
    private CanvasGroup versesGroup;
    private CanvasGroup verseGroup;

    private int currentSurahIndex = -1;
    private int currentVerseIndex = -1;

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

        BuildSurahList();
    }

    // =========================
    // PANELS
    // =========================

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

    // =========================
    // SURAH LIST
    // =========================

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
            int capturedIndex = i;

            Button button =
                Instantiate(
                    surahButtonPrefab,
                    surahContent);

            RTLTextMeshPro text =
                button.GetComponentInChildren<RTLTextMeshPro>(true);

            if (text != null)
            {
                text.text = database.surahs[i].namePersian;
            }

            button.onClick.RemoveAllListeners();

            button.onClick.AddListener(
                () => OpenSurah(capturedIndex));
        }
    }

    public void OpenSurah(int index)
    {
        if (database == null ||
            database.surahs == null)
            return;

        if (index < 0 ||
            index >= database.surahs.Length)
            return;

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

    // =========================
    // VERSE LIST
    // =========================

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
            int capturedIndex = i;

            Button frame =
                Instantiate(
                    verseFramePrefab,
                    verseContent);

            RTLTextMeshPro text =
                frame.GetComponentInChildren<RTLTextMeshPro>(true);

            if (text != null)
            {
                text.text =
                    surah.verses[i].arabicText;
            }

            frame.onClick.RemoveAllListeners();

            frame.onClick.AddListener(
                () => OpenVerse(
                    currentSurahIndex,
                    capturedIndex));
        }
    }

    // =========================
    // VERSE PANEL
    // =========================

    public void OpenVerse(
        int surahIndex,
        int verseIndex)
    {
        if (database == null ||
            database.surahs == null)
            return;

        if (surahIndex < 0 ||
            surahIndex >= database.surahs.Length)
            return;

        SurahData surah =
            database.surahs[surahIndex];

        if (surah.verses == null ||
            verseIndex < 0 ||
            verseIndex >= surah.verses.Length)
            return;

        VerseData verse =
            surah.verses[verseIndex];

        currentSurahIndex = surahIndex;
        currentVerseIndex = verseIndex;

        if (verseTitle != null)
        {
            verseTitle.text =
                $"{surah.namePersian} - آیه {verse.number}";
        }

        if (verseText != null)
        {
            verseText.text =
                verse.arabicText;
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

    private void PlayVerseAudio(AudioClip clip)
    {
        if (audioSource == null)
            return;

        audioSource.Stop();
        audioSource.clip = clip;

        if (clip != null)
            audioSource.Play();
    }

    private void StopAudio()
    {
        if (audioSource != null)
            audioSource.Stop();
    }

    // =========================
    // RECORDING
    // =========================

    public void ToggleRecording()
    {
        if (recorder == null)
            return;

        if (!recorder.IsRecording)
        {
            StopAudio();
            recorder.StartRecording();

            ResetPercent();
        }
        else
        {
            AudioClip recording =
                recorder.StopRecording();

            EvaluateRecording(recording);
        }
    }

    private void EvaluateRecording(
        AudioClip recording)
    {
        if (recording == null)
            return;

        if (pronunciationScore == null)
            return;

        if (currentSurahIndex < 0 ||
            currentVerseIndex < 0)
            return;

        VerseData verse =
            database.surahs[currentSurahIndex]
                .verses[currentVerseIndex];

        // فعلاً چون سیستم تشخیص گفتار عربی نداریم،
        // این بخش بعداً به Speech Recognition متصل می‌شود.
        //
        // recognizedText باید متن تشخیص داده‌شده
        // از صدای کودک باشد.

        string recognizedText = "";

        float score =
            pronunciationScore.Compare(
                verse.arabicText,
                recognizedText);

        SetPercent(score);
    }

    private void SetPercent(float value)
    {
        if (percentText == null)
            return;

        percentText.text =
            $"{Mathf.RoundToInt(value)}%";
    }

    private void ResetPercent()
    {
        SetPercent(0f);
    }

    // =========================
    // PANEL TRANSITION
    // =========================

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

            p =
                p * p * (3f - 2f * p);

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

    // =========================
    // HELPERS
    // =========================

    private CanvasGroup PrepareGroup(
        GameObject panel)
    {
        if (panel == null)
            return null;

        CanvasGroup group =
            panel.GetComponent<CanvasGroup>();

        if (group == null)
            group =
                panel.AddComponent<CanvasGroup>();

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
            Destroy(
                parent.GetChild(i).gameObject);
        }
    }
}