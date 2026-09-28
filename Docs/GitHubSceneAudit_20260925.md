# GitHub 원본 씬·코드 대조 — 2026-09-25

후속 기준 변경: 사용자가 시간·리소스 조정을 허용하고 **기능 전체 통합**을 기준으로 확정했다. 아래는 원본 씬과 설정까지 같아야 한다고 해석했던 당시의 차이 기록이다. 시간·음원 차이를 기능 누락으로 간주하지 않는다. 최신 연결 검사와 제출 판정은 `FinalRebuild_20260925.md`를 참조한다.

판정: **팀원 핵심 코드를 재사용하고 있지만, 각 작업 씬의 내용과 설정을 전부 반영한 상태는 아니다.** 씬별 추가 설정 미반영과 실행 시 사운드 덮어쓰기를 확인했다. 이 검토는 실행 로그가 아니라 GitHub에서 받은 Git 객체, 씬 YAML, 프리팹과 코드의 직접 비교다. 시청각 실행 합격을 의미하지 않는다.

## 확인한 GitHub 기준

`git fetch origin`, `git ls-remote --heads origin`으로 확인했다. 원격 저장소는 https://github.com/ApexLegendIsFun/Magic-Survive 이다.

| 브랜치 | 최신 커밋 | 현재 HEAD 조상 여부 |
|---|---|---|
| Seondong | `5ab47f5ed729bd3a6cc6ea09cf33d4a210306afc` | 포함 |
| Yushin | `a946cb0af96c9ca2e2e7c45abdf536a0fcf3e842` | 포함 |
| Seungbum | `cd9d060e5445bdf4783a5f71987d4ab118a5ef26` | 포함 |

비교한 로컬 HEAD는 `228e6ddff6fa1fbc92320e044ad64d4e63ea7f5c`다. 병합 이력만으로 사용 여부를 판정하지 않았다. 이 감사 중 게임 소스·씬·빌드 변경은 하지 않았다.

## 실제 재사용 확인

| 원본 | 최종 연결 | 판정 범위 |
|---|---|---|
| 유신 Combat·Progression·Shared 코드 | PlayerSkillSystem → MagicRuntime/WeaponRunner → ProjectileLauncher/Projectile/Enemy | 지정 폴더 C# 46개 중 43개 Git blob 동일, 3개 수정, 삭제 0. 담당자 단독 저작 파일 수는 아님 |
| 실제 돌진자·소환술사·보스 EnemyData | 최종 씬 IntegrationEliteSpawner / BossSpawner | 세 데이터의 GUID 참조 존재. 전달된 적 프리팹과 전투 컴포넌트 사용 |
| 승범 UI C# | FinalTeamUI, FinalTeamEffects, 적 상태 표시 | 34개 중 25개 Git blob 동일, 9개 수정, 삭제 0. 테스트·구형·빈 클래스도 포함한 폴더 파일 수라 전체 실행 수로 해석하면 안 됨 |
| 승범 GameplayUI / Effect_All_Manager | FinalSubmissionWiring이 원본을 인스턴스화하고 FinalTeamUI / FinalTeamEffects로 풀어 저장 | 원본 프리팹 자체는 원격 최신 blob과 동일. 최종 씬에 풀린 구성과 필드 수정이 있으므로 프리팹 원본 동일 = 씬 내용 동일은 아님 |
| 승범 TitleScene | 빌드 첫 씬 | 원본 씬을 사용하며 버튼 연결·음원·스케일 설정 등을 통합 시 변경 |
| 선동 통합 게임·성장·그래픽 | 빌드 두 번째 씬 | 기존 전용 통합 씬과 Graphics/Magic·Profiles를 사용 |

최종 씬 GUID 의존 경로와 원격 작업 씬의 스크립트 집합도 비교했다. 승범 작업 씬에서 최종 의존 경로에 없는 프로젝트 스크립트는 `GrayboxGameFlowView`와 `TestEnemySpawner`다. 유신 씬에는 추가로 `BossHpbar`가 있으나 클래스 본문이 비어 있다. 이들은 각각 팀원 최종 UI, 실제 스폰 흐름, BossHudUi로 대응된다. 디버그 트리거 제외는 기존 제출 계획에 명시된 범위다.

`LevelUpSlotMapper`는 씬 컴포넌트가 아닌 정적 코드이며 PopupUi의 두 호출 지점에서 사용된다. 따라서 씬 GUID에 없다는 이유만으로 누락 판정하지 않았다. 이전 `LightningChainEffectPlayer`는 팀원이 확장한 `ChainReactionEffectPlayer`로 연결됐다. 이 대조는 코드 종류와 참조를 확인하며, 모든 인스턴스의 활성 상태·실행 경로까지 증명하지 않는다.

## 원본 씬과 다른 내용 — 실제 확인된 항목

### 1. 승범 씬의 등장 안내 추가 설정 미반영

