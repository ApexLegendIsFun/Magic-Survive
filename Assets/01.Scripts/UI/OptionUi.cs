using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;


public class OptionsUi : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Button exitButton;

    [Header("ESC Toggle")]
    [Tooltip("인게임 씬에 놓인 인스턴스에서만. 메인 메뉴에서는 꺼두면 ESC와 무관하게 버튼으로만 열립니다.")]
    [SerializeField] private bool enableEscToggle = true;

    [Header("Sound Sliders (SoundManager와 별개로 이 씬의 슬라이더를 직접 연결)")]
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;

    private const string BgmPrefKey = "BGM";
    private const string SfxPrefKey = "SFX"; // SoundManager.SetSfxVolume이 저장하는 키와 대소문자까지 맞춤

    private void Awake()
    {
        if (exitButton != null) exitButton.onClick.AddListener(Close);

        if (bgmSlider != null) bgmSlider.onValueChanged.AddListener(OnBgmSliderChanged);
        if (sfxSlider != null) sfxSlider.onValueChanged.AddListener(OnSfxSliderChanged);
    }

    private void OnEnable()
    {
        RefreshSlidersFromSavedVolume();
    }

    private void Update()
    {
        if (!enableEscToggle) return;

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Toggle();
        }
    }

    private void RefreshSlidersFromSavedVolume()
    {
        if (bgmSlider != null) bgmSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat(BgmPrefKey, 0.5f));
        if (sfxSlider != null) sfxSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat(SfxPrefKey, 0.5f));
    }

    private void OnBgmSliderChanged(float value)
    {
        if (SoundManager.instance != null) SoundManager.instance.SetBgmVolume(value);
    }

    private void OnSfxSliderChanged(float value)
    {
        if (SoundManager.instance != null) SoundManager.instance.SetSfxVolume(value);
    }

    public void Open()
    {
        if (panelRoot == null) return;
        panelRoot.SetActive(true);
        RefreshSlidersFromSavedVolume();
    }

    public void Close()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    public void Toggle()
    {
        if (panelRoot == null) return;

        if (panelRoot.activeSelf) Close();
        else Open();
    }
}