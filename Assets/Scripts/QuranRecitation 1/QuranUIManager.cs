using System.Collections;
using System.Threading.Tasks;
using RTLTMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuranUIManager : MonoBehaviour
{
    public static QuranUIManager Instance { get; private set; }

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

    [Header("Progress, Stars & Locks")]
    [Tooltip("Shows earned stars / required stars for the current surah, for example 12/28.")]
    [SerializeField] private RTLTextMeshPro surahProgressText;

    [Tooltip("Assign exactly 6 sprites in order: 0Stars, 1Star, 2Stars, 3Stars, 4Stars, 5Stars.")]
    [SerializeField] private Sprite[] verseStarSprites = new Sprite[6];

    [SerializeField] private Sprite lockSprite;
    [Tooltip("Single filled star shown beside the surah progress counter.")]
    [SerializeField] private Sprite progressFilledStarSprite;
    [SerializeField] private Vector2 verseStarsSize = new Vector2(230f, 50f);
    [SerializeField] private Vector2 verseStarsOffset = new Vector2(14f, -10f);
    [SerializeField] private Vector2 lockSize = new Vector2(82f, 82f);
    [SerializeField] private Vector2 progressStarSize = new Vector2(48f, 48f);
    [SerializeField] private Vector2 progressStarOffset = new Vector2(-8f, 0f);

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

    [Header("Record Button Visual")]
    [Tooltip("Image component on the Record button.")]
    [SerializeField] private Image recordButtonImage;
    [Tooltip("Sprite shown only while the microphone is recording.")]
    [SerializeField] private Sprite recordingButtonSprite;

    [Header("Shared Button Audio")]
    [Tooltip("One AudioSource shared by every button in this scene.")]
    [SerializeField] private AudioSource sharedButtonAudioSource;
    [SerializeField] private AudioClip startButtonSound;
    [SerializeField] private AudioClip exitAndSettingsButtonSound;
    [SerializeField] private AudioClip surahButtonSound;
    [SerializeField] private AudioClip verseButtonSound;
    [Tooltip("One shared sound for every Back button.")]
    [SerializeField] private AudioClip backButtonSound;

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
    private Image progressStarImage;
    private Sprite defaultRecordButtonSprite;

    private void Awake()
    {
        Instance = this;

        mainGroup = PrepareGroup(mainMenuPanel);
        quranGroup = PrepareGroup(quranPanel);
        versesGroup = PrepareGroup(versesListPanel);
        verseGroup = PrepareGroup(versePanel);

        if (recordButtonImage != null)
            defaultRecordButtonSprite = recordButtonImage.sprite;

        SetRecordingButtonVisual(false);

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

        if (Instance == this)
            Instance = null;
    }

    public void PlayButtonSound(
        QuranButtonFeedback.ButtonSoundType soundType)
    {
        if (sharedButtonAudioSource == null)
            return;

        AudioClip clip = null;

        switch (soundType)
        {
            case QuranButtonFeedback.ButtonSoundType.Start:
                clip = startButtonSound;
                break;

            case QuranButtonFeedback.ButtonSoundType.ExitAndSettings:
                clip = exitAndSettingsButtonSound;
                break;

            case QuranButtonFeedback.ButtonSoundType.Surah:
                clip = surahButtonSound;
                break;

            case QuranButtonFeedback.ButtonSoundType.Verse:
                clip = verseButtonSound;
                break;

            case QuranButtonFeedback.ButtonSoundType.Back:
                clip = backButtonSound;
                break;
        }

        if (clip != null)
            sharedButtonAudioSource.PlayOneShot(clip);
    }

    public void OpenQuranPanel()
    {
        BuildSurahList();

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
        BuildSurahList();

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

        if (database != null &&
            database.surahs != null &&
            currentSurahIndex >= 0 &&
            currentSurahIndex < database.surahs.Length)
        {
            BuildVerseList(
                database.surahs[currentSurahIndex]);
        }

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

            bool isUnlocked =
                QuranProgressManager.IsSurahUnlocked(
                    database,
                    capturedIndex);

            RTLTextMeshPro text =
                button.GetComponentInChildren<RTLTextMeshPro>(true);

            if (text != null)
                text.text = database.surahs[i].namePersian;

            button.onClick.RemoveAllListeners();
            button.interactable = isUnlocked;

            if (isUnlocked)
            {
                button.onClick.AddListener(
                    () => OpenSurah(capturedIndex));
            }
            else
            {
                CreateLockImage(button.transform);
            }
        }
    }

    public void OpenSurah(int index)
    {
        if (database == null ||
            database.surahs == null ||
            index < 0 ||
            index >= database.surahs.Length ||
            !QuranProgressManager.IsSurahUnlocked(
                database,
                index))
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

        UpdateSurahProgressText(surah);

        for (int i = 0; i < surah.verses.Length; i++)
        {
            if (surah.verses[i] == null)
                continue;

            int capturedIndex = i;
            VerseData verse = surah.verses[i];

            Button frame = Instantiate(
                verseFramePrefab,
                verseContent);

            int earnedStars =
                QuranProgressManager.GetBestStars(
                    surah.number,
                    verse.number);

            bool isUnlocked =
                QuranProgressManager.IsVerseUnlocked(
                    surah,
                    capturedIndex);

            RTLTextMeshPro text =
                frame.GetComponentInChildren<RTLTextMeshPro>(true);

            if (text != null)
            {
                text.text = GetVerseText(
                    surah.number,
                    verse.number);
            }

            frame.onClick.RemoveAllListeners();

            frame.interactable = isUnlocked;

            CreateVerseStarsImage(
                frame.transform,
                earnedStars);

            if (isUnlocked)
            {
                frame.onClick.AddListener(
                    () => OpenVerse(
                        currentSurahIndex,
                        capturedIndex));
            }
            else
            {
                CreateLockImage(frame.transform);
            }
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
            verseIndex >= surah.verses.Length ||
            !QuranProgressManager.IsVerseUnlocked(
                surah,
                verseIndex))
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

            if (recorder.IsRecording)
            {
                SetRecordingButtonVisual(true);
                ResetPercent();
            }

            return;
        }

        recorder.StopRecording();
        SetRecordingButtonVisual(false);
    }

    private void OnRecordingFinished(AudioClip recording)
    {
        SetRecordingButtonVisual(false);

        if (recording == null || isAssessing)
            return;

        _ = EvaluateRecordingAsync(recording);
    }

    private void SetRecordingButtonVisual(bool recording)
    {
        if (recordButtonImage == null)
            return;

        if (recording && recordingButtonSprite != null)
        {
            recordButtonImage.sprite = recordingButtonSprite;
            return;
        }

        recordButtonImage.sprite = defaultRecordButtonSprite;
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

        QuranProgressManager.SaveBestResult(
            surah.number,
            verse.number,
            result.score);

        Debug.Log(
            "Quran assessment | " +
            "Verse=" + surah.number + ":" + verse.number +
            " | Score=" + result.score.ToString("0.0") + "%" +
            " | Stars=" +
            QuranProgressManager.ScoreToStars(result.score) +
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

    private void UpdateSurahProgressText(SurahData surah)
    {
        if (surahProgressText == null || surah == null)
            return;

        int required =
            QuranProgressManager
                .GetRequiredStarsForNextSurah(surah);

        int earned =
            QuranProgressManager.GetEarnedStars(surah);

        surahProgressText.text =
            Mathf.Min(earned, required) +
            "/" +
            required;

        EnsureProgressStarImage();
    }

    private void EnsureProgressStarImage()
    {
        if (surahProgressText == null ||
            progressFilledStarSprite == null)
        {
            return;
        }

        if (progressStarImage == null)
        {
            Transform existing =
                surahProgressText.transform.Find(
                    "Progress Star");

            if (existing != null)
            {
                progressStarImage =
                    existing.GetComponent<Image>();
            }
        }

        if (progressStarImage == null)
        {
            GameObject imageObject =
                new GameObject(
                    "Progress Star",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));

            RectTransform rect =
                imageObject.GetComponent<RectTransform>();

            rect.SetParent(
                surahProgressText.transform,
                false);

            progressStarImage =
                imageObject.GetComponent<Image>();
        }

        RectTransform starRect =
            progressStarImage.rectTransform;

        starRect.anchorMin = new Vector2(0f, 0.5f);
        starRect.anchorMax = new Vector2(0f, 0.5f);
        starRect.pivot = new Vector2(1f, 0.5f);
        starRect.anchoredPosition = progressStarOffset;
        starRect.sizeDelta = progressStarSize;

        progressStarImage.sprite = progressFilledStarSprite;
        progressStarImage.preserveAspect = true;
        progressStarImage.raycastTarget = false;
    }

    private void CreateVerseStarsImage(
        Transform parent,
        int earnedStars)
    {
        if (parent == null ||
            verseStarSprites == null ||
            verseStarSprites.Length < 6)
        {
            return;
        }

        int spriteIndex = Mathf.Clamp(
            earnedStars,
            0,
            QuranProgressManager.StarsPerVerse);

        Sprite sprite = verseStarSprites[spriteIndex];

        if (sprite == null)
            return;

        CreateRuntimeImage(
            parent,
            "Verse Stars",
            sprite,
            new Vector2(0f, 1f),
            verseStarsOffset,
            verseStarsSize);
    }

    private void CreateLockImage(Transform parent)
    {
        if (parent == null || lockSprite == null)
            return;

        CreateRuntimeImage(
            parent,
            "Lock",
            lockSprite,
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            lockSize);
    }

    private static void CreateRuntimeImage(
        Transform parent,
        string objectName,
        Sprite sprite,
        Vector2 anchor,
        Vector2 offset,
        Vector2 size)
    {
        GameObject imageObject =
            new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));

        RectTransform rect =
            imageObject.GetComponent<RectTransform>();

        rect.SetParent(parent, false);
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = offset;
        rect.sizeDelta = size;

        Image image = imageObject.GetComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = false;

        imageObject.transform.SetAsLastSibling();
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