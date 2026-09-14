using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Connects existing public UI/audio APIs. No duplicate combat or pooling implementation.
public sealed class AssetPresentationFeedback : MonoBehaviour
{
    public SoundManager soundPrefab;
    public AudioClip clickClip;
    public AudioClip levelUpClip;
    public TMP_FontAsset font;
    public Sprite buttonSprite;
    public Sprite panelSprite;
    public Canvas damageCanvas;
    private readonly List<Button> buttons = new List<Button>();
    private SoundManager sound;
    private PlayerSkillSystem skills;
    private int levelUpFrame = -1;
    private int damageFrame = -1;
    private int damageCount;

    private void Start()
    {
        sound = SoundManager.instance;
        if (sound == null && soundPrefab != null) sound = Instantiate(soundPrefab);
        if (sound != null) sound.soundClip = new[] { clickClip, levelUpClip };
        skills = FindFirstObjectByType<PlayerSkillSystem>();
        if (skills != null) skills.SkillPointSpent += HandleLevelUp;
        foreach (var button in FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            buttons.Add(button);
            button.onClick.AddListener(HandleClick);
        }
        // Graybox creates these at Awake. Its flow logic and authored UI stay untouched.
        var fallback = FindFirstObjectByType<GrayboxGameFlowView>();
        if (fallback != null)
        {
            foreach (var label in fallback.GetComponentsInChildren<TMP_Text>(true))
                if (font != null) label.font = font;
            foreach (var button in fallback.GetComponentsInChildren<Button>(true))
                if (button.image != null && buttonSprite != null) button.image.sprite = buttonSprite;
            foreach (var image in fallback.GetComponentsInChildren<Image>(true))
                if (image.name.EndsWith("Panel") && panelSprite != null)
                { image.sprite = panelSprite; image.type = Image.Type.Sliced; }
        }
        if (damageCanvas != null) GameEvents.EnemyDamaged += HandleDamage;
    }
    private void OnDestroy()
    {
        if (skills != null) skills.SkillPointSpent -= HandleLevelUp;
        GameEvents.EnemyDamaged -= HandleDamage;
        foreach (var button in buttons)
            if (button != null) button.onClick.RemoveListener(HandleClick);
    }
    private void HandleClick()
    {
        if (sound != null && clickClip != null && levelUpFrame != Time.frameCount)
            sound.PlaySFXUI(SFXType.Attack); // Existing slot 0: UI click.
    }
    private void HandleLevelUp(MagicElement element)
    {
        levelUpFrame = Time.frameCount;
        if (sound != null && levelUpClip != null) sound.PlaySFX(SFXType.LevelUp);
    }
    private void HandleDamage(Vector2 position, float damage)
    {
        if (UiObjectPool.instance == null || Camera.main == null) return;
        if (damageFrame != Time.frameCount) { damageFrame = Time.frameCount; damageCount = 0; }
        if (++damageCount > 12) return;
        Vector3 screen = Camera.main.WorldToScreenPoint(position);
        if (screen.z < 0 || screen.x < 0 || screen.x > Screen.width || screen.y < 0 || screen.y > Screen.height) return;
        var popup = UiObjectPool.instance.GetObject<DamageUi>("DamageText");
        if (popup != null) popup.Show(damage, new Vector2(screen.x, screen.y));
    }
}
