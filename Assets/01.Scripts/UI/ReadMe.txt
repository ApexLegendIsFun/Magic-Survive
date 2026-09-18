

Damage_Number를 호출하려면 

Enemy가 받은 데미지,(Player가 가한 데미지) 위치정보(Transform.position)으로 호출하시면 됩니다.*테스트 할때는 Enemy의 TakeDamage에서 amount와 transform.position을 활용했습니다. * 

(UiManager의 EnemyDamageTextUi() 함수)

현재 캔버스 구상도

Canvas_HUD_Static(가장 변동이 적은 )
└── 아이템/스킬아이콘(상시 확인가능한)
  + 스킬포인트


Canvas_HUD_Dynamic (가장 변동이 많은)
└── EXP Bar /Lv Text
├── 타이머
└── 킬수   


Canvas_World_Space (Damage Number&Sound(피격음,공격사운드 등) => 오브젝트 풀링 사용예정)
└── Damage Number (뱀서류 특성상 Damage 텍스트가 있을 경우 매우 많으므로 따로 관리)
    Hp Bar (플레이어, 몬스터) 플레이어의 경우, 고정 캔버스 Hpbar 또는 WorldSpace hp bar 둘중 하나 (미정)
    


Canvas_Popup    => 레벨업 시 스킬 고르는 Ui 창, 게임오버 => 씬 전환 전 Ui(없을수도 있음) 
├── LevelUp
├── GameOver
└── Option

09_09 x 
09_10
PopupUi_Canavas 수정 및 보완

Boss Hp bar, Boss Name, Boss Timer 추가 (Ui만)
Hud를 수정, 및 보완했습니다.
게임시작 => 원소 선택할 때  화염탄을 제외한 다른 스킬을 선택할 수 없는 문제를 수정했습니다.
무기를 선택하면 하단의 Hud에 자신의 스킬이 뜨는것을 확인했습니다.
다른사람의 작업브렌치의 Sprite가 적용하지 않는 부분은 확인중입니다.

09_11
Boss Hp bar, Boss Name, Boss Timer 추가완료
Hud를 수정, 및 보완했습니다.
대지속성의 스킬 아이콘을 추가했습니다.
현재 타이틀 씬의 사운드매니저는 사운드를 추가하기만 하면 되도록 만들었습니다. 
아직 레벨업취소 => 스킬포인트 로직은 확인되지 않아 추가하지 않았습니다.

09_14
레벨업 관련 Ui를 기획에 맞춰 전부 수정완료했습니다.
획득 연동 및 잠김, 첫 해금 관련해서 전부 정상작동 확인했습니다.

하단의 현재 획득 원소를 원소 전부 확인 가능하고, Lv.0 => 1 과 같은 표기로 변경완료했습니다.

현재 Result Panel은 연동 미완료 상태입니다. 


09_15

ElementReactionEffectPlayer 클래스  = Player 공격 이펙트 매니저 
EnemyFrozenVisualController 클래스 = 빙결효과 전문 매니저 


Git 병합 전, Git브랜치의 유신님 스크립트의 변동사항을 참고하여 

작업에 맞춰 스크립트를 작성, 이펙트 작업을 진행했습니다.
빙결효과의 경우 Enemy에 남아있는 일정시간 지속되는 상태이므로, 
시각적으로 표현하기 위해 EnemyFrozenVisualController를 Enemy에 붙인 후 FrozenChanged 를 구독하는 형태로 제작했습니다.

(Enemy 프리펩에 넣어야 작동하는 스크립트 입니다.)

이펙트 작업의 테스트는 선동님과 유신님의 작업물이 합쳐진 후,  테스트하고 나서  사용방법까지 배포하거나 제가 프리펩화 하겠습니다.

선동님께 요청사항 : ResultUi(결과화면) 에 각 원소에 레벨을 추가해야하므로, Runresult, RunDirector에 추가 부탁드립니다. 

또한, ResultUi는 게임(런) 종료 시,  ShowResult(RunResult result)를 통해 객체를 넘겨주시면 호출 가능합니다. 

