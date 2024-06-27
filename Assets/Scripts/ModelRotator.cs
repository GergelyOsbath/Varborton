using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

public class ModelRotator : MonoBehaviour
{
    [SerializeField] private bool _readFromConfig = true;

    [SerializeField] private float
        _minRotation = 20.0f, //if we are standing in the most left side of the camera picture, how much the phone should turn left
        _maxRotation = -20.0f, //if we are standing in the most right side of the camera picture, how much the phone should turn right
        _rotationSpeed = 90.0f, //the maximum speed for the rotation
        _minXPos = -0.1f, //if we are standing in the most left side of the camera picture, how much the phone should move left
        _maxXPos = 0.1f, //if we are standing in the most right side of the camera picture, how much the phone should move right
        _movementSpeed = 0.1f, //the maximum movement distance
        _secondsToRemovePerson = 1.5f, //how long should we wait for an update for a detected person's position
        _lockTime = 0.5f, // the time to switch between locked and unlocked state
        _minRotationAngle = 1.5f, //the needed minimum rotation from the last calculated rotations, to actually start rotate the phone
        _minMovementDistance = 0.0f, //the needed minimum distance from the last calculated position to actually start move the phone
        _idleWaitTimeBeforePhoneSwitch = 48.0f, // seconds to wait between idle phone switches (48 is 2 times the duration of the idle animation)
        _timeBetweenFlips = 20.0f; // seconds to wait until current phone flips

    [SerializeField] private Transform _modelParent; //the parent object of the phone, this will be moved and rotated
    [SerializeField] private Animator _animator; //animator, right now it makes the idle animation
    [SerializeField] private CanvasGroup[] _lockedObjects, _unlockedObjects; //ui elements for the locked and unlocked messages
    [SerializeField] private Renderer[] _screens; //the screens of the phones, where we want to switch the visual, when the phone is locked/unlocked
    [SerializeField] private GameObject[] _phoneModels; // This probably should be just a material reference instead of a whole gameobject

    [SerializeField] private UnityEvent _newPersonFollowing;
    
    private Quaternion _targetRotation = Quaternion.identity; //where the phone will rotate
    private Vector3 _targetPosition = Vector3.zero; //where the phone will go
    private List<PositionData> _positionDatas = new List<PositionData>(); //the list of the detected persons

    private Coroutine _lockRoutine, _unlockRoutine, _idleWaitingRoutine, _flipRoutine; //coroutines to animate between the locked and unlocked states 

    private int _currentModelIndex = 0;
    private bool _newPersonFound, _phoneSwitchInProgress, _waitForFlip, _isPhoneFlipped, _flipInProgress;

    private float _timeElapsed;

    private MaterialPropertyBlock[] _screenBlocks;
    
    private void Awake()
    {
        if (!_readFromConfig) return;

        _minRotation = Config.Rotation;
        _maxRotation = -Config.Rotation;
        _rotationSpeed = Config.RotationSpeed;
        _minXPos = -Config.Movement;
        _maxXPos = Config.Movement;
        _movementSpeed = Config.MovementSpeed;
        _secondsToRemovePerson = Config.SecondsToRemovePerson;
        _lockTime = Config.LockTime;
        _minRotationAngle = Config.MinRotationAngle;
        _minMovementDistance = Config.MinMovementDistance;
        
    }

    private void Start()
    {
        _screenBlocks = new MaterialPropertyBlock[_screens.Length];

        for (int i = 0; i < _screens.Length; i++)
        {
            _screenBlocks[i] = new MaterialPropertyBlock();
            _screens[i].GetPropertyBlock(_screenBlocks[i]);
        }
    }

    [ContextMenu("DebugFollow")]
    public void DebugFollowing()
    {
        NewFollowing();
        _newPersonFollowing.Invoke();
    }

    public void PersonDetected(PositionData positionData)
    {
        PositionData existingData = _positionDatas.FirstOrDefault(x => x.PersonIndex == positionData.PersonIndex); //if we already know a person just update the position, otherwise just add to the list
        
        if (existingData != null) existingData.UpdatePosition(positionData.Position); //this will also update the last seen time
        else _positionDatas.Add(positionData);
        
    }

