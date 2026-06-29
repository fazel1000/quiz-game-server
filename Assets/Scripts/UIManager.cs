using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.Video;
using DG.Tweening;
using CandyCoded.HapticFeedback;

public class UIManager : MonoBehaviour
{
    [Header("Panels")]
    public CanvasGroup mainMenu;
    public CanvasGroup page1;
    public CanvasGroup page2;
    public CanvasGroup page3;
    public CanvasGroup page4;

    [Header("Popup")]
    public CanvasGroup exitPopup;

    [Header("Fade Layer")]
    public CanvasGroup fadeLayer;

    [Header("Music")]
    public AudioSource musicSource;

    public AudioClip page1Music;
    public AudioClip page2Music;
    public AudioClip page3Music;
    public AudioClip page4Music;

    [Header("Button Sounds")]
    public AudioSource sfxSource;
    public AudioClip buttonClickSound;

    [Header("Background Video")]
    public VideoPlayer videoPlayer;

    public VideoClip mainMenuVideo;
    public VideoClip page1Video;
    public VideoClip page2Video;
    public VideoClip page3Video;
    public VideoClip page4Video;

    private CanvasGroup currentPanel;

    void Start()
    {
        Screen.orientation = ScreenOrientation.Portrait;

        HidePanel(mainMenu);
        HidePanel(page1);
        HidePanel(page2);
        HidePanel(page3);
        HidePanel(page4);
        HidePanel(exitPopup);

        ShowPanel(mainMenu);
        currentPanel = mainMenu;

        if (fadeLayer != null)
        {
            fadeLayer.alpha = 0;
            fadeLayer.interactable = false;
            fadeLayer.blocksRaycasts = false;
        }

        SetupButtonSounds();
        StopMusic();

        if (videoPlayer != null)
        {
            videoPlayer.playOnAwake = false;
            videoPlayer.isLooping = true;
            videoPlayer.skipOnDrop = false;
            videoPlayer.waitForFirstFrame = true;

            videoPlayer.prepareCompleted += OnVideoPrepared;
            videoPlayer.errorReceived += OnVideoError;
            videoPlayer.started += OnVideoStarted;
            videoPlayer.loopPointReached += OnVideoLoopReached;
        }

        PlayVideo(mainMenuVideo);
    }

    void OnDestroy()
    {
        if (videoPlayer != null)
        {
            videoPlayer.prepareCompleted -= OnVideoPrepared;
            videoPlayer.errorReceived -= OnVideoError;
            videoPlayer.started -= OnVideoStarted;
            videoPlayer.loopPointReached -= OnVideoLoopReached;
        }
    }

    void Update()
    {
        if (Keyboard.current != null &&
            Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (exitPopup != null && exitPopup.gameObject.activeSelf)
            {
                HideExitPopup();
                return;
            }

            if (currentPanel != mainMenu)
            {
                BackToMenu();
            }
            else
            {
                ShowExitPopup();
            }
        }
    }

    // ==================================
    // PANEL SYSTEM
    // ==================================

    void HidePanel(CanvasGroup panel)
    {
        if (panel == null) return;

        panel.alpha = 0;
        panel.interactable = false;
        panel.blocksRaycasts = false;
        panel.gameObject.SetActive(false);
    }

    void ShowPanel(CanvasGroup panel)
    {
        if (panel == null) return;

        panel.gameObject.SetActive(true);
        panel.alpha = 1;
        panel.interactable = true;
        panel.blocksRaycasts = true;
    }

    public void OpenPage(CanvasGroup target)
    {
        if (target == null) return;
        if (currentPanel == target) return;

        if (currentPanel != null)
        {
            CanvasGroup oldPanel = currentPanel;

            oldPanel.interactable = false;
            oldPanel.blocksRaycasts = false;

            oldPanel.DOFade(0, 0.25f)
                .OnComplete(() =>
                {
                    oldPanel.gameObject.SetActive(false);
                });
        }

        target.gameObject.SetActive(true);
        target.alpha = 0;
        target.transform.localScale = Vector3.one * 0.9f;

        target.DOFade(1, 0.3f);
        target.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack);

        target.interactable = true;
        target.blocksRaycasts = true;

        currentPanel = target;

        HapticFeedback.LightFeedback();

