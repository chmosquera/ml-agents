using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Utils
{
    public static string GetUniqueID()
    {
        string [] split = System.DateTime.Now.TimeOfDay.ToString().Split(new char [] {':','.'});
        string id = "";
        for(int i = 0; i < split.Length; i++) {
            id+=split[i];
        }
        return id;
    } 

}
