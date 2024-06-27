using System;
using System.Globalization;
using System.IO;
using UnityEngine;

[DefaultExecutionOrder(-10000)]
public class Config : MonoBehaviour
{
    public static bool DebugEnabled { get; private set; } = false;
    public static float Rotation { get; private set; } = 20.0f;
    public static float RotationSpeed { get; private set; } = 90.0f;
    public static float Movement { get; private set; } = 0.1f;
    public static float MovementSpeed { get; private set; } = 0.05f;
    public static float SecondsToRemovePerson { get; private set; } = 1.5f;
    public static float MinRotationAngle { get; private set; } = 1.5f;
    public static float MinMovementDistance { get; private set; } = 0.01f;
    public static float LockTime { get; private set; } = 0.5f;
    
    public static bool StatisticsEnabled { get; private set; } = false;

    private void Awake()
    {
        string path = Path.Combine(Application.streamingAssetsPath, "config.ini");
        if (!File.Exists(path))
        {
            Debug.Log($"Config file missing!");
            return;
        }
        string[] fileContent = File.ReadAllLines(path);


        if (fileContent.Length < 6)
        {
            Debug.Log($"Config file bad!");
            return;
        }

        DebugEnabled = fileContent[0].Contains("true", StringComparison.InvariantCultureIgnoreCase);
        Rotation = float.Parse(fileContent[1].Split('=')[1].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture);
        RotationSpeed = float.Parse(fileContent[2].Split('=')[1].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture);
        Movement = float.Parse(fileContent[3].Split('=')[1].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture);
        MovementSpeed = float.Parse(fileContent[4].Split('=')[1].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture);
        SecondsToRemovePerson = float.Parse(fileContent[5].Split('=')[1].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture);
        MinRotationAngle = float.Parse(fileContent[6].Split('=')[1].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture);
        MinMovementDistance = float.Parse(fileContent[7].Split('=')[1].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture);
        LockTime = float.Parse(fileContent[8].Split('=')[1].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture);
        StatisticsEnabled = fileContent[9].Contains("true", StringComparison.InvariantCultureIgnoreCase);
    }
}