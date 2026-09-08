# Magic Survive — 9월 3일 이후 팀 일정

## 일정 기준

- 프로젝트 시작: 2026년 8월 26일
- 오늘: 2026년 9월 3일, Day 9
- Day 30 제출일: 2026년 9월 24일
- 기능 완료 시각: 9월 17일 18:00
- 기능 동결: 9월 18일~24일. 신규 기능 금지, P0 결함·성능·밸런스만 수정
- 작업 카드 하나는 0.5~1인일이다. 한 사람의 하루 필수량 합계는 1인일을 넘기지 않는다. 0.5일 카드 두 개는 같은 날 가능하다.
- 9월 5~6일과 12~13일은 밀린 카드 회수용 주말 버퍼다. 새 필수 기능을 넣지 않는다.

필수 구현은 평일에만 배치했다. 주말 작업이 가능하면 9월 5~6일과 12~13일을 지연 회수에 쓰고, 불가능하면 회수 버퍼가 0이 된다. 첫 P0 지연이 생기는 즉시 P1을 동결하고 새 일정보다 미완 P0를 먼저 옮긴다.

## 9월 3일 기준 현실 확인

- 9/4 00:34 선동이 `95b9d86`에 병합 완료: 유신 `dbdbd2f`(`Enemy.ApplyDifficulty(h,d)`, `Enemy_A/B`→`Enemy_Basic/Fast`, GUID 유지)·`ba459d9`(`Enemy : IElementMarkTarget` + `ElementMarkState` 저장·만료·풀 초기화, 효과 없음), 승범 `2ec3cfb`~`6241846`·`929dfbe`(`Canvas_InGame` 분리, UiObjectPool·DamageUi 풀 반환, `SoundManager`·`Sound.prefab`, `ResultUi.cs`·`EnemyHpbar.cs` 스텁, TitleScene 재작업). 기준 씬에 UiObjectPool 연결됨.
- 병합 뒤 남은 것: `SpawnDirector.EnemySpawned`→`Enemy.ApplyDifficulty` 호출 없음. `MvpIntegrationEditor` 19~20행이 `Enemy_A/B.asset` 경로를 참조해 파일명 변경 뒤 깨져 있음. Tank 슬롯 null. `DamageUi.Show` 호출자 없음(`EnemyDamaged` 선행).
- `RunDirector.ReportBossDefeated()` 호출자 0, `BossSpawnRequested` 구독자 0, 보스 클래스·프리팹 0. 승리 상태에 도달할 방법이 없다. 1순위 P0 공백.
- `normalEnemies` 슬롯 2·3(Tank·Ranged) 비어 있음. 엘리트 0(`EliteSpawnRequested` 구독자 0). 엘리트는 이번 제출에서 삭제.
- 범위 마법은 카탈로그 정의만 있고 실행기 0. `PlayerSkillSystem`은 `ProjectileMagicDefinition`만 색인해 범위 노드를 얻어도 효과가 없다. `FlameRing` 반경 미정.
- `ProjectileSpec`에 표식 payload 없음. `PlasmaLance` 정의 에셋 없음(`03.Data/Magic`은 조준 5개). `ElementCombatEvents` 발행자 0.
- `GameEvents`에 `EnemyDamaged` 없음. Damage Number 연결의 선행.
- 에셋 13종은 팀 보유·저장소 미반입. `Assets`에 SPUM·Bosses·2D Pixel FX·Damage Numbers Pro·Casual GUI/SFX 폴더 없음, 폰트는 LiberationSans뿐.
- 9월 2~3일 통합 SHA에 대한 정확한 `6000.3.17f1` 검증은 없다.

따라서 첫 작업은 전원 `95b9d86` 이후 문서 동기화와 baseline 확인이다. A안(PD 승인)에 따라 SD02'에서 `ProjectileSpec` 하위 호환 payload(적용 표식 목록)를 확정하며, 연쇄·제어는 넣지 않는다.

## 매일 전달 규칙

### 오전 시작

1. 자기 브랜치의 WIP를 커밋한다.
2. 최신 `origin/Seondong`을 자기 브랜치에 병합한다.
3. 오늘 카드, 막힌 것, 예상 전달 시각을 알린다.
4. 선행 카드가 없으면 추측 구현하지 않고 바로 막힘을 알린다.

