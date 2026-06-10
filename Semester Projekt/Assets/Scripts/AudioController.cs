using UnityEngine;
using UnityEngine.Audio;

[RequireComponent(typeof(AudioSource))]
public class AudioController : MonoBehaviour
{
    public static AudioController Instance { get; private set; }

    [Header("Menu Music")]
    public AudioClip menuMusic;
    public AudioMixerGroup menuMusicMixerGroup;
    [Range(0f, 1f)] public float menuMusicVolume = 1f;

    [Header("Game Music")]
    public AudioClip gameMusic;
    public AudioMixerGroup gameMusicMixerGroup;
    [Range(0f, 1f)] public float gameMusicVolume = 1f;

    [Header("Settings")]
    public bool playOnStart = true;

    [Header("State Sounds")]
    public AudioClip waterStateClip;
    public AudioClip solidStateClip;
    public AudioClip gasStateClip;
    public AudioMixerGroup stateSoundMixerGroup;
    [Range(0f, 1f)] public float stateSoundVolume = 1f;

    private AudioSource musicSource;
    private AudioSource stateSource;
    private bool isPlayingGameMusic = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        EnsureSources();
        ConfigureMusicSource();
        ConfigureStateSource();
    }

    void Start()
    {
        if (playOnStart)
            PlayMenuMusic();
    }

    private void OnValidate()
    {
        EnsureSources();
        ConfigureMusicSource();
        ConfigureStateSource();
    }

    public void PlayMenuMusic()
    {
        EnsureSources();

        if (musicSource == null)
            return;

        StopMusic();
        isPlayingGameMusic = false;

        musicSource.clip = menuMusic;
        musicSource.outputAudioMixerGroup = menuMusicMixerGroup;
        musicSource.volume = menuMusicVolume;
        musicSource.loop = true;

        if (menuMusic != null && !musicSource.isPlaying)
            musicSource.Play();
    }

    public void PlayGameMusic()
    {
        EnsureSources();

        if (musicSource == null)
            return;

        StopMusic();
        isPlayingGameMusic = true;

        musicSource.clip = gameMusic;
        musicSource.outputAudioMixerGroup = gameMusicMixerGroup;
        musicSource.volume = gameMusicVolume;
        musicSource.loop = true;

        if (gameMusic != null && !musicSource.isPlaying)
            musicSource.Play();
    }

    public void PlayMusic()
    {
        if (isPlayingGameMusic)
            PlayGameMusic();
        else
            PlayMenuMusic();
    }

    public void StopMusic()
    {
        if (musicSource == null)
            musicSource = GetComponent<AudioSource>();

        if (musicSource != null && musicSource.isPlaying)
            musicSource.Stop();
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
        EnsureSources();

        if (stateSource == null || clip == null)
            return;

        stateSource.outputAudioMixerGroup = stateSoundMixerGroup;
        stateSource.PlayOneShot(clip, stateSoundVolume);
    }

    private void EnsureSources()
    {
        AudioSource[] sources = GetComponents<AudioSource>();

        if (musicSource == null)
        {
            if (sources.Length > 0)
                musicSource = sources[0];
            else
                musicSource = gameObject.AddComponent<AudioSource>();
        }

        if (stateSource == null)
        {
            if (sources.Length > 1)
            {
                stateSource = sources[1];
            }
            else
            {
                stateSource = gameObject.AddComponent<AudioSource>();
            }
        }
    }

    private void ConfigureMusicSource()
    {
        if (musicSource == null)
            return;

        musicSource.playOnAwake = false;
        musicSource.loop = true;
    }

    private void ConfigureStateSource()
    {
        if (stateSource == null)
            return;

        stateSource.playOnAwake = false;
        stateSource.loop = false;
        stateSource.volume = 1f;
        stateSource.outputAudioMixerGroup = stateSoundMixerGroup;
    }
}
