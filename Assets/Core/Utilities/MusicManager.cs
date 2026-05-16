using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MusicManager : MonoBehaviour
{
    [Header("--- AUDIO REFERENCES ---")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip[] musicTracks;

    [Header("--- SETTINGS ---")]
    [SerializeField] private bool shuffle = true;
    [Range(0f, 1f)]
    [SerializeField] private float volume = 0.3f;
    [SerializeField] private bool playOnStart = true;

    private List<int> playListIndices = new List<int>();
    private int currentTrackPointer = -1;

    private void Awake()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        audioSource.loop = false; 
        audioSource.playOnAwake = false;
        audioSource.volume = volume;
    }

    private void Start()
    {
        if (musicTracks != null && musicTracks.Length > 0)
        {
            PreparePlaylist();
            if (playOnStart)
            {
                PlayNextTrack();
            }
        }
    }

    private void Update()
    {
        if (IsPlaylistReady() && !audioSource.isPlaying)
        {
            PlayNextTrack();
        }
    }

    private bool IsPlaylistReady()
    {
        return musicTracks != null && musicTracks.Length > 0;
    }

    private void PreparePlaylist()
    {
        playListIndices.Clear();
        for (int i = 0; i < musicTracks.Length; i++)
        {
            playListIndices.Add(i);
        }

        if (shuffle)
        {
            ShuffleList(playListIndices);
        }

        currentTrackPointer = -1;
    }

    private void ShuffleList(List<int> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int temp = list[i];
            int randomIndex = Random.Range(i, list.Count);
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }

    private void PlayNextTrack()
    {
        if (musicTracks.Length == 0) return;

        currentTrackPointer++;

        if (currentTrackPointer >= playListIndices.Count)
        {
            PreparePlaylist();
            currentTrackPointer = 0;
        }

        int trackIndex = playListIndices[currentTrackPointer];
        if (musicTracks[trackIndex] != null)
        {
            audioSource.clip = musicTracks[trackIndex];
            audioSource.Play();
        }
        else
        {
            PlayNextTrack();
        }
    }

    public void SetVolume(float newVolume)
    {
        volume = Mathf.Clamp01(newVolume);
        audioSource.volume = volume;
    }

    public void StopMusic()
    {
        audioSource.Stop();
    }

    public void PauseMusic()
    {
        audioSource.Pause();
    }

    public void ResumeMusic()
    {
        audioSource.UnPause();
    }
}