using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class deleteLaterAudioTest : MonoBehaviour
{
    private AudioSource audioSource;
    private AudioClip audioClip;

    // Start is called before the first frame update
    void Start()
    {
        audioSource = GetComponent<AudioSource>();

        audioClip = Resources.Load<AudioClip>("Audio/NoteAmbientSound_50midi");

        Debug.Log(audioClip);

        audioSource.clip = audioClip;

        audioSource.Play();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
