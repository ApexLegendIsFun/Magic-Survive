// Generate browser sprite metadata; original textures are referenced, never modified.
const fs=require('fs'),path=require('path');
const root=path.resolve(__dirname,'../../..');
const base='Assets/Untracked Asset/';
const paths={human:'SPUM/Resources/Addons/Legacy/0_Unit/0_Sprite/1_Body/Human_1.png',zombie:'SPUM/Resources/Addons/Undead/0_Unit/0_Sprite/0_Body/Zombie_1.png',orc:'SPUM/Resources/Addons/Legacy/0_Unit/0_Sprite/1_Body/Orc_1.png',hat:'SPUM/Resources/Addons/RetroHeroes/0_Unit/0_Sprite/4_Helmet/Helmet_Sorcerer.png',robe:'SPUM/Resources/Addons/RetroHeroes/0_Unit/0_Sprite/2_Cloth/Cloth_Sorcerer.png',wand:'SPUM/Resources/Addons/RetroHeroes/0_Unit/0_Sprite/6_Weapons/5_Wand/Weapon_Sorcerer.png',demon:'FantasyMonsters/Bosses/Titans/Demon/Demon01.png',frame:'CasualFantasyGUIPack/Art/Textures/UI/SubScreen/Levelup_Reward_Level_Frame.png',fire:'2D_PFX/FX/Fire/01/Sprites/01_ground.png'};
const result={};
for(const [key,p] of Object.entries(paths)){const full=path.join(root,base,p),png=fs.readFileSync(full),meta=fs.readFileSync(full+'.meta','utf8');const parts={};const re=/\s+name: (.+)\r?\n\s+rect:\r?\n\s+serializedVersion: \d+\r?\n\s+x: ([\d.]+)\r?\n\s+y: ([\d.]+)\r?\n\s+width: ([\d.]+)\r?\n\s+height: ([\d.]+)/g;for(const m of meta.matchAll(re))parts[m[1]]={x:+m[2],y:+m[3],w:+m[4],h:+m[5]};result[key]={url:'../../../'+base+p,w:png.readUInt32BE(16),h:png.readUInt32BE(20),parts};}
fs.writeFileSync(path.join(__dirname,'assets.js'),'const ART = '+JSON.stringify(result,null,2)+';\n');
console.log(Object.fromEntries(Object.entries(result).map(([k,v])=>[k,Object.keys(v.parts)])));