### 17:00 담당자 전달

```text
[작업 전달]
카드 ID / 담당자:
브랜치와 커밋 SHA:
변경 파일:
Play Mode 확인 절차:
실제 결과:
남은 제한 또는 실패:
```

### 18:00 선동 통합

- 담당자 커밋을 저자 이력 그대로 `Seondong`에 병합한다.
- 선동은 `SampleScene` 배선과 통합 검증만 한다.
- 담당자 코드 결함이면 재현을 돌려보내고 대신 고치지 않는다.
- 통과한 `Seondong` SHA만 다음 날 태환에게 보낸다. Gate의 담당자 self-test는 당일 마감하고 태환 확인은 다음 날 끝낸다.
- 하루 카드가 미완이면 다음 카드는 시작하지 않는다. 같은 카드를 다음 날로 옮기고 P1부터 뺀다.

### QA 보고

```text
[QA]
테스트 SHA / Unity 버전 / 해상도·환경:
시나리오:
Expected / Actual:
Error·Exception:
평균·최저 FPS / 활성 적 수:
재현 절차와 로그·영상:
판정: PASS / FAIL / BLOCKED
```

## 작업 카드

### 이선동 — 기획·통합

| ID | 예상 | 결과물 | 선행 |
|---|---:|---|---|
| SD01 | 0.5일 | 완료(9/3). 기획 요약, 우선순위, 일정, 완료 기준 배포 | 없음 |
| SD01B | 0.5일 | 완료(9/4). A안 컷 반영: Brief·TenMinuteRunPlan·TeamRequests·DevelopmentPlan·이 문서 갱신, 삭제 목록과 Hidden 노드 명시 | SD01 |
| SD-STYLE | 0.5일 | `GrayboxGameFlowView` 색 6종·크기·라벨 상수를 직렬화 스타일 필드로 분리, 로직 불변. 승범 SB13 경계 확보 | SD01B |
| SD02' | 0.5일 | `ProjectileSpec` payload 필드(적용 표식 목록: 원소·중첩), 기본 10 표(조준 5 피해·쿨다운·관통, 범위 5 피해·쿨다운·반경, `FlameRing` 반경 확정), 플라즈마 판정 표(3+3 소비, 24/1.8, 연쇄 8×3, 공격 관통 3·화염1+번개1) | SD01B |
| SD03' | 0.5일 | 보스 1패턴 acceptance 표: 8:00·HP 2000·크기·이동·충돌 반경·접촉 피해, 부채꼴 3발 각도·속도·피해·주기 N초, 사망→`ReportBossDefeated`, 임시 색상 프리팹 기준 | SD01B |
| SD04' | 0.5일 | 에셋 반입 일정표(13종·담당·9/8 정오), 2D Pixel FX 3팩 반입, Noto Sans KR(OFL) 제공, QA 신규 5명 예약 | 없음 |
| SD05 | 매일 0.5일 | 담당 SHA 병합, `SampleScene` 배선, 정적·smoke 검증, 새 기준 SHA 배포 | 담당 SHA |
| SD-AREA | 1일 | Progression `AreaMagicRuntime`: 플레이어 중심 원형 즉시 피해 + 원소 표식 1, 카탈로그 피해·쿨다운·반경만 다름, `PlayerSkillSystem` 등록·`EnemyManager` 주입, 범위 5종 검증 도구 | SD02'·YS03' 통합 |
| SD06A | 0.5일 | 8·10분 경계와 보스 승패를 빠르게 재현하는 QA 절차(시간 스킵) | P0 기반 |
| SD06B | 0.5일 | 보스 전환·`DespawnAll`을 끈 전용 적 100마리·표식 stress 모드와 측정 환경 | P0 기반 |
| SD07 | 1일 | 1차 밸런스 조정과 RC 판정 | QA 결과 |

