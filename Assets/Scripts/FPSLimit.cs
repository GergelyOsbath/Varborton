using UnityEngine;

[DefaultExecutionOrder(-10000)]
public class FPSLimit : MonoBehaviour
{
    private void Awake() => Application.targetFrameRate = 25;
}