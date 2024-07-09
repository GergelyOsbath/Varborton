using System;
using System.Globalization;
using System.IO;
using UnityEngine;

[DefaultExecutionOrder(-10000)]
public class ConfigHandler : MonoBehaviour
{
    public static string[] FelonyTexts { get; private set; } = {"Bűn #1", "Bűn #2", "Bűn #3", "Bűn #4", "Bűn #5", "Bűn #6", "Bűn #7", "Bűn #8", "Bűn #9", "Bűn #10"};
    public static float TimeBetweenRandomPicks { get; private set; } = 0.1f;
    public static float RandomizationDuration { get; private set; } = 3.0f;
    public static float ResultOnScreenDuration { get; private set; } = 10.0f;
    public static float TrackLostGracePeriod { get; private set; } = 1.0f;

    private void Awake()
    {
        string path = Path.Combine(Application.streamingAssetsPath, "config.ini");
        if (!File.Exists(path))
        {
            Debug.Log($"Config file missing!");
            return;
        }
        string[] fileContent = File.ReadAllLines(path);


        if (fileContent.Length < 4)
        {
            Debug.Log($"Config file bad!");
            return;
        }

        FelonyTexts = fileContent[0].Split(',');
        TimeBetweenRandomPicks = float.Parse(fileContent[1].Split('=')[1].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture);
        RandomizationDuration = float.Parse(fileContent[2].Split('=')[1].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture);
        ResultOnScreenDuration = float.Parse(fileContent[3].Split('=')[1].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture);
        TrackLostGracePeriod = float.Parse(fileContent[4].Split('=')[1].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture);
    }
}