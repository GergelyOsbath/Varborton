using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugModeHandler : MonoBehaviour
{
    private void Awake()
    {
        if (!ConfigHandler.DebugEnabled)
        {
            Cursor.visible = false;
            gameObject.SetActive(false);
        }
    }
}
