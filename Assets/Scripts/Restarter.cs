using System;
using UnityEngine;
using System.Diagnostics;
using System.Globalization;
using TMPro;
using UnityEngine.Serialization;
using Debug = UnityEngine.Debug;

public class Restarter : MonoBehaviour
{
	[FormerlySerializedAs("restartKey")] [SerializeField] private KeyCode _restartKey = KeyCode.R;
	[SerializeField] private TMP_Text _currentTimeText, _restartTimeText;

	private void Start()
	{
		_restartTimeText.text = $"{ConfigHandler.RestartHour}:{ConfigHandler.RestartMinute}:{ConfigHandler.RestartSecond}";
	}

	private void Update()
	{
		if (Input.GetKeyDown(_restartKey)) RestartApp();
		DateTime now = DateTime.Now;
		_currentTimeText.text = now.ToString(CultureInfo.InvariantCulture);
		//Debug.Log($"{now.Hour} -> {_restartHour} : {now.Minute} -> {_restartMinute} : {now.Second} -> {_restartSecond}");
		if (ConfigHandler.DebugEnabled)
		{
			if (now.Minute % ConfigHandler.RestartMinute == 0 && now.Second == ConfigHandler.RestartSecond) RestartApp();
		}
		else
		{
			if (ConfigHandler.RestartEveryXHour)
			{
				if (now.Hour % ConfigHandler.RestartHour == 0 && now.Minute == ConfigHandler.RestartMinute && now.Second == ConfigHandler.RestartSecond) RestartApp();
			}
			else
			{
				if (now.Hour == ConfigHandler.RestartHour && now.Minute == ConfigHandler.RestartMinute && now.Second == ConfigHandler.RestartSecond) RestartApp();
			}
		}
	}
	private void RestartApp()
	{
		//Debug.Log("Restarting App");
		// Replace with your actual application path
		string applicationPath = ConfigHandler.ApplicationPath;

		Process.Start(applicationPath);
		Application.Quit();
	}
}