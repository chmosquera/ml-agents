#if UNITY_IOS
using UnityEngine;
using System.Runtime.InteropServices;

public class iPhoneSpeaker : MonoBehaviour 
{
    [DllImport("__Internal")]
    private static extern void _forceToSpeaker();

    [DllImport("__Internal")]
    private static extern void _forceToEarpiece();
	
    public static void ForceToSpeaker() 
    {
        if (Application.platform == RuntimePlatform.IPhonePlayer) 
        {
            _forceToSpeaker();
        }   
    }

    public static void ForceToEarpiece()
    {
        if (Application.platform == RuntimePlatform.IPhonePlayer) 
        {
            _forceToEarpiece();
        }   
    }
}
#endif