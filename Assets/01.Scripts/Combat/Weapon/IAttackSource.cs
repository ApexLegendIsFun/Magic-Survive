using UnityEngine;


// [연동:성장] 마법 SO가 이 인터페이스 구현 시 자동공격에 등록됨
public interface IAttackSource
{
    // 공격 재사용 시간
    float Cooldown { get; }

    // 자동 타겟 탐색 범위
    float Range { get; }


    bool Execute(in AttackContext context);

}


// [연동:성장] Execute가 전달받는 값 묶음.
public readonly struct AttackContext
{
    public readonly Vector2 Origin;
    public readonly Enemy Target;
    public readonly ProjectileLauncher Launcher;

    public AttackContext(Vector2 origin, Enemy target, ProjectileLauncher launcher)
    {
        Origin = origin;
        Target = target;
        Launcher = launcher;

    }
}
