using UnityEngine;
using System.Collections.Generic;
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

    // [연동:UI] 번개 연쇄처럼 "중심 -> 개별 대상"으로 이어지는 반응.
    // ElementReactionTriggered(원형)로는 표현이 안 되어 별도로 둠.
    // targetPositions는 발행할 때마다 새로 만든 복사본이라 구독부가 보관해도 안전.
    // 피해 적용 전 위치이며, 대상이 0명이면 이 이벤트 자체가 발행되지 않음.
    public static event Action<MagicElement, Vector2, IReadOnlyList<Vector2>> ChainReactionTriggered;

    // [연동:UI] 소환 예고. (예고 위치들, 예고 시간)
    // 예고가 끝나면 정확히 이 위치에 적이 나옴.
    // positions는 발행할 때마다 새로 만든 복사본. 구독부 보관시도 안전
    public static event Action<IReadOnlyList<Vector2>, float> SummonTelegraph;

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

    // 연쇄형 반응 발동 시 호출 (번개 등)
    public static void RaiseChainReaction(
        MagicElement element, Vector2 originPosition, IReadOnlyList<Vector2> targetPositions)
    {
        ChainReactionTriggered?.Invoke(element, originPosition, targetPositions);
    }

    // 소환 예고 시작 시 호출
    public static void RaiseSummonTelegraph(IReadOnlyList<Vector2> positions, float telegraphSeconds)
    {
        SummonTelegraph?.Invoke(positions, telegraphSeconds);
    }

    // [연동:UI] 보스 2페이즈 원형 충격파 예고. (중심, 반경, 예고 시간)
    // 예고가 끝나면 정확히 이 중심과 반경에서 터진다. 중심은 예고 시점에 고정.
    //
    // 예고 중에 보스가 죽으면 BossShockwaveTriggered 가 오지 않
    // radius 는 월드 반경.
    public static event Action<Vector2, float, float> BossShockwaveTelegraph;

    // [연동:UI] 보스 2페이즈 원형 충격파 발동. (중심, 반경)
    // 플레이어가 반경 밖이어도 발행가능. 피해 여부와 무관한 발동 연출용
    public static event Action<Vector2, float> BossShockwaveTriggered;


    // 보스 충격파 예고 시작 시 호출
    public static void RaiseBossShockwaveTelegraph(
        Vector2 center, float radius, float telegraphSeconds)
    {
        BossShockwaveTelegraph?.Invoke(center, radius, telegraphSeconds);
    }

    // 보스 충격파가 실제로 터질 때 호출
    public static void RaiseBossShockwaveTriggered(Vector2 center, float radius)
    {
        BossShockwaveTriggered?.Invoke(center, radius);
    }

    // 전체 이벤트 초기화 
    // [연동:성장]
    public static void Clear()
    {
        EnemyKilled = null;
        PlayerDied = null;
        EnemyDamaged = null;
        ElementReactionTriggered = null;
        ChainReactionTriggered = null;
        SummonTelegraph = null;
        BossShockwaveTelegraph = null;
        BossShockwaveTriggered = null;

    }


}

