using UnityEngine;
using System;


// 플레이어, 적의 공통적인 체력 처리 관련

public class Health : MonoBehaviour, IDamageable
{
    [SerializeField] private float maxHealth = 100f;

    // [임시] 인스펙터 확인용. HP 확인가능한 UI 붙으면 필드 제거
    [SerializeField] private float currentHealth;

    private bool isAlive;


    
    public float MaxHealth => maxHealth;
    public float CurrentHealth => currentHealth;
    public bool IsAlive => isAlive;


    // [연동:UI] HP바, (현재 체력, 최대 체력) 순서로 전달
    public event Action<float, float> HealthChanged;

    // [연동:UI] 사망 연출
    // 체력이 0이 되면 1회 호출, 적은 사망, 플레이어는 게임오버로
    public event Action Died;



    private void Awake()
    {
        ResetHealth();
    }


    // 현재 최대 체력 기준 완전 회복
    // 재사용, 초기화시 사용
    public void ResetHealth()
    {
        currentHealth = maxHealth;

        isAlive = true;

        HealthChanged?.Invoke(currentHealth, maxHealth);

    }

    // 최대 체력까지 새로 지정해서 초기화
    // EnemyData 적용 시 사용
    public void ResetHealth(float newMaxHealth)
    {
        maxHealth = Mathf.Max(1f, newMaxHealth);

        ResetHealth();
    }

    // 최대 체력 변경
    // fill = true일시 변경된 최대 체력만큼 채움
    public void SetMaxHealth(float value, bool fill = true)
    {
        maxHealth = Mathf.Max(1f, value);

        currentHealth = fill ? maxHealth : Mathf.Min(currentHealth, maxHealth);

        HealthChanged?.Invoke(currentHealth, maxHealth);
    }


    public void TakeDamage(float amount)
    {
        if (!isAlive || amount <= 0f)
        {
            return;
        }

        currentHealth = Mathf.Max(0f, currentHealth - amount);

        HealthChanged?.Invoke(currentHealth, MaxHealth);

        if (currentHealth <= 0f)
        {
            isAlive = false;

            Died?.Invoke();
        }
    }

    public void Heal(float amount)
    {
        if (!isAlive || amount <= 0f)
        {
            return;
        }

        currentHealth = Mathf.Min(MaxHealth, currentHealth + amount);

        HealthChanged?.Invoke(currentHealth, maxHealth);
    }



}
