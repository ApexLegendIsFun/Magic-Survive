# 선동 전용 통합 게임

## 2026-09-22 추가 통합

유신의 화염·대지·번개 Lv8 작업과 승범의 보스 HUD·사운드 슬롯 작업 병합.
전용 보스 표시명은 `수호자`, 사운드 슬롯은 13개로 반영했다.
오늘은 컴파일·씬 배선 확인만 수행. 구매 에셋 누락은 계속되며 push 보류.
상세: `Docs/DailyIntegration_20260922.md`.

## 2026-09-21 현재 상태

최신 통합 경로는 이 폴더의 `Scenes/TitleScene.unity`와 `Scenes/SampleScene.unity`다.
`Tools > Seondong Integration > 2. Play From Title`로 실행한다. 일반 Build Settings의 원본 씬과 구분한다.

유신의 실제 Dasher·Summoner·Boss 데이터와 승범의 번개·소환·돌진·보스·장판 효과, 등장 알림을 전용 씬에 연결했다.
공용 프리팹은 수정하지 않았다. 암흑 적 표현·냉기 장판 표현은 담당자 전달이 더 필요하다.
현재 Mac에는 구매 에셋 원본이 없어 그래픽·사운드 검증이 실패한다. 빌드 성공이나 입력 검사 성공만으로 정상 통합본으로 판정하지 않는다.
이번 결과와 미완료 항목은 `Docs/DailyIntegration_20260921.md`, 미발송 요청은 `Requests.md`를 참조한다.

Mac 빌드: `Tools > Seondong Integration > 6. Build Mac`.
출력: `Builds/SeondongIntegrationMac/Magic-Survive.app`.
빌드는 진단용 실행 파일도 생성하며, handoff 검증을 대신하지 않는다.

```bash
python3 Assets/Seondong/IntegrationGame/Editor/Run-BuildCheck.py --scenario death
python3 Assets/Seondong/IntegrationGame/Editor/Run-BuildCheck.py --scenario growth --element Frost
```

Python 검사는 새 근거 폴더에 `report.json`, `process.json`, `Player.log`, 화면을 보존한다.
실행 검사 PASS는 report PASS와 실제 프로세스 exit code 0이 모두 필요하다.
시각 원본 검사: `Seondong.IntegrationGame.IntegrationGraphicsCheck.ValidateAssets`.
팀 배선 검사: `Tools > Seondong Integration > 5. Validate Team Handoff`.
아래 Windows/간이 전투 기록은 과거 기록이며 현재 Mac 소스의 통과 증거가 아니다.

## 실행

Unity 6000.3.17f1에서 `Tools > Seondong Integration > 2. Play From Title`.
전용 타이틀과 게임 씬만 임시 등록하고 Play Mode 종료 시 기존 씬 목록을 복원한다.
씬 이름이 기존 씬과 같으므로 일반 Play 버튼보다 전용 메뉴를 사용한다.

Windows 빌드: `Tools > Seondong Integration > 3. Build Windows`.
출력: 프로젝트 루트의 `Builds/SeondongIntegrationFinal/Magic-Survive.exe`.
배포하려면 같은 폴더의 Data, DLL, MonoBleedingEdge도 함께 전달한다.

WASD/방향키 이동, 자동 공격. 시작 원소를 선택하고 레벨업 시 제시된 카드 선택 후 CONFIRM.
보스 처치/사망/시간 초과 결과에서 RESTART 또는 TITLE. 타이틀 QUIT으로 종료.

## 기존 작업 재사용

- `Assets/00.Scenes/SampleScene.unity`를 최초 복사했다. 이후 전용 씬을 보존하며 원본을 덮어쓰지 않는다.
- 이동·EnemyManager 풀·전투·투사체·표식·스폰·성장·RunDirector·BossSpawner는 기존 코드.
- 팀원 HUD와 GameplayHudBinder 유지. 전용 씬에서 PopupUi를 제외하고 기존 GrayboxGameFlowView로 선택·결과 표시.
- 일반 적·플레이어 외형은 기존 프리팹 참조. 투사체는 전용 Graphics 프리팹으로 시각 부분만 교체했다. 구매 에셋은 Git 제외이므로 별도 로컬 임포트 필요.
- 이전 빌드에서 SPUM SpriteEditManager의 UnityEditor using에 UNITY_EDITOR 조건이 필요했다. 해당 로컬 패키지 수정은 Git 제외다.

## 이전 간이 엘리트·보스 기록

