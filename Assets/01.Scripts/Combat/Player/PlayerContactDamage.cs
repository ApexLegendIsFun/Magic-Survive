using UnityEngine;
using System.Collections.Generic;
using Unity.VisualScripting;


// 플레이어가 적과 겹쳤는지 주기적으로 검사하여 피해 적용

[RequireComponent(typeof(Health))]
public class PlayerContactDamage : MonoBehaviour
{

    [SerializeField] private EnemyManager enemyManager;
    [SerializeField] private float checkRadius = 0.4f;
    [SerializeField] private float checkInterval = 0.15f;

    // 피격 후 이 시간 동안 무적. 접촉 피해 빈도 설정
    [SerializeField] private float invincibilityDuration = 0.5f;

    // 겹친 적 담는 버퍼. 매 프레임 재사용
    private readonly List<Enemy> overlappedEnemies = new List<Enemy>(64);

    private Health health;
    private float checkTimer;


    // [임시] 인스펙터 확인용 필드화
    [SerializeField] private float invincibleTimer;


    // [연동:UI] 피격 깜빡임 연출 필요하면 사용 가능
    public bool IsInvincible => invincibleTimer > 0f;

    private void Awake()
    {
        health = GetComponent<Health>();

        if (enemyManager == null)
        {
            Debug.LogError("[PlayerContactDamage] EnemyManager 미연결. 접촉 피해를 비활성화 합니다.", this);

            enabled = false;

        }

    }


    private void Update()
    {
        float deltaTime = Time.deltaTime;

        if (invincibleTimer > 0f)
        {
            invincibleTimer -= deltaTime;
        }

        checkTimer -= deltaTime;

        if (checkTimer > 0f)
        {
            return;
        }

        checkTimer += Mathf.Max(0.02f, checkInterval);

        if (!health.IsAlive || invincibleTimer > 0f)
        {
            return;
        }

        ApplyHighestContactDamage();
    }


    // 동시에 겹친 적 중 ContactDamage가 가장 높은 하나만 적용.
    // 전부 합산 시 과도한 피해로 증발 방지
    private void ApplyHighestContactDamage()
    {
        enemyManager.FindOverlappingEnemies(transform.position, checkRadius, overlappedEnemies);

        float highestDamage = 0f;

        for (int i = 0; i < overlappedEnemies.Count; i++)
        {
            float damage = overlappedEnemies[i].ContactDamage;

            if (damage > highestDamage)
            {
                highestDamage = damage;

            }

        }

        if (highestDamage <= 0f)
        {
            return;
        }

        health.TakeDamage(highestDamage);

        invincibleTimer = invincibilityDuration;
    }

}
