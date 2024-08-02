using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class AppFlowManager : MonoBehaviour
{
    [SerializeField] private GameObject _criminalBlock, _silhuette, _fakeBGWithSilhuette;
    [SerializeField] private TMP_Text _felony;
    [SerializeField] private RawImage _videoImage;
    private bool _faceFound;

    private float timer;
    private bool _flowStarted;

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
        if (_faceFoundHistory.Count > historyLength) _faceFoundHistory.Dequeue();

        // Check if all values in the history are false (face lost)
        if (_faceFoundHistory.All(value => !value)) FaceFound = false; // Set FaceFound to false if all history is false

        if (_faceFoundHistory.All(value => value)) FaceFound = true; // Set FaceFound to true if all history is true
    }

    private void OnFaceFound()
    {
        _silhuette.SetActive(false);
        _fakeBGWithSilhuette.SetActive(true);
        _videoImage.gameObject.SetActive(false);
        _criminalBlock.SetActive(true);
        _flowRoutine ??= StartCoroutine(CycleText());
    }

    private void OnFaceLost()
    {
        _silhuette.SetActive(true);
        _fakeBGWithSilhuette.SetActive(false);
    }

    private void OnResultDurationOver()
    {
        _flowRoutine = null;
        _faceFoundHistory.Clear();
        if (!FaceDetected)
        {
            _videoImage.gameObject.SetActive(true);
            _silhuette.SetActive(false);
            _fakeBGWithSilhuette.SetActive(true);
            _criminalBlock.SetActive(false);
        }
        else OnFaceFound();
    }
    
    private IEnumerator CycleText()
    {
        timer = ConfigHandler.RandomizationDuration;
        while (timer > 0f)
        {
            _felony.text = ConfigHandler.FelonyTexts[Random.Range(0, ConfigHandler.FelonyTexts.Length)];
            yield return new WaitForSeconds(ConfigHandler.TimeBetweenRandomPicks);
            timer -= ConfigHandler.TimeBetweenRandomPicks;
        }
        yield return new WaitForSeconds(ConfigHandler.ResultOnScreenDuration);
        OnResultDurationOver();
    }

}
