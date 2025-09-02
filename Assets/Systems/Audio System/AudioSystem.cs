using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This script manages the audio system, allowing for the playback of different types of audio clips.
// IMPORTANT: This script is complete but has not been tested yet 
public class AudioSystem : Singleton<AudioSystem>
{
    [SerializeField]
    private GameObject audioPrefab;
    private Dictionary<Type, List<AudioSource>> audios = new Dictionary<Type, List<AudioSource>>();
    public Dictionary<Type, List<AudioSource>> Audios
    { get { return audios; } }

    private Dictionary<Type, float> audioVolumes = new Dictionary<Type, float>();
    public Dictionary<Type, float> AudioVolumes
    { get { return audioVolumes; } }

    private float masterVolume = 1f; // Master volume for all audio sources
    private void Update()
    {
        // Garbage collector for the audio sources in the dictionary
        foreach (var audio in audios)
        {
            for(int i = audio.Value.Count - 1; i >= 0; i--)
            {
                if (!audio.Value[i].isPlaying) // Checks if the audio source is not playing
                {
                    GameObjectPool.ReturnObject(audio.Value[i].gameObject); // Returns the object to the pool
                    audio.Value.Remove(audio.Value[i]); // Removes the audio source from the dictionary
                }
            }
        }
    }
    public void PlayAudio(AudioData audio, Vector3? location = null)
    {
        // Checks if the type of audio exists inside the dictionary 
        if (!audios.ContainsKey(audio.GetType()))
        {
            audios.Add(audio.GetType(), new List<AudioSource>());
        }

        // Initializes the volume for the audio type if it doesn't exist
        if (!audioVolumes.ContainsKey(audio.GetType()))
        {
            audioVolumes.Add(audio.GetType(), 1f); 
        }

        GameObject obj = GameObjectPool.GetObject(audioPrefab); // Gets an object with an attached audio source

        AudioSource source = obj.GetComponent<AudioSource>(); // Gets a reference to the audio source from the object

        if (source != null)
        {
            // Assigns values to the audio source
            source.clip = audio.Clip;
            source.spatialBlend = 0f;
            source.volume = audio.Volume * masterVolume * audioVolumes[audio.GetType()];
            source.loop = audio.IsLooping;

            // Assigns values to spatial audio
            if (audio is SFXAudioData audio3D)
            {
                obj.transform.position = location ?? Vector3.zero;
                source.spatialBlend = 1f;
                source.minDistance = audio3D.MinDistance;
                source.maxDistance = audio3D.MaxDistance;
            }

            source.Play(); // Plays the audio source
            audios[audio.GetType()].Add(source); //Adds the audio source to the dictionary
        }
        else
        {
            Debug.LogError("Audio source Game Object requires an Audio Source component");
        }
    }
    public void AdjustMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume); // Clamps the volume between 0 and 1

        // / Adjusts the volume of all audio sources in the dictionary
        foreach (var audio in audios)
        {
            foreach (AudioSource audioSource in audio.Value)
            {
                audioSource.volume = audioVolumes[audio.Key] * masterVolume;
            }
        }
    }
    public void AdjustVolumeByType(Type audioType, float volume)
    {
        if (!audioVolumes.ContainsKey(audioType))
        {
            audioVolumes.Add(audioType, Mathf.Clamp01(volume)); // Initializes the volume for the audio type if it doesn't exist
        }
        else audioVolumes[audioType] = Mathf.Clamp01(volume); // Clamps the volume between 0 and 1

        // Adjusts the volume of all audio sources of the specified type
        foreach (AudioSource audioSource in audios[audioType])
        {
            audioSource.volume = audioVolumes[audioType];
        }
    }
}
public abstract class AudioData
{
    public AudioClip Clip;
    public float Volume;
    public float Pitch;
    public bool IsLooping;

    public AudioData(AudioClip clip, float volume = 1f, float pitch = 1f, bool isLooping = false)
    {
        Clip = clip;
        Volume = Mathf.Clamp01(volume);
        Pitch = Mathf.Clamp(pitch, -3f, 3f);
        IsLooping = isLooping;
    }
}

public class BGMAudioData : AudioData
{ 
    public BGMAudioData(AudioClip clip, float volume = 1f, float pitch = 1f, bool isLooping = false) : base(clip, volume, pitch, isLooping)
    {

    }
}
public class SFXAudioData : AudioData
{
    public bool Spatialize;
    public float MinDistance; 
    public float MaxDistance;

    public SFXAudioData(AudioClip clip, float volume = 1f, float pitch = 1f, bool isLooping = false, bool spatialize = true, float minDistance = 1f, float maxDistance = 500f) : base (clip, volume, pitch, isLooping)
    {
        Spatialize = spatialize;
        MinDistance = minDistance;
        MaxDistance = maxDistance;
    }
}
public class UIAudioData : AudioData
{
    public UIAudioData(AudioClip clip, float volume = 1f, float pitch = 1f, bool isLooping = false) : base(clip, volume, pitch, isLooping)
    {

    }
}
public class DialogueAudioData : AudioData
{
    public DialogueAudioData(AudioClip clip, float volume = 1f, float pitch = 1f, bool isLooping = false) : base(clip, volume, pitch, isLooping)
    {

    }
}