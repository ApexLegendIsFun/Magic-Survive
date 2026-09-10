using UnityEngine;
using System;


// 게임 주요 이벤트 전달
// TODO: 소유권 명확히
public static class GameEvents
{

    // [연동:성장] Exp
    // [연동:UI] 사망 vfx, sfx
    public static event Action<Vector2, int> EnemyKilled;

    
    // [연동:성장] 게임 오버 플로우
    // [연동:UI] 게임 오버 화면
    public static event Action PlayerDied;

    // [연동:UI] P1 Damage Number. (사망 위치가 아니라 피격 시점 위치, 실제로 깎인 피해)
    // 표식 효과가 적용된 뒤의 값이라 정수가 아닐 수 있음
    public static event Action<Vector2, float> EnemyDamaged;


    // 적 사망 시 호출
    public static void RaiseEnemyKilled(Vector2 position, int experienceReward)
    {
        EnemyKilled?.Invoke(position, experienceReward);
    }


    // 플레이어 사망 처리시 호출
    public static void RaisePlayerDied()
    {
        PlayerDied?.Invoke();
    }


    // 적이 실제로 피해를 입었을 때 호출
    public static void RaiseEnemyDamaged(Vector2 position, float amount)
    {
        EnemyDamaged?.Invoke(position, amount);
    }

    // 전체 이벤트 초기화 
    // [연동:성장]
    public static void Clear()
    {
        EnemyKilled = null;
        PlayerDied = null;
        EnemyDamaged = null;
    }


}