    private void CheckForTarget()
    {
        if (_positionDatas.Count == 0 || _phoneSwitchInProgress) return;
        
        PositionData currentlyFollowing = _positionDatas.FirstOrDefault(x => x.Following); //check if we are following somebody
        
        // This is for debugging without webcam
        /*
        currentlyFollowing = new PositionData();
        currentlyFollowing.PersonIndex = 0;
        currentlyFollowing.Position = new Vector2(20.0f, 0.0f);
        currentlyFollowing.SetFollowing();
        */

        if (currentlyFollowing == null)
        {
            currentlyFollowing = FindMostCentered().SetFollowing();
            NewFollowing();
            _newPersonFollowing.Invoke();
        }
            
        Quaternion newTargetRotation = Quaternion.Euler(0.0f, Mathf.Lerp(_minRotation, _maxRotation, currentlyFollowing.Position.x), Mathf.Lerp(_minRotation / 4.0f, _maxRotation / 4.0f, currentlyFollowing.Position.x));
        
       if (newTargetRotation.y < 180.0f && _isPhoneFlipped && !_flipInProgress) newTargetRotation *= Quaternion.Euler(0.0f, 180.0f, 0.0f);
        
        _targetRotation = Quaternion.Angle(_modelParent.rotation, newTargetRotation) >= _minRotationAngle
            ? newTargetRotation
            : _targetRotation; //if the person did not move, just keep the previous rotation, to avoid jitter
        
        if (!_waitForFlip) _flipRoutine = StartCoroutine(FlipPhone());
        
        // the floating offset calculation
        _timeElapsed += Time.deltaTime * 0.7f;
        // Calculate a smooth time value (0 to 1) using Mathf.PingPong
        float smoothTime = Mathf.PingPong(_timeElapsed, 1.0f);
        // Use a damped sine wave for continuous movement
        float newY = Mathf.Sin(smoothTime * Mathf.PI * 2.0f) * 0.01f;
        
        Vector3 newTargetPosition = new Vector3(Mathf.Lerp(_minXPos, _maxXPos, currentlyFollowing.Position.x), newY, 0.0f);

        _targetPosition = Vector3.Distance(newTargetPosition, _targetPosition) >= _minMovementDistance
            ? newTargetPosition
            : _targetPosition; //if the person did not move, just keep the previous position, to avoid jitter
    }

    private IEnumerator FlipPhone()
    {
        _waitForFlip = true;
        yield return new WaitForSeconds(_timeBetweenFlips);

        _flipInProgress = true;
        Quaternion startRotation = _modelParent.transform.rotation;
        Quaternion targetRotation = Quaternion.Euler(0.0f, 180.0f, 0.0f) * _modelParent.transform.rotation;
        float t = 0.0f;

        while (t < 1.0f)
        {
            t += Time.deltaTime / 1.0f;
            _modelParent.transform.rotation = Quaternion.Lerp(startRotation, targetRotation, t);
            yield return null;
        }
        
        _flipInProgress = false;
        _waitForFlip = false;
        _isPhoneFlipped = !_isPhoneFlipped;
    }

    private void ManageDatas()
    {
        //check who is currently visible
        for (int i = _positionDatas.Count - 1; i >= 0; i--)
        {
            if (_positionDatas[i].LastSeen < DateTime.Now - TimeSpan.FromSeconds(_secondsToRemovePerson)) _positionDatas.Remove(_positionDatas[i]);
        }
        
        if (_positionDatas.Count == 0) NobodyToFollow(); // if there is nobody, we just go to "idle" state
    }
    
    private void Update()
    {
        ManageDatas();
        CheckForTarget();

        // move and rotate to the previously calculated targets
        //_modelParent.rotation = Quaternion.RotateTowards(_modelParent.rotation, _targetRotation, _rotationSpeed * Time.deltaTime);
        //_modelParent.position = Vector3.MoveTowards(_modelParent.position, _targetPosition, _movementSpeed * Time.deltaTime);
        
        // Lerping like this provides a much smoother movement curve
        float duration = 1.0f; // Set the desired duration (in seconds)
        float t = Mathf.Clamp01(Time.deltaTime / duration); // Normalize time
        // Lerp from the current position and rotation to the target position and rotation
        _modelParent.rotation = Quaternion.Lerp(_modelParent.rotation, _targetRotation, t);
        _modelParent.position = Vector3.Lerp(_modelParent.position, _targetPosition, t);
    }

    private PositionData FindMostCentered()
    {
        if (_positionDatas.Count == 0) return null;

        return _positionDatas.OrderBy(x => Mathf.Abs(x.Position.x - 0.5f)).First(); //find who is in the middle of the camera's view angle
    }

    [ContextMenu("NewFollowing")]
    private void NewFollowing()
    {
        if (_idleWaitingRoutine != null) StopCoroutine(_idleWaitingRoutine);
        _idleWaitingRoutine = null;
        // turn off animator, to let the code move and rotate the phone
        _animator.Play("Idle");
        _animator.enabled = false;

        _newPersonFound = true;
        //stop the locking routine, and start the unlock
        
        if (_lockRoutine != null) StopCoroutine(_lockRoutine);
        _unlockRoutine = StartCoroutine(UnLock()); 
    }

