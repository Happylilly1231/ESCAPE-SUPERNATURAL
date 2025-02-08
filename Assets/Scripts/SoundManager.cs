using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SoundManager : MonoBehaviour
{
    public static SoundManager instance;

    public AudioSource bgmSource;   // 배경음악용 AudioSource
    public AudioSource sfxSource;   // 효과음용 AudioSource

    public AudioClip[] bgmClips;    // 여러 개의 배경음악 클립
    public AudioClip[] sfxClips;    // 여러 개의 효과음 클립

    public Slider bgmSlider; // BGM 볼륨 슬라이더
    public Slider sfxSlider; // SFX 볼륨 슬라이더

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // void Start()
    // {
    //     Init();
    // }

    public void Init()
    {
        // 슬라이더 값 변경 시 볼륨 조절
        if (bgmSlider)
        {
            bgmSlider.value = bgmSource.volume;
            bgmSlider.onValueChanged.AddListener(SetBGMVolume);
        }

        if (sfxSlider)
        {
            sfxSlider.value = sfxSource.volume;
            sfxSlider.onValueChanged.AddListener(SetSFXVolume);
        }
    }

    // 배경음악 재생
    public void PlayBGM(int index)
    {
        if (index < 0 || index >= bgmClips.Length) return;

        bgmSource.clip = bgmClips[index];
        bgmSource.loop = true; // 루프 설정
        bgmSource.Play();
    }

    // 효과음 재생
    public void PlaySFX(int index)
    {
        if (index < 0 || index >= sfxClips.Length) return;

        sfxSource.PlayOneShot(sfxClips[index]);
    }

    // 배경음악 정지
    public void StopBGM()
    {
        bgmSource.Stop();
    }

    // 효과음 볼륨 조절
    public void SetSFXVolume(float volume)
    {
        sfxSource.volume = volume;
    }

    // 배경음악 볼륨 조절
    public void SetBGMVolume(float volume)
    {
        bgmSource.volume = volume;
    }
}