SD05 고정 항목. 9/4: `MvpIntegrationEditor` `Enemy_Basic/Fast` 경로 갱신(도구 복구). 9/7: `SpawnDirector.EnemySpawned`→`Enemy.ApplyDifficulty` 배선, Tank 슬롯 배선. 9/9: `BossSpawnRequested` 구독 배선, `MagicRuntime` 표식 목록 전달부, `PlasmaLance` 정의 에셋 생성·배선. 9/14: P1 융합 4종 8노드 Hidden 규칙. 9/17: RC0 판정, 미완 P1 비활성화.

### 맹유신 — 전투

| ID | 예상 | 결과물 | 선행 |
|---|---:|---|---|
| YS01 | 0.5일 | 완료(9/3). 최신 `Seondong` 병합, baseline 실행, 문서 수신 확인 | SD01 |
| YS02' | 1일 | 최신 `Seondong`(`95b9d86` 이후) 병합, Basic·Fast 수치 확인(10/2.0/10/1, 6/3.8/8/1), Tank EnemyData+프리팹 변형(35/1.3/20/2, 코드 0), `GameEvents.EnemyDamaged(position, amount)` 발행, 표식·배율 포함 풀 초기화 점검 | SD01B |
| YS-IMP | 0.5일 | SPUM 4팩·Fantasy Monsters [Bosses] 반입, 저장소 크기·라이선스·컴파일 확인. 9/8 정오 후면 외형은 P1 | 없음 |
| YS03' | 0.5일 | `ba459d9` 표식 저장 검증: 5원소 독립·3중첩·5초·재적중 갱신·풀 반환 초기화·`ElementMarkChanged`를 Play Mode에서 확인, 통합 충돌 해결 | YS02' |
| YS04' | 0.5일 | 원소 공통 표식 효과 1종: 총 중첩당 받는 피해 +5%(`Enemy.TakeDamage`) | YS03' |
| YS07' | 0.5일 | `ProjectileSpec` 하위 호환 확장(적용 표식 목록)과 `Projectile` 적중 시 `ApplyElementMark`. 연쇄 없음 | SD02'·YS03' |
| YS09' | 1일 | 플라즈마 3+3 판정·6표식 소비·`ElementCombatEvents` 발행(`FusionUnlocked`·`ElementMarkChanged` 구독) | YS03'·YS07' |
| YS10' | 1일 | 플라즈마 반응 실행: 즉시 피해 24 반경 1.8 + 연쇄 8×3, 플라즈마 숙련 반경 2.4·연쇄 5. 화염 시작→번개 가지→플라즈마 공격(관통 3, 화염1+번개1 payload) 끝에서 끝 검증 | YS09' |
| YS11' | 1일 | `BossSpawnRequested` 구독, 8:00 보스 생성(HP 2000, 이동·충돌·접촉 피해, 임시 색상 프리팹, 생성 이벤트 공개), 사망→`RunDirector.ReportBossDefeated()`. 일반 스폰 중단은 `SpawnDirector`가 이미 처리 | SD03' |
| YS12' | 1일 | 보스 패턴 1: 조준 투사체 부채꼴 3발 주기 N초, 플레이어 피격용 적 투사체 풀·초기화 | YS11'·SD03' |
| YS13 | 0.5일 | 적 100마리에서 표식·플라즈마·보스 상태 재사용 확인 | YS10'·YS12' |
| YS14 | 0.5일 | `[P1]` Ranged 일반 적(14/별도/투사체 10/EXP 2, 4:30), 적 투사체 풀 재사용 | YS12' |
| YS15 | 1일 | `[P1]` 원소별 고유 표식 효과 5종(카탈로그 수치) | YS04' |
| YS06' | 1일 | `[P1]` 원소 숙련 5종의 `2 → 3` 1회 발동과 재무장 | YS15 |
| YS17 | 1일 | `[P1]` 나머지 융합 4종 공격·판정·반응 | YS10' |
| YS17B | 0.5일 | `[P1]` 융합 2종 이상 동시 3+3 선판정·후소비 | YS17 |
| YS18 | 1일 | `[P1]` SPUM·Bosses 외형 적용 | YS-IMP |

### 한승범 — UI·연출

