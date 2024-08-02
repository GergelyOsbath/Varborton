using System;
using System.Globalization;
using System.IO;
using UnityEngine;

[DefaultExecutionOrder(-10000)]
public class ConfigHandler : MonoBehaviour
{
    public static string[] FelonyTexts { get; private set; } = {"Bűn #1", "Bűn #2", "Bűn #3", "Bűn #4", "Bűn #5", "Bűn #6", "Bűn #7", "Bűn #8", "Bűn #9", "Bűn #10"};
    public static string ApplicationPath { get; private set; } = "C:/Unity Builds/VarbortonBuilds/Varborton_NDI/ndi-segmentation.exe";
    public static float TimeBetweenRandomPicks { get; private set; } = 0.1f;
    public static float RandomizationDuration { get; private set; } = 3.0f;
    public static float ResultOnScreenDuration { get; private set; } = 10.0f;
    public static float TrackLostGracePeriod { get; private set; } = 1.0f;
    public static float FaceDetectionMinWidth { get; private set; } = 0.1f;
    public static float FaceDetectionMinHeight { get; private set; } = 0.25f;
    public static float FaceDetectionCenterWidthMin { get; private set; } = 0.40f;
    public static float FaceDetectionCenterWidthMax { get; private set; } = 0.60f;
    public static float FaceDetectionCenterHeightMin { get; private set; } = 0.15f;
    public static float FaceDetectionCenterHeightMax { get; private set; } = 0.85f;
    public static float LevelChangeThreshold { get; private set; } = 0.1f;
    public static int RestartHour { get; private set; } = 4;
    public static int RestartMinute { get; private set; } = 30;
    public static int RestartSecond { get; private set; } = 30;
    public static bool DebugEnabled { get; private set; } = false;
    public static bool RestartEveryXHour { get; private set; } = false;

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

        FelonyTexts = fileContent[0].Split(';');
        TimeBetweenRandomPicks = float.Parse(fileContent[1].Split('=')[1].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture);
        RandomizationDuration = float.Parse(fileContent[2].Split('=')[1].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture);
        ResultOnScreenDuration = float.Parse(fileContent[3].Split('=')[1].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture);
        TrackLostGracePeriod = float.Parse(fileContent[4].Split('=')[1].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture);
        DebugEnabled = fileContent[5].Contains("true", StringComparison.InvariantCultureIgnoreCase);
        ApplicationPath = fileContent[6].Split('=')[1].Trim();
        RestartHour = int.Parse(fileContent[7].Split('=')[1].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture);
        RestartMinute = int.Parse(fileContent[8].Split('=')[1].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture);
        RestartSecond = int.Parse(fileContent[9].Split('=')[1].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture);
        FaceDetectionMinWidth = float.Parse(fileContent[10].Split('=')[1].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture);
        FaceDetectionMinHeight = float.Parse(fileContent[11].Split('=')[1].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture);
        FaceDetectionCenterWidthMin = float.Parse(fileContent[12].Split('=')[1].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture);
        FaceDetectionCenterWidthMax = float.Parse(fileContent[13].Split('=')[1].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture);
        FaceDetectionCenterHeightMin = float.Parse(fileContent[14].Split('=')[1].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture);
        FaceDetectionCenterHeightMax = float.Parse(fileContent[15].Split('=')[1].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture);
        RestartEveryXHour = fileContent[16].Contains("true", StringComparison.InvariantCultureIgnoreCase);
        LevelChangeThreshold = float.Parse(fileContent[17].Split('=')[1].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture);
    }
}