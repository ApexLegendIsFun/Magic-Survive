# 최종 제출 통합 — 2026-09-25

현재 상태: **v6 재빌드 완료 — 사용자 지정 최소 검사 통과**. 제출 파일은 `Builds/FinalSubmission-Windows-20260925-v6.zip`. Unity Windows 접근성 종료 충돌을 우회 수정했고 새 실행 파일과 ZIP 압축 해제본 모두 종료 코드 0을 확인했다. 최신 근거·검사 범위·해시는 `Docs/FinalRebuild_20260925.md`에 기록했다. 아래 과거 실패 기록은 이력이며 최신 v6 판정으로 대체된다. 전체 장시간 재검사와 사람 전체 플레이는 수행하지 않았다.

GitHub 원본 재대조에서 등장 안내 시간과 레벨업 음원이 작업 씬과 다른 점을 기록했다. 이후 사용자가 시간·리소스 변경을 허용하고 기능 전체의 통합을 기준으로 확정했다. 따라서 이 차이를 기능 누락으로 취급하지 않는다. 원본 비교는 `Docs/GitHubSceneAudit_20260925.md`, 최신 재빌드 판정과 기능 대조는 `Docs/FinalRebuild_20260925.md`를 참조한다.

2026-09-25 플레이어 표시 재검사: 제출 폴더 실행본을 Windows 창에서 직접 클릭해 시작했고 중앙 마법사 렌더링을 확인했다. 추가 자동 실행 `Logs/SeondongIntegration/player-visibility-20260925-02/03-combat.png`에서도 이동 후 플레이어가 보였다. 사용자가 보고한 비표시 현상은 이번 실행에서 재현되지 않아 해결로 판정하지 않는다.

추가 실행 01은 가상 키보드 이동 검사 실패, 실행 02는 이동·사망·재시작·타이틀·종료 버튼까지 진행했다. 두 실행 모두 프로세스 종료 코드 `-1073741819`이며 Windows 이벤트에서 `UnityPlayer.dll`의 `0xc0000005`를 확인했다. 종료 오류 원인은 아직 미확정이다. 기존 ZIP은 그대로 보존하며 재검증 전 제출 가능으로 취급하지 않는다. 원본 검사 기록은 덮어쓰지 않았다.

## 통합 기준

- Unity 6000.3.17f1, Windows x64, Development Build 해제.
- 원격 선동 `5ab47f5`, 유신 `a946cb0`, 승범 `cd9d060` 포함. 팀 저자 이력 유지.
- 사용자의 기존 변경과 복구 씬은 별도 백업 및 stash로 보존하고 작업 브랜치에 다시 적용했다.
- 구매 에셋 1.77GB는 로컬 백업했다. 공개 저장소에는 추가하지 않는다.
- 타이틀: `Assets/01.Scripts/UI/TitleScene.unity`.
- 게임: `Assets/Seondong/IntegrationGame/Scenes/SampleScene.unity`.
- 일반 Build Settings와 전용 빌드 메뉴가 같은 두 씬을 사용한다.

## 작업 내용 대조

| 담당 | 전달된 내용 | 최종 반영 |
|---|---|---|
| 유신 | 5원소 Lv1~8, 표식·반응·전염·장판·낙뢰·낙석·처형, 특화 15종 | 기존 Combat와 MagicRuntime, 전달된 데이터 유지 |
| 유신 | 돌진자·소환술사·보스 패턴, 보스 면역·2페이즈, 적 겹침 분리 | 실제 EnemyData로 통합 소환기 연결 |
| 승범 | 타이틀·설정, 오각형 원소 선택·특화 카드 | 최신 TitleScene 및 GameplayUI의 실제 PopupUi 사용 |
| 승범 | 결과·HUD·보스 HUD·등장 안내·피해 숫자·사운드 | 기존 UI 컴포넌트와 풀을 실제 이벤트에 연결 |
| 승범 | 반응·연쇄·돌진·소환·보스·장판 연출 | Effect_All_Manager 재사용, 중복 관리자는 제거 |
| 승범 | 빙결·암흑 상태 표시 | 기존 컨트롤러와 전달된 오버레이를 적 프리팹에 연결 |
| 선동 | 성장·런 진행·소환 시점·마법 외형 20개·배경 | 기존 통합 게임 보존, 팀원 UI와 함께 동작 |
| 태환 및 작업 씬 | 각 작업 씬의 구성과 참조 | 씬별 컴포넌트 대조표로 최종 의존성 포함 여부 확인 |

