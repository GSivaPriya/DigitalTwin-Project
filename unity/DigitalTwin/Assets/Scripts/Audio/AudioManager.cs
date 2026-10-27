using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [SerializeField] AudioSource audioSource;
    [SerializeField] AudioClip boxPickUpClip;

    void OnEnable()
    {
        SimulationMasterCheck.MachineStart+=PlayMachineAudio;
        SimulationMasterCheck.MachineStop+=StopMachineAudio;
    }
    void OnDisable()
    {
        SimulationMasterCheck.MachineStart-=PlayMachineAudio;
        SimulationMasterCheck.MachineStop-=StopMachineAudio;
    }

    public void PlayMachineAudio()
    {
        audioSource.Play();
    }
    public void StopMachineAudio()
    {
        audioSource.Stop();
    }

    public void PlayAudioOneShot(AudioSource audio)
    {
        audio.PlayOneShot(boxPickUpClip);
    }
}
