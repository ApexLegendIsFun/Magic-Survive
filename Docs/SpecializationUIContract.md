# 특화 UI 연결 안내

승범 담당: 아래 API로 표시·확정한다. UI에서 강화량·누적량을 따로 계산하거나 저장하지 않는다.

## 조회와 확정

| API | 용도 |
|---|---|
| `skills.GetGrowthPreview(element)` | 원소 레벨·기본 수치 전후·상태·특화 카드 스냅샷 |
| `skills.Choices` | 현재 성장 창에서 선택 가능한 모든 보유·인접 원소. 오각형 순서, 추첨 없음 |
| `levelUp.TryConfirmSpecialization(element, id)` | 보유 원소 레벨 +1, 선택한 특화 횟수 +1, 성장 기회 1회 소비 |
| `levelUp.TrySelectSkill(element)` → `levelUp.ConfirmSelectedSkill()` | 새 원소 해금. 기존 기본 레벨업 버튼도 이 경로 유지 |
| `skills.GrowthChanged` | 확정·성장 기회 소비·다음 창/전투 복귀 처리 후 조회 갱신 |
| `skills.GetSpecializationCount(id)` / `GetSpecializationBonus(id)` | 해당 특화 선택 횟수 / 합산 보너스 |

`skills`는 현재 판의 `PlayerSkillSystem`, `levelUp`은 같은 판의 `LevelUpController` 참조다. 재시작은 씬을 다시 로드하므로 새 컴포넌트에 연결한다. 정적 변수에 플레이어 참조·누적 상태를 보관하지 않는다.

### 스냅샷 필드

- `CurrentLevel`, `NextLevel`: 미보유는 0→1. MAX는 `NextLevel == null`.
- `CurrentStats`, `NextStats`: 기존 `MagicContentCatalog` 기본 피해·쿨다운·관통이다. 특화는 포함하지 않는다. 미보유의 현재 수치, MAX의 다음 수치는 `null`.
- `State`: `Invalid` / `Locked` / `Unlock` / `Upgrade` / `Max`. 잠긴 원소도 다음 기본 수치는 미리 볼 수 있다.
- `CanSelect`: 지금 확정 가능한가. 성장 창이 아니거나 기회가 없으면 false. 미보유는 카드 없이 해금 버튼 하나만 표시한다.
- `Cards`: 보유 원소의 고정 3종. MAX에서는 누적량을 계속 볼 수 있지만 모든 카드가 선택 불가다.
- 카드: `Id`, `Name`, `Description`, `SelectionCount`, `AmountPerSelection`, `Unit`, `CurrentBonus`, `AfterSelectionBonus`, `CanSelect`.
- 바로 표시할 문자열: `AccumulationDescription`, `AfterSelectionDescription`. 후자는 선택 불가일 때 `선택 불가`. 숫자 `AfterSelectionBonus`는 가정상 1회 추가 값이므로 비활성 카드의 실제 다음 보상으로 표시하지 않는다.
- 모든 카드의 `CombatStatus == NotConnected`. **전투 미연결**을 표시한다. 레벨업 기본 수치와 특화 전투 적용을 혼동하지 않는다.

### UI 호출 예제

아래는 연결 예시다. 실제 승범 UI는 이번 작업에서 수정하지 않았다.

```csharp
[SerializeField] private PlayerSkillSystem skills;
[SerializeField] private LevelUpController levelUp;
private MagicElement selected = MagicElement.Fire;

void OnEnable()
{
    skills.GrowthChanged += Refresh;
    levelUp.LevelUpOpened += Refresh;
    levelUp.LevelUpClosed += Refresh;
    Refresh();
}

void OnDisable()
{
    skills.GrowthChanged -= Refresh;
    levelUp.LevelUpOpened -= Refresh;
    levelUp.LevelUpClosed -= Refresh;
}

void Refresh()
{
    GrowthPreview preview = skills.GetGrowthPreview(selected);
    // preview.CurrentLevel / NextLevel, CurrentStats / NextStats 표시.
    // State == Unlock: 해금 버튼만 표시.
    // Cards: Name, Description, AccumulationDescription,
    // AfterSelectionDescription 표시. 버튼 interactable = card.CanSelect.
    // CombatStatus == NotConnected: '전투 미연결' 표시.
}

void ChooseElement(MagicElement element)
{
    selected = element;  // 열람만으로는 성장하지 않는다.
    Refresh();
}

void ConfirmCard(SpecializationId id)
{
    if (!levelUp.TryConfirmSpecialization(selected, id)) Refresh();
    // 성공하면 GrowthChanged로 갱신. UI가 직접 횟수를 차감하지 않는다.
}

void UnlockElement()
{
    if (skills.GetGrowthPreview(selected).State != ElementGrowthState.Unlock) return;
    if (!levelUp.TrySelectSkill(selected) || !levelUp.ConfirmSelectedSkill()) Refresh();
}
```

창 열기/닫기는 기존 `LevelUpController` 흐름을 이용한다. `GrowthChanged`를 다음 카드 자동 확정 트리거로 사용하지 않는다. 확정 콜백 도중 재진입은 거절한다. 이벤트 핸들러에서는 조회·표시만 한다.

## 카드 15종

