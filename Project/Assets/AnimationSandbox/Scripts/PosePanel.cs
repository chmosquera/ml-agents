using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PosePanel : MonoBehaviour
{
    [SerializeField] private MomentaryButton buttonPrefab;
    [SerializeField] private Canvas canvas;

    private List<MomentaryButton> buttons = new List<MomentaryButton>();

    private void Start()
    {
        canvas.enabled = false;
    }

    public void SetCharacterTarget(ArmPoseAnimator character)
    {
        if(character == null)
        {
            for(int i = 0; i < buttons.Count; ++i)
            {
                buttons[i].onPress.RemoveAllListeners();
                buttons[i].onRelease.RemoveAllListeners();
            }
            canvas.enabled = false;
            return;
        }

        canvas.enabled = true;

        for(int i = 0; i < character.poses.Length; ++i)
        {
            MomentaryButton button;

            if(i >= buttons.Count)
            {
                buttons.Add(Instantiate<MomentaryButton>(buttonPrefab, transform));
            }

            button = buttons[i];
            button.gameObject.SetActive(true);

            int poseIndex = i;
            button.onPress.AddListener(() => character.SetPose(poseIndex));
            button.onRelease.AddListener(() => character.ReleasePose());

            if(button.gameObject.GetComponentInChildren<Text>() is Text text)
            {
                text.text = character.poses[i].displayName;
            }
        }
        for(int i = character.poses.Length; i < buttons.Count; ++i)
        {
            buttons[i].gameObject.SetActive(false);
        }
    }
}