[원격 작업 씬](https://github.com/ApexLegendIsFun/Magic-Survive/blob/cd9d060e5445bdf4783a5f71987d4ab118a5ef26/Assets/00.Scenes/Work/SampleScene_Seungbum.unity#L852)에는 GameplayUI 인스턴스에 별도로 저장된 값이 있다. 최종 통합 도구는 그 씬 인스턴스가 아닌 GameplayUI 프리팹에서 시작했으므로 아래 값은 프리팹 기본값으로 남았다.

| 항목 | 승범 작업 씬 | 최종 게임 씬 |
|---|---|---|
| bossTitle | 보스가 접근합니다... | 보스가 등장했습니다. |
| enterDuration | 0.4초 | 0.35초 |
| holdDuration | 3초 | 1.2초 |
| exitDuration | 0.4초 | 0.3초 |

원격 원본 852~875행과 `Assets/Seondong/IntegrationGame/Scenes/SampleScene.unity` 10969~10974행의 직접 비교다. 기능 코드가 사라진 것은 아니지만 작성자가 씬에 저장한 설정은 동일하게 반영되지 않았다. 이 차이를 의도적인 최종 선택으로 설명하는 기존 대조 기록도 없었다.

### 2. 승범 레벨업 음원을 런타임에 덮어씀

승범 작업 씬 800행의 `soundClip[1]`은 GUID `9ab7be34eb9cd374e9c28db4d6f9f8bf`인 `Bing02.wav`다. 최종 SoundManager 직렬화에도 이 값이 남아 있지만, `AssetPresentationFeedback.Start()`가 `soundClip[(int)SFXType.LevelUp]`을 `levelUpClip`으로 바꾼다.

최종 씬 995행의 `levelUpClip`은 GUID `5374ad241068c4c07b82ccc323cee8a9`, `USER_INTERFACE_Bubble_Zoom_01.wav`다. 즉 씬 파일의 음원 슬롯이나 AudioSource 활동 로그만 보면 원본 소리가 사용되는 것으로 잘못 판단할 수 있다. 원본 씬 음원 보존 요구에 대한 불일치다. 실제 청취 검사는 이번 감사에서 하지 않았다.

### 3. 소환 음원도 작업 씬과 프리팹 값이 다름

승범 씬 804행의 `soundClip[7]`은 `Wup21.wav`다. 최종 씬 슬롯 7은 `ShortIntro41.wav`, 슬롯 9는 `Wup21.wav`다. 현재 열거형에서 7은 Summon, 9는 Dash다. 최신 프리팹과 작업 씬 중 어느 쪽 설정을 최종 기준으로 삼았는지 명시해야 하며, 둘 다 그대로 사용했다는 주장은 성립하지 않는다.

### 4. 마법 데이터·투사체는 전용 통합 그래픽 버전을 사용

작업 씬들이 참조하는 `Assets/03.Data/Magic`의 5개 정의와 `Assets/02.Prefabs/Presentation/Projectile_*` 대신, 최종 씬은 `Assets/Seondong/IntegrationGame/Graphics/Magic` 및 성장 외형 프리팹을 참조한다. 기존 제출 계획의 성장 외형 유지와 부합하지만, 원본 작업 씬의 에셋을 전부 그대로 쓰는 상태는 아니다. 공통 전투 코드를 재사용하는 것과 에셋 인스턴스를 동일하게 쓰는 것을 구분해야 한다.

## 작업 씬의 최신성

| 씬 | 해당 기준 브랜치에서 마지막 씬 변경 |
|---|---|
| SampleScene_Seungbum | 2026-09-22, `a254ff1`, hansb123 |
| SampleScene_Yushin | 2026-09-08, `1c58921`, aoddbtls123 |
| SampleScene_Taehwan | 2026-09-08, `839d3df`, leeseondong의 작업 씬 복사 커밋 |
| UI_SampleScene | 2026-09-08, `e738e47`, hansb123 |

유신 최신 기능은 이후 코드·데이터 커밋에 들어 있으므로 씬 수정 날짜만으로 최신 기능 누락이라고 판단하지 않았다. GitHub 원격에는 별도 Taehwan 브랜치가 없고 태환 작업 씬은 생성 이후 수정 커밋이 없다. GitHub 밖의 미커밋 작업 존재 여부는 이 자료로 판단할 수 없다.

## 기존 검증의 한계와 남은 판정

기존 `scene-content-coverage.csv`는 컴포넌트 종류가 최종 씬에도 있는지를 중심으로 작성돼 있었다. 씬 인스턴스의 개별 설정, 런타임에 바뀌는 값, 모든 시각·음향을 보장하지 못한다. 이번에 확인된 위 차이들은 기존 “전부 반영” 표현을 철회할 구체적인 근거다.

GitHub 내용으로 확정할 수 있는 것은 **최신 커밋 포함, 주요 코드 재사용, 위 설정 불일치**다. 모든 작업물이 정상 실행된다는 결론은 내리지 않는다. 별도로 재현된 UnityPlayer.dll 종료 오류도 미해결이므로 제출 보류를 유지한다.

감사 원자료: `Logs/GitHubSceneAudit-20260925/summary.json`, `code-comparison.csv`, `scene-script-comparison.csv`, `scene-overrides.csv`, `source-assets-outside-final-closure.csv`, `final-script-instances.csv`, 재현용 `audit.py`.

GUID 추적은 Git에 있는 에셋 기준이다. 로컬 구매 에셋 내부나 런타임 동적 생성은 이 추적만으로 전부 펼치지 않는다. 또한 프리팹을 풀어 넣으면 원본 프리팹 GUID가 사라질 수 있어 `source-assets-outside-final-closure.csv` 전체를 누락 목록으로 사용하면 안 된다. 해당 CSV는 후속 비교 후보 목록이다.
