using UnityEngine;
using System.Collections.Generic;


//플레이어 보유 공격 실행, 쿨타임 관련
public class WeaponRunner : MonoBehaviour
{
    //자동 공격 타겟 검색용
    [SerializeField] private EnemyManager enemyManager;

    [SerializeField] private ProjectileLauncher projectileLauncher;

    // 게임 시작 시 가지고 보유하는 공격 SO(ScriptableObjects)
    // 이후 레벨업, 마법 획득 시스템에서 공격 추가 가능
    [SerializeField] private ScriptableObject[] startingAttacks;

    // 쿨타임 너무 적어지는 거 방지용
    private const float MinimumCooldown = 0.05f;

    private readonly List<IAttackSource> attacks = new List<IAttackSource>(8);

    // attacks와 인덱스가 일대일 대응, 추가 제거를 항상 같이 해야함
    private readonly List<float> cooldownTimers = new List<float>(8);



    private void Awake()
    {
        if (startingAttacks == null)
        {
            return;
        }



        // StartingAttacks에 등록된 공격 SO 추가
        for (int i = 0; i < startingAttacks.Length; i++)
        {
            if (startingAttacks[i] is IAttackSource source)
            {
                Register(source);
            }
        }

    }



    // [연동:성장] 마법 획득 시 호출
    public void Register(IAttackSource source)
    {
        if (source == null || attacks.Contains(source))
        {
            return;
        }

        attacks.Add(source);

        cooldownTimers.Add(0f);   //새 공격은 바로 사용되게 쿨타임 0
    }

    // [연동:성장] 마법 회수 시 호출
    public void Unregister(IAttackSource source)
    {
        int index = attacks.IndexOf(source);
        if (index < 0)
        {

            return;

        }

        attacks.RemoveAt(index);
        cooldownTimers.RemoveAt(index);
    }

    private void Update()
    {

        if (enemyManager == null)
        {
            return;
        }

        float deltaTime = Time.deltaTime;

        Vector2 origin = transform.position;

        for (int i = 0; i < attacks.Count; i++)
        {
            cooldownTimers[i] -= deltaTime;

            if (cooldownTimers[i] > 0f)
            {
                continue;
            }

            IAttackSource source = attacks[i];

            Enemy target = enemyManager.FindNearest(origin, source.Range);

            AttackContext context = new AttackContext(origin, target, projectileLauncher);

            // 실제 공격 실행된 경우에만 쿨타임 돌아가도록
            if (source.Execute(context))
            {
                cooldownTimers[i] = Mathf.Max(MinimumCooldown, source.Cooldown);
            }

        }

    }
}
