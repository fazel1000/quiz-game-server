using CandyCoded.HapticFeedback;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public sealed class QuranButtonFeedback : MonoBehaviour,
    IPointerDownHandler,
    IPointerUpHandler,
    IPointerExitHandler
{
    public enum HapticStrength
    {
        Off,
        Light,
        Medium,
        Heavy
    }

    public enum ButtonSoundType
    {
        None = 0,
        Start = 1,
        ExitAndSettings = 2,
        Surah = 3,
        Verse = 4,
        Back = 5
    }

    [Header("Button")]
    [SerializeField] private Button button;
    [Tooltip("Object that becomes smaller. Leave empty to animate this button.")]
    [SerializeField] private RectTransform animationTarget;

    [Header("Press Animation")]
    [SerializeField, Range(0.75f, 1f)] private float pressedScale = 0.92f;
    [SerializeField, Min(0.01f)] private float pressDuration = 0.08f;
    [SerializeField, Min(0.01f)] private float releaseDuration = 0.12f;
    [SerializeField] private Ease pressEase = Ease.OutQuad;
    [SerializeField] private Ease releaseEase = Ease.OutBack;

    [Header("Android / iOS Haptic")]
    [SerializeField] private HapticStrength hapticStrength =
        HapticStrength.Light;

    [Header("Shared Button Sound")]
    [SerializeField] private ButtonSoundType soundType =
        ButtonSoundType.None;

    private Vector3 normalScale;
    private Tween scaleTween;
    private bool isPressed;

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (animationTarget == null)
            animationTarget = transform as RectTransform;

        if (animationTarget != null)
            normalScale = animationTarget.localScale;
    }

    private void OnEnable()
    {
        isPressed = false;

        if (animationTarget != null)
        {
            animationTarget.localScale = normalScale;
        }
    }

    private void OnDisable()
    {
        isPressed = false;

        if (scaleTween != null)
        {
            scaleTween.Kill();
            scaleTween = null;
        }

        if (animationTarget != null)
            animationTarget.localScale = normalScale;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!CanInteract())
            return;

        isPressed = true;

        AnimateScale(
            normalScale * pressedScale,
            pressDuration,
            pressEase);

        PlayHaptic();
        PlayButtonSound();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        ReleaseButton();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ReleaseButton();
    }

    private void ReleaseButton()
    {
        if (!isPressed)
            return;

        isPressed = false;

        AnimateScale(
            normalScale,
            releaseDuration,
            releaseEase);
    }

    private bool CanInteract()
    {
        return isActiveAndEnabled &&
               (button == null || button.IsInteractable());
    }

    private void AnimateScale(
        Vector3 targetScale,
        float duration,
        Ease ease)
    {
        if (animationTarget == null)
            return;

        if (scaleTween != null)
            scaleTween.Kill();

        scaleTween = animationTarget
            .DOScale(targetScale, duration)
            .SetEase(ease)
            .SetUpdate(true);
    }

    private void PlayHaptic()
    {
#if UNITY_ANDROID || UNITY_IOS
        switch (hapticStrength)
        {
            case HapticStrength.Light:
                HapticFeedback.LightFeedback();
                break;

            case HapticStrength.Medium:
                HapticFeedback.MediumFeedback();
                break;

            case HapticStrength.Heavy:
                HapticFeedback.HeavyFeedback();
                break;
        }
#endif
    }

    private void PlayButtonSound()
    {
        if (soundType == ButtonSoundType.None ||
            QuranUIManager.Instance == null)
        {
            return;
        }

        QuranUIManager.Instance.PlayButtonSound(soundType);
    }
}