

// 투사체는 이 인터페이스로만 대상 탐색
// Health 외의 구현물(파괴가능한 오브젝트 등등)도 피격 대상 가능
public interface IDamageable
{
    // 살아있는 대상인지 확인, 데미지 적용
    bool IsAlive { get; }
    void TakeDamage(float amount);
}