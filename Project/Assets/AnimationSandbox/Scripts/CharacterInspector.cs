using Edwon.Tools;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;

public class CharacterInspector : MonoBehaviour
{
    [SerializeField] private Toggle togglePrefab = default;
    [SerializeField] private ToggleGroup toggleGroupPrefab = default;
    [SerializeField] private RectTransform slideInOutTransform = default;
    [SerializeField] private float autoCloseDuration = 5f;
    [SerializeField] private Button closeButton = default;
    [SerializeField] private RectTransform instructionsText = default;
    [SerializeField] private GameEvent characterSelectedEvent = default;
    [SerializeField] private GameEvent characterDeselectedEvent = default;
    [SerializeField] private GameEvent[] hideEvents = default;

    private List<Toggle> childToggles;
    private Dictionary<string, ToggleGroup> childToggleGroups;
    private List<CharacterFeatureToggle> inspectedCharacterFeatures;

    private Tween currentTween;

    private float openTime;
    private GameObject inspectedCharacterRoot;

    private void Start()
    {
        inspectedCharacterFeatures = new List<CharacterFeatureToggle>();
        childToggles = new List<Toggle>();
        childToggleGroups = new Dictionary<string, ToggleGroup>();

        characterSelectedEvent.RegisterListenerGameObject(OnCharacterSelected);
        for(int i = 0; i < hideEvents.Length; ++i)
        {
            if (hideEvents[i].parameterType == GameEvent.ParameterType.Bool)
            {
                hideEvents[i].RegisterListenerBool(OnHideEvent);
            }
            else
            {
                hideEvents[i].RegisterListener(OnHideEvent);
            }
        }
        closeButton.onClick.AddListener(OnHideEvent);
        closeButton.onClick.AddListener(() => characterDeselectedEvent.Raise());
        PlaytimeController.onPlayStart += OnHideEvent;

        enabled = false;
        slideInOutTransform.localScale = Vector3.zero;
    }

    private void OnDestroy()
    {
        characterSelectedEvent.UnregisterListenerGameObject(OnCharacterSelected);
        for(int i = 0; i < hideEvents.Length; ++i)
        {
            hideEvents[i].UnregisterListener(OnHideEvent);
        }
        closeButton.onClick.RemoveAllListeners();
        PlaytimeController.onPlayStart -= OnHideEvent;
    }

    private void OnCharacterSelected(GameObject characterRoot)
    {
        inspectedCharacterRoot = characterRoot;

        for(int i = 0; i < childToggles.Count; ++i)
        {
            childToggles[i].group = null;
            childToggles[i].onValueChanged.RemoveAllListeners();
            childToggles[i].SetIsOnWithoutNotify(false);
        }

        inspectedCharacterFeatures.Clear();

        foreach(ICharacterFeatureToggleContainer toggleContainer in characterRoot.GetComponentsInChildren<ICharacterFeatureToggleContainer>())
        {
            toggleContainer.GetCharacterFeatureToggles(inspectedCharacterFeatures);
        }

        for(int i = 0; i < inspectedCharacterFeatures.Count; ++i)
        {
            CharacterFeatureToggle feature = inspectedCharacterFeatures[i];
            if(i >= childToggles.Count)
            {
                childToggles.Add(Instantiate<Toggle>(togglePrefab, transform));
            }
            Toggle toggle = childToggles[i];
            Text text = toggle.GetComponentInChildren<Text>();
            if (text != null) text.text = feature.displayName;

            toggle.isOn = !feature.IsOn;
            toggle.isOn = feature.IsOn;

            toggle.onValueChanged.AddListener(feature.SetValue);
            toggle.onValueChanged.AddListener(OnChildToggleInteracted);

            if(string.IsNullOrWhiteSpace(feature.groupName))
            {
                toggle.group = null;
                toggle.transform.SetAsFirstSibling();
            }
            else
            {
                if(!childToggleGroups.TryGetValue(feature.groupName, out ToggleGroup group))
                {
                    group = Instantiate<ToggleGroup>(toggleGroupPrefab, transform);
                    Text groupText = group.GetComponentInChildren<Text>();
                    if (groupText != null) groupText.text = feature.groupName;
                    childToggleGroups.Add(feature.groupName, group);
                }

                group.RegisterToggle(toggle);
                toggle.group = group;
                toggle.transform.SetSiblingIndex(group.transform.GetSiblingIndex() + 1);
            }
        }

        instructionsText.SetAsLastSibling();
        closeButton.transform.SetAsLastSibling();
        Show();
    }

    private void OnHideEvent() => Close();
    private void OnHideEvent(bool _) => Close();

    private void OnChildToggleInteracted(bool value)
    {
        openTime = Time.time;
    }

    private void Update()
    {
        if(Time.time - openTime > autoCloseDuration)
        {
            characterDeselectedEvent.Raise();
            Close();
        }
    }

    private void Show()
    {
        openTime = Time.time;

        if (enabled) return;
        this.enabled = true;

        if(currentTween != null)
        {
            currentTween.Kill();
        }

        currentTween = slideInOutTransform.DOScale(1f, 0.2f)
            .OnComplete(() => currentTween = null);
    }
    
    public void Close()
    {
        if (!enabled) return;
        this.enabled = false;

        if(currentTween != null)
        {
            currentTween.Kill();
        }

        currentTween = slideInOutTransform.DOScale(0f, 0.2f)
            .OnComplete(() => currentTween = null);
    }
}
