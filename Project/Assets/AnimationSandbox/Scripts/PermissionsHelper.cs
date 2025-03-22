using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif

public class PermissionsHelper : MonoBehaviour
{
    void Start()
    {
#if UNITY_IOS
        if(!Application.HasUserAuthorization(UserAuthorization.Microphone))
        {
            Application.RequestUserAuthorization(UserAuthorization.Microphone);
        }
#elif UNITY_ANDROID
        if(!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            Permission.RequestUserPermission(Permission.Microphone);
        }
#endif
    }
}
