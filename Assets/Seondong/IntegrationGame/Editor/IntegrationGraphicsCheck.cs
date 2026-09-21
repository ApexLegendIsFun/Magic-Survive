using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Seondong.IntegrationGame
{
    [InitializeOnLoad]
    public static class IntegrationGraphicsCheck
    {
        private const string Key="Seondong.GraphicsFixture";
        static IntegrationGraphicsCheck()
        {
            EditorApplication.playModeStateChanged+=State;
            EditorApplication.update+=()=> { if(SessionState.GetBool(Key+".Running",false)&&EditorApplication.timeSinceStartup-SessionState.GetFloat(Key+".Started",0)>150) { Debug.LogError("Graphics fixture exceeded deadline."); EditorApplication.Exit(2); } };
        }
        public static void InstallAndPreview(){IntegrationGraphicsEditor.Install();Run();}
        public static void Run()
        {
            ValidateAssets();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            EditorSceneManager.playModeStartScene=null;
            SessionState.SetBool(Key,true);SessionState.SetBool(Key+".Running",true);SessionState.SetFloat(Key+".Started",(float)EditorApplication.timeSinceStartup);EditorApplication.EnterPlaymode();
        }
        private static void State(PlayModeStateChange state)
        {
            if(state!=PlayModeStateChange.EnteredPlayMode||!SessionState.GetBool(Key,false))return;
            SessionState.SetBool(Key,false);
            var runner=new GameObject("Graphics Fixture").AddComponent<IntegrationGraphicsPreview>();
            runner.profiles=MagicContentCatalog.PentagonElements.Select(e=>AssetDatabase.LoadAssetAtPath<MagicVisualProfile>(IntegrationGraphicsEditor.Root+"/Profiles/"+e+".asset")).ToArray();
            runner.oldVisuals=MagicContentCatalog.PentagonElements.Select(e=>AssetDatabase.LoadAssetAtPath<GameObject>("Assets/02.Prefabs/Presentation/Projectile_"+e+".prefab").transform.Find("AssetVisual").gameObject).ToArray();
            runner.actorVisual=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/02.Prefabs/Presentation/Enemy_Basic.prefab").transform.Find("AssetVisual").gameObject;
            runner.groundMaterial=AssetDatabase.LoadAssetAtPath<Material>(IntegrationGraphicsEditor.Root+"/Materials/StoneGround.mat");
            runner.font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/02.Prefabs/UI/nanum-gothic/NanumGothic SDF.asset");
            runner.completed=passed=>{SessionState.SetBool(Key+".Running",false);EditorApplication.Exit(passed?0:2);};
        }
        public static void ValidateAssets()
        {
            int count=0;
            foreach(var element in MagicContentCatalog.PentagonElements)
            {
                var profile=AssetDatabase.LoadAssetAtPath<MagicVisualProfile>(IntegrationGraphicsEditor.Root+"/Profiles/"+element+".asset");
                if(profile==null||!profile.IsComplete)throw new Exception("Missing profile "+element);
                foreach(var prefab in profile.tiers)
                {
                    count++;
                    if(prefab.GetComponent<AssetVisualLifecycle>()==null||prefab.GetComponentsInChildren<Collider2D>(true).Length>0||prefab.GetComponentsInChildren<Projectile>(true).Length>0)throw new Exception("Invalid visual-only prefab "+prefab.name);
                    if(!prefab.GetComponentsInChildren<SpriteRenderer>(true).Any(sprite=>sprite.sprite!=null))
                        throw new Exception("Missing source sprites: "+prefab.name+". Import the original third-party packages with their metadata.");
                    if(prefab.GetComponentsInChildren<Animator>(true).Any(animator=>animator.runtimeAnimatorController==null))
                        throw new Exception("Missing animation controller: "+prefab.name);
                }
                var original=AssetDatabase.LoadAssetAtPath<ProjectileMagicDefinition>("Assets/03.Data/Magic/"+MagicContentCatalog.GetMagicId(element)+".asset");
                var own=AssetDatabase.LoadAssetAtPath<ProjectileMagicDefinition>(IntegrationGraphicsEditor.Root+"/Magic/"+element+".asset");
                IntegrationGraphicsEditor.ValidateStats(original,own);
            }
            for(int level=1;level<=8;level++)if(MagicVisualProfile.TierForLevel(level)!=(level<3?0:level<5?1:level<8?2:3))throw new Exception("Tier boundary failed.");
            var ground=AssetDatabase.LoadAssetAtPath<Material>(IntegrationGraphicsEditor.Root+"/Materials/StoneGround.mat");
            if(ground==null||ground.GetTexture("_BaseMap")==null)throw new Exception("Missing stone ground source texture.");
            Directory.CreateDirectory("Logs/GraphicsGrowth");File.WriteAllText("Logs/GraphicsGrowth/assets-check.txt","PASS: "+count+" visual prefabs, 5 matching combat definitions, level boundaries 1..8.\n");
        }
    }
}