| ID | 예상 | 결과물 | 선행 |
|---|---:|---|---|
| SB01 | 0.5일 | 완료(9/3). 최신 `Seondong` 병합, 자기 UI 충돌 해결, 문서 수신 확인 | SD01 |
| SB02' | 0.5일 | `95b9d86` 병합 결과 확인: `Canvas_InGame`·UiObjectPool 기준 씬 연결 상태 점검, DamageText 풀은 P1까지 호출 없이 대기, `EnemyHpbar.cs` 삭제, `ResultUi.cs` 스텁은 SB07' 스킨으로 흡수하거나 삭제, `SoundManager`·`Sound.prefab`은 SB16 선행으로 보존. 이후 `SampleScene.unity` 변경은 전달 제외 | SB01 |
| SB-IMP | 0.5일 | Damage Numbers Pro·All In 1 Sprite Shader·Casual Fantasy GUI·Casual SFX 2팩 반입, 저장소 크기·라이선스·컴파일 확인 | 없음 |
| SB13 | 1일 | `GrayboxGameFlowView` 원소 선택·트리 패널 표현층 스킨(스타일 필드·배치·폰트·라벨). 공개 이벤트·Show/Hide/Refresh 시그니처 불변 | SB02'·SD-STYLE |
| SB07' | 0.5일 | 결과 패널 표현층 스킨, 승리·패배·시간 초과 라벨, 재시작·타이틀 연결 확인 | SB13 |
| SB08 | 1일 | `[P0]` 기존 도형·색으로 표식 1~3단계와 플라즈마 반응 최소 표시 | YS03'·YS09' 공개 이벤트, `SpawnDirector.EnemySpawned`·보스 생성 이벤트 |
| SB09 | 0.5일 | UI를 자기 작업 씬·프리팹으로 포장하고 선동에게 SHA 전달 | SB13·SB07'·SB08 |
| SB10 | 1일 | P0 UI 해상도·일시정지·재시작 생명주기 결함 수정 | 통합 SHA·QA |
| SB11' | 1일 | `[P1]` `GameEvents.EnemyDamaged`부터 숫자 생성·Fade·풀 반환까지 Damage Number 연결. 자기 풀 또는 Damage Numbers Pro 택1 | YS02' 통합·SB-IMP·SB09 |
| SB12 | 0.5일 | `[P1]` 한글 TMP 글리프 검증(Noto Sans KR) | SD04' |
| SB15 | 1일 | `[P1]` 표식 단계·맥동·플라즈마 반응 VFX 연결(2D Pixel FX) | SD04' 반입·SB08 |
| SB16 | 1일 | `[P1]` SFX·GUI 스킨(Casual 팩) | SB-IMP·SB09 |

`EnemyHpbar.cs`는 삭제한다. Damage Number는 SB02'에서 독립 WIP로 두고 Gate 1 PASS 뒤 SB11'로만 확장한다.

### 유태환 — QA

| ID | 예상 | 결과물 | 선행 |
|---|---:|---|---|
| QA01 | 0.5일 | 완료(9/3). SHA·Unity·환경·Expected·Actual·재현·로그·FPS가 있는 공용 보고 양식 | 없음 |
| QA02 | 0.5일 | 현재 `Seondong` baseline을 정확한 Unity `6000.3.17f1`로 검증 | SD01 |
| QA03 | 1일 | 시작 원소 5종·28노드·인접성·다중 레벨업 회귀. 스킨 적용 후 재확인 포함 | 통합 SHA |
| QA04' | 0.5일 | 표식 저장·5초 만료·재적중 갱신·공통 +5% 효과·풀 재사용 | YS07' 통합 SHA |
| QA05' | 0.5일 | 플라즈마 3+3 소비·재발동·반응 피해와 최소 표시 | YS10'·SB08 통합 SHA |
| QA06' | 0.5일 | 8·10분 경계, 일반 스폰 중단, EXP 차단, 시간 초과 패배 | SD06A·통합 SHA |
| QA07' | 0.5일 | 보스 처치 승리·시간 초과·사망·결과·재시작 | YS11'·SD06A 통합 SHA |
| QA08 | 1일 | 동일 QA PC, 1080p Game View, VSync off, 전용 모드에서 적 100마리·50마리 이상 표식 후 60초 평균·최저 FPS와 profiler | SD06B·통합 SHA |
| QA09 | 1일 | 신규 플레이어 5명이 각 1회 플레이한 보스 도달·승리 횟수와 막힌 지점 | SD04'·RC |
| QA10 | 매일 0.5일 | 동결 기간 RC 회귀와 P0 결함 재검증 | RC |
| QA11 | 0.5일 | 시작 원소별 조준 마법 5종의 피해·쿨다운·관통·표식 payload smoke | YS07' 통합 SHA |
| QA12' | 0.5일 | `[P0]` 범위 마법 5종의 피해·쿨다운·반경·표식 smoke | SD-AREA 통합 SHA |
| QA13 | 0.5일 | `[P1]` 융합 2종 이상 포함 시 동시 3+3 선판정·후소비 | YS17B 통합 SHA |

