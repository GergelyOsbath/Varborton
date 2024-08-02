using UnityEngine;
using UnityEngine.Video;
using System.Collections.Generic;
using System.IO;

public class VideoPlayerLoop : MonoBehaviour
{
	public VideoPlayer videoPlayer;
	private List<string> videoPaths;
	private int currentVideoIndex = 0;

	void Start()
	{
		// Initialize the video paths list
		videoPaths = new List<string>();

		// Get the path to the StreamingAssets folder
		string streamingAssetsPath = Application.streamingAssetsPath;

		// Load all video files from the folder
		LoadVideoFiles(streamingAssetsPath);

		// Check if we have any videos
		if (videoPaths.Count > 0)
		{
			// Start playing the first video
			PlayVideo(currentVideoIndex);
		}
	}

	void LoadVideoFiles(string path)
	{
		// Get all files in the folder
		string[] files = Directory.GetFiles(path);

		// Filter out only video files (you can add more extensions if needed)
		foreach (string file in files)
		{
			if (file.EndsWith(".mp4") || file.EndsWith(".mov") || file.EndsWith(".avi"))
			{
				videoPaths.Add(file);
			}
		}
	}

	void PlayVideo(int index)
	{
		videoPlayer.Stop();
		// Set the video clip
		videoPlayer.url = videoPaths[index];
		videoPlayer.isLooping = false;

		// Subscribe to the loop point reached event to handle video end
		videoPlayer.loopPointReached += OnVideoEnd;

		videoPlayer.Prepare();
		// Play the video
		videoPlayer.Play();
	}

	void OnVideoEnd(VideoPlayer vp)
	{
		// Unsubscribe from the event
		vp.loopPointReached -= OnVideoEnd;

		// Move to the next video
		currentVideoIndex = (currentVideoIndex + 1) % videoPaths.Count;

		// Play the next video
		PlayVideo(currentVideoIndex);
	}
}