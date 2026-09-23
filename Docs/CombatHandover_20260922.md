# 전투 파트 인계 — 2026-09-22

작성: 맹유신

안녕하세요. 전투 파트 작업분을 정리해 전달드립니다.

기준 브랜치는 `origin/Yushin`의 bb904bf (9/22 16:24)이고, 통합본 `origin/Seondong`(e9acf04) 기준으로 15커밋이 아직 반영되지 않았습니다.

전투 본체(레벨별 효과, 엘리트, 보스)와 특화 카드 15종 전투 적용이 모두 끝났습니다. 오늘 겹침 분리와 특화 연결까지 마무리해서, 제 쪽에서 새로 구현할 전투 기능은 남아 있지 않습니다.

내일(9/23 수)이 최종 빌드일로 알고 있어 문서를 두 부분으로 나눴습니다. 3절부터 5절까지는 빌드 전에 확인이 필요한 항목이고, 6절부터는 빌드 이후 디버깅 기간에 보셔도 되는 내용입니다.

전투 코드를 직접 수정하실 필요는 없습니다. 고쳐야 할 부분이 있으면 말씀해 주시면 제가 반영하겠습니다.

---

## 1. 전달드리는 커밋 (미통합 15개)

| SHA | 날짜 | 내용 |
|---|---|---|
| `da15d12` | 9/18 | 냉기 Lv5 파괴 |
| `68fdab4` | 9/18 | `origin/Seondong` 병합 |
| `5cf9086` | 9/18 | 화염/암흑 Lv5 사망 전염 |
| `349b41f` | 9/21 | Lv6/7 투사체 쪽 강화 7종 |
| `0dfa816` | 9/21 | Lv6/7 냉기 둔화, 암흑 증폭 강화 |
| `6ba4c1e` | 9/21 | 암흑 Lv8 처형 |
| `c3f0c85` | 9/21 | 냉기 Lv8 서리 장판 |
| `872c60e` | 9/21 | 화염 Lv8 불장판 |
| `f96576a` | 9/21 | 대지 Lv8 낙석 |
| `33ee0d2` | 9/21 | 번개 Lv8 낙뢰 |
| `dfb9d09` | 9/22 | 적 겹침 분리 |
| `ab6b195` | 9/22 | 특화 1단계: 스냅샷 전달 경로와 기본 피해 5종 |
| `ac38278` | 9/22 | 특화 2단계: 반응 수치 7종 |
| `0a45820` | 9/22 | 특화 3단계: 지연 효과 3종 |
| `bb904bf` | 9/22 | 특화 4단계: `CombatStatus`를 `Applied`로 전환 |

위 목록은 개별 cherry-pick 지시가 아니라 브랜치 차이 확인용입니다. `68fdab4`는 과거 `origin/Seondong` 병합 커밋이므로, 최신 `origin/Seondong`에서 `origin/Yushin` 전체를 병합하는 방식으로 반영해 주시면 됩니다.

한 가지 참고 부탁드립니다. `Combat/`과 `Shared/`는 CP949 + CRLF로 되어 있습니다. 병합 도구 설정에 따라 한글 주석이 깨질 수 있어서, 병합 후 파일 하나만 열어 확인해 주시면 감사하겠습니다.

---

## 2. 전투 구현 현황

| 항목 | 상태 |
|---|---|
| 표식 5원소 독립 3중첩, 5초, 갱신, 풀 초기화 | 완료 |
| Lv3 고유 효과 5종 (점화 / 연쇄 / 빙결 / 충격파 / 암흑 취약) | 완료 |
| Lv5 확장 5종 (전염 / 방전 / 파괴 / 지진 구역 / 사망 전염) | 완료 |
| Lv6 강화 (관통, 연쇄 대상, 충격파 반경, 냉기 둔화) | 완료 |
| Lv7 강화 5종 | 완료 |
| Lv8 각성 5종 (불장판 / 낙뢰 / 서리 장판 / 낙석 / 처형) | 완료 |
| 엘리트 돌진자, 소환술사 | 완료 |
| 보스 1/2페이즈 3패턴 | 완료 |
| 보스 면역 (빙결 / 냉기 계열 둔화 / 밀치기 / 처형) | 완료 |
| 적 겹침 분리 | 완료 (9/22 추가) |
| 특화 카드 15종의 전투 적용 | 완료 (9/22, 15/15) |
| 보스 2페이즈 외형 변화 (크기 1.15배 등) | 미착수. 연출 범위로 보이나 크기 조정 담당은 문서에 명시되지 않았습니다 |