사용자가 허용한 임시 소환 대상이다. 유신의 최종 패턴 구현 완료를 뜻하지 않는다.

| 등장 | 전용 데이터 | 재사용 대상 | 기본 HP / 속도 / 접촉 피해 / EXP |
|---|---|---|---|
| 3:00 | TemporaryCharger | Fast 외형·기존 추적 행동 | 180 / 1.8 / 20 / 15 |
| 6:00 | TemporarySummoner | Tank 외형·기존 추적 행동 | 260 / 1.0 / 12 / 25 |
| 8:00 | TemporaryBoss | 기존 Boss 외형·행동 | 2000 / 1.3 / 20 / 0 |

엘리트는 RunDirector 이벤트를 받아 각 한 번 생성하고 생성 시 난이도 배율을 적용한다.
기존 EnemyManager를 통해 등록하므로 보스 전환 시 일반 적과 함께 제거되며 EXP를 지급하지 않는다.
최종 데이터는 전용 씬 GameSystems의 IntegrationEliteSpawner에 두 슬롯으로 연결한다.
최종 보스 데이터는 BossSpawner의 bossData에 연결한다. 이후 빌드가 사용자 지정 슬롯을 덮어쓰지 않는다.
돌진·소환·보스 신규 패턴은 구현하지 않았다. 유신 전달 요청은 `Requests.md`에 기록한다.

## 자동 검사 — Computer Use 사용 안 함

- Play Mode: `Tools > Seondong Integration > 4. Auto Check Play Mode (Natural Death)`.
- Windows: `Editor/Run-BuildCheck.ps1 -Scenario death -Width 1280 -Height 720`.
- 시나리오: death / victory / timeout / layout / growth. growth는 `-Element Fire`처럼 시작 원소를 지정해 8레벨 성장 외형을 검사한다.
- 프로덕션 실행에서는 자동 입력이 꺼져 있다. 명시적 `-integration-auto` 인자 또는 에디터 검사 메뉴에서만 켜진다.
- Unity Input System 가상 장치로 입력한다. 버튼 위치·가림·범위를 검사하고 down/up 이벤트를 보낸다.
- 플레이어 직접 이동, HP/EXP 조작, 강제 승패, 시간 변경, onClick.Invoke 사용 없음.
- 정상 속도 자동 조작이므로 원하는 결과 전에 사망하면 그 시나리오는 FAIL이다. 이를 강제 통과시키지 않는다.
- 결과 확인 → 재시작 초기화 → 두 번째 자연 사망 → 타이틀 복귀 → QUIT 후 프로세스 종료까지 검사한다.
- 출력: `Logs/SeondongIntegration/<실행명>/`의 report.json, process.json, Player.log, 단계별 PNG.
- 가상 입력 검증이며 실제 물리 마우스/키보드 검증은 아니다. 간이 소환 성공과 최종 몬스터 패턴 검증을 구분한다.

## 이번 마법 외형·배경 적용

5원소 × 4단계 외형 20개, 전용 데이터·투사체·프로필, 월드 기준 4유닛 돌바닥을 적용했다.
설치·구조는 [Graphics/README.md](Graphics/README.md), 검증은 [REPORT.md](../../../Logs/GraphicsGrowth/REPORT.md) 참조.
예외 승인된 GrayboxGameFlowView의 7→8 설명 오류를 수정했다.
Unity 시각 진단과 Windows 빌드는 통과했다. 실제 화염 1→8 성장·재시작 초기화·자연 사망·타이틀·프로세스 종료도 통과했다.
사용자 요청으로 추가 구현을 멈춘 2026-09-14 01:08 시점에 나머지 원소 성장과 승리·시간 초과 검사는 실행 중이다. 전체 통과로 판정하지 않았다.
실행 중 검사는 `Logs/SeondongIntegration/graphics-*/report.json`과 `process.json`에 결과를 저장한다. PASS 판정에는 두 파일 모두 PASS가 필요하다.
이번 소스 상태·실행 파일 해시는 `Logs/GraphicsGrowth/build-source-manifest.csv`, `build-manifest.csv`에 있다. 빌드 자동 변경 설정은 patch를 보관하고 복원했다.

## 이전 통합본 검증 이력

아래 표는 **이번 그래픽 적용 전 빌드**의 기록이다. 당시 최대 레벨 결함은 이번에 수정했다.

