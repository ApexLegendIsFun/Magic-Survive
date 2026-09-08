# 10분 빌드 — 팀 요청

> **이 문서는 하루치 업무 지시가 아니라 이번 제출의 전체 인수 목록이다.** 범위는 [GameDesignBrief.md](GameDesignBrief.md) 「콘텐츠 목표와 제출 약속」의 P0 표가 기준이며, `[P1]`은 Gate 통과 때만 순서대로 포함하고 `[삭제]`는 이번 제출에 넣지 않는다. 먼저 최신 `origin/Seondong`을 자기 브랜치에 병합하고 [TeamSchedule.md](TeamSchedule.md)의 오늘 배정량만 수행한다. 미완 카드는 다음 날로 넘기며 새 카드를 겹쳐 시작하지 않는다.

지시는 구현 방법보다 필요한 결과와 연결 계약만 공유한다.

에셋은 팀 보유 13종([DevelopmentPlan.md](DevelopmentPlan.md) §5)을 쓰며 저장소에 아직 없다. 반입 담당은 §6 담당 에셋 소유자, 컷오프 9/8 정오. 미반입 시 관련 `[P1]`은 자동 보류하고 P0는 에셋 없이 도형·색으로 완성한다. 선동 반입: 2D Pixel FX 3팩, Noto Sans KR(OFL).

## 맹유신 — 전투 결과 요청

기존 `EnemyManager`, `Enemy`, `WeaponRunner`, 투사체 풀링을 유지한 채 다음 전투 결과가 필요하다.