보스 면역에 대해 한 가지 정정드립니다. 기획서에는 "빙결, 기절, 밀치기, 처형"으로 적혀 있는데, 현재 기절 시스템 자체가 없어 기절은 해당 사항이 없습니다. 실제로 보스에게 적용되지 않는 것은 빙결, 냉기 표식 둔화와 서리 장판 둔화, 밀치기, 처형입니다. 대지 지진 구역의 둔화는 기획대로 보스에게도 적용됩니다.

### 남은 UI 연결 두 가지

아래 둘은 전투 쪽에 아직 접점이 없습니다. 필요하시면 만들어 드리겠습니다.

```text
보스 2페이즈 전환을 알리는 공개 이벤트가 없습니다.
  BossPhaseController 가 내부에서만 전환을 처리합니다. 연출이 필요하면 이벤트를 추가하겠습니다.

보스 표시 이름 필드가 없습니다.
  EnemyData 에 이름이 없어 IntegrationBossHud 가 "TEMP BOSS" 를 하드코딩하고 있습니다.
  EnemyData 에 필드를 넣을지, UI 쪽 고정 문자열로 둘지 정해 주시면 맞추겠습니다.
```

---

## 3. 선동님께 — 빌드 전 반영 부탁드립니다 (1): 데이터 교체

통합본에 임시로 들어가 있는 데이터를 실제 데이터로 바꿔 주시면 감사하겠습니다.

| 슬롯 | 현재 통합본 | 교체 부탁드리는 에셋 |
|---|---|---|
| 보스 | `Assets/Seondong/IntegrationGame/Data/TemporaryBoss.asset` | `Assets/03.Data/Enemy/Enemy_Boss.asset` |
| 돌진자 | `.../Data/TemporaryCharger.asset` | `Assets/03.Data/Enemy/Enemy_Dasher.asset` |
| 소환술사 | `.../Data/TemporarySummoner.asset` | `Assets/03.Data/Enemy/Enemy_Summoner.asset` |

특히 보스 쪽을 꼭 확인 부탁드립니다. `TemporaryBoss.asset`에는 `isBoss` 필드가 없어 false로 읽힙니다. 그래서 지금 통합 빌드의 보스는 빙결/냉기 계열 둔화/밀치기/처형 면역과 번개 연쇄 1대상 제한, 그리고 이번에 추가한 겹침 분리 면역까지 전부 꺼진 상태로 동작합니다. 최종 빌드에서 보스가 얼어붙거나 잡몹에 밀릴 수 있습니다.

프리팹에 붙어 있는 컴포넌트는 다음과 같습니다.

```text
Assets/02.Prefabs/Enemy/Enemy_Boss.prefab       Health, Enemy, EnemyRangedAttack, EnemySummon,
                                                BossPhaseController, BossShockwave
Assets/02.Prefabs/Enemy/Enemy_Dasher.prefab     Health, Enemy, EnemyDash
Assets/02.Prefabs/Enemy/Enemy_Summoner.prefab   Health, Enemy, EnemyRangedAttack,
                                                EnemyKeepDistance, EnemySummon
```

보스 생성과 처치 배선은 기존 계약 그대로입니다. `BossSpawner`가 `RunDirector.BossSpawnRequested`를 구독하고, 사망 시 `ReportBossDefeated()`를 호출합니다. 엘리트도 기존 `EliteSpawnRequested` 경로를 그대로 사용합니다. 보스 전환으로 제거되는 적은 EXP를 지급하지 않습니다(`DespawnAll`은 `EnemyKilled`를 발행하지 않습니다).

