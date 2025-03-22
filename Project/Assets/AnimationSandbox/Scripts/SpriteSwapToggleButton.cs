using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class SpriteSwapToggleButton : MonoBehaviour
{
    [SerializeField] private Button button = default;
    [SerializeField] private Image image = default;
    [SerializeField] private Sprite onSprite = default;
    [SerializeField] private Sprite offSprite = default;

    public UnityEvent<bool> OnToggle;

    private bool isOn;

    private void Awake()
    {
        isOn = false;

        button.onClick.AddListener(OnButtonPressed);

        UpdateButtonSprite();
    }

    private void OnDestroy()
    {
        button.onClick.RemoveListener(OnButtonPressed);
    }

    public void SetIsOnWithoutNotify(bool isOn)
    {
        this.isOn = isOn;
        UpdateButtonSprite();
    }

    private void OnButtonPressed()
    {
        isOn = !isOn;
        UpdateButtonSprite();
        OnToggle?.Invoke(isOn);
    }

    private void UpdateButtonSprite()
    {
        image.sprite = isOn ? onSprite : offSprite;
    }
}
