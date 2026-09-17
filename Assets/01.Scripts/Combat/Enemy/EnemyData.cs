using UnityEngine;



[CreateAssetMenu(fileName = "EnemyData", menuName = "Combat/Enemy Data")]


// 적 수치 데이터. 같은 프리팹도 이 값으로 다른 적이 됨
public class EnemyData : ScriptableObject
{

    [SerializeField] private Enemy prefab;


    [Header("전투")]
    [SerializeField] private float maxHealth = 10f;
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float contactDamage = 5f;

    [Header("보상")]

    // 적 사망시 획득 경험치
    [SerializeField] private int experienceReward = 1;

    [Header("보스")]
    [SerializeField] private bool isBoss;

    // EnemyManager.Spawn()에서 사용하는 프리팹
    public Enemy Prefab => prefab;


    // Initialize()에서 적용
    public float MaxHealth => maxHealth;
    public float MoveSpeed => moveSpeed;
    public float ContactDamage => contactDamage;

    // [연동:성장] 적 사망 시 EnemyKilled 이벤트로 전달되는 값
    public int ExperienceReward => experienceReward;

    // 기획이 보스에게만 지정한 면역 묶음. 빙결 둔화 밀치기 일반 적 처형
    // 넷을 따로 두지 않는 이유는 기획이 보스에 한정해 묶어 규정하므로.
    // 엘리트에 개별 면역이 필요해지면 그때 나누기
    public bool IsBoss => isBoss;

}