---

## 4. 선동님께 — 빌드 전 반영 부탁드립니다 (2): 코드 두 곳

테스트 편의를 위해 9/16에 반응 해금 레벨을 1로 낮춰 둔 상태입니다. 빌드 전에 기획값 3으로 되돌려야 합니다.

```text
Assets/01.Scripts/Combat/ElementReactionValues.cs:15
    ReactionUnlockLevel = 1  ->  3                         (제가 수정하겠습니다)

Assets/Seondong/IntegrationGame/Editor/IntegrationGameEditor.cs:211
    Require(ElementReactionValues.ReactionUnlockLevel == 1, ...)  ->  3
```

두 곳을 같은 시점에 바꿔야 검증이 통과합니다. 빌드 직전 마지막 커밋으로 잡는 것이 좋겠습니다. 신호만 주시면 제가 먼저 올리겠습니다.

---

## 5. 특화 15종 — 연결을 완료했습니다

### 5-1. 진행 경위

`Docs/SpecializationUIContract.md`와 `Docs/TeamNextSteps_20260914.md` 기준으로 분담은 이렇게 되어 있습니다.

```text
선동님   특화 카드 정의, 선택/누적/조회 API, 그리고 특화 값을 Combat 으로 넘기는 계약
유신     전달받은 특화 값을 실제 전투 효과에 적용
승범님   카드 UI 와 적용/미적용 상태 표시
```

선동님 쪽의 카드 15종, 선택 횟수, 누적 보너스 계산은 이미 완성되어 있었고, 남은 것은 누적값이 Combat 까지 오는 경로와 제 쪽의 전투 적용이었습니다.

빌드 일정상 전달 계약을 기다리기 어려워, 제 브랜치에서 전달 경로까지 함께 구현했습니다. 기존 Progression API의 선택/누적 규칙은 바꾸지 않았고, 누적값을 발사 시점 스냅샷으로 Combat에 넘기는 방식입니다. 다른 방식을 생각하고 계셨다면 말씀해 주세요. 바로 맞추겠습니다.

### 5-2. 연결 구조

```text
PlayerSkillSystem
  └ 특화 누적값으로 원소별 스냅샷 생성 (ElementSpecializationBonus)
       ↓
MagicRuntime
  └ 기본 피해에 피해 특화 적용
       ↓
ProjectileSpec
  └ 나머지 특화 수치를 발사 시점 값으로 전달
       ↓
Projectile / Enemy
  └ 반경, 시간, 증폭, 전염 수치 적용
```

`PlayerSkillSystem.TryCommitGrowth`가 특화 횟수를 먼저 올리고 그 뒤에 레벨 확정 이벤트가 발생하므로, `HandleSkillLevelChanged`에서 누적값을 읽으면 방금 고른 카드까지 그 자리에서 반영됩니다.

단위 해석(Percent는 기준값 × (1 + b/100), PercentagePoints는 기준 비율 + b/100)은 스냅샷을 만들 때 한 번만 합니다. Combat은 곱하거나 더하기만 하고 단위를 다시 해석하지 않습니다. 적용 순서는 기본 성장(레벨 배율)을 먼저, 그 결과에 특화를 적용합니다.

### 5-3. 15종 적용 위치

| 카드 | 적용 지점 |
|---|---|
| 원소별 기본 피해 5종 | `MagicRuntime.SetSkillLevel` |
| 점화 반경 / 연쇄 탐색 거리 / 충격파 반경 | `Projectile` — 판정과 이벤트가 같은 값을 씁니다 |
| 불장판 지속 / 방전 피해 / 빙결 지속 / 밀치기 거리 | `Projectile` |
| 서리 장판 반경 / 암흑 전염 거리 | 투사체가 적에게 반경을 미리 남기고, 빙결 해제와 사망 시점에 사용 |
| 암흑 3중첩 증폭 | 기존 값 전달 방식에 퍼센트포인트를 더합니다 |