| 검사 | 결과 | 실제 전투 시간 / 근거 폴더 |
|---|---|---|
| Windows 승리, 1920×1080 | PASS | 530.83초, 564처치, 성장 15회 / `final-victory-1080` |
| Windows 시간 초과, 1280×720 | PASS | 600.00초, 582처치, 성장 15회 / `final-timeout-1280` |
| Windows 사망, 1280×720 | PASS | 26.39초, 16처치 / `final-death-1280` |
| Unity Play Mode 사망, 1920×1080 | PASS | 30.40초 / `death-editor` |
| 공용 스킬 7→8 확정 | 당시 FAIL | 9레벨 설명 예외 / `victory-1080-final`, `timeout-1280-final` |
| 유신 최종 엘리트/보스 행동, 물리 입력 | 미검증 | 간이 데이터와 Unity 가상 입력만 사용 |

근거 폴더는 모두 `Logs/SeondongIntegration/` 아래에 있다.
Windows 세 검사 모두 재시작 초기화 → 두 번째 자연 사망 → 타이틀 복귀 → QUIT 클릭 → 실제 프로세스 종료(exit code 0)를 확인했다.
에디터에서는 같은 흐름 뒤 Play Mode가 종료되고 기존 씬 목록이 복원됐다. Editor 프로세스를 종료한 검사는 아니다.
승리·시간 초과 두 판에서 180/360초 엘리트와 480초 보스가 각 한 번 생성됐다. 보스 전환 시 기존 적 제거와 경험치 중단을 확인했다.
보스 HUD는 기존 팀원 UI를 연결해 체력·남은 시간을 표시하고 결과에서 숨긴다. 두 해상도에서 스크린샷을 확인했다.
타이틀·원소·레벨업·결과 버튼의 화면 범위와 최상위 클릭 대상을 검사했다. 최종 검사 중 포착한 Error/Exception/Assert는 0건이다.
이전 실패 로그와 수정 전 검사도 보존했다. 과거 smoke 로그나 강제 이벤트를 완주 증거로 사용하지 않았다.

주요 화면: 각 폴더의 `00-title.png`, `01-element-select.png`, `03-levelup.png`, `06-boss.png`, `07-result.png`.
사망 검사는 보스 이전에 끝나므로 `04-result.png`다. 각 `report.json`은 엔진 검사, `process.json`은 외부 프로세스 종료 증거다.

- Unity: 6000.3.17f1 (cf0352b38e81), Windows x64.
- 기준 커밋: `70e9d0ebaf5ad5e3be9dfff630212776130fc2a1` + 당시 미커밋 작업 + 이 전용 폴더. 새 커밋은 만들지 않았다.
- 실제 사용한 소스: `Logs/SeondongIntegration/final-build-source-manifest.csv`.
- 실행 파일 전체 해시: `Logs/SeondongIntegration/final-build-manifest.csv`.
- 당시 변경 목록: `final-build-worktree.txt`, 빌드 로그: `build-final-timer.log`.
- 원본 TitleScene/SampleScene은 최초 기록의 SHA256과 동일하다 (`original-scenes-check.json`). 빌드가 자동 변경한 원래 깨끗했던 URP/ProjectSettings/UnityConnect 설정은 검사 뒤 복원했다. 당시 변경은 `final-build-generated-settings.patch`에 보관했다. 전용 코드·씬은 빌드 당시 해시와 일치한다.

### 이전 결함과 현재 미검증 범위

과거 화염탄 7→8 확정 시 9레벨 설명 조회 예외가 발생했다. 이번 사용자 승인으로 CanUpgrade 검사 후 다음 레벨 설명을 조회하도록 수정했다.
최신 화염 성장 검사에서 실제 8레벨 외형과 전투 복귀를 확인했다. 위 과거 승리/시간 초과 기록을 이번 수정의 검증 증거로 사용하지 않는다.
유신의 최종 돌진/소환/보스 패턴 및 실제 물리 키보드·마우스 조작은 미검증이다.

### 검사 명령

프로젝트 루트 PowerShell:

```powershell
& Assets/Seondong/IntegrationGame/Editor/Run-BuildCheck.ps1 -Scenario victory -Width 1920 -Height 1080
& Assets/Seondong/IntegrationGame/Editor/Run-BuildCheck.ps1 -Scenario timeout -Width 1280 -Height 720
& Assets/Seondong/IntegrationGame/Editor/Run-BuildCheck.ps1 -Scenario death -Width 1280 -Height 720
```

검사마다 새로운 출력 폴더가 생긴다. 실행 중 게임 창을 닫거나 최소화하지 않는다. 가상 입력은 엔진 내부에서만 전달된다.