| 원소 | ID | 이름 | 1회 증가 |
|---|---|---|---|
| 화염 | `FireDamage` | 집중 화염 | 화염탄 피해 +10% |
| 화염 | `FireExplosionRadius` | 넓은 점화 | 점화 폭발 반경 +10% |
| 화염 | `FireGroundDuration` | 오래 타는 불 | 불장판 지속시간 +10% |
| 번개 | `LightningDamage` | 고압 전격 | 번개탄 피해 +10% |
| 번개 | `LightningChainRange` | 멀리 뻗는 전류 | 연쇄 탐색 거리 +10% |
| 번개 | `LightningDischargeDamage` | 강한 방전 | 방전 피해 +10% |
| 냉기 | `FrostDamage` | 날카로운 얼음 | 얼음창 피해 +10% |
| 냉기 | `FrostFreezeDuration` | 깊은 빙결 | 빙결 지속시간 +10% |
| 냉기 | `FrostGroundRadius` | 퍼지는 서리 | 서리 장판 반경 +10% |
| 대지 | `EarthDamage` | 무거운 바위 | 바위창 피해 +10% |
| 대지 | `EarthShockwaveRadius` | 넓은 충격 | 충격파 반경 +10% |
| 대지 | `EarthKnockbackDistance` | 거센 밀침 | 밀치기 거리 +10% |
| 암흑 | `DarkDamage` | 응축된 암흑 | 그림자 구체 피해 +10% |
| 암흑 | `DarkSpreadRange` | 퍼지는 저주 | 표식 전염 탐색 거리 +10% |
| 암흑 | `DarkThreeStackAmplification` | 깊은 저주 | 3중첩 피해 증가 효과 +2%p |

합산: +10% 두 번은 +20%, +2%p 두 번은 +4%p. 반환 숫자는 각각 `20`, `4`다. 모든 증가량은 단위와 함께 읽는다. 전투 연결 시 Percent는 기준 값 × `(1 + bonus / 100)`, PercentagePoints는 기준 비율에 `bonus / 100`을 더한다. 기본 수치 성장과 특화 배율 적용 순서는 유신과 연결 시 확인하며, 현재는 특화로 전투 수치를 바꾸지 않는다.

## 규칙과 담당

- 시작 원소는 무료 1레벨, 특화 없음. 새 원소 해금은 성장 기회 1회 소비, 특화 없음.
- 보유 원소의 특화 확정은 레벨 +1과 함께 처리. 8레벨 이후 추가 특화 불가. 같은 특화를 최대 7회 선택할 수 있다.
- 기존 기본 확정 API는 특화를 선택하지 않고 레벨만 올린다. 기존 UI·자동 플레이 호환 경로다.
- 잘못된 원소/카드, 비인접, 미보유 특화, MAX, 기회 없음, 확정 중 재진입은 false. 실패 시 레벨·누적·기회는 바뀌지 않는다.
- 새로운 성장 기회가 남아 있으면 다음 정상 버튼 입력은 별도 확정이다. 버튼 애니메이션 동안 중복 입력을 막는 UI 처리는 승범 담당이다.
- **선동:** 카탈로그·누적·조회·확정 API. 원소 선택 목록은 최대 5개로 변경했다.
- **승범 요청:** 오각형 5원소, 해금 버튼, 고정 특화 3카드, 누적 수치·MAX·잠금·미연결 상태 표시 및 버튼 연결.
- **유신 요청:** 위 ID별 누적 보너스를 실제 전투 효과에 적용하고, 효과 해금·적용 상태를 제공. 미구현 효과의 반경·시간은 임의로 생성하지 않았다.
- 요청서는 이 문서로 전달한다. 외부 메시지는 발송하지 않았다.

## 검사

- Unity 메뉴: `Tools > Magic Survive > Validate Specialization API`.
- 배치: Unity 6000.3.17f1에 `-batchmode -nographics -projectPath <검사 프로젝트> -executeMethod SpecializationValidationEditor.RunBatch -logFile <로그 경로>` 전달. 검사 스스로 종료하므로 `-quit`은 붙이지 않는다.
- 전용 통합 게임 씬을 열고 Play Mode에서 실제 성장 API를 호출한다. 정상 속도 플레이 완주 검사가 아니다. 저장하지 않은 씬은 먼저 저장한다.
- 원본이 열려 있으면 `Assets`, `Packages`, `ProjectSettings`, `Docs/Mockups/ElementSkillReview/unified.js`를 임시 프로젝트로 복사해 검사한다.
- 결과: 검사 프로젝트의 `Logs/Specialization/ApiReport.txt`. 실행 결과는 아래에 기록한다.
- 실제 승범 UI 표시·클릭, 유신 특화 전투 적용: **미검증 / 담당자 연결 필요**.

### 2026-09-14 실행 결과

- **통과:** Unity 6000.3.17f1 컴파일, 특화 API Play Mode 검사 124개 단언. 목업 15종 일치 및 각 카드 확정, 합산·독립 누적, 불변 미리보기, 해금·잠금·MAX, 재진입 거절, 성장 기회 소비 후 이벤트, 씬 재시작 초기화.
- **통과:** 기존 `MvpPlayModeSmokeEditor.RunBatch` — 전투·풀 재사용·성장·기본 강화·사망·재시작. 종료 코드 0.
- 원본 에디터 종료 후 원본 프로젝트에서 검사했다. 임시 복사본 검사는 중단했다.
- 로그: `Logs/SpecializationUnity.log`, `Logs/Specialization/ApiReport.txt`, `Logs/SpecializationLegacySmoke.log` (로컬, Git 제외).
- 정상 속도 한 판 완주·최종 승범 UI·특화 전투 연동은 이번 검사 범위가 아니다.
