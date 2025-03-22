using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ToggleSwitch : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private Graphic isOnGraphic;
    [SerializeField] private Graphic isOffGraphic;
    [SerializeField] private bool isOn;

    public UnityEvent<bool> onValueChanged;
    public UnityEvent onSwitchedOn;
    public UnityEvent onSwitchedOff;

    public bool IsOn => isOn;

    private void Start()
    {
        UpdateVisuals();
    }

    public void SetValue(bool value)
    {
        if (isOn == value) return;

        isOn = value;
        UpdateVisuals();

        onValueChanged.Invoke(isOn);

        if (isOn) onSwitchedOn.Invoke();
        else onSwitchedOff.Invoke();
    }

    public void SetValueWithoutNotify(bool value)
    {
        if (isOn == value) return;

        isOn = value;
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        isOnGraphic.enabled = isOn;
        isOffGraphic.enabled = !isOn;
    }

    void IPointerClickHandler.OnPointerClick(PointerEventData eventData)
    {
        SetValue(!isOn);
    }
}