카드가 없는 항목은 그대로입니다. 점화 피해, 방전 반경, 연쇄 피해 비율, 충격파 피해, 지진 구역(반경, 지속, 둔화), 화염 전염 거리가 여기 해당합니다.

### 5-4. 제가 수정한 다른 분 담당 파일

일정상 미리 양해를 구하지 못하고 수정했습니다. 죄송합니다. 문제가 되면 바로 되돌리겠습니다.

| 파일 | 수정 내용 |
|---|---|
| `Progression/Skills/PlayerSkillSystem.cs` | 누적값을 스냅샷으로 묶어 `MagicRuntime`에 전달 (선택/누적 규칙은 변경하지 않았습니다) |
| `Progression/Skills/MagicRuntime.cs` | 기본 피해에 피해 특화 배율 적용, `SetSkillLevel` 오버로드 추가 |
| `Progression/Skills/Content/SpecializationCatalog.cs` | `SpecializationCombatStatus`에 `Applied` 추가, `CombatStatus` 반환값 변경 |
| `Editor/SpecializationValidationEditor.cs` | `NotConnected` 단언을 `Applied` 기준으로 수정 |
| `Progression/CombatContracts/ElementSpecializationBonus.cs` | 신규. 스냅샷 구조체와 카드 15종 매핑 |

`Docs/SpecializationUIContract.md`는 담당 문서라 제가 고치지 않았습니다. 다만 코드가 이미 `Applied`를 보고하고 있어 문서와 반대 상태이니, 5-5의 요청을 확인해 주시면 감사하겠습니다.

기존 공개 API는 하나도 지우지 않았습니다. `ProjectileSpec`의 6인자와 8인자 생성자, `Enemy.ApplyFreeze(float, bool, bool)`, `Enemy.ArmDeathSpread(MagicElement)`는 호환 오버로드로 남겨 두었으니 기존 호출부는 그대로 동작합니다.

### 5-5. 부탁드리는 것

- 선동님: `Docs/SpecializationUIContract.md`의 전투 미연결 상태 설명을 현재 구현에 맞게 갱신 부탁드립니다. 관련 위치는 27, 60, 104, 125, 133행입니다. 지금은 코드가 15종 전부 `Applied`를 보고하는데 문서는 반대로 적혀 있어, 이 문서를 먼저 읽는 분이 오해할 수 있습니다.
- 선동님: 승범님의 결과 UI가 원소별 레벨을 표시할 수 있도록 `RunResult` 계약 확장을 검토 부탁드립니다. 현재는 `Elements` 목록만 있습니다.
- 선동님: 재시작 경로의 static 이벤트 초기화 정책을 확인 부탁드립니다. `GameEvents.Clear()`를 쓰신다면 새 씬의 구독이 시작되기 전에 호출해야 합니다. 구독을 `OnEnable`과 `OnDisable` 쌍으로 관리하고 있다면 별도 초기화 없이도 문제가 없습니다.
- 승범님: 현재 UI에 "전투 미연결" 표시가 구현돼 있다면, `Applied` 상태에서는 표시되지 않도록 확인 부탁드립니다. 조회 API와 필드는 그대로입니다.

---

## 6. 승범님께 — 보스 HP는 이미 열려 있습니다

`UI/ReadMe.txt` 09_21의 "BossHud 이벤트 대기중" 건입니다. 보스 인스턴스를 넘기는 이벤트를 `BossSpawner`에 이미 열어 두었습니다. 따로 기다리지 않으셔도 됩니다.

```csharp
// BossSpawner 를 인스펙터로 받거나 FindFirstObjectByType 으로 찾습니다
private Health bossHealth;

private void OnEnable()  { spawner.BossSpawned += OnBossSpawned; }

private void OnDisable()
{
    spawner.BossSpawned -= OnBossSpawned;
    if (bossHealth != null) bossHealth.HealthChanged -= Refresh;
}

private void OnBossSpawned(Enemy boss)
{
    if (bossHealth != null) bossHealth.HealthChanged -= Refresh;

    bossHealth = boss.GetComponent<Health>();
    bossHealth.HealthChanged += Refresh;

    // 구독 시점에는 이벤트가 오지 않으므로 초기 표시를 한 번 맞춰 주세요
    Refresh(bossHealth.CurrentHealth, bossHealth.MaxHealth);
}

private void Refresh(float current, float maximum) => bossHpBar.fillAmount = current / maximum;
```

