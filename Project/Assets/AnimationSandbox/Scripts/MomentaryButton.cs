using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class MomentaryButton : MonoBehaviour, IPointerDownHandler, IPointerExitHandler, IPointerUpHandler
{
    public UnityEvent onPress;
    public UnityEvent onRelease;

    private bool isPressed;

    void IPointerDownHandler.OnPointerDown(PointerEventData eventData)
    {
        if (isPressed) return;

        isPressed = true;
        onPress.Invoke();
    }

    void IPointerExitHandler.OnPointerExit(PointerEventData eventData)
    {
        if(isPressed)
        {
            isPressed = false;
            onRelease.Invoke();
        }
    }

    void IPointerUpHandler.OnPointerUp(PointerEventData eventData)
    {
        if(isPressed)
        {
            isPressed = false;
            onRelease.Invoke();
        }
    }
}
