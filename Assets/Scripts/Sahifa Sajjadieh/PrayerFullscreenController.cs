using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PrayerFullscreenController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private RectTransform headerPanel;
    [SerializeField] private RectTransform playerPanel;
    [SerializeField] private RectTransform prayerScrollView;

    [Header("Fullscreen Exit Controls")]
    [SerializeField] private GameObject fullscreenControls;

    [Header("Animation")]
    [SerializeField] private float animationDuration = 0.2f;
    [SerializeField] private float hideExtraDistance = 20f;

    [Header("Fullscreen Scroll Padding")]
    [SerializeField] private float fullscreenTopPadding = 0f;
    [SerializeField] private float fullscreenBottomPadding = 0f;

    private Vector2 normalHeaderPosition;
    private Vector2 normalPlayerPosition;

    private Vector2 normalScrollOffsetMin;
    private Vector2 normalScrollOffsetMax;

    private bool isFullscreen;
    private Coroutine animationCoroutine;

    private void Start()
    {
        Canvas.ForceUpdateCanvases();

        normalHeaderPosition = headerPanel.anchoredPosition;
        normalPlayerPosition = playerPanel.anchoredPosition;

        normalScrollOffsetMin = prayerScrollView.offsetMin;
        normalScrollOffsetMax = prayerScrollView.offsetMax;

        if (fullscreenControls != null)
            fullscreenControls.SetActive(false);
    }

    public void ToggleFullscreen()
    {
        SetFullscreen(!isFullscreen);
    }

    public void ExitFullscreen()
    {
        SetFullscreen(false);
    }

    private void SetFullscreen(bool fullscreen)
    {
        if (animationCoroutine != null)
            StopCoroutine(animationCoroutine);

        animationCoroutine = StartCoroutine(
            AnimateFullscreen(fullscreen)
        );
    }

    private IEnumerator AnimateFullscreen(bool fullscreen)
    {
        Canvas.ForceUpdateCanvases();

        Vector2 startHeaderPosition =
            headerPanel.anchoredPosition;

        Vector2 startPlayerPosition =
            playerPanel.anchoredPosition;

        Vector2 startScrollOffsetMin =
            prayerScrollView.offsetMin;

        Vector2 startScrollOffsetMax =
            prayerScrollView.offsetMax;

        Vector2 hiddenHeaderPosition =
            normalHeaderPosition +
            Vector2.up *
            (headerPanel.rect.height + hideExtraDistance);

        Vector2 hiddenPlayerPosition =
            normalPlayerPosition +
            Vector2.down *
            (playerPanel.rect.height + hideExtraDistance);

        Vector2 fullscreenScrollOffsetMin =
            new Vector2(
                normalScrollOffsetMin.x,
                fullscreenBottomPadding
            );

        Vector2 fullscreenScrollOffsetMax =
            new Vector2(
                normalScrollOffsetMax.x,
                -fullscreenTopPadding
            );

        Vector2 targetHeaderPosition = fullscreen
            ? hiddenHeaderPosition
            : normalHeaderPosition;

        Vector2 targetPlayerPosition = fullscreen
            ? hiddenPlayerPosition
            : normalPlayerPosition;

        Vector2 targetScrollOffsetMin = fullscreen
            ? fullscreenScrollOffsetMin
            : normalScrollOffsetMin;

        Vector2 targetScrollOffsetMax = fullscreen
            ? fullscreenScrollOffsetMax
            : normalScrollOffsetMax;

        if (fullscreen && fullscreenControls != null)
            fullscreenControls.SetActive(true);

        float elapsedTime = 0f;

        while (elapsedTime < animationDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01(
                elapsedTime / animationDuration
            );

            float smoothProgress = Mathf.SmoothStep(
                0f,
                1f,
                progress
            );

            headerPanel.anchoredPosition = Vector2.Lerp(
                startHeaderPosition,
                targetHeaderPosition,
                smoothProgress
            );

            playerPanel.anchoredPosition = Vector2.Lerp(
                startPlayerPosition,
                targetPlayerPosition,
                smoothProgress
            );

            prayerScrollView.offsetMin = Vector2.Lerp(
                startScrollOffsetMin,
                targetScrollOffsetMin,
                smoothProgress
            );

            prayerScrollView.offsetMax = Vector2.Lerp(
                startScrollOffsetMax,
                targetScrollOffsetMax,
                smoothProgress
            );

            yield return null;
        }

        headerPanel.anchoredPosition =
            targetHeaderPosition;

        playerPanel.anchoredPosition =
            targetPlayerPosition;

        prayerScrollView.offsetMin =
            targetScrollOffsetMin;

        prayerScrollView.offsetMax =
            targetScrollOffsetMax;

        isFullscreen = fullscreen;

        if (!fullscreen && fullscreenControls != null)
            fullscreenControls.SetActive(false);

        LayoutRebuilder.ForceRebuildLayoutImmediate(
            prayerScrollView
        );

        animationCoroutine = null;
    }
}