보스는 풀에서 재사용되므로 `BossSpawned`가 올 때마다 이전 구독을 해제해 주셔야 합니다. 같은 패턴이 `Assets/Seondong/IntegrationGame/Runtime/IntegrationBossHud.cs`에 이미 동작하고 있으니 참고하셔도 좋습니다.

### 각성과 장판 연출 훅

암흑 처형을 제외한 효과는 기존 이벤트로 발행되고 있습니다. 새 이벤트를 기다리지 않으셔도 됩니다. 대지 Lv5 지진 구역도 같은 장판 이벤트를 사용합니다.

| 효과 | 이벤트 | 인자 |
|---|---|---|
| 화염 Lv8 불장판 | `GroundAreaCreated` | `(Fire, 중심, 반경, 지속시간)` |
| 냉기 Lv8 서리 장판 | `GroundAreaCreated` | `(Frost, 중심, 반경, 지속시간)` |
| 대지 Lv5 지진 구역 | `GroundAreaCreated` | `(Earth, 중심, 반경, 지속시간)` |
| 대지 Lv8 낙석 | `ChainReactionTriggered` | `(Earth, 플레이어 위치, 낙석 3지점)` |
| 번개 Lv8 낙뢰 | `ElementReactionTriggered` | `(Lightning, 대상 위치, 0)` — 대상 1명당 1회 |
| 암흑 Lv8 처형 | 전용 이벤트 없음 | 아래 설명을 참고해 주세요 |

주의 사항 두 가지입니다.

1. 번개는 반경으로 구분합니다. `ElementReactionTriggered(Lightning, ...)`이 방전(반경 1.2)과 낙뢰(반경 0) 양쪽에 쓰입니다. 반경 0은 "단일 대상" 규약이며 냉기 빙결도 동일합니다. 구분이 불편하시면 낙뢰 전용 이벤트를 새로 추가해 드리겠습니다. 말씀만 주세요.

2. 암흑 처형에는 전용 이벤트가 없습니다. 현재 구현의 발행 순서는 다음과 같습니다.

```text
최초 타격의 EnemyDamaged
  → 처형 사망 경로 실행 (Health.Died 가 동기로 돌아 EnemyKilled 발행)
  → 처형으로 깎인 잔여 체력의 EnemyDamaged 가 뒤이어 발행
```

즉 사망 이벤트가 먼저 나가고 피해 숫자가 그 뒤에 한 번 더 나갑니다. 이 순서를 전제로 연출을 짜시면 되고, 처형 전용 연출이 필요하면 별도 이벤트를 추가하겠습니다.

### 반경은 레벨과 특화에 따라 변합니다

Lv6/7에서 점화 반경(1.3 → 1.56), 충격파 반경(1.5 → 1.725)이 커지고, 특화 카드가 쌓이면 더 커집니다. 이벤트의 `radius` 인자는 항상 실제 판정 반경과 같은 값으로 보내고 있으니, 연출 크기를 상수로 두지 마시고 인자를 그대로 사용해 주시면 이후 수치가 바뀌어도 자동으로 따라갑니다.

---

## 7. 태환님께 — 이번 빌드에서 확인 부탁드리는 것

새로 들어간 기능들입니다. 문제가 보이면 재현 순서와 함께 알려 주시면 제가 확인하겠습니다.

