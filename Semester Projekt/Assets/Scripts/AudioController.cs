using UnityEngine;
using UnityEngine.Audio;

[RequireComponent(typeof(AudioSource))]
public class AudioController : MonoBehaviour
{
    public static AudioController Instance { get; private set; }

    [Header("Menu Music")]
    public AudioClip menuMusic;
    public AudioMixerGroup outputMixerGroup;
    [Range(0f, 1f)] public float volume = 1f;
    public bool playOnStart = true;

    [Header("State Sounds")]
    public AudioClip waterStateClip;
    public AudioClip solidStateClip;
    public AudioClip gasStateClip;
    [Range(0f, 1f)] public float stateSoundVolume = 1f;

    private AudioSource audioSource;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = true;
        audioSource.volume = volume;
        audioSource.outputAudioMixerGroup = outputMixerGroup;

        if (menuMusic != null)
            audioSource.clip = menuMusic;
    }

    void Start()
    {
        if (playOnStart)
            PlayMusic();
    }

    private void OnValidate()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
            return;

        audioSource.playOnAwake = false;
        audioSource.loop = true;
        audioSource.volume = volume;
        audioSource.outputAudioMixerGroup = outputMixerGroup;

        if (menuMusic != null)
            audioSource.clip = menuMusic;
    }

    public void PlayMusic()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (audioSource == null || audioSource.clip == null)
            return;

        audioSource.loop = true;

        if (!audioSource.isPlaying)
            audioSource.Play();
    }

    public void StopMusic()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (audioSource != null && audioSource.isPlaying)
            audioSource.Stop();
    }

    public void PlayWaterStateSound()
    {
        PlayStateSound(waterStateClip);
    }

    public void PlaySolidStateSound()
    {
        PlayStateSound(solidStateClip);
    }

    public void PlayGasStateSound()
    {
        PlayStateSound(gasStateClip);
    }

    private void PlayStateSound(AudioClip clip)
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (audioSource == null || clip == null)
            return;

        audioSource.outputAudioMixerGroup = outputMixerGroup;
        audioSource.PlayOneShot(clip, stateSoundVolume);
    }
}
