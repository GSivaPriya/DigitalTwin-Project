using UnityEngine;

public class PlayAudioOnce : MonoBehaviour
{
    AudioSource audioSource;
    AudioManager audioManager;

    void Start()
    {
        audioSource=GetComponent<AudioSource>();
        audioManager=FindFirstObjectByType<AudioManager>();
    }

    public void BoxPickUpAudio()
    {
        audioManager.PlayAudioOneShot(audioSource);
    }
}