```text
Lv6/7/8   각 원소를 6, 7, 8레벨까지 올렸을 때 효과가 실제로 바뀌는지
          (화염 불장판 / 번개 낙뢰 / 냉기 서리 장판 / 대지 낙석 / 암흑 처형)
특화      카드를 고른 만큼 수치가 커지는지
          대부분 같은 카드 2회는 +20% 이고, 암흑 "깊은 저주"만 +4%p 입니다
          반경은 연출 크기로, 지속시간은 효과가 유지되는 시간으로 확인하시면 빠릅니다
엘리트    3:00 돌진자 예고선 방향과 실제 돌진 방향 일치, 6:00 소환술사 소환 상한 6마리
보스      8:00 등장, HP 70%에서 2페이즈 전환
          빙결 / 냉기 계열 둔화 / 밀치기 / 처형 면역 (대지 지진 구역의 둔화는 적용됩니다)
겹침 분리  적이 서로 겹치지 않는지, 플레이어 중심에 완전히 포개지지 않는지,
          뭉쳤을 때 좌우로 떨리지 않는지, 플레이어 이동이 적에게 막히지 않는지
```

겹침 분리 수치는 씬의 `EnemyManager` 인스펙터에서 조정할 수 있습니다. 체감이 이상하면 아래를 참고해 주세요.

| 항목 | 기본값 | 조정 방향 |
|---|---:|---|
| Enemy Separation Factor | 0.7 | 너무 퍼지면 낮춤, 아직 뭉치면 높임 |
| Player Separation Distance | 0.6 | 0.9 미만 유지 필수. 그 이상이면 접촉 피해가 끊깁니다 |
| Separation Response | 12 | 떨리면 낮춤, 굼뜨면 높임 |
| Max Separation Speed | 3 | 순간이동처럼 보이면 낮춤 |

적이 플레이어 몸에 어느 정도 붙어 있는 것은 정상입니다. 분리 거리(0.6)가 접촉 피해 판정 거리(0.9)보다 작아야 접촉 피해가 계속 들어가기 때문에, 적은 0.6에서 0.9 사이에 머뭅니다. 완전히 떨어뜨리면 접촉 피해가 끊깁니다.

적 수치는 기존과 동일하게 `03.Data/Enemy/`의 `EnemyData` 에셋에서 조정하시면 됩니다. 프리팹은 열지 않으셔도 됩니다.

---

## 8. Combat 공개 API — 9/17 이후 변경분

| 시그니처 | 용도 | 호출 주체 |
|---|---|---|
| `ElementSpecializationBonus` (신규 구조체) | 특화 누적값 스냅샷. `Build(element, 조회 델리게이트)` | Progression |
| `ProjectileSpec(..., element, skillLevel, specialization)` | 9인자 오버로드. 6인자와 8인자는 그대로 남아 있습니다 | Progression |
| `MagicRuntime.SetSkillLevel(int level, ElementSpecializationBonus)` | 특화 반영. 기존 1인자 버전도 유지 | Progression |
| `Enemy.ApplyFreeze(float, bool, float groundRadius)` | 서리 장판 반경까지 전달. 기존 bool 버전 유지 | Combat 내부 |
| `Enemy.ArmDeathSpread(MagicElement, float spreadRadius)` | 전염 반경까지 전달. 기존 1인자 버전 유지 | Combat 내부 |
| `Enemy.SetDarkAmplificationUnlocked(float bonus)` | 암흑 증폭량 전달 (bool 에서 값으로) | Combat 내부 |
| `Enemy.SetFrostSlowPerStack(float slowPerStack)` | 냉기 둔화 중첩당 감소량 전달 | Combat 내부 |
| `Enemy.ArmDarkExecute()` | 처형 권한 | Combat 내부 |
| `Enemy.CanReceiveSeparation` / `ApplySeparation(Vector2)` | 겹침 분리 (9/22 추가) | Combat 내부 |
| `Enemy.SuppressExperienceReward()` | 소환된 적과 보스 전환 제거 시 EXP 차단 | 스폰 담당 |
| `Enemy.MultiplyMoveSpeed(float)` / `SetGroundSlowMultiplier(float)` | 2페이즈 가속, 장판 둔화 | Combat 내부 |
| `Enemy.IsBoss` / `IsFrozen` / `IsDarkAmplified` / `HasAnyElementMark` | 상태 조회 | UI |
| `Enemy.FrozenChanged` / `DarkAmplifiedChanged` / `ElementMarkChanged` | 상태 변화 알림 | UI |
| `EnemyManager.AddGroundArea(element, center, radius, duration, slow [, damagePerTick, interval])` | 장판 생성 | Combat 내부 |
| `EnemyManager.ResolveRockfall()` / `ResolveLightningStorm()` / `ResolveFrostShatter(center)` | Lv8과 Lv5 광역 처리 | Combat 내부 |
| `EnemyManager.QueueDeathSpread(element, center, radius, maxTargets)` | 사망 전염 예약 (LateUpdate 적용) | Combat 내부 |
| `EnemyManager.GroundAreaCount` / `PendingDeathSpreadCount` / `IsResolvingFrostShatter` | 검증용 | QA, 에디터 검사 |
| `ProjectileLauncher.LightningStormTimer` / `IsLightningStormUnlocked` | 낙뢰 주기 검증용 | QA, 에디터 검사 |
| `BossSpawner.BossSpawned` (`Action<Enemy>`) | 보스 등장 | UI |

