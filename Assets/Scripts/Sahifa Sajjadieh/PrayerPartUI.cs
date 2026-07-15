using System;
using System.Collections;
using RTLTMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(LayoutElement))]
[RequireComponent(typeof(VerticalLayoutGroup))]
[RequireComponent(typeof(Button))]
public class PrayerPartUI : MonoBehaviour
{
    [Header("Texts")]
    public RTLTextMeshPro arabicText;
    public RTLTextMeshPro persianText;

    [Header("Background")]
    public Image backgroundImage;
    public Color normalColor = Color.white;
    public Color selectedColor = new Color(1f, 0.85f, 0.55f, 1f);

    [Header("Audio Manager")]
    public PrayerAudioManager audioManager;

    [Header("Arabic Audio Segment")]
    public AudioClip arabicClip;
    public float arabicStartTime;
    public float arabicEndTime;

    [Header("Persian Audio Segment")]
    public AudioClip persianClip;
    public float persianStartTime;
    public float persianEndTime;

    public event Action<PrayerPartUI> Clicked;

    private LayoutElement itemLayout;
    private LayoutElement arabicLayout;
    private LayoutElement persianLayout;

    private VerticalLayoutGroup layoutGroup;
    private RectTransform itemRect;
    private Button button;

    private Coroutine refreshCoroutine;

    private static PrayerPartUI selectedItem;

    private void Awake()
    {
        itemRect = GetComponent<RectTransform>();
        itemLayout = GetComponent<LayoutElement>();
        layoutGroup = GetComponent<VerticalLayoutGroup>();
        button = GetComponent<Button>();

        arabicLayout = GetOrAddLayoutElement(arabicText.gameObject);
        persianLayout = GetOrAddLayoutElement(persianText.gameObject);

        button.onClick.AddListener(OnItemClicked);

        if (backgroundImage != null)
            backgroundImage.color = normalColor;
    }

    public void SetTexts(string arabic, string persian)
    {
        arabicText.text = arabic;
        persianText.text = persian;

        RefreshLayout();
    }

    public void SetAudioManager(PrayerAudioManager manager)
    {
        audioManager = manager;
    }

    public void RefreshLayout()
    {
        if (refreshCoroutine != null)
            StopCoroutine(refreshCoroutine);

        refreshCoroutine = StartCoroutine(RefreshSize());
    }

    private void OnItemClicked()
    {
        SelectAndPlay();
        Clicked?.Invoke(this);
    }

    public void SelectAndPlay()
    {
        if (selectedItem != null && selectedItem != this)
            selectedItem.SetSelected(false);

        selectedItem = this;
        SetSelected(true);

        if (audioManager == null)
            return;

        // فعلاً صوت فارسی در اولویت است.
        if (persianClip != null && persianEndTime > persianStartTime)
        {
            audioManager.PlayPersianSegment(
                persianClip,
                persianStartTime,
                persianEndTime
            );

            return;
        }

        if (arabicClip != null && arabicEndTime > arabicStartTime)
        {
            audioManager.PlayArabicSegment(
                arabicClip,
                arabicStartTime,
                arabicEndTime
            );
        }
    }

    public void SetSelected(bool isSelected)
    {
        if (backgroundImage == null)
            return;

        backgroundImage.color =
            isSelected ? selectedColor : normalColor;
    }

    private IEnumerator RefreshSize()
    {
        yield return null;

        Canvas.ForceUpdateCanvases();

        arabicText.ForceMeshUpdate();
        persianText.ForceMeshUpdate();

        float availableWidth =
            itemRect.rect.width -
            layoutGroup.padding.left -
            layoutGroup.padding.right;

        availableWidth = Mathf.Max(1f, availableWidth);

        float arabicHeight = arabicText.GetPreferredValues(
            arabicText.text,
            availableWidth,
            0f
        ).y;

        float persianHeight = persianText.GetPreferredValues(
            persianText.text,
            availableWidth,
            0f
        ).y;

        arabicLayout.preferredHeight = Mathf.Ceil(arabicHeight);
        persianLayout.preferredHeight = Mathf.Ceil(persianHeight);

        itemLayout.preferredHeight =
            layoutGroup.padding.top +
            arabicLayout.preferredHeight +
            layoutGroup.spacing +
            persianLayout.preferredHeight +
            layoutGroup.padding.bottom;

        LayoutRebuilder.ForceRebuildLayoutImmediate(itemRect);

        if (transform.parent is RectTransform parentRect)
            LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);

        refreshCoroutine = null;
    }

    private LayoutElement GetOrAddLayoutElement(GameObject target)
    {
        LayoutElement element = target.GetComponent<LayoutElement>();

        if (element == null)
            element = target.AddComponent<LayoutElement>();

        return element;
    }

    private void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(OnItemClicked);

        if (selectedItem == this)
            selectedItem = null;
    }
}