- `[P0]` 시작 원소별 조준 마법 5종이 피해·쿨다운·관통 수치대로 작동하고, SD02' payload의 표식 목록을 적중 시 적용. 연쇄 없음
- `[P0]` `Enemy : IElementMarkTarget` (`ba459d9`): 5원소 독립 저장, 최대 3중첩, 5초, 재적중 갱신, 풀 반환 초기화, `ElementMarkChanged` 발행을 Play Mode에서 검증
- `[P0]` 표식 공통 효과: 모든 원소 중첩 합산, 중첩당 받는 피해 +5%
- `[P0]` 플라즈마 창 융합 공격(관통 3, 화염·번개 표식 각 1), 3+3 판정·6개 소비, 반응(즉시 24, 반경 1.8, 8 피해 연쇄 3명), 플라즈마 숙련 수치, `ElementCombatEvents` 발행
- `[P0]` Tank: 코드 0, `EnemyData`·프리팹 변형 (HP 35, 속도 1.3, 피해 20, EXP 2), 3:00 슬롯 연결
- `[P0]` 난이도 HP·피해 배율 실제 적용: `Enemy.ApplyDifficulty`(`dbdbd2f`, 병합 완료)를 선동이 `SpawnDirector.EnemySpawned`에서 배선. 유신은 배율 적용 결과와 풀 초기화만 검증
- `[P0]` 보스: `BossSpawnRequested` 구독으로 8:00 생성, HP 2000, 1단계만, 접촉 피해 + 조준 투사체 부채꼴 3발 패턴 1개(수치 SD03'), 사망 시 `RunDirector.ReportBossDefeated()` 호출, 풀 초기화. 외형은 Bosses 팩 반입 시 사용, 아니면 임시 색상 프리팹
- `[P0]` 동시 100마리 + 표식 + 보스 상태에서 안전하게 초기화·재사용, 1080p 60FPS
- `[에셋 반입, 9/8 정오]` SPUM 4팩과 Fantasy Monsters Animated [Bosses] 저장소 반입. 미반입 시 SPUM 적용·보스 외형은 임시 프리팹 유지
- `[P1]` Ranged 일반 적: 보스 적 투사체 재사용, HP 14·투사체 10·EXP 2, 4:30 슬롯
- `[P1]` 원소별 고유 표식 효과 5종 (`MagicContentCatalog` 수치)
- `[P1]` 숙련 발동 5종 (`2 → 3중첩` 1회 발동·재무장)
- `[P1]` 나머지 융합 4종과 동시 완성 선판정·후소비
- `[P1]` SPUM 캐릭터 적용
- `[삭제]` 냉기·대지 제어, 보스 제어 저항·밀치기 면역, 보스 2단계·예고 장판·원형 파동, 연쇄 전격 연쇄, 돌진·소환 엘리트

연결 기준:

- 표식: `IElementMarkTarget`, `ElementMarkRules`
- 반응: `FusionReactionRules`, `ElementCombatEvents`
- 콘텐츠 수치: `MagicContentCatalog`
- 조준 payload: `ProjectileSpec` 하위 호환 확장 (SD02')
- 난이도: `SpawnDirector.EnemySpawned` → `Enemy.ApplyDifficulty`
- 보스: `RunDirector.BossSpawnRequested` 구독, `RunDirector.ReportBossDefeated()` 호출
- 범위 마법: 선동 `AreaMagicRuntime`가 Combat 공개 API(반경 내 적 탐색, `Enemy.TakeDamage`, `IElementMarkTarget.ApplyElementMark`)만 호출한다. 유신은 그 API를 제공하고 실행기를 만들지 않는다
- P1 Damage Number: `GameEvents.EnemyDamaged` 발행

P0 완료 기준은 시작 원소별 조준 공격과 표식 payload, 표식 3중첩·공통 효과, 플라즈마 3+3 소비·반응, Tank 3:00 등장, 난이도 배율 적용, 보스 생성·패턴 1개·처치 승리, 100마리 성능이 Play Mode에서 확인되는 것이다. P1 완료 기준은 [TeamSchedule.md](TeamSchedule.md)의 대기열에서 실제로 당겨온 항목에만 적용한다.

현재 통합본 `95b9d86`은 이벤트·데이터 계약과 표식 저장(`ElementMarkState`)까지 있다. `BossSpawnRequested` 구독자와 `ReportBossDefeated()` 호출자가 없고, 일반 적은 Basic·Fast만 연결되어 있으며, 난이도 배율은 계산만 하고, 표식은 저장만 되고 효과·payload가 없다. 실제 전투 연결이 완료되면 선동이 기준 씬에 병합한다.

## 한승범 — UI·연출 결과 요청

현재 HUD와 타이틀 작업을 유지한다. `GrayboxGameFlowView`는 이름 그대로 이번 제출 최종 흐름 UI다. 로직·이벤트 연결은 선동이 유지하고, 승범은 표현(색·배치·폰트·라벨)만 스킨한다. 오각형 기하 요구는 삭제.

- `[P0]` `GrayboxGameFlowView` 시작 원소 선택 화면 스킨
- `[P0]` `GrayboxGameFlowView` 트리 화면 스킨: 공개 노드 20의 이름, 효과, 선행 조건, 현재값→적용값, 확인 버튼, Hidden·Locked·Available·Owned 구분
- `[P0]` 승리·패배 결과와 재시작·타이틀 연결 확인. `ResultUi.cs` 스텁은 Graybox 결과 패널 스킨으로 흡수하거나 삭제
- `[P0]` 기존 도형·색을 쓴 표식 1~3단계와 플라즈마 반응 최소 표시
- `[P0]` 기존 HUD·타이틀 유지, 해상도·일시정지·재시작 생명주기 결함 수정
- `[에셋 반입, 9/8 정오]` Damage Numbers Pro, All In 1 Sprite Shader, Casual Fantasy GUI, Casual Games SFX Pack, Casual Game UI Sound 저장소 반입. 미반입 시 해당 `[P1]` 자동 보류
- `[P1]` Damage Number: 유신 `GameEvents.EnemyDamaged`부터 숫자 생성·Fade·풀 반환. 자기 풀 또는 Damage Numbers Pro 택1. `95b9d86`에 병합된 UiObjectPool·DamageUi는 호출자 없이 대기
- `[P1]` 한글 TMP: Noto Sans KR (OFL), 선동 9/8 제공
- `[P1]` 표식 단계·맥동·플라즈마 반응 VFX 연결 (2D Pixel FX, 선동 반입)
- `[P1]` SFX·GUI 스킨 (Casual 팩). `SoundManager`·`Sound.prefab`(`929dfbe`) 위에 클립만 연결
- `[삭제]` 오각형 신규 UI, Enemy HP Bar (`EnemyHpbar.cs` 빈 범위), 마법별 고유 VFX, 셰이더·화면 연출

연결 기준:

- 상태: `GameFlowController.StateChanged`
- 트리: `PlayerSkillSystem` 조회·선택·확정 API와 이벤트. `GrayboxGameFlowView`가 이미 연결, 승범은 표현만
- 결과: `RunDirector.ResultReady`
- HUD: 기존 `GameplayHudBinder`
- 표식·반응 표시: `IElementMarkTarget.ElementMarkChanged`, `ElementCombatEvents.FusionReactionOccurred`
- P1 Damage Number: `GameEvents.EnemyDamaged`

`GrayboxGameFlowView`를 제거하거나 대체하지 않는다. 승범은 SD-STYLE로 분리된 스타일 필드, 레이아웃 생성 구역, 라벨 문자열, 폰트, 프리팹만 수정하고 상태·이벤트·`PlayerSkillSystem` 호출부는 수정하지 않는다. 표현 변경은 승범 작업 씬·프리팹·SHA로 전달하고 선동이 병합한다.

## 유태환 — QA 결과 요청

- `[P0]` 시작 원소 5종 전체 플레이, 조준·범위 마법 동작
- `[P0]` 공개 노드 20의 인접성·선행 조건·다중 레벨업·10% 회복 검증, 8노드 Hidden 고정 확인
- `[P0]` 표식 저장·만료·갱신·풀 재사용, 중첩당 +5% 합산 검증
- `[P0]` 플라즈마 융합 공격·3+3 소비·반응·최소 표시 검증
- `[P0]` Tank 3:00 등장, 난이도 HP·피해 배율 적용, 8분 비중 재정규화 확인
- `[P0]` 8분·10분 경계, 보스 생성·패턴 1개·처치 승리·시간 초과·사망·결과·재시작 검증
- `[P0]` 신규 플레이어 5명이 각 1회 플레이해 보스 도달 4명, 승리 3명 목표 확인
- `[P0]` 적 100마리·표식·보스 상태 1080p 60FPS 확인, 포함된 P1 VFX도 함께 확인
- `[P0]` Unity `6000.3.17f1` 최종 컴파일·Missing·런타임 오류 0 확인
- `[에셋 반입]` QA는 반입하지 않는다. 9/8 정오 반입 여부와 미반입 P1 보류 목록을 보고
- `[P1]` 포함된 항목만: Ranged 4:30 경계, 원소별 고유 효과·숙련 발동, 나머지 융합 동시 반응, Damage Number·TMP 글리프·VFX·SFX
- `[삭제]` 3분·6분 엘리트 경계, 보스 2단계·제어 저항 검증
