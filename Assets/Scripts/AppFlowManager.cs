using System.Collections;
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

    private string[] texts = new[] {"Bűn #1", "Bűn #2", "Bűn #3", "Bűn #4", "Bűn #5", "Bűn #6", "Bűn #7", "Bűn #8", "Bűn #9", "Bűn #10"};
    [SerializeField] private float _timeBetweenRandomPicks = 0.5f; // Time between text changes (in seconds)
    [SerializeField] private float _randomizationDuration = 10.0f; // Total duration for cycling text (in seconds)
    [SerializeField] private float _resultOnScreenDuration = 10.0f; // Total duration for cycling text (in seconds)
    [SerializeField] private float _trackLostGracePeriod = 1.0f; // Total duration for cycling text (in seconds)
    
    private float timer;
    private bool _isCycling, _flowStarted, _isResultShowing;

    private Coroutine _flowRoutine, _lostFaceGracePeriod;

    public bool FaceFound
    {
        get => _faceFound;
        set => _faceFound = value;
    }

    private void Update()
    {
        if (FaceFound && !_flowStarted)
        {
            _flowRoutine = StartCoroutine(CycleText());
        }

        if (!FaceFound && !_isResultShowing && _flowStarted)
        {
            if (_lostFaceGracePeriod == null)
            {
                _lostFaceGracePeriod = StartCoroutine(FaceLostGracePeriod());
            }
        }
        else
        {
            if (_lostFaceGracePeriod != null)
            {
                Debug.Log("Interrupted lost grace period");
                StopCoroutine(_lostFaceGracePeriod);
            }
        }
    }

    private void OnFaceFound()
    {
        
    }
    
    private IEnumerator CycleText()
    {
        _videoImage.gameObject.SetActive(false);
        _flowStarted = true;
        _criminalBlock.SetActive(true);
        _isCycling = true;
        timer = _randomizationDuration;
        while (timer > 0f)
        {
            _felony.text = texts[Random.Range(0, texts.Length)];
            yield return new WaitForSeconds(_timeBetweenRandomPicks);
            timer -= _timeBetweenRandomPicks;
        }
        _isCycling = false;
        _isResultShowing = true;
        yield return new WaitForSeconds(_resultOnScreenDuration);
        _isResultShowing = false;
        _silhuette.SetActive(false);
        _criminalBlock.SetActive(false);
        _flowStarted = false;
    }

    private IEnumerator FaceLostGracePeriod()
    {
        yield return new WaitForSeconds(_trackLostGracePeriod);
        if (_flowRoutine != null && _flowStarted) StopCoroutine(_flowRoutine);
        if (_isResultShowing)
        {
            _silhuette.SetActive(true);
        }
        _videoImage.gameObject.SetActive(true);
        _flowStarted = false;
        _isCycling = false;
    }
    
}
