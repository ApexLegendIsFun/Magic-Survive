using UnityEngine;
using UnityEngine.UI;

public enum BgmType
{
    StartBgm,
    GameBgm
}

public enum SFXType
{

    Attack = 0,
    LevelUp = 1,
    Ignite = 2, //점화 3스택
    Freeze = 3, //빙결 3스택
    Chain_lightning = 4,//3중첩 연쇄 번개
    Eart_Shork = 5, //대지3스택, 밀치기
    Dark_Increased_damage = 6,//암흑 3중첩, 데미지 증가
    Summon = 7, //소환될떄 나는 소리 
    Dash = 9
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
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
    }

    private void OnDestroy() { if (instance == this) instance = null; }

    void Start()
    {
        float bgm = PlayerPrefs.GetFloat("BGM", 0.5f);
        float sfx = PlayerPrefs.GetFloat("SFX", 0.5f);
        if (bgmSlider != null) bgmSlider.SetValueWithoutNotify(bgm);
        if (sfxSlider != null) sfxSlider.SetValueWithoutNotify(sfx);
        if (bgmAudioSurce != null) bgmAudioSurce.volume = bgm;
        if (sfxAudioSource != null) sfxAudioSource.volume = sfx;
        if (sfxUiAudioSource != null) sfxUiAudioSource.volume = sfx;
        ChangeBgm(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "TitleScene" ? BgmType.StartBgm : BgmType.GameBgm);
    }

    public void ChangeBgm(BgmType type)
    {
        int index = (int)type;
        if (bgmAudioSurce == null || bgmClip == null ||
            index < 0 || index >= bgmClip.Length || bgmClip[index] == null)
            return;

        if (bgmAudioSurce.clip == bgmClip[(int)type])
            return;

        bgmAudioSurce.Stop();
        bgmAudioSurce.clip = bgmClip[(int)type];
        bgmAudioSurce.Play();
    }

    public void PlaySFXUI(SFXType type)
    {
        if (soundClip == null || (int)type < 0 || (int)type >= soundClip.Length || soundClip[(int)type] == null)
            return;


        sfxUiAudioSource?.PlayOneShot(soundClip[(int)type]);
    }


    public void PlaySFX(SFXType type)
    {
        if (soundClip == null || (int)type < 0 || (int)type >= soundClip.Length || soundClip[(int)type] == null)
            return;


        sfxAudioSource?.PlayOneShot(soundClip[(int)type]);
    }


    public void SetBgmVolume(float volume)
    {
        bgmAudioSurce.volume = volume;
        PlayerPrefs.SetFloat("BGM", volume);
    }

    public void SetSfxVolume(float volume)
    {
        if (sfxAudioSource != null) sfxAudioSource.volume = volume;
        if (sfxUiAudioSource != null) sfxUiAudioSource.volume = volume;
        PlayerPrefs.SetFloat("SFX", volume);
    }





}
