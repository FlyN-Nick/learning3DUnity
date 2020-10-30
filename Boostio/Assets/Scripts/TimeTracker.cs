﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using Firebase.Firestore;
using Firebase.Extensions;

public class TimeTracker : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI messageUGUI = null;
    [SerializeField] private GameObject canvas = null;

    private int rememberedSceneIndex = 0;
    private int totalNumLevels;
    private bool isTrackingTime = true;
    private float time = 0f;
    private float[] levelTimes;

    private FirebaseFirestore db;

    private void Awake()
    {
        int amount = FindObjectsOfType<TimeTracker>().Length;
        if (amount > 1)
        {
            gameObject.SetActive(false);
            Destroy(gameObject);
        }
        else
        {
            DontDestroyOnLoad(gameObject);
            canvas.SetActive(false);
            totalNumLevels = SceneManager.sceneCountInBuildSettings;
            levelTimes = new float[totalNumLevels];
        }
    }

    private void Start() { db = FirebaseFirestore.DefaultInstance; }

    private async void Update()
    {
        int currentIndex = SceneManager.GetActiveScene().buildIndex;
        if (rememberedSceneIndex != currentIndex)
        {
            if (currentIndex == totalNumLevels - 1)
            {
                isTrackingTime = false;
                canvas.SetActive(true);
                GetLeaderboard();
            }
            else if (currentIndex == 0)
            {
                messageUGUI.text = "";
                canvas.SetActive(false);
                time = 0;
            }
            else if (currentIndex == 1) { isTrackingTime = true; }
            
            rememberedSceneIndex = currentIndex;
            if (currentIndex > 1)
            {
                int adjustedIndex = currentIndex - 2;
                float levelTime;
                if (adjustedIndex == 0)
                {
                    levelTime = time;
                }
                else
                {
                    levelTime = time - levelTimes[adjustedIndex-1];
                }
                levelTimes[adjustedIndex] = levelTime;
                if (InternetAvailable()) { await UploadLevelData(levelTime, adjustedIndex + 1); }
            }
        }
        else if (isTrackingTime) { time += Time.deltaTime; }   
    }

    private bool InternetAvailable()
    {
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            return false;
        }
        else { return true; }
    }

    private async void GetLeaderboard()
    {
        int flooredTime = (int) Math.Floor(time);
        long timeRecord = -1;
        if (InternetAvailable())
        {
            DocumentReference docRef = db.Collection("recordData").Document("record");
            Task<DocumentSnapshot> fetchRecordTask = docRef.GetSnapshotAsync();
            await Task.WhenAll(UploadTotalData(flooredTime), fetchRecordTask);
            Dictionary<string, object> recordData = (await fetchRecordTask).ToDictionary();
            timeRecord = (long) recordData["time"];
        }
        CreateMessage(flooredTime, timeRecord);
    }

    private async void CreateMessage(int userTime, long timeRecord = -1)
    {
        string message = $"Time to completion: {userTime} seconds.";
        if (timeRecord == -1)
        {
            message += "\nGG WP :D";
        }
        else
        {
            if (userTime < timeRecord)
            {
                message += $"\nThat's the fastest playthrough EVER.\nThe old record was {timeRecord} seconds.";
                if (InternetAvailable()) { await NewRecord(userTime); }
            }
            else if (userTime == timeRecord)
            {
                message += $"\nThat's the same time as the record!";
            }
            else
            {
                message += $"\nThe current playthrough time record is {timeRecord} seconds.";
            }
        }
        messageUGUI.text = message;
    }

    private Task UploadLevelData(float time, int levelNum)
    {
        DocumentReference docRef = db.Collection($"levelData/eachLevel/{levelNum}").Document();
        Dictionary<string, object> data = new Dictionary<string, object> { { "time", time } };
        return docRef.SetAsync(data);
    }

    private Task UploadTotalData(float time)
    {
        DocumentReference docRef = db.Collection("levelData/eachLevel/total").Document();
        Dictionary<string, object> data = new Dictionary<string, object> { { "time", time } };
        return docRef.SetAsync(data);
    }

    private Task NewRecord(int time)
    {
        DocumentReference docRef = db.Collection("recordData").Document("record");
        Dictionary<string, object> data = new Dictionary<string, object> { { "time", time } };
        return docRef.SetAsync(data);
    }

    public void Restart() { SceneManager.LoadScene(0); }
}