`GameEvents`에 추가된 이벤트: `ElementReactionTriggered`, `ChainReactionTriggered`, `GroundAreaCreated`, `SummonTelegraph`, `DashTelegraph`, `BossShockwaveTelegraph`, `BossShockwaveTriggered`.

---

## 9. 기획 확인 요청 (빌드 이후에 답 주셔도 됩니다)

기획서에 수치나 규정이 없어 제가 임시로 정해 구현한 부분입니다. 답을 주시면 상수만 교체하면 되고, 대부분 `ElementReactionValues.cs` 한 파일 안에 모여 있습니다.

| # | 항목 | 현재 구현 |
|---|---|---|
| 1 | 냉기 둔화 "+10%" | 배율로 해석. 중첩당 0.10 → 0.11 |
| 2 | 암흑 피해 증가 "+10%" | 배율로 해석. 0.15 → 0.165 |
| 3 | 충격파 반경 +15%가 지진 구역에도 적용되는지 | 미적용 (지진은 1.5 유지) |
| 4 | 전염 반경 (화염, 암흑) | 각각 1.3 |
| 5 | 번개 연쇄 반경 | 1.5 |
| 6 | 대지 Lv1 밀치기 거리 | 0.5 |
| 7 | 서리 장판 반경 | 1.5. 특화 "서리 장판 반경 +10%"의 기준값입니다 |
| 8 | 불장판 반경 | 점화 폭발 반경과 동일. 따라서 "점화 폭발 반경" 특화가 불장판 크기까지 키웁니다 |
| 9 | 낙뢰 사거리 | 제한 없음. 살아 있는 적 중 플레이어에게 가까운 순 3명 |
| 10 | 낙석 "5번째 대지 공격마다" | 적중이 아니라 발사를 셉니다 |
| 11 | "보스에게는 연쇄 대상이 보스 1명" | 보스가 연쇄를 받는 쪽일 때로 해석. 단독 보스전에서는 연쇄 0명입니다 |
| 12 | 소환술사가 빙결됐을 때 원거리 공격 | "멈춘다"에 공격 중지를 포함했습니다 |
| 13 | 겹침 분리 수치 | 적 간격 계수 0.7, 플레이어 최소 거리 0.6은 체감 기준 시작값입니다 |
| 14 | 보스 면역의 "기절" | 기절 시스템이 없어 해당 사항 없음으로 두었습니다 |

보스 2페이즈에서 접촉 20 + 원거리 8 + 충격파 18이 같은 프레임에 겹칠 수 있습니다. 의도하신 난이도가 맞는지 확인 부탁드립니다.

---

## 10. 알려진 한계

