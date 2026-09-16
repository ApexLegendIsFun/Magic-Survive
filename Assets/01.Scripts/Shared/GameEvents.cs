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

    // [연동:UI] 표식 3중첩 반응이 발동했을 때. (원소, 발동 위치, 효과 반경)
    // 단일 대상 반응(빙결)은 반경 0으로 보냄. 원소로 어떤 반응인지 구분.
    // 반응 자체의 피해는 별도로 EnemyDamaged로도 나감
    public static event Action<MagicElement, Vector2, float> ElementReactionTriggered;


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

    // 표식 3중첩 반응 발동 시 호출
    public static void RaiseElementReaction(MagicElement element, Vector2 position, float radius)
    {
        ElementReactionTriggered?.Invoke(element, position, radius);
    }

    // 전체 이벤트 초기화 
    // [연동:성장]
    public static void Clear()
    {
        EnemyKilled = null;
        PlayerDied = null;
        EnemyDamaged = null;
        ElementReactionTriggered = null;

    }


}