## 날짜별 배분

각 칸은 그날의 필수 결과 하나다. `통합`은 담당자 코드를 대신 수정한다는 뜻이 아니라 전달 SHA를 병합·배선·검증한다는 뜻이다.

| 날짜 | Gate | 이선동 | 맹유신 | 한승범 | 유태환 |
|---|---|---|---|---|---|
| 9/3 목 | 문서 배포 | SD01 완료·푸시, SD04' 시작, 00:34 팀 작업 통합 `95b9d86` | YS01 수신 확인, `dbdbd2f`·`ba459d9` 푸시 | SB01 수신 확인, `929dfbe` 푸시 | QA01 작성 |
| 9/4 금 | baseline | SD01B 완료·푸시, SD-STYLE·SD02', Editor 경로 복구 | YS02' | SB02'·SB-IMP | QA02·QA03 전반 |
| 9/5 토 | 버퍼 | 9/4 전달 SHA 통합만, VFX 3팩 반입 전용 | YS-IMP 반입 전용, 그 외 미완 카드만 | 미완 카드만 | QA02 결함 재현만 |
| 9/6 일 | 버퍼 | 공백 결정·밀린 카드 정리 | 미완 카드만 | 미완 카드만 | 새 필수 테스트 없음 |
| 9/7 월 | Core 1 | SD05(배율 배선·Tank 슬롯)·SD03' | YS03'·YS04' | SB13 | QA03 후반 |
| 9/8 화 | Gate 1 | SD05·Gate 1 판정·SD06A, 정오 에셋 반입 확인 | YS11' | SB07' | 9/7 SHA smoke: 수치·배율·표식 저장·스킨 |
| 9/9 수 | Payload·Boss path | SD05(보스 구독·payload 전달부·`PlasmaLance`)·SD-AREA 전반 | YS07', 미반입 시 YS-IMP | Gate 1 PASS·P0 대기 없음 시 SB12 | QA07' 전반 |
| 9/10 목 | Fusion base | SD05·SD-AREA 후반 | YS09' | Gate 1 PASS·P0 대기 없음 시 SB11' | QA11·QA04' |
| 9/11 금 | Fusion e2e | SD05 | YS10' | SB08 | QA12'·QA05' 전반 |
| 9/12 토 | Gate 2 | Gate 2 통합 판정만 | 미완 P0만 | 미완 P0만 | Gate 2 smoke만 |
| 9/13 일 | 버퍼 | 미완 카드·통합만 | Gate 2 PASS 시 P1 하나 가능 | Gate 2 PASS 시 P1 하나 가능 | 새 필수 테스트 없음 |
| 9/14 월 | Boss 1 | SD05(P1 융합 8노드 Hidden) | YS12' | SB09 | QA05' 후반·P0 UI 통합 회귀 |
| 9/15 화 | Stability | SD05 | YS13 | SB10 | QA06'·QA07' 후반 |
| 9/16 수 | Soft lock | SD05·SD06B | P0 결함, 여유 시 YS14 | P0 UI 결함만 | 100마리 사전 확인·P0 재검증 |
| 9/17 목 | Feature Complete 18:00 | RC0 판정, 미완 P1 비활성화 | P0 결함만 | P0 결함만 | QA07' 최종·RC0 smoke |
| 9/18 금 | Hard Freeze | P0 버그 분류·통합 | 자기 P0 버그만 | 자기 P0 버그만 | QA10 회귀 |
| 9/19 토 | 성능 | 프로파일 병목 분류 | 전투·풀 병목만 | Canvas·UI 풀 병목만 | QA08 |
| 9/20 일 | RC1 | RC1 생성 | 재현된 P0만 | 재현된 P0만 | 10분 soak 3회 시작 |
| 9/21 월 | 밸런스 | SD07, 수치만 조정 | 수치 적용 결함만 | 가독성 결함만 | QA09 시작 |
| 9/22 화 | RC2 | 알려진 문제 고정 | 회귀 결함만 | 회귀 결함만 | 경계·재시작·풀 회귀 |
| 9/23 수 | SHA lock | 최종 후보 SHA 잠금 | 자기 영역 승인 | 자기 영역 승인 | `6000.3.17f1` 최종 검수·QA09 완료 |
| 9/24 목 | 제출 | QA 통과 시에만 `main` 병합·태그 | 최종 빌드 전투 확인 | 최종 빌드 UI 확인 | 최종 빌드 승인 |

