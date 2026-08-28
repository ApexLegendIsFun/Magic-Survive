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


    // 전체 이벤트 초기화 
    // [연동:성장]
    // TODO: 호출부 없음. 재시작 기능 만들기 전에 연결
    public static void Clear()
    {
        EnemyKilled = null;
        PlayerDied = null;
    }


}
