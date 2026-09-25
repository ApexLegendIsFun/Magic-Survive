using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Seondong.IntegrationGame
{
    public static class FinalSubmissionAudit
    {
        public static string Output
        {
            get
            {
                string path = File.Exists("Logs/final-submission-current.txt")
                    ? File.ReadAllText("Logs/final-submission-current.txt").Trim() : "Logs/FinalSubmission/Current";
                Directory.CreateDirectory(path);
                return path;
            }
        }
        public static void Run()
        {
            var log = new StringBuilder();
            foreach (string path in new[] { "Assets/02.Prefabs/UI/GameplayUI.prefab", "Assets/02.Prefabs/UI/Effect_Manager/Effect_All_Manager.prefab", IntegrationGameEditor.Game, "Assets/01.Scripts/UI/TitleScene.unity", "Assets/00.Scenes/Work/SampleScene_Seungbum.unity", "Assets/00.Scenes/Work/SampleScene_Yushin.unity", "Assets/00.Scenes/Work/SampleScene_Taehwan.unity", "Assets/01.Scripts/UI/UI_SampleScene.unity" })
            {
                log.AppendLine("SOURCE " + path);
                GameObject[] roots;
                if (path.EndsWith(".unity")) roots = EditorSceneManager.OpenScene(path).GetRootGameObjects();
                else roots = new[] { AssetDatabase.LoadAssetAtPath<GameObject>(path) };
                foreach (var root in roots) Dump(root.transform, log, "");
            }
            File.WriteAllText(Path.Combine(Output, "scene-inventory.txt"), log.ToString());
            Debug.Log("Final submission scene inventory written.");
        }
        private static void Dump(Transform t, StringBuilder log, string prefix)
        {
            string path = prefix + "/" + t.name;
            var scripts = t.GetComponents<MonoBehaviour>();
            log.AppendLine(path + " active=" + t.gameObject.activeSelf + " [" + string.Join(",", scripts.Select(s => s ? s.GetType().Name : "MISSING")) + "]");
            foreach (var c in t.GetComponents<Component>().Where(c => c != null))
            {
                var so = new SerializedObject(c); var p = so.GetIterator();
                while (p.NextVisible(true))
                    if (p.propertyType == SerializedPropertyType.ObjectReference && p.objectReferenceValue == null && p.objectReferenceInstanceIDValue != 0)
                        log.AppendLine("  MISSING " + c.GetType().Name + "." + p.propertyPath);
            }
            foreach (Transform child in t) Dump(child, log, path);
        }
    }
}
