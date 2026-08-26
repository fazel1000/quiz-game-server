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
    [SerializeField] private GameObject settingsPanel;

    [Header("Surah List")]
    [SerializeField] private Transform surahContent;
    [SerializeField] private Button surahButtonPrefab;

    [Header("Verse List")]
    [SerializeField] private Transform verseContent;
    [SerializeField] private Button verseFramePrefab;
    [SerializeField] private bool autoResizeVerseItems = true;
    [Tooltip("Height added to the Verse Item background for every wrapped line after the first line.")]
    [SerializeField, Min(0f)] private float verseItemExtraHeightPerLine = 65f;

    [Header("Arabic Verse Text")]
    [Tooltip("Removes characters that the selected TMP font and its fallbacks cannot display, so missing glyph squares are not shown.")]
    [SerializeField] private bool hideUnsupportedVerseCharacters = true;

    [Header("Progress, Stars & Locks")]
    [Tooltip("Shows earned stars / required stars for the current surah, for example 12/28.")]
    [SerializeField] private global::RTLTMPro.RTLTextMeshPro surahProgressText;

    [Tooltip("Assign exactly 6 sprites in order: 0Stars, 1Star, 2Stars, 3Stars, 4Stars, 5Stars.")]
    [SerializeField] private Sprite[] verseStarSprites = new Sprite[6];

    [SerializeField] private Sprite lockSprite;
    [Tooltip("Single filled star shown beside the surah progress counter.")]
    [SerializeField] private Sprite progressFilledStarSprite;
    [SerializeField] private Vector2 verseStarsSize = new Vector2(230f, 50f);
    [SerializeField] private Vector2 verseStarsOffset = new Vector2(14f, -10f);

    [global::UnityEngine.Header("Surah Lock Layout")]
    [global::UnityEngine.SerializeField]
    private Vector2 surahLockSize = new Vector2(82f, 82f);
    [global::UnityEngine.SerializeField]
    private Vector2 surahLockOffset = Vector2.zero;

    [global::UnityEngine.Header("Verse Lock Layout")]
    [global::UnityEngine.Serialization.FormerlySerializedAs("lockSize")]
    [global::UnityEngine.SerializeField]
    private Vector2 verseLockSize = new Vector2(82f, 82f);
    [global::UnityEngine.Serialization.FormerlySerializedAs("lockOffset")]
    [global::UnityEngine.SerializeField]
    private Vector2 verseLockOffset = Vector2.zero;

    [SerializeField] private Vector2 progressStarSize = new Vector2(48f, 48f);
    [SerializeField] private Vector2 progressStarOffset = new Vector2(-8f, 0f);

    [Header("Verse Panel")]
    [SerializeField] private global::RTLTMPro.RTLTextMeshPro verseText;
    [SerializeField] private global::RTLTMPro.RTLTextMeshPro verseTitle;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private Button playButton;

    [Header("Data")]
    [SerializeField] private QuranDatabase database;
    [SerializeField] private QuranJsonRepository quranRepository;

    [Header("Recording & Assessment")]
    [SerializeField] private QuranRecorder recorder;
    [SerializeField] private QuranAssessmentEngine assessmentEngine;
    [SerializeField] private global::RTLTMPro.RTLTextMeshPro percentText;
    [Tooltip("Shown above the Record button until the speech recognizer is ready.")]
    [SerializeField] private global::RTLTMPro.RTLTextMeshPro recognizerLoadingText;
    [Tooltip("Image object containing the Loading text. It is hidden when the recognizer becomes ready.")]
    [SerializeField] private Image recognizerLoadingBackgroundImage;

    [Header("Settings Panel")]
    [SerializeField] private Button exitButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button settingsBackButton;
    [SerializeField] private Toggle hapticToggle;
    [SerializeField] private Toggle buttonSoundToggle;
    [SerializeField] private Toggle backgroundMusicToggle;

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

    [Header("Background Music")]
    [SerializeField] private AudioSource backgroundMusicAudioSource;
    [SerializeField] private AudioClip mainMenuBackgroundMusic;
    [global::UnityEngine.Tooltip("Played in both the Surah List and Verses List panels.")]
    [global::UnityEngine.SerializeField] private AudioClip quranBrowserBackgroundMusic;

    [global::UnityEngine.Header("Transition")]
    [global::UnityEngine.SerializeField, global::UnityEngine.Min(0.05f)]
    private float transitionDuration = 0.25f;

    private CanvasGroup mainGroup;
    private CanvasGroup quranGroup;
    private CanvasGroup versesGroup;
    private CanvasGroup verseGroup;
    private CanvasGroup settingsGroup;

    private int currentSurahIndex = -1;
    private int currentVerseIndex = -1;
    private AudioClip currentVerseAudio;
    private bool isPlayingVerse;
    private bool isAssessing;
    private Image progressStarImage;
    private Sprite defaultRecordButtonSprite;
    private Button recordButton;
    private bool hapticFeedbackEnabled = true;
    private bool buttonSoundsEnabled = true;
    private bool backgroundMusicEnabled = true;

    private const string HapticEnabledKey =
        "Quran.Settings.HapticEnabled";
    private const string ButtonSoundEnabledKey =
        "Quran.Settings.ButtonSoundEnabled";
    private const string BackgroundMusicEnabledKey =
        "Quran.Settings.BackgroundMusicEnabled";

    public bool HapticFeedbackEnabled
    {
        get { return hapticFeedbackEnabled; }
    }

    private void Awake()
    {
        Instance = this;
        LoadSavedSettings();

        mainGroup = PrepareGroup(mainMenuPanel);
        quranGroup = PrepareGroup(quranPanel);
        versesGroup = PrepareGroup(versesListPanel);
        verseGroup = PrepareGroup(versePanel);
        settingsGroup = PrepareGroup(settingsPanel);

        if (recordButtonImage != null)
        {
            defaultRecordButtonSprite = recordButtonImage.sprite;
            recordButton =
                recordButtonImage.GetComponentInParent<Button>();
        }

        SetRecordingButtonVisual(false);
        SetRecognizerReadyState(
            assessmentEngine != null && assessmentEngine.IsReady);
        StartCoroutine(WaitForRecognizerReady());

        ShowInstant(mainMenuPanel, mainGroup);
        HideInstant(quranPanel, quranGroup);
        HideInstant(versesListPanel, versesGroup);
        HideInstant(versePanel, verseGroup);
        HideInstant(settingsPanel, settingsGroup);

        BindSettingsControls();
        PlayBackgroundMusic(mainMenuBackgroundMusic);

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
        UnbindSettingsControls();

        if (recorder != null)
            recorder.RecordingFinished -= OnRecordingFinished;

        if (Instance == this)
            Instance = null;
    }

    public void PlayButtonSound(
        QuranButtonFeedback.ButtonSoundType soundType)
    {
        if (!buttonSoundsEnabled ||
            sharedButtonAudioSource == null)
        {
            return;
        }

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

    private void LoadSavedSettings()
    {
        hapticFeedbackEnabled =
            PlayerPrefs.GetInt(HapticEnabledKey, 1) == 1;

        buttonSoundsEnabled =
            PlayerPrefs.GetInt(ButtonSoundEnabledKey, 1) == 1;

        backgroundMusicEnabled =
            PlayerPrefs.GetInt(BackgroundMusicEnabledKey, 1) == 1;
    }

    private void BindSettingsControls()
    {
        if (exitButton != null)
            exitButton.onClick.AddListener(ExitGame);

        if (settingsButton != null)
            settingsButton.onClick.AddListener(OpenSettingsPanel);

        if (settingsBackButton != null)
            settingsBackButton.onClick.AddListener(CloseSettingsPanel);

        if (hapticToggle != null)
        {
            hapticToggle.SetIsOnWithoutNotify(
                hapticFeedbackEnabled);
            hapticToggle.onValueChanged.AddListener(
                SetHapticFeedbackEnabled);
        }

        if (buttonSoundToggle != null)
        {
            buttonSoundToggle.SetIsOnWithoutNotify(
                buttonSoundsEnabled);
            buttonSoundToggle.onValueChanged.AddListener(
                SetButtonSoundsEnabled);
        }

        if (backgroundMusicToggle != null)
        {
            backgroundMusicToggle.SetIsOnWithoutNotify(
                backgroundMusicEnabled);
            backgroundMusicToggle.onValueChanged.AddListener(
                SetBackgroundMusicEnabled);
        }
    }

    private void UnbindSettingsControls()
    {
        if (exitButton != null)
            exitButton.onClick.RemoveListener(ExitGame);

        if (settingsButton != null)
            settingsButton.onClick.RemoveListener(OpenSettingsPanel);

        if (settingsBackButton != null)
            settingsBackButton.onClick.RemoveListener(CloseSettingsPanel);

        if (hapticToggle != null)
        {
            hapticToggle.onValueChanged.RemoveListener(
                SetHapticFeedbackEnabled);
        }

        if (buttonSoundToggle != null)
        {
            buttonSoundToggle.onValueChanged.RemoveListener(
                SetButtonSoundsEnabled);
        }

        if (backgroundMusicToggle != null)
        {
            backgroundMusicToggle.onValueChanged.RemoveListener(
                SetBackgroundMusicEnabled);
        }
    }

    public void SetHapticFeedbackEnabled(bool enabled)
    {
        hapticFeedbackEnabled = enabled;
        PlayerPrefs.SetInt(
            HapticEnabledKey,
            enabled ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void SetButtonSoundsEnabled(bool enabled)
    {
        buttonSoundsEnabled = enabled;
        PlayerPrefs.SetInt(
            ButtonSoundEnabledKey,
            enabled ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void SetBackgroundMusicEnabled(bool enabled)
    {
        backgroundMusicEnabled = enabled;
        PlayerPrefs.SetInt(
            BackgroundMusicEnabledKey,
            enabled ? 1 : 0);
        PlayerPrefs.Save();

        if (enabled)
            RefreshBackgroundMusicForCurrentPanel();
        else if (backgroundMusicAudioSource != null)
            backgroundMusicAudioSource.Stop();
    }

    public void OpenSettingsPanel()
    {
        PlayBackgroundMusic(mainMenuBackgroundMusic);

        StartCoroutine(
            SwitchPanel(
                mainMenuPanel,
                mainGroup,
                settingsPanel,
                settingsGroup));
    }

    public void CloseSettingsPanel()
    {
        PlayBackgroundMusic(mainMenuBackgroundMusic);

        StartCoroutine(
            SwitchPanel(
                settingsPanel,
                settingsGroup,
                mainMenuPanel,
                mainGroup));
    }

    public void ExitGame()
    {
        PlayerPrefs.Save();

#if UNITY_EDITOR
        global::UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void PlayBackgroundMusic(AudioClip clip)
    {
        if (backgroundMusicAudioSource == null)
            return;

        backgroundMusicAudioSource.loop = true;

        if (!backgroundMusicEnabled || clip == null)
        {
            backgroundMusicAudioSource.Stop();
            return;
        }

        if (backgroundMusicAudioSource.clip == clip &&
            backgroundMusicAudioSource.isPlaying)
        {
            return;
        }

        backgroundMusicAudioSource.Stop();
        backgroundMusicAudioSource.clip = clip;
        backgroundMusicAudioSource.Play();
    }

    private void RefreshBackgroundMusicForCurrentPanel()
    {
        if (versePanel != null && versePanel.activeSelf)
        {
            PlayBackgroundMusic(null);
            return;
        }

        if ((quranPanel != null && quranPanel.activeSelf) ||
            (versesListPanel != null && versesListPanel.activeSelf))
        {
            PlayBackgroundMusic(quranBrowserBackgroundMusic);
            return;
        }

        PlayBackgroundMusic(mainMenuBackgroundMusic);
    }

    public void OpenQuranPanel()
    {
        BuildSurahList();
        PlayBackgroundMusic(quranBrowserBackgroundMusic);

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
        PlayBackgroundMusic(mainMenuBackgroundMusic);

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
        PlayBackgroundMusic(quranBrowserBackgroundMusic);

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
        PlayBackgroundMusic(quranBrowserBackgroundMusic);

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

            global::RTLTMPro.RTLTextMeshPro text =
                button.GetComponentInChildren<
                    global::RTLTMPro.RTLTextMeshPro>(true);

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
                CreateLockImage(
                    button.transform,
                    surahLockOffset,
                    surahLockSize);
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
        PlayBackgroundMusic(quranBrowserBackgroundMusic);

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

            global::RTLTMPro.RTLTextMeshPro text =
                frame.GetComponentInChildren<
                    global::RTLTMPro.RTLTextMeshPro>(true);

            if (text != null)
            {
                text.text = PrepareVerseTextForFont(
                    GetVerseText(
                        surah.number,
                        verse.number),
                    text);
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
                CreateLockImage(
                    frame.transform,
                    verseLockOffset,
                    verseLockSize);
            }
        }

        if (autoResizeVerseItems)
            StartCoroutine(ResizeVerseItemsAfterLayout());
    }

    private global::System.Collections.IEnumerator
        ResizeVerseItemsAfterLayout()
    {
        yield return null;

        if (verseContent == null ||
            verseFramePrefab == null)
        {
            yield break;
        }

        Canvas.ForceUpdateCanvases();

        RectTransform prefabRect =
            verseFramePrefab.transform as RectTransform;

        float baseHeight = prefabRect != null
            ? Mathf.Max(
                prefabRect.rect.height,
                prefabRect.sizeDelta.y)
            : 0f;

        global::RTLTMPro.RTLTextMeshPro prefabText =
            verseFramePrefab.GetComponentInChildren<
                global::RTLTMPro.RTLTextMeshPro>(true);

        float baseTextHeight = prefabText != null
            ? Mathf.Max(
                prefabText.rectTransform.rect.height,
                prefabText.rectTransform.sizeDelta.y)
            : 0f;

        for (int i = 0;
             i < verseContent.childCount;
             i++)
        {
            Transform itemTransform =
                verseContent.GetChild(i);

            Button frame =
                itemTransform.GetComponent<Button>();

            global::RTLTMPro.RTLTextMeshPro text =
                itemTransform.GetComponentInChildren<
                    global::RTLTMPro.RTLTextMeshPro>(true);

            RectTransform frameRect =
                itemTransform as RectTransform;

            if (frame == null ||
                text == null ||
                frameRect == null)
            {
                continue;
            }

            global::UnityEngine.UI.LayoutRebuilder
                .ForceRebuildLayoutImmediate(text.rectTransform);

            text.ForceMeshUpdate();

            int lineCount = Mathf.Max(
                1,
                text.textInfo.lineCount);

            float originalHeight = baseHeight > 0f
                ? baseHeight
                : Mathf.Max(
                    frameRect.rect.height,
                    frameRect.sizeDelta.y);

            float extraHeight =
                (lineCount - 1) *
                verseItemExtraHeightPerLine;

            float targetHeight =
                originalHeight + extraHeight;

            LayoutElement layoutElement =
                frame.GetComponent<LayoutElement>();

            if (layoutElement == null)
                layoutElement = frame.gameObject.AddComponent<LayoutElement>();

            layoutElement.minHeight = targetHeight;
            layoutElement.preferredHeight = targetHeight;

            frameRect.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Vertical,
                targetHeight);

            RectTransform textRect = text.rectTransform;

            if (extraHeight > 0f &&
                Mathf.Approximately(
                    textRect.anchorMin.y,
                    textRect.anchorMax.y))
            {
                textRect.SetSizeWithCurrentAnchors(
                    RectTransform.Axis.Vertical,
                    (baseTextHeight > 0f
                        ? baseTextHeight
                        : textRect.rect.height) +
                    extraHeight);
            }
        }

        RectTransform contentRect =
            verseContent as RectTransform;

        if (contentRect != null)
        {
            global::UnityEngine.UI.LayoutRebuilder
                .ForceRebuildLayoutImmediate(contentRect);
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
            verseText.text = PrepareVerseTextForFont(
                GetVerseText(
                    surah.number,
                    verse.number),
                verseText);
        }

        ResetPercent();
        PlayBackgroundMusic(null);
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

        return quranRepository.GetVerseDisplayText(
            surahNumber,
            verseNumber);
    }

    private string PrepareVerseTextForFont(
        string value,
        global::RTLTMPro.RTLTextMeshPro targetText)
    {
        if (!hideUnsupportedVerseCharacters ||
            string.IsNullOrEmpty(value) ||
            targetText == null ||
            targetText.font == null)
        {
            return value;
        }

        global::TMPro.TMP_FontAsset fontAsset = targetText.font;
        global::System.Text.StringBuilder filtered =
            new global::System.Text.StringBuilder(value.Length);

        foreach (char character in value)
        {
            if (IsTextSpacingOrDirectionCharacter(character) ||
                fontAsset.HasCharacter(
                    character,
                    true,
                    true))
            {
                filtered.Append(character);
                continue;
            }

            // Preserve the base letter when Alef Wasla is unsupported.
            if (character == '\u0671' &&
                fontAsset.HasCharacter(
                    '\u0627',
                    true,
                    true))
            {
                filtered.Append('\u0627');
                continue;
            }

            // The Imlaei display text already contains every normal Alef
            // in its correct place. If the font lacks superscript Alef,
            // remove only that unsupported mark (and an optional Tatweel).
            if (character == '\u0670')
            {
                if (filtered.Length > 0 &&
                    filtered[filtered.Length - 1] == '\u0640')
                {
                    filtered.Length--;
                }
            }
        }

        return filtered.ToString();
    }

    private static bool IsTextSpacingOrDirectionCharacter(
        char character)
    {
        return char.IsWhiteSpace(character) ||
               character == '\u200C' ||
               character == '\u200D' ||
               character == '\u200E' ||
               character == '\u200F';
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

    private global::System.Collections.IEnumerator WaitForAudioFinish()
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

    private global::System.Collections.IEnumerator WaitForRecognizerReady()
    {
        while (assessmentEngine != null &&
               !assessmentEngine.IsReady)
        {
            yield return null;
        }

        SetRecognizerReadyState(
            assessmentEngine != null && assessmentEngine.IsReady);
    }

    private void SetRecognizerReadyState(bool isReady)
    {
        if (recordButton != null)
            recordButton.interactable = isReady;

        if (recognizerLoadingBackgroundImage != null)
        {
            recognizerLoadingBackgroundImage.gameObject.SetActive(
                !isReady);
        }

        if (recognizerLoadingText != null)
        {
            recognizerLoadingText.gameObject.SetActive(!isReady);
        }
    }

    public void ToggleRecording()
    {
        if (recorder == null ||
            assessmentEngine == null ||
            !assessmentEngine.IsReady ||
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

    private async global::System.Threading.Tasks.Task EvaluateRecordingAsync(
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

    private void CreateLockImage(
        Transform parent,
        Vector2 offset,
        Vector2 size)
    {
        if (parent == null || lockSprite == null)
            return;

        CreateRuntimeImage(
            parent,
            "Lock",
            lockSprite,
            new Vector2(0.5f, 0.5f),
            offset,
            size);
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

    private global::System.Collections.IEnumerator SwitchPanel(
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