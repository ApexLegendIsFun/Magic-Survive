

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



