using Lean.Touch;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class CharacterSelectionProxy : MonoBehaviour
{
    [SerializeField] private float minDragThreshold = default;
    [SerializeField] private float holdTime = default;

    public UnityEvent OnCharacterSelected;

    public UnityEvent<LeanFinger> OnCharacterActivated;
    public UnityEvent OnCharacterDeactivated;

    private LeanFinger selectingFinger;
    private Vector2 selectionScreenPos;
    private float selectionTime;

    private void Awake()
    {
        this.enabled = false;
    }

    public void Select(LeanFinger finger)
    {
        selectingFinger = finger;
        selectionScreenPos = finger.ScreenPosition;
        selectionTime = Time.time;

        this.enabled = true;
    }

    public void Deselect()
    {
        if(this.enabled)
        {
            OnCharacterSelected.Invoke();
            this.enabled = false;
        }
        else
        {
            OnCharacterDeactivated.Invoke();
        }
    }

    private void Update()
    {
        if(Time.time - selectionTime > holdTime || 
            (selectingFinger.ScreenPosition - selectionScreenPos).sqrMagnitude > minDragThreshold * minDragThreshold)
        {
            OnCharacterActivated.Invoke(selectingFinger);
            this.enabled = false;
        }
    }
}
