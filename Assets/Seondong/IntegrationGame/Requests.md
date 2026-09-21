# 유신 전달 요청 — 미발송

최신 요청은 문서 하단의 2026-09-21 항목이 우선한다. 아래 최초 요청 이후 실제 전투 데이터가 전달됐다.

연락 경로를 확인하지 못해 실제 전송은 하지 않았다.

선동 전용 통합본에는 3분/6분 엘리트 이벤트 연결과 8분 보스 연결을 만들고 기존 적 프리팹을 사용하는 임시 데이터를 배치했다.

유신 전달 요청:
- 실제 돌진자·소환술사 EnemyData와 Enemy 프리팹, 행동 구현.
- 최종 보스 EnemyData와 Enemy 프리팹, 행동 구현.
- 기존 EnemyManager.Spawn/풀 반환 및 Health 사망 계약 유지.
- 엘리트 EXP 15/25, 보스 전환 제거 시 EXP 미지급, 재사용 초기화 확인.
- 프리팹·데이터 경로와 커밋 SHA, 자체 검사 결과 전달.

소환 시점과 통합 배선은 선동 담당이다. 전달된 데이터로 전용 씬 슬롯을 교체한다.

## 공용 성장/UI 결함 — 사용자 승인으로 수정

- 화염탄을 8레벨로 확정하면 GrayboxGameFlowView.RefreshSkillTree가 현재 레벨+1인 9레벨 설명을 조회한다.
- MagicContentCatalog.GetStats가 ArgumentOutOfRangeException(level)을 던진다.
- PlayerSkillSystem.ConfirmSelectedSkill의 choices.Clear / SkillPointSpent까지 도달하지 못해 레벨업 화면에 갇힌다.
- 재현: 정상 속도 자동 입력, victory-1080-final 177.24초 / timeout-1280-final 171.56초. 각 Player.log와 report.json 참조.
- 소유: 선동 성장·Graybox 로직. 사용자가 이번 그래픽 작업에서 예외 수정을 명시적으로 승인했다.
- 수정: IsOffered와 Tree.CanUpgrade가 모두 참인 카드만 다음 레벨 설명을 조회한다. 8레벨 도달 직후 남아 있는 선택 목록 때문에 9레벨을 조회하지 않는다.
- 검증 근거: 이번 `Logs/SeondongIntegration/graphics-growth-*` 검사와 `Logs/GraphicsGrowth/REPORT.md` 참조. 과거 실패 로그는 보존했다.

## 2026-09-21 미발송 요청 — 이전 미도착 본문 대체

아래 요청은 아직 발송하지 않았다. 실제 Dasher·Summoner·Boss asset은 이미 도착했으므로, 과거의 해당 asset 미도착 요청 본문은 이 블록으로 대체한다.

- 소환 사운드 원본 GUID `fa7b58d87446c20498273d3f3453ed24`와 HUD `TMP_ExBold_BlackShadow` 원본 GUID `4aa5b9036179b094c8b59f9f41224451` 전달.
- `Assets/04.ThirdParty/` 원본 package는 현재 로컬과 보조 worktree에 없다.
- 승범 담당 `EnemyDarkVisualController`와 `StatusEffectVisual`을 Enemy prefab에 부착한 완성본 전달.
- `Ground_Effect_Manager`에는 Earth만 있어 Frost Lv8 장판 표현이 누락됐다. Frost Lv8 장판을 포함한 완성본 전달.
- 각 담당자는 원본 GUID를 유지한 채 package를 재전달하거나 완성 prefab을 전달한다.
- 승범: `HudDynamicUi.UpdateLvtext`의 깨진 문자열/소스 인코딩 수정 요청. Mac 플레이어에서 U+FFFD가 사각형으로 표시됨. 숫자와 나머지 HUD 연결은 통합 담당이 유지했다.
