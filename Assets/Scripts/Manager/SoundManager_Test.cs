using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

[Serializable]
class CBackgroundSound_Test
{
    public string soundType;
    public AudioSource audioClip;
    public float volume = 1.0f;
    public bool loop = true;
}

[Serializable]
class CEffectSound_Test
{
    public string soundType;
    public AudioSource audioClip;
    public float volume = 1.0f;
    public bool loop = false;
}

public class SoundManager_Test : MonoBehaviour
{
    [SerializeField] private List<CBackgroundSound_Test> _backgroundSounds;
    [SerializeField] private List<CEffectSound_Test> _effectSounds;
    [SerializeField] private AudioMixer _audioMixer;

    Dictionary<string, AudioSource> _bgmDict;
    Dictionary<string, AudioSource> _seDict;
    private string _lastBackGround = "";
    private string _currentBackGround = "";

    private float _fadeFactor = 1.0f;

    private static SoundManager_Test _instance;

    public static SoundManager_Test Instance
    {
        get { return _instance; }
        protected set { _instance = value; }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        _instance = this;
        _bgmDict = new Dictionary<string, AudioSource>();
        _seDict = new Dictionary<string, AudioSource>();
        ResetDictionary();
    }
    void Update()
    {
        if (string.IsNullOrEmpty(_lastBackGround) && !string.IsNullOrEmpty(_currentBackGround))
        {
            _fadeFactor = 1.0f;
            _lastBackGround = _currentBackGround;
            _bgmDict[_currentBackGround].Play();
        }
        else if (_lastBackGround != _currentBackGround)
        {
            if (_fadeFactor > 0.0f)
            {
                _fadeFactor -= Time.deltaTime * 4;
            }
            if (_fadeFactor <= 0.0f)
            {
                _fadeFactor = 0.0f;
                _lastBackGround = _currentBackGround;
                StopBGMInternal();
                if (!string.IsNullOrEmpty(_currentBackGround)) _bgmDict[_currentBackGround].Play();
            }
        }
        else
        {
            if (_fadeFactor < 1.0f)
            {
                _fadeFactor += Time.deltaTime * 4;
            }
            if (_fadeFactor > 1.0f)
            {
                _fadeFactor = 1.0f;
            }
        }
        SetVolume();
    }

    private void ResetDictionary()
    {
        foreach (var bgm in _backgroundSounds)
        {
            if (!_bgmDict.ContainsKey(bgm.soundType))
            {
                _bgmDict.Add(bgm.soundType, bgm.audioClip);
                _bgmDict[bgm.soundType].volume = bgm.volume;
                _bgmDict[bgm.soundType].loop = bgm.loop;
            }
            else
            {
                CLogger.Error($"중복된 사운드 있음 - {bgm.soundType}");
            }
        }
        foreach (var se in _effectSounds)
        {
            if (!_seDict.ContainsKey(se.soundType))
            {
                _seDict.Add(se.soundType, se.audioClip);
                _seDict[se.soundType].volume = se.volume;
                _seDict[se.soundType].loop = se.loop;
            }
            else
            {
                CLogger.Error($"중복된 사운드 있음 - {se.soundType}");
            }
        }
    }

    private void SetVolume()
    {
        float bgmVolume = 1f * _fadeFactor;
        float seVolume = 1f;

        if (bgmVolume <= 0.0001f)
        {
            _audioMixer.SetFloat("BGMLevel", -80f);
        }
        else
        {
            _audioMixer.SetFloat("BGMLevel", Mathf.Log10(bgmVolume) * 20);
        }

        if (seVolume <= 0.0001f)
        {
            _audioMixer.SetFloat("SELevel", -80f);
        }
        else
        {
            _audioMixer.SetFloat("SELevel", Mathf.Log10(seVolume) * 20);
        }
    }

    public void PlaySE(string soundType)
    {
        if (_seDict.ContainsKey(soundType))
        {
            _seDict[soundType].Play();
        }
        else
        {
            CLogger.Error($"사운드 없음 - {soundType}");
        }
    }

    public void PlayBGM(string soundType)
    {
        if (string.IsNullOrEmpty(soundType))
        {
            _currentBackGround = "";
        }
        else if (_bgmDict.ContainsKey(soundType))
        {
            _currentBackGround = soundType;
        }
        else
        {
            CLogger.Error($"사운드 없음 - {soundType}");
        }
    }

    public void StopBGM()
    {
        _fadeFactor = 0.0f;
        _currentBackGround = "";
        StopBGMInternal();
    }

    private void StopBGMInternal()
    {
        foreach (var audioSource in _bgmDict.Values)
        {
            audioSource.Stop();
        }
    }
}