    private void NobodyToFollow()
    {
        //set the default targets
        _targetRotation = Quaternion.identity;
        _targetPosition = Vector3.zero;
        
        //let the animator do it's idle animation
        //_animator.enabled = true;
        //_animator.Play("PhoneIdle_Extra");
        if(_newPersonFound) StartCoroutine(SwitchPhonesThenStartIdle());
        
        
        //stop the unlocking routine, start the lock
        if (_unlockRoutine != null) StopCoroutine(_unlockRoutine);
        _lockRoutine = StartCoroutine(Lock());

        if (_idleWaitingRoutine == null) _idleWaitingRoutine = StartCoroutine(SwitchPhonesIfStillIdle());
    }

    public void CycleModels()
    {
        _currentModelIndex++;
        if (_currentModelIndex == _phoneModels.Length) _currentModelIndex = 0;
        foreach (GameObject model in _phoneModels) model.SetActive(false);
        _phoneModels[_currentModelIndex].SetActive(true);
        _newPersonFound = false;
    }

    private IEnumerator SwitchPhonesThenStartIdle()
    {
        _phoneSwitchInProgress = true;
        if (_flipRoutine != null) StopCoroutine(_flipRoutine);
        _isPhoneFlipped = false;
        _flipInProgress = false;
        _waitForFlip = false;
        yield return new WaitForSeconds(1.0f);
        _animator.enabled = true;
        _animator.Play("PhoneSwitch");
        yield return new WaitForSeconds(2.0f);
        _animator.Play("PhoneIdle_Extra");
        _phoneSwitchInProgress = false;

        yield return new WaitForSeconds(1.0f);
        if (_idleWaitingRoutine == null) _idleWaitingRoutine = StartCoroutine(SwitchPhonesIfStillIdle());
    }

    private IEnumerator SwitchPhonesIfStillIdle()
    {
        yield return new WaitForSeconds(_idleWaitTimeBeforePhoneSwitch);
        StartCoroutine(SwitchPhonesThenStartIdle());
        _idleWaitingRoutine = null;
    }

    private IEnumerator Lock()
    {
        float t = Mathf.InverseLerp(0.0f, 1.0f, _lockedObjects[0].alpha);

        foreach (CanvasGroup unlockedObject in _unlockedObjects) unlockedObject.alpha = 1.0f - t; //when we lock the screen we want to show the unlocked icon again.
        
        while (t <= 1.0f)
        {
            t += Time.deltaTime * _lockTime;
            var t2 = Mathf.InverseLerp(0.0f, _lockTime, t);
            float lockedA = Mathf.Lerp(0.0f, 1.0f, t2);
            foreach (CanvasGroup lockedObject in _lockedObjects) lockedObject.alpha = lockedA;
            
            float unlockedA = Mathf.Lerp(1.0f, 0.0f, t2);
            foreach (CanvasGroup unlockedObject in _unlockedObjects) unlockedObject.alpha = unlockedA;

            Color screenColor = Color.Lerp(Color.white, Color.grey, t2);

            for (int i = 0; i < _screens.Length; i++)
            {
                _screenBlocks[i].SetColor("_Color", screenColor);
                _screenBlocks[i].SetColor("_EmissionColor", screenColor);
                _screens[i].SetPropertyBlock(_screenBlocks[i]);                
            }
            
            yield return null;
        }

        _lockRoutine = null;
    }
    
    private IEnumerator UnLock()
    {
        float t = Mathf.InverseLerp(0.0f, 1.0f, _unlockedObjects[0].alpha);

        while (t <= 1.0f)
        {
            t += Time.deltaTime;

            var t2 = Mathf.InverseLerp(0.0f, _lockTime, t);
            float lockedA = Mathf.Lerp(1.0f, 0.0f, t2);
            foreach (CanvasGroup lockedObject in _lockedObjects) lockedObject.alpha = lockedA;
            
            float unlockedA = Mathf.Lerp(0.0f, 1.0f, t2);
            foreach (CanvasGroup unlockedObject in _unlockedObjects) unlockedObject.alpha = unlockedA; 

            Color screenColor = Color.Lerp(Color.grey, Color.white, t2);
            
            for (int i = 0; i < _screens.Length; i++)
            {
                _screenBlocks[i].SetColor("_Color", screenColor);
                _screenBlocks[i].SetColor("_EmissionColor", screenColor);
                _screens[i].SetPropertyBlock(_screenBlocks[i]);                
            }
            
            yield return null;
        }

        foreach (CanvasGroup unlockedObject in _unlockedObjects) unlockedObject.alpha = 0.0f; //after the screen unlocked we don't want to show the unlocked icon anymore.
        
        _unlockRoutine = null;
    }
}