## Gate와 자동 컷

### Gate 1 — 9월 8일

필수: 전원 SD01B 포함 SHA 사용, QA02 baseline PASS(`6000.3.17f1`), `EnemySpawned`→`ApplyDifficulty` 배선, Basic·Fast·Tank 수치와 3:00 Tank 등장, YS03' 표식 저장·초기화와 YS04' 공통 효과, SB13 원소 선택·트리 스킨이 통합 SHA에서 동작. YS11' 보스 생성·처치→승리는 9/8 담당 self-test 완료, 태환 확인은 9/9. 에셋 13종 반입 상태를 정오에 확정하고 미반입 팩의 P1은 보류.

실패하면 주말 버퍼를 미완 P0에 쓰고 P1 대기열을 동결한다.

### Gate 2 — 9월 12일

필수: 조준 마법 5종의 표식 payload, 범위 마법 5종(SD-AREA), 플라즈마 3+3 판정·소비·반응 끝에서 끝(YS10'), 보스 처치 승리·시간 초과·사망·결과·재시작(QA07' 전반 PASS), 결과 패널 스킨, SB08 최소 표시가 9/11 통합 SHA에서 동작.

실패하면 9월 13일 버퍼를 미완 P0에 쓴다. YS14~YS18·SB11'·SB12·SB15·SB16을 포함한 P1과 완성 VFX를 전부 보류한다. 스킨 미완 패널은 회색 상태로 유지하고 기능을 지우지 않는다.

### Soft lock — 9월 16일

필수: YS12' 보스 패턴 1과 적 투사체 풀, YS13 100마리 상태 재사용, SB10 P0 UI 결함 0, SD06B stress 모드가 통합 SHA에서 동작.

패턴 1이 불안정하면 패턴을 비활성화하고 접촉 피해만으로 보스전을 진행한다. 보스 HP 2000은 유지하고 수치 조정은 SD07로 넘긴다.

### Feature Complete — 9월 17일 18:00

- P0가 아니거나 끝까지 연결되지 않은 기능은 비활성화한다.
- 융합 4종 8노드(폭풍·동토·묘지·지옥불의 마법·숙련)는 정의를 유지한 채 Hidden으로 잠근다. 당겨온 P1이 끝까지 연결된 경우에만 공개한다.
- Ranged·엘리트·Enemy HP Bar·제어 효과는 되살리지 않는다. 미완 P1 슬롯은 비운다.
- 9월 18일 이후 미완 기능을 완성하려 하지 않는다.
- `main`은 9월 24일 최종 QA 승인 전까지 건드리지 않는다.

## P1 당겨오기 대기열

필수 카드를 일찍 끝내고 다음 Gate가 PASS일 때만 위에서 하나씩 가져간다. 선행 대기로 당일 시작할 P0가 없는 날도 같은 조건으로 하나만 당기며, P0 선행이 풀리면 P1을 중단한다.

