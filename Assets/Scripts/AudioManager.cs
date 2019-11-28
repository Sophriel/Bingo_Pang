using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public AudioSource Source;
    public AudioClip Sound_Click;

    private void Start()
    {
        DontDestroyOnLoad(gameObject);
    }

    public void PlaySound()
    {
        Source.PlayOneShot(Sound_Click);
    }
}
