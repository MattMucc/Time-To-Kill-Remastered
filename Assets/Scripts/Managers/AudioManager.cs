using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance {  get; private set; }

    [Header("Audio Mixer")]
    public AudioMixer masterMixer;

    [Header("UI Sounds")]
    public AudioClip buttonHover;
    public AudioClip buttonPress;
    public AudioClip preview;

    [Header("Audio Sources")]
    public AudioSource masterSource;
    public AudioSource uiSource;
    public AudioSource sfxSource;
    public AudioSource musicSource;

    [Header("Audio Mixer Groups")]
    public AudioMixerGroup masterGroup;
    public AudioMixerGroup uiGroup;
    public AudioMixerGroup sfxGroup;
    public AudioMixerGroup musicGroup;

    private void Awake()
    {
        if (!instance)
            instance = this;
        else
            Destroy(gameObject);

        DontDestroyOnLoad(gameObject);
    }

    public void PlayUISound(AudioClip clip)
    {
        if (clip)
            uiSource.PlayOneShot(clip);
        else
            Debug.LogWarning($"{clip.name} is missing or not assigned!");
    }

    public void PlayPreview(AudioClip clip, string mixerGroup)
    {
        if (!clip)
        {
            Debug.LogWarning("Preview audio clip is missing or not assigned!");
            return;
        }

        GameObject existingTemp = GameObject.Find("PreviewSound");
        if (existingTemp)
            Destroy(existingTemp);

        GameObject temp = new GameObject("PreviewSound", typeof(AudioSource));
        temp.hideFlags = HideFlags.HideInHierarchy; // Makes it invisible in the hierarchy

        AudioSource source = temp.GetComponent<AudioSource>();
        source.spatialBlend = 0f;
        if (mixerGroup == "Master")
            source.outputAudioMixerGroup = masterGroup;
        else if (mixerGroup == "UI")
            source.outputAudioMixerGroup = uiGroup;
        else if (mixerGroup == "SFX")
            source.outputAudioMixerGroup = sfxGroup;
        else if (mixerGroup == "Music")
            source.outputAudioMixerGroup = musicGroup;
        else
        {
            Debug.LogWarning($"Unrecognized mixer group `{mixerGroup}`, using default output!");
            source.outputAudioMixerGroup = masterGroup;
        }

        source.PlayOneShot(clip);
        Destroy(temp, clip.length + 0.1f);
    }

    public void SetVolume(string exposedParam, float volume)
    {
        if (!masterMixer)
        {
            Debug.LogWarning("MasterMixer is missing or not assigned!");
            return;
        }

        float db = Mathf.Log10(Mathf.Clamp(volume, 0.0001f, 1f)) * 20f;
        masterMixer.SetFloat(exposedParam, db);
    }

    public void SetMasterVolume(float volume)
    {
        SetVolume("MasterVol", volume);
        PlayPreview(preview, "Master");
    }

    public void SetUIVolume(float volume)
    {
        SetVolume("UIVol", volume);
        PlayPreview(preview, "UI");
    }

    public void SetSFXVolume(float volume)
    {
        SetVolume("SFXVol", volume);
        PlayPreview(preview, "SFX");
    }

    public void SetMusicVolume(float volume)
    {
        SetVolume("MusicVol", volume);
        PlayPreview(preview, "Music");
    }

    public void PlayButtonHover() => PlayUISound(buttonHover);
    public void PlayButtonPress() => PlayUISound(buttonPress);
}