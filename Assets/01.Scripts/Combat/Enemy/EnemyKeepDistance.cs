using UnityEngine;

// 플레이어와 일정 거리를 유지하는 이동. 소환술사가 사용
//
// 이 컴포넌트는 방향만 답하고 위치는 옮기지 않음
// 자기 Update에서 transform을 직접 옮기면 같은 프레임에 Enemy.Tick과
// 둘이 위치를 써서 오브젝트 실행 순서에 따라 떨림
[RequireComponent(typeof(Enemy))]
public class EnemyKeepDistance : MonoBehaviour
{
    [Header("기획에 수치가 없어 정한 임시값")]
    // 플레이어 자동공격 탐색 범위 8 안이어야 플레이어가 반격가능
    [SerializeField] private float preferredDistance = 4f;

    // 유지 거리 앞뒤 여유
    // 없으면 제자리에서 떨림
    [SerializeField] private float tolerance = 0.5f;

    /// <summary>
    /// 이번 프레임에 갈 방향. Vector2.zero면 멈춘다.
    /// Enemy.Tick이 호출하고 실제 이동은 그쪽에서 한다
    /// </summary>
    public Vector2 GetMoveDirection(Vector2 selfPosition, Vector2 playerPosition)
    {
        Vector2 toPlayer = playerPosition - selfPosition;

        float sqrDistance = toPlayer.sqrMagnitude;

        // 완전히 겹치면 방향을 정할 수 없음
        // 멈추면 영영 못 벗어나므로 아무 방향으로나 물러남
        if (sqrDistance < 0.0001f)
        {
            return Vector2.right;
        }

        float near = Mathf.Max(0f, preferredDistance - tolerance);
        float far = preferredDistance + tolerance;

        if (sqrDistance > far * far)
        {
            // 멀어서 다가감
            return toPlayer.normalized;
        }

        if (sqrDistance < near * near)
        {
            // 가까우니 물러남
            return -toPlayer.normalized;
        }

        // 유지 구간 멈춤
        return Vector2.zero;
    }
}