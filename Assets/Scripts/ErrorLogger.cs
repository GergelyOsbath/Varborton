using UnityEngine;
using System.IO;

public class ErrorLogger : MonoBehaviour
{
    private string logFilePath;

    private void Start()
    {
        logFilePath = Application.persistentDataPath + "/error.log";
        Application.logMessageReceived += HandleLog;
    }

    private void OnDestroy()
    {
        Application.logMessageReceived -= HandleLog;
    }

    private void HandleLog(string logString, string stackTrace, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception)
        {
            string logMessage = $"{System.DateTime.Now:yyyy-MM-dd HH:mm:ss} - {logString}\n{stackTrace}\n";
            File.AppendAllText(logFilePath, logMessage);
        }
    }
}
