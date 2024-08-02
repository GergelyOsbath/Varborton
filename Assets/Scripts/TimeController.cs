using TMPro;
using UnityEngine;

public class TimeController : MonoBehaviour
{
	public bool isAccelerated = false;
	public float accelerationFactor = 10;
	public TMP_Text runningTimeText;
	public KeyCode accelerationKey = KeyCode.Space; // Replace with your desired key

	void Update()
	{
		if (Input.GetKeyDown(accelerationKey))
		{
			isAccelerated = !isAccelerated;
		}

		if (isAccelerated)
		{
			 Time.timeScale = accelerationFactor;
		}
		else
		{
			Time.timeScale = 1;
		}

		// Display actual running time
		int minutes = Mathf.FloorToInt(Time.time / 60);
		int seconds = Mathf.FloorToInt(Time.time % 60);
		runningTimeText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
	}
}