```text
직접 맞은 적은 전염으로 표식을 받은 뒤 죽어도 전염을 퍼뜨립니다 (표식 출처를 기록하지 않습니다)
사망 전염과 번개 연쇄의 대상 선택은 관리 목록 순서에 의존합니다 (이웃이 상한보다 많을 때)
반응 순회 도중 다른 반응이 끼어들면 피해 순서 의존이 남아 있습니다 (점화 루프 중 파괴 등)
투사체 이동 간격이 HitRadius x 2 보다 커지면 적을 통과합니다. 투사체 속도를 크게 올리면 재검토가 필요합니다
겹침 분리는 밀집할수록 천천히 풀립니다 (진동을 없애기 위해 이웃 수로 평균을 내기 때문입니다)
```

---

## 11. 검증 결과

Lv6/7/8의 개별 기능과 특화 경로는 임시 스크립트로 확인했지만, 최신 통합본에서 정상 속도로 한 판을 진행한 통합 실측은 아직 하지 않았습니다. 빌드 이후 정리해서 추가로 전달드리겠습니다.

검증에는 임시 스크립트를 썼고, 스크립트와 테스트용 데이터는 커밋에 포함하지 않았습니다. `Time.captureDeltaTime`으로 프레임 간격을 고정해 실행할 때마다 같은 결과가 나오게 했습니다.

### 겹침 분리

```text
겹침 해소 2 / 3 / 10마리   0.6696 / 0.6666 / 0.6659   (평형값 0.665)
보스 이동량 / 간격          0.0000 / 0.6662             보스는 밀리지 않음
빙결 적 / 상대 이동량       0.0000 / 0.6662             빙결도 밀리지 않음
돌진 예고 중 이동량         0.0000                      예고선과 궤도 일치
서리 장판 중심 오차         0.0083                      (상한 0.05)
30 / 60 / 120 FPS 간격     0.6693 / 0.6657 / 0.6654    편차 0.58%
접촉 피해                   10초 17회                   무적 0.5초 기준 상한 근처
```

### 특화 15종

```text
카드 매핑 15종        전부 지정한 칸으로만 들어감 (냉기는 다른 원소와 순서가 반대라 별도 확인)
기본 피해 5원소       0회 / 1회 / 2회 = 기본 / x1.1 / x1.2, 다른 카드로는 불변
실제 적중 피해        7.5900 (MagicRuntime 계산값과 일치)
반응 수치 7종         점화 반경 1.5600 → 1.7160 / 불장판 2.0 → 2.2 / 연쇄 1.5 → 1.65
                      방전 12.5 → 13.75 / 빙결 0.9 → 0.99 / 충격파 1.725 → 1.8975
                      밀치기 0.65 → 0.715
Projectile 배선       ElementReactionTriggered 의 radius 로 1.5600 → 1.7160 확인
지연 효과 3종         서리 장판 이벤트 반경 1.5000 → 1.6500
                      전염 거리 1.85 이웃 표식 0중첩 → 1중첩
                      암흑 4타 피해 9.0703 → 9.2109
풀 재사용             권한을 든 채 풀로 반환 후 재스폰 -> 장판 반경 0.0000
특화 0회              모든 게터가 기존 값을 그대로 돌려줌
```

암흑 증폭은 게터에서 0.165 → 0.185의 퍼센트포인트 가산을 확인했고, 실제 피해 경로에서도 증가를 확인했습니다. 표시된 9.0703과 9.2109는 테스트용으로 체력을 크게 잡은 상태에서 피해 전후 체력을 빼는 과정의 float 정밀도 영향을 받은 값입니다.

### 다시 돌려야 하는 검사

```text
SpecializationValidationEditor   bb904bf 의 Applied 전환 이후 재실행 필요
MvpPlayModeSmokeEditor           같음
```

적 100마리 기준 성능은 9/14 측정치(에디터 오프스크린 102FPS) 이후 장판, 낙석, 낙뢰, 겹침 분리가 추가되었으므로 통합본에서 재측정이 필요합니다. 제 쪽에서 Profiler로 확인해 공유드리겠습니다.

---

문의사항이 있으시면 언제든 말씀해 주세요. 빌드 전에 제가 처리해야 할 부분이 있으면 바로 반영하겠습니다.