작업들 전부 병합되는대로 바로 진행하겠습니다.


09_ 16 

*제 요청으로, 제가 직접 선동님의 Magic Runtime 클래스 46줄~이하 부분을 수정하였습니다.*
*제 요청으로, 제가 직접 유신님의 SkilLevel =3 에서 1로 조정했습니다. (테스트 용이하기 위함, 일시적인 조정입니다.)*

이펙트 및 사운드 : 점화 및 동결의 사운드, 이펙트작업을 했습니다.

연쇄번개의 이펙트 부분 처리를 미리 작업했습니다. 번개는 다른 로직으로 작동하기 떄문에 별도의 매니저를 사용합니다.

제 작업장소에있는 Stack_Sound_Effect_Manager와 Stack_Effect & Sound Tester 두가지를 활용하시면 다른 씬에서도 활용 가능합니다.

프리펩의경우, Prefabs \ Effect_Manager 안에 있습니다.

(테스트매니저의 경우, F1을 누르면 해당 위치에 사운드와 이펙트 작동하는 테스터입니다.)

09_17

대지 이펙트 연결 완료했습니다. 
대지의 3타공격의경우 밀치기의 이펙트는 구현하지않고 충격파만 구현했습니다.

Chain_Lightning 이펙트 매니저의 버그를 수정보완했습니다.
연쇄번개의 경우 , 피격 대상에게 별도의 터지는 이펙트와 연결되는 대상(LineRenderer) 두가지를 이용하여 이펙트가 발생하므로
별도의 Effect_Manager를 사용했습니다.


소환술사 이벤트 연결 전 스크립트를 미리 완성,  테스트 및 연결 준비 완료했습니다.

암흑공격:  이벤트를 연결할 수단이 없어 대기중입니다. 만약 만들어진다면 각 Enemy에 별도의 스크립트를 부착해야할수도 있을거같습니다.(다른 효과와달리, Enemy에 계속해서 머물러있어야하는 이펙트이므로.)
보스 Hud : 이벤트 연결 대기중입니다.
결과 Ui : 이벤트 연결 대기중입니다.

이펙트 테스트하실분들은  Prefabs / UI / Effect_Manager / Effect_All_Manager 프리펩 사용하시면 됩니다.

기술들이 전부 3타에 효과가 터지므로 Enemy의체력을 늘려야 발동합니다.(테스트 완료)

09_18

원소 반응 (5원소 전부 연결)

*유신님의 5원소 공격 관련 연결사항*

화염 — 점화(3레벨) 이펙트/사운드
냉기 — 빙결(3레벨) 이펙트/사운드
대지 — 충격파(3레벨) + 지진 구역(5레벨) GroundAreaEffectPlayer
번개 (유신님의 디버그확인, 요청사항)— 연쇄(3레벨) LightningChainEffectPlayer + 방전(5레벨) ElementReactionEffectPlayer 항목 추가
암흑 — IsDarkAmplified/DarkAmplifiedChanged 반영, StatusEffectVisual + EnemyDarkVisualController로 분리 설계해서 완료
           =>이부분은 여전히 모든 Enemy 프리펩에 False 되어있는 애니메이션 오브젝트를 넣고, 스크립트를 직접 넣어야 작동합니다. 
                 기획상 해당 방식으로 동작하게 확정되면 제가 직접 넣겠습니다. 현재는 테스트 까지 완료한 상태입니다.(제가 임의로 프리펩에 넣지는 않았습니다. 따로 테스트했습니다.)

엘리트 / 보스

소환술사 — 예고(SummonTelegraph) + 버스트 연출 SummonEffectPlayer
돌진자 — 예고선 DashTelegraphEffectPlayer (직선 + 폭 있는 레인 스타일)
엘리트/보스 등장 패널 — EliteAnnouncementUi (오른쪽→중앙→왼쪽 슬라이드)
보스 충격파 — BossShockwaveEffectPlayer, 예고 링/버스트 스케일 보정(telegraphScaleMultiplier/burstScaleMultiplier) 분리해서  해결 완료



