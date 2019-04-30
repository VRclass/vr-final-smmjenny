using System.Collections;
using UnityEngine;

public class MyPlaySound : MonoBehaviour
{
    public AudioClip SoundToPlay;
    public float Volume;
    AudioSource source;
    public bool alreadyPlayed = false;
    // Start is called before the first frame update

    void Start()
    {
        source = GetComponent<AudioSource>();
    }

    void OnTriggerEnter()
    {
        if (!alreadyPlayed)
        {
            source.PlayOneShot(SoundToPlay, Volume);
            alreadyPlayed = true;
        }
    }

}