        if (target == page1)
            PlayMusic(page1Music);
        else if (target == page2)
            PlayMusic(page2Music);
        else if (target == page3)
            PlayMusic(page3Music);
        else if (target == page4)
            PlayMusic(page4Music);
        else
            StopMusic();

        if (target == page1)
            PlayVideo(page1Video);
        else if (target == page2)
            PlayVideo(page2Video);
        else if (target == page3)
            PlayVideo(page3Video);
        else if (target == page4)
            PlayVideo(page4Video);
        else
            PlayVideo(mainMenuVideo);
    }

    public void BackToMenu()
    {
        OpenPage(mainMenu);
    }

    // ==================================
    // VIDEO SYSTEM
    // ==================================

    void PlayVideo(VideoClip clip)
    {
        if (videoPlayer == null)
        {
            Debug.LogError("VideoPlayer is NULL");
            return;
        }

        if (clip == null)
        {
            Debug.LogError("VideoClip is NULL");
            return;
        }

        Debug.Log("Preparing Video: " + clip.name);

        videoPlayer.Stop();
        videoPlayer.clip = clip;
        videoPlayer.isLooping = true;

        videoPlayer.Prepare();
    }

    void OnVideoPrepared(VideoPlayer vp)
    {
        Debug.Log("VIDEO PREPARED: " + vp.clip.name);

        vp.Play();

        Debug.Log("IS PLAYING: " + vp.isPlaying);
        Debug.Log("FRAME COUNT: " + vp.frameCount);
        Debug.Log("WIDTH: " + vp.width);
        Debug.Log("HEIGHT: " + vp.height);
    }

    void OnVideoStarted(VideoPlayer vp)
    {
        Debug.Log("VIDEO STARTED: " + vp.clip.name);
    }

    void OnVideoLoopReached(VideoPlayer vp)
    {
        Debug.Log("VIDEO LOOP REACHED");
    }

    void OnVideoError(VideoPlayer vp, string message)
    {
        Debug.LogError("VIDEO ERROR: " + message);
    }

    // ==================================
    // MUSIC
    // ==================================

    void PlayMusic(AudioClip clip)
    {
        if (musicSource == null) return;
        if (clip == null) return;

        musicSource.Stop();
        musicSource.clip = clip;
        musicSource.Play();
    }

    void StopMusic()
    {
        if (musicSource == null) return;

        musicSource.Stop();
        musicSource.clip = null;
    }

    // ==================================
    // BUTTON SOUNDS
    // ==================================

    void SetupButtonSounds()
    {
        Button[] buttons = FindObjectsByType<Button>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        foreach (Button btn in buttons)
        {
            btn.onClick.AddListener(PlayButtonSound);
        }
    }

    public void PlayButtonSound()
    {
        if (sfxSource == null) return;
        if (buttonClickSound == null) return;

        sfxSource.PlayOneShot(buttonClickSound);
    }

    // ==================================
    // EXIT POPUP
    // ==================================

    public void ShowExitPopup()
    {
        if (exitPopup == null) return;

        HapticFeedback.LightFeedback();

        exitPopup.gameObject.SetActive(true);
        exitPopup.alpha = 0;
        exitPopup.interactable = true;
        exitPopup.blocksRaycasts = true;
        exitPopup.transform.localScale = Vector3.one * 0.8f;

        exitPopup.DOFade(1, 0.25f);
        exitPopup.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack);
    }

    public void HideExitPopup()
    {
        if (exitPopup == null) return;

        exitPopup.interactable = false;
        exitPopup.blocksRaycasts = false;

        exitPopup.DOFade(0, 0.2f)
            .OnComplete(() =>
            {
                exitPopup.gameObject.SetActive(false);
            });
    }

    public void ExitApp()
    {
        HapticFeedback.LightFeedback();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ==================================
    // BUTTON ANIMATION
    // ==================================

    public void ButtonPress(GameObject button)
    {
        HapticFeedback.LightFeedback();

        if (button == null) return;

        button.transform.DOKill();
        button.transform.DOScale(0.85f, 0.12f)
            .SetEase(Ease.OutCubic);
    }

    public void ButtonRelease(GameObject button)
    {
        if (button == null) return;

        button.transform.DOKill();
        button.transform.DOScale(1f, 0.15f)
            .SetEase(Ease.OutBack);
    }
}