세부 5열 대조표: `Logs/FinalSubmission/20260925-162246/implementation-coverage.csv` (담당자 / 구현 내용 / 원본 위치 / 최종 연결 위치 / 검증 근거).

작업 씬의 TestEnemySpawner, SimpleProjectileAttack 및 DebugTrigger는 출시 게임에 넣지 않는다. 실제 생성·공격·연출은 기존 런 이벤트로 작동한다. 과거 Graybox 화면과 IntegrationBossHud는 최신 팀원 PopupUi/ResultUi/BossHudUi로 연결했다. 미착수 기획 기능을 새로 구현한 것으로 표시하지 않는다.

## 제출 결함 최소 수정

- 비어 있던 PlayerInput 기본 맵을 Player로 연결해 이동 복구.
- 같은 Canvas에 붙었던 PopupUi·ResultUi를 각각 담당 패널로 연결.
- 결과 이벤트를 ResultUi로 연결하고 실제 원소 레벨 표시.
- BossHudUi는 이벤트 수신 컴포넌트를 유지하고 표시 패널만 숨김.
- SoundManager의 씬 전환 중복·자식 DontDestroyOnLoad·널 참조·슬롯 범위 처리 보완.
- 빙결 이벤트 구독·풀 반환 표시 초기화 연결.
- 누락 원소 아이콘은 기존 보유 원소 이펙트의 스프라이트로 연결.
- 화염 장판, 암흑 전염, 대지 낙석의 기존 공개 이벤트에 기존 연출 프리팹 연결.
- 씬에 붙지 않았던 ElementMarkStackVisualController를 실제 적 6종에 연결. 기존 아이콘·숫자 폰트로 표식 표시.
- 비어 있던 BGM 슬롯을 보유 Music_Curious_Piano / Music_Run_Piano로 채우고 반복 재생. 암흑 증폭·돌진 소리도 기존 슬롯 사용.
- 무한 맵에서 절대 Y 좌표 때문에 캐릭터가 배경 아래로 숨던 정렬을 카메라 기준 1~4000 범위로 수정.
- 우측 처치 수의 화면 잘림, 긴 소수 피해 숫자, 효과음 슬라이더의 UI 소리 누락 수정.
- 인계서의 미착수 보스 2페이즈 외형(1.15배)을 기존 BossPhaseController 상태와 AssetVisualLifecycle에 연결. 전투 패턴·수치 유지.
- 과거 생성 도구의 팀원 UI 삭제·낡은 연출 검사 제거. 필수 참조·최신 해금 레벨·마법 외형 검사 유지.
- 특화 API 검사의 오래된 미적용 기대값을 실제 +10% 적용값으로 변경.

## 검증과 증거

최종 로그 위치는 프로젝트 루트 `Logs/final-submission-current.txt`에 기록한다.

- `scene-inventory.txt`, `scene-content-coverage.csv`: 원본 씬별 구성과 포함·제외 근거.
- `final-asset-dependencies.txt`, `final-component-types.txt`: 최종 씬에서 참조하는 실제 자산과 컴포넌트.
- `source-manifest.csv`, `asset-manifest.csv`, `build-manifest.csv`: 소스·로컬 에셋·전체 빌드 파일 SHA256.
- 성장 API: 124개 검사 통과. 누적·거부·중복·재진입·연속 레벨업·MAX·재시작 검사. 실제 완주의 대체 증거는 아니다.
- 자연 플레이: Unity Input System 가상 키보드·마우스 사용. 화면의 실제 버튼에 down/up 입력을 보내며 시간·체력·경험치·승패를 조작하지 않는다.
- 성능: 별도 Editor 진단. 1080p RenderTexture, 99 일반 적+보스, 표식·자동 공격·합성 연출 부하. 자연 플레이 결과와 분리한다.
- 과거 실패 및 중단 실행은 보존하며, 최종 빌드 통과로 계산하지 않는다.

물리 키보드·마우스를 사람이 조작한 검사는 별도로 기록해야 한다.

## 빌드 식별·보존

- 런타임 소스 커밋: `9d565f68f3f0795adea1d858110881e4f45f1e58`.
- 작업 브랜치: `codex/final-submission-20260925-162246`. 원격 push는 하지 않았다.
- 실제 빌드 로그: `Logs/FinalSubmission/20260925-162246/build-08.log`.
- 기존 사용자 변경은 UniversalRP.asset, ProjectSettings.asset, UnityConnectSettings.asset, `_Recovery`에 남아 있다. 설정 값은 백업과 동일하다. UnityConnectSettings는 줄바꿈만 정규화됐다.
- 원본 파일·복구 씬·구매 에셋 백업: `Logs/FinalSubmission/20260925-162246/Original`. 원본 stash도 보존했다.
- `Assets/Untracked Asset`의 보유 에셋과 로컬 패키지 코드에 의존한다. SPUM의 UnityEditor 조건부 코드 등 로컬 패키지 코드도 소스 해시에 포함했다. 실행 ZIP은 필요한 런타임 리소스를 포함하므로 원본 구매 에셋 폴더가 필요 없다.
- `source-manifest.csv`: 소스·설정·로컬 패키지 코드 1,144개 파일. `asset-manifest.csv`: 최종 의존 에셋·메타 648개. `build-manifest.csv`: 전체 빌드 파일 SHA256.
- 원본 구매 에셋 `FantasyMonsters/Bosses/Titans/Demon/Demon01.prefab`의 비활성 제작 가이드 `BossRef`에만 사용하지 않는 스프라이트 GUID가 없다. 실제 보스·그림자·애니메이션 필수 참조에는 영향 없음. 원본을 임의 수정하지 않고 예외로 기록했다. 최종 씬 필수 참조와 Missing Script 검사는 통과했다.

## 검증 구분

- **자동 입력**: 최종 Windows 실행 파일에 Unity 가상 키보드·마우스로 입력. 시간 배속, 경험치 추가, 체력 변경, 강제 승패 없이 진행.
- **보조 전투 검사**: 내구도가 높은 적과 성장 상태만 준비한 뒤 기존 자동 공격을 25초씩 관찰. 5원소 피해·표식 UI·연출·빙결·암흑·처형·풀 반환 확인. 정상 완주와 별개다.
- **성능 부하**: Editor Play Mode, 1920×1080 RenderTexture, 일반 적 99+보스 1, 5원소 표식 표시와 합성 연출, 자동 공격. 5초 준비 후 20초 측정. RTX 3080 Ti / Direct3D12. 별도 스모크 실행 1개가 동시 구동됐다.
- **사람 물리 입력 검사**: 0회. 자동 결과를 사람이 조작한 결과로 표시하지 않는다. 저장된 화면은 직접 시각 검토했다.
- 이전 후보의 이동 실패, 화면 정렬 결함, 배경음악 누락, 두 번째 사망 유도 제한시간 초과·중단 기록은 보존했다. 최종 통과 수에 포함하지 않는다.

## 제출 파일

- 폴더: `Builds/FinalSubmission-Windows-20260925`.
- ZIP: `Builds/FinalSubmission-Windows-20260925.zip` (59,504,358 bytes).
- ZIP SHA256: `edc2b7ea32b3358f9baf6b9f61097fa9fc2ec4327a5d0384c3acb9c5d3ce5b93`.
- `Magic-Survive.exe`, Data, DLL, D3D12, MonoBleedingEdge, 한글 조작 안내, BuildInfo.json, SHA256SUMS.txt 포함. BurstDebugInformation_DoNotShip은 제외.
- 별도 압축 해제 폴더: `Builds/FinalSubmission-Extracted-20260925/FinalSubmission-Windows-20260925`.
- ZIP 무결성 검사 및 압축 해제 파일 190개 SHA256 일치. 이 폴더를 작업 디렉터리로 독립 실행해 게임 진입·사망·재시작·타이틀·종료(코드 0)를 확인했다.

## 자동 검사기의 알림 슬라이드 오탐

최종 실행 파일의 원본 `report.json`은 일부 실행에서 `Clipped UI text: Text (TMP) at elite-1/elite-2/boss`를 기록했다. 해당 글자는 `FinalTeamUI/Canvas_InGame/Encounter_Massage/Canvas/Panel/Image/Text (TMP)`의 고정 “경고!” 문구다. 원본 `EliteAnnouncementUi.PlayAnnouncement/AnimateX`가 패널을 화면 밖 +1400에서 중앙으로, 이후 -1400으로 이동시키는 동안 찍힌 정상 상태다. 같은 패널의 Warn_Massage는 검사에서 제외했지만 형제 문구가 남아 발생했다.

원본 보고서·스크린샷·프로세스 결과는 수정하지 않았다. `Review-BuildCheck.ps1`이 위 세 시점의 정확한 메시지만 오탐으로 분리해 `review.json`을 만든다. 다른 화면 잘림·런타임 오류·잘못된 결과·재시작 실패·비정상 종료는 계속 실패다. 승인된 오탐과 원본 FAIL 여부도 각 review.json에 기록된다. 판정 도구 수정은 실행 파일의 게임 동작을 바꾸지 않는다.

100적 부하 최종 결과: 평균 **74.79FPS**, 평균 **13.370ms**, p95 **14.717ms**, 1,497 프레임. 표식 UI까지 포함한 최종 구성으로 측정했으며 60FPS 기준을 통과했다.

## 최종 실행 결과

| 검사 | 해상도 | 결과 | 전투 시간 | 종료 | 판정 |
|---|---|---|---:|---:|---|
| final-smoke-08 | 1280x720 | Defeat | 14.69초 | 0 | PASS |
| final-extracted-01 | 1920x1080 | Defeat | 20.24초 | 0 | PASS |
| final-victory-03 | 1920x1080 | Victory | 499.71초 | 0 | 오탐 분리 후 PASS |
| final-timeout-03 | 1280x720 | Timeout | 600.00초 | 0 | 오탐 분리 후 PASS |
| final-growth-Fire-02 | 1280x720 | Defeat | 239.96초 | 0 | 오탐 분리 후 PASS |
| final-growth-Lightning-02 | 1280x720 | Defeat | 219.24초 | 0 | PASS |
| final-growth-Frost-02 | 1280x720 | Defeat | 220.89초 | 0 | 오탐 분리 후 PASS |
| final-growth-Earth-02 | 1280x720 | Defeat | 223.92초 | 0 | 오탐 분리 후 PASS |
| final-growth-Dark-02 | 1280x720 | Defeat | 224.82초 | 0 | 오탐 분리 후 PASS |

모든 실행에서 재시작 초기화, 타이틀 복귀, 실제 QUIT 버튼과 프로세스 종료를 확인했다. 5원소 모두 최대 8레벨, 실제 투사체 외형 4단계, 풀 재사용을 확인했다. 보스전에서는 5발 부채꼴 탄, 소환, 2페이즈 충격파·1.15배 외형을 관찰했다. 엘리트는 180초·360초 각 1회, 보스는 480초 1회, 시간 초과는 정확히 600초다.

최종 실행 기록의 런타임 예외·한글 누락·고정 UI 잘림·버튼 가림 미해결 항목은 0건이다. 원본 후보 기록은 보존했다. 종합 판정 파일은 `Logs/FinalSubmission/20260925-162246/validation-summary.json`이다. 사람 물리 입력 검사를 완료했다고 주장하지 않는다.

`DamageText`와 `SoundObjectPool`의 빈 템플릿, 이전 `FusionLine`/`BossHpbar`/`LightningChainEffectPlayer`는 해당 기능을 담당하는 최신 팀원 구현(DamageUi/UiObjectPool, FusionLineManager, BossHudUi, ChainReactionEffectPlayer)으로 대조했다. 빈 템플릿을 구현된 기능으로 세지 않았다.

검사 판정 도구는 이 최종 씬 SHA256과 일치할 때만 알림 슬라이드 오탐을 허용한다. 다른 씬 버전이나 다른 오류는 자동 면제하지 않는다. 이 도구 변경은 빌드 이후 외부 PowerShell 검사기에만 적용됐다. 빌드한 C#·씬·프리팹은 해시 대조상 그대로다.
