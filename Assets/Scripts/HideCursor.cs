using UnityEngine;

[DefaultExecutionOrder(-1000)]
public class HideCursor : MonoBehaviour
{
    private void Awake() => Cursor.visible = false;
}