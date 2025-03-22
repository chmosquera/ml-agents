using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CharacterFeatureToggle 
{
    public string displayName;
    public string groupName;

    public bool IsOn
    {
        get => _isOn;
        set => SetValue(value);
    }

    public void SetValue(bool value)
    {
        if (_isOn == value) return;
        _isOn = value;
        ValueChanged?.Invoke(_isOn);
    }

    public event System.Action<bool> ValueChanged;

    private bool _isOn;
}

public interface ICharacterFeatureToggleContainer
{
    void GetCharacterFeatureToggles(List<CharacterFeatureToggle> togglesBuffer);
}
