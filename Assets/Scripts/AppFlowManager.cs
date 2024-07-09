using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class AppFlowManager : MonoBehaviour
{
    [SerializeField] private GameObject _criminalBlock, _silhuette;
    [SerializeField] private TMP_Text _felony;
    [SerializeField] private RawImage _videoImage;
    private bool _faceFound;

    /*
    private string[] texts = new[] {"Bűn #1", "Bűn #2", "Bűn #3", "Bűn #4", "Bűn #5", "Bűn #6", "Bűn #7", "Bűn #8", "Bűn #9", "Bűn #10"};
    [SerializeField] private float _timeBetweenRandomPicks = 0.5f; // Time between text changes (in seconds)
    [SerializeField] private float _randomizationDuration = 10.0f; // Total duration for cycling text (in seconds)
    [SerializeField] private float _resultOnScreenDuration = 10.0f; // Total duration for cycling text (in seconds)
    [SerializeField] private float _trackLostGracePeriod = 1.0f; // Total duration for cycling text (in seconds)
    */
    
    private float timer;
    private bool _isCycling, _flowStarted, _isResultShowing;

    private float _currentTrackLostTimer, _currentResultShowingTimer;

    private Coroutine _flowRoutine, _lostFaceGracePeriod;
    
    private Queue<bool> _faceFoundHistory = new Queue<bool>();

    public bool FaceDetected;

    public bool FaceFound
    {
        get => _faceFound;
        set
        {
            if (value == _faceFound) return;
            _faceFound = value;
            if (_faceFound)
            {
                OnFaceFound();
            }
            else
            {
                OnFaceLost();
            }
        }
    }

    private void Update()
    {
        if (_currentTrackLostTimer > 0.0f) _currentTrackLostTimer -= Time.deltaTime;
        if (_currentResultShowingTimer > 0.0f) _currentResultShowingTimer -= Time.deltaTime;
        
        // Update faceFoundHistory with the current _faceFound value
        _faceFoundHistory.Enqueue(FaceDetected);

        // Limit the size of the history to fit the trackLostGracePeriod
        int historyLength = Mathf.CeilToInt(ConfigHandler.TrackLostGracePeriod / Time.deltaTime);
        if (_faceFoundHistory.Count > historyLength)
        {
            _faceFoundHistory.Dequeue();
        }

        // Check if all values in the history are false (face lost)
        if (_faceFoundHistory.All(value => !value))
        {
            FaceFound = false; // Set FaceFound to false if all history is false
        }
        
        if (_faceFoundHistory.All(value => value))
        {
            FaceFound = true; // Set FaceFound to true if all history is true
        }
        
        if (FaceDetected && _silhuette.activeSelf) _silhuette.SetActive(false);
    }

    private void OnFaceFound()
    {
        Debug.Log("FaceFound");
        _silhuette.SetActive(false);
        _videoImage.gameObject.SetActive(false);
        _criminalBlock.SetActive(true);
        _flowRoutine ??= StartCoroutine(CycleText());
    }

    private void OnFaceLost()
    {
        Debug.Log("FaceLost");
        _silhuette.SetActive(true);
    }

    private void OnResultDurationOver()
    {
        Debug.Log("Result duration over");
        _flowRoutine = null;
        _faceFoundHistory.Clear();
        if (!FaceDetected)
        {
            _videoImage.gameObject.SetActive(true);
            _silhuette.SetActive(false);
            _criminalBlock.SetActive(false);
        }
        else OnFaceFound();
    }
    
    private IEnumerator CycleText()
    {
        _isCycling = true;
        timer = ConfigHandler.RandomizationDuration;
        while (timer > 0f)
        {
            _felony.text = ConfigHandler.FelonyTexts[Random.Range(0, ConfigHandler.FelonyTexts.Length)];
            yield return new WaitForSeconds(ConfigHandler.TimeBetweenRandomPicks);
            timer -= ConfigHandler.TimeBetweenRandomPicks;
        }
        _isCycling = false;
        _isResultShowing = true;
        yield return new WaitForSeconds(ConfigHandler.ResultOnScreenDuration);
        _isResultShowing = false;
        OnResultDurationOver();
    }

}
