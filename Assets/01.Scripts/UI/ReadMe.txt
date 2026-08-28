

역할구분 용이하게 하기 위한 폴더 유지용입니다.


현재 캔버스 구상도

Canvas_HUD_Static(가장 변동이 적은 )
└── 아이템/스킬아이콘(상시 확인가능한)


Canvas_HUD_Dynamic (가장 변동이 많은)
├── Hp Bar
└── EXP Bar /Lv Text
├── 타이머
└── 골드 / 킬수      골드 => 기획상,굳이? 

Canvas_Damage => 뱀서류 특성상 Damage 텍스트가 매우 많으므로 따로 관리
└── Damage Number

Canvas_Popup    => 레벨업 시 스킬 고르는 Ui 창, 게임오버 => 씬 전환 전 Ui(없을수도 있음) 
├── LevelUp
├── GameOver
└── Option

