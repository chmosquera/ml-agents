using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class CharacterToggleComponent : MonoBehaviour, ICharacterFeatureToggleContainer
{
    public CharacterFeatureToggle feature;
    public bool setOnAtAwake = false;

    public UnityEvent<bool> onValueChanged;

    private void Awake()
    {
        feature.IsOn = setOnAtAwake;
        feature.ValueChanged += Feature_ValueChanged;
    }

    private void OnDestroy()
    {
        feature.ValueChanged -= Feature_ValueChanged;
    }

    private void Feature_ValueChanged(bool obj)
    {
        onValueChanged.Invoke(obj);
    }

    void ICharacterFeatureToggleContainer.GetCharacterFeatureToggles(List<CharacterFeatureToggle> togglesBuffer)
    {
        togglesBuffer.Add(feature);
    }
}
