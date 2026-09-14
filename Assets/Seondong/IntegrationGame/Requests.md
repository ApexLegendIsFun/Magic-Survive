# 유신 전달 요청 — 미발송

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
