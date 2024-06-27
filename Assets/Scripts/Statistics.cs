using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class Statistics : MonoBehaviour
{
    private bool _statsEnabled;
    private List<int> _detectedPersonsInThisHour = new List<int>();
    private List<int> _detectedPersonsInPreviousHour = new List<int>();
    private int _followedCount = 0;

    private void Start()
    {
        _statsEnabled = Config.StatisticsEnabled;
        
#if UNITY_EDITOR 
        _statsEnabled = false; //I don't want to save a thousand useless data into the project
#endif
        
        if(_statsEnabled) InvokeRepeating(nameof(SaveDataToFile), 3600, 3600); //save the collected data to file in every hour.
    }

    public void PersonDetected(PositionData positionData) //event when somebody detected
    {
        if (_detectedPersonsInThisHour.Any(x => x == positionData.PersonIndex) || _detectedPersonsInThisHour.Any(x => x == positionData.PersonIndex)) return; //if we know this person, we don't care anymore
        
        _detectedPersonsInThisHour.Add(positionData.PersonIndex);
    }

    private void OnApplicationQuit() => SaveDataToFile(); //if the app stops, save all the data what is already collected.

    public void NewPersonFollowed() => _followedCount++; //event when somebody else is followed

    private void SaveDataToFile() //save logic
    {
        if (!_statsEnabled) return;
        
        string path = Path.Combine(Application.streamingAssetsPath, "Statistics", $"{DateTime.Today:yy-MM-dd}.csv"); //we want daily files, if a new day starts, we just create a new file
        if (!Directory.Exists(Path.GetDirectoryName(path))) Directory.CreateDirectory(Path.GetDirectoryName(path)); // create directory 
        
        if (!File.Exists(path)) File.WriteAllText(path, ""); //if the file not yet exist, we just create it

        string fileContent = File.ReadAllText(path); 
        
        fileContent += $"{DateTime.Now:HH:mm};{_detectedPersonsInThisHour.Count};{_followedCount}\n"; //adding a new line to the file
        _detectedPersonsInPreviousHour.Clear(); //clear the data from the previous hour
        _detectedPersonsInPreviousHour = _detectedPersonsInThisHour.ToList(); //copy the data from the current hour to the previous hour
        _detectedPersonsInThisHour.Clear(); 
        _followedCount = 0;
        
        File.WriteAllText(path, fileContent); //save the file
    }
}