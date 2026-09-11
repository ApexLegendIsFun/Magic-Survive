using UnityEngine;
using UnityEngine.UI;

public enum BgmType
{
    StartBgm,
    GameBgm
}

public enum SFXType
{

    Attack,
    LevelUp

}

public enum SFXUIType
{
    Hover,
    Click
}

public class SoundManager : MonoBehaviour 
{
    public static SoundManager instance;

    [Header("BGM / SFX / SFX_UI ")]
    [SerializeField] AudioSource bgmAudioSurce;
    [SerializeField] AudioSource sfxAudioSource;
    [SerializeField] AudioSource sfxUiAudioSource;

    [SerializeField] Slider bgmSlider;
    [SerializeField] Slider sfxSlider;

    public AudioClip[] bgmClip; //배경음
    public AudioClip[] soundClip; //효과음
    public AudioClip[] soundUiClip; //Ui효과음 

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
        {
            Destroy(gameObject);
        }
        DontDestroyOnLoad(gameObject);
    }


    void Start()
    {
        //로컬에 사운드 기록 저장 (Ui효과음은 조절 x)
        bgmSlider.value = PlayerPrefs.GetFloat("BGM", 0.5f);
        sfxSlider.value = PlayerPrefs.GetFloat("Sfx", 0.5f);

        float bgm = PlayerPrefs.GetFloat("BGM", 0.5f);
        float sfx = PlayerPrefs.GetFloat("SFX", 0.5f);

        bgmAudioSurce.volume = bgm;
        sfxAudioSource.volume = sfx;

        ChangeBgm(BgmType.StartBgm);
    }


    public void ChangeBgm(BgmType type)
    {
        if (bgmAudioSurce.clip == bgmClip[(int)type])
            return;

        bgmAudioSurce.Stop();
        bgmAudioSurce.clip = bgmClip[(int)type];
        bgmAudioSurce.Play();
    }

    public void PlaySFXUI(SFXType type)
    {
        if ((int)type > soundClip.Length)
            return;


        sfxUiAudioSource.PlayOneShot(soundClip[(int)type]);
    }


    public void PlaySFX(SFXType type)
    {
        if ((int)type > soundClip.Length)
            return;


        sfxAudioSource.PlayOneShot(soundClip[(int)type]);
    }


    public void SetBgmVolume(float volume)
    {
        bgmAudioSurce.volume = volume;
        PlayerPrefs.SetFloat("BGM", volume);
    }

    public void SetSfxVolume(float volume)
    {
        sfxAudioSource.volume = volume;
        PlayerPrefs.SetFloat("SFX", volume);
    }





}