1. 유신: YS14 Ranged(0.5, 보스 적 투사체 풀 재사용) → YS15 원소별 고유 표식 효과 5(1) → YS06' 숙련 발동 5(1) → YS17·YS17B 나머지 융합 4 + 동시 판정(1.5) → YS18 SPUM·Bosses 외형(반입 후)
2. 승범: SB11' Damage Number(1, `GameEvents.EnemyDamaged`; 자기 풀 또는 Damage Numbers Pro) → SB12 한글 TMP(0.5, 선동 Noto Sans KR 9/8) → SB15 표식·융합 VFX(1, 선동 반입 2D Pixel FX) → SB16 SFX·GUI 스킨(Casual 팩)
3. 태환: QA12'는 P0로 승격. 포함된 융합에 QA13. QA14 삭제
4. 선동: 추가 밸런스와 VFX 매핑
5. 삭제 목록(냉기·대지 제어, 보스 제어 저항·밀치기 면역·2단계·예고 장판·파동, 연쇄 전격 연쇄, 돌진·소환 엘리트, Enemy HP Bar, 마법별 고유 VFX, 셰이더·화면 연출, 오각형 UI 신규 제작)은 이번 제출에서 되살리지 않는다. P2 등급은 없다.

에셋 13종은 팀 보유다. 9월 8일 정오까지 저장소에 반입되지 않은 팩의 P1 카드는 자동 보류한다. 담당자가 임시 에셋을 새로 만들어 대체하지 않고 P0는 기존 도형·색만 쓴다. SB08은 유지한다.

## 브랜치와 소유권

- 유신은 `Combat/**`, 적·투사체 프리팹과 데이터, `GameEvents` 발행부, Combat-side 플라즈마 판정·반응·보스만 수정한다.
- 승범은 `UI/**`, UI 프리팹·작업 씬, VFX·SFX 연결, `GrayboxGameFlowView`의 스타일 필드·레이아웃 생성 구역·라벨·폰트만 수정하고 공개 전투 이벤트를 구독한다. 이벤트 시그니처와 `PlayerSkillSystem` 호출부는 바꾸지 않는다.
- 선동은 Progression 규칙·카탈로그·`AreaMagicRuntime`·Editor 통합 도구·Docs·`SampleScene` 최종 연결·`GrayboxGameFlowView` 로직만 수정한다. 승범 표현 커밋 위에 로직 변경이 필요하면 SD05에서 병합한다.
- 태환은 제품 소스를 수정하지 않고 통합된 `Seondong` SHA만 검증한다.
- 범위 마법 공용 실행기 `AreaMagicRuntime`은 선동이 Progression에 만든다. Combat 공개 API(`EnemyManager.FindOverlappingEnemies`, `Enemy.TakeDamage`, `IElementMarkTarget.ApplyElementMark`)만 호출하고 Combat 코드를 수정하지 않는다. `AttackContext`에 `EnemyManager`가 없으므로 `PlayerSkillSystem`이 참조를 주입한다.
- 선동은 SD02'에서 `ProjectileSpec` 하위 호환 payload(적용 표식 목록: 원소·중첩)를 확정한다. YS07'에서 유신이 Combat 쪽 `ProjectileSpec`·`Projectile` 적중부를 확장하면, 같은 날 SD05에서 선동이 Progression `MagicRuntime` 전달부(카탈로그 원소·융합 부모 원소 → 표식 목록)와 `PlasmaLance` 정의 에셋을 연결·검증한다. 연쇄는 없다.
- 융합 판정·반응 실행기는 유신이 Combat 쪽에서 `PlayerSkillSystem.FusionUnlocked`·`GetOwnedFusions()`와 `IElementMarkTarget.ElementMarkChanged`를 구독한다. 보스는 `RunDirector.BossSpawnRequested`를 구독하고 `ReportBossDefeated()`를 호출한다. 유신이 Progression 코드를 고쳐 연결하지 않는다.
- `SampleScene.unity` 변경은 선동만 커밋한다. 담당자 전달 SHA에 씬 변경이 섞이면 선동이 프리팹·코드만 취한다.
- 충돌은 파일 소유자 결과를 보존한다. 다른 담당자가 같은 기능을 다시 만들지 않는다.
