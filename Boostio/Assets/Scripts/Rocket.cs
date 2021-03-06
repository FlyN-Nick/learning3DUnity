﻿//using System.Collections;
//using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class Rocket : MonoBehaviour
{
    [SerializeField] private float rcsThrust = 100f;
    [SerializeField] private float mainThrust = 100f;
    [SerializeField] private float levelLoadDelay = 1f;
    [SerializeField] private float successfullyLandDelay = 1.5f;

    [SerializeField] private AudioClip engineThrustSFX = null;
    [SerializeField] private AudioClip deathSFX = null;
    [SerializeField] private AudioClip successOneSFX = null;
    [SerializeField] private AudioClip successTwoSFX = null;

    [SerializeField] private ParticleSystem engineVFX = null;
    [SerializeField] private ParticleSystem deathVFX = null;
    [SerializeField] private ParticleSystem successVFX = null;

    [SerializeField] private AudioSource engineAudioSource = null;
    [SerializeField] private AudioSource completionAudioSource = null;

    [SerializeField] private bool UILevel = false; // menu screen or game over

    private Rigidbody rigidBody = null;
    private bool isCollisionsEnabled = true;

    private enum State { alive, dead, transcending, transcended }
    State state = State.alive;

    //private Vector3 initialPos = new Vector3(0, 2.55f, 0);
    //rivate Quaternion initialRot = new Quaternion(0, 0, 0, 1);

    private void Start()
    {
        rigidBody = GetComponent<Rigidbody>();
        //initialPos = transform.position;
        //initialRot = transform.rotation;
    }

    private void Update()
    {
        if (state == State.alive)
        {
            Thrust();
            Rotate();
        }
        DebugKeys();
    }

    private void Thrust()
    {
        if (Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.UpArrow))
        {
            // thrust rocket 
            rigidBody.AddRelativeForce(Vector3.up * mainThrust * Time.deltaTime);
            // play thrust sfx
            if (!engineAudioSource.isPlaying) { engineAudioSource.PlayOneShot(engineThrustSFX); }
            // play thrust vfx
            if (!engineVFX.isPlaying) { engineVFX.Play(); }
        }
        else if (engineAudioSource.isPlaying) { engineAudioSource.Stop(); engineVFX.Stop(); }
    }

    private void Rotate()
    {
        rigidBody.angularVelocity = Vector3.zero; // remove any unwanted rotation

        float rotSpeed = rcsThrust * Time.deltaTime;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
        {
            // rotate left
            transform.Rotate(Vector3.forward * rotSpeed);
        }
        else if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
        {
            // rotate right 
            transform.Rotate(Vector3.back * rotSpeed);
        }
    }

    private void DebugKeys()
    {
        if (Debug.isDebugBuild)
        {
            if (Input.GetKeyDown(KeyCode.L))
            {
                LoadNextLevel();
            }
            else if (Input.GetKeyDown(KeyCode.C))
            {
                isCollisionsEnabled = !isCollisionsEnabled;
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (state == State.dead || state == State.transcended || !isCollisionsEnabled) { return; }

        if (collision.gameObject.CompareTag("Finish") && state != State.transcending)
        {
            state = State.transcending;
            if (engineAudioSource.isPlaying) { engineAudioSource.Stop(); }
            if (engineVFX.isPlaying) { engineVFX.Stop(); }
            completionAudioSource.PlayOneShot(successOneSFX);
            Invoke(nameof(SuccessfullyLand), successfullyLandDelay);
        }
        else if (!collision.gameObject.CompareTag("Friendly") && !collision.gameObject.CompareTag("Finish"))
        {
            state = State.dead;
            if (engineAudioSource.isPlaying) { engineAudioSource.Stop(); }
            if (engineVFX.isPlaying) { engineVFX.Stop(); }
            completionAudioSource.PlayOneShot(deathSFX);
            deathVFX.Play();
            Invoke(nameof(ReloadLevel), levelLoadDelay);
        }
    }

    public void SuccessfullyLand()
    {
        if (state != State.dead && state != State.transcended)
        {
            state = State.transcended;
            completionAudioSource.PlayOneShot(successTwoSFX);
            successVFX.Play();
            if (!UILevel) { Invoke(nameof(LoadNextLevel), levelLoadDelay); }
            else { Invoke(nameof(ReloadLevel), levelLoadDelay); }
        }
    }

    public void LoadNextLevel()
    {
        int currentIndex = SceneManager.GetActiveScene().buildIndex;
        int numberOfLevels = SceneManager.sceneCountInBuildSettings;
        int newIndex;
        if (currentIndex == numberOfLevels - 1) { newIndex = 0; }
        else { newIndex = currentIndex + 1; }
        SceneManager.LoadScene(newIndex);
    }

    void ReloadLevel()
    {
        /*state = State.alive;
        transform.position = initialPos;
        transform.rotation = initialRot;
        rigidBody.velocity = new Vector3(0, 0, 0);
        rigidBody.angularVelocity = new Vector3(0, 0, 0);
        completionAudioSource.Stop();*/
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void Restart() { SceneManager.LoadScene(0); }
}
