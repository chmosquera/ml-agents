using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class PlaytimeClipView : MonoBehaviour, IPointerDownHandler, IPointerClickHandler, IDragHandler, IBeginDragHandler, IEndDragHandler, IInitializePotentialDragHandler
{
    public RectTransform RectTransform => transform as RectTransform;

    public event System.Action<PlaytimeClipView> OnPointerClick;

    void IEndDragHandler.OnEndDrag(PointerEventData eventData) { }

    void IBeginDragHandler.OnBeginDrag(PointerEventData eventData) { }

    void IDragHandler.OnDrag(PointerEventData eventData) { }

    void IInitializePotentialDragHandler.OnInitializePotentialDrag(PointerEventData eventData) { }

    void IPointerClickHandler.OnPointerClick(PointerEventData eventData)
    {
        OnPointerClick?.Invoke(this);
    }

    void IPointerDownHandler.OnPointerDown(PointerEventData eventData) { }
}
