using UnityEngine;

[DefaultExecutionOrder(-10000)]
public class FPSLimit : MonoBehaviour
{
    private void Awake()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 25;
    }
}