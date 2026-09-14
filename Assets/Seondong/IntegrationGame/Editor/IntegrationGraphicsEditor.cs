using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Seondong.IntegrationGame
{
    public static class IntegrationGraphicsEditor
    {
        public const string Root = IntegrationGameEditor.Root + "/Graphics";
        private const string Packages = "Assets/Untracked Asset/";
        public static readonly string[] Effects = {
            Packages + "2D_PFX/Prefabs/Fire/Fire_05.prefab",
            Packages + "2D_PFX/Prefabs/Electric/Electric_07.prefab",
            Packages + "2D_PFX/Prefabs/IceWater/IceWater_01.prefab",
            Packages + "PixelAttackFx/Prefebs/Skill_18.prefab",
            Packages + "PixelAttackFx/Prefebs/Skill_19.prefab" };
        public static readonly Color[] Colors = {
            new Color(1,.48f,.15f), new Color(.6f,.83f,1), new Color(.5f,1,1),
            new Color(.72f,.45f,.21f), new Color(.72f,.35f,1) };

        [MenuItem("Tools/Seondong Integration/5. Apply Magic Growth And Ground")]
        public static void Install()
        {
            if (EditorApplication.isPlaying) throw new Exception("Stop Play Mode first.");
            IntegrationGameEditor.CreateScenes();
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                foreach (string folder in new[] { "Visuals", "Profiles", "Projectiles", "Magic", "Materials" }) Directory.CreateDirectory(Root + "/" + folder);
                AssetDatabase.Refresh();
                var ink = Material("VisualInk", "Sprites/Default");
                var glow = AssetDatabase.LoadAssetAtPath<Sprite>(Packages + "FantasyMonsters/Common/Sprites/Light.png");
                if (glow == null) throw new Exception("Existing Light sprite missing.");
                for (int e = 0; e < 5; e++)
                {
                    var element = MagicContentCatalog.PentagonElements[e];
                    var profile = Asset<MagicVisualProfile>(Root + "/Profiles/" + element + ".asset");
                    profile.element = element; profile.tiers = new GameObject[4];
                    for (int tier = 0; tier < 4; tier++) profile.tiers[tier] = BuildVisual(e, tier, ink, glow);
                    EditorUtility.SetDirty(profile);
                    var source = AssetDatabase.LoadAssetAtPath<ProjectileMagicDefinition>("Assets/03.Data/Magic/" + MagicContentCatalog.GetMagicId(element) + ".asset");
                    if (source == null || source.ProjectilePrefab == null) throw new Exception("Missing source magic: " + element);
                    string projectilePath = Root + "/Projectiles/" + element + ".prefab";
                    var projectile = PrefabUtility.LoadPrefabContents(AssetDatabase.GetAssetPath(source.ProjectilePrefab));
                    try
                    {
                        foreach (var lifecycle in projectile.GetComponents<AssetVisualLifecycle>()) UnityEngine.Object.DestroyImmediate(lifecycle);
                        var visual = projectile.transform.Find("AssetVisual");
                        if (visual != null) UnityEngine.Object.DestroyImmediate(visual.gameObject);
                        var binder = projectile.GetComponent<IntegrationMagicVisual>() ?? projectile.AddComponent<IntegrationMagicVisual>();
                        binder.profile = profile;
                        projectile.SetActive(false);
                        PrefabUtility.SaveAsPrefabAsset(projectile, projectilePath);
                    }
                    finally { PrefabUtility.UnloadPrefabContents(projectile); }
                    string dataPath = Root + "/Magic/" + element + ".asset";
                    var data = AssetDatabase.LoadAssetAtPath<ProjectileMagicDefinition>(dataPath);
                    if (data == null) { data = UnityEngine.Object.Instantiate(source); AssetDatabase.CreateAsset(data, dataPath); }
                    var serialized = new SerializedObject(data);
                    serialized.FindProperty("projectilePrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(projectilePath).GetComponent<Projectile>();
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                    ValidateStats(source, data);
                }
                var ground = Material("StoneGround", "Seondong/World Ground");
                ground.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Packages + "Plugins/AllIn1SpriteShader/Demo/Textures/RockTexture.png"));
                ground.SetFloat("_TileSize", 4);
                EditorUtility.SetDirty(ground);
                AssetDatabase.SaveAssets();
                var scene = EditorSceneManager.OpenScene(IntegrationGameEditor.Game);
                var roots = scene.GetRootGameObjects();
                var skills = roots.SelectMany(r => r.GetComponentsInChildren<PlayerSkillSystem>(true)).Single();
                var so = new SerializedObject(skills);
                var definitions = so.FindProperty("targetedMagicDefinitions"); definitions.arraySize = 5;
                for (int i = 0; i < 5; i++) definitions.GetArrayElementAtIndex(i).objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<ProjectileMagicDefinition>(Root + "/Magic/" + MagicContentCatalog.PentagonElements[i] + ".asset");
                so.ApplyModifiedPropertiesWithoutUndo();
                var existing = roots.SelectMany(r => r.GetComponentsInChildren<IntegrationGround>(true)).FirstOrDefault();
                if (existing == null)
                {
                    var go = GameObject.CreatePrimitive(PrimitiveType.Quad); go.name = "Integration Stone Ground";
                    UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
                    existing = go.AddComponent<IntegrationGround>();
                }
                existing.followCamera = roots.SelectMany(r => r.GetComponentsInChildren<Camera>(true)).First(c => c.CompareTag("MainCamera"));
                var renderer = existing.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/StoneGround.mat");
                renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
                existing.transform.position = new Vector3(0, 0, 5); existing.transform.localScale = new Vector3(30,20,1);
                EditorSceneManager.SaveScene(scene);
                AssetDatabase.SaveAssets();
                Debug.Log("[Integration Graphics] 20 visuals, 5 isolated projectile/data sets, world-ground installed.");
            }
            finally { if (setup.Any(s => s.isLoaded)) EditorSceneManager.RestoreSceneManagerSetup(setup); }
        }
        private static T Asset<T>(string path) where T : ScriptableObject
        {
            var value = AssetDatabase.LoadAssetAtPath<T>(path);
            if (value != null) return value;
            value = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(value, path); return value;
        }
        private static Material Material(string name, string shaderName)
        {
            string path = Root + "/Materials/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            var shader = Shader.Find(shaderName);
            if (shader == null) throw new Exception("Missing shader: " + shaderName);
            material = new Material(shader); AssetDatabase.CreateAsset(material, path); return material;
        }
        private static GameObject BuildVisual(int element, int tier, Material ink, Sprite glow)
        {
            var name = MagicContentCatalog.PentagonElements[element] + "_Tier" + (tier + 1);
            var root = new GameObject(name);
            try
            {
                root.AddComponent<SortingGroup>().sortingOrder = 5000;
                var life = root.AddComponent<AssetVisualLifecycle>();
                life.visual = root.transform; life.repeatAnimation = true;
                life.tint = element >= 3 ? Colors[element] : Color.white;
                Effect(root.transform, element, .46f + Mathf.Max(0, tier - 1) * .035f, Vector2.zero, 0, 1);
                if (tier >= 1)
                {
                    for (int n = 0; n < 3; n++)
                    {
                        var tailPosition = new Vector2(-.24f - n * .14f, 0);
                        Effect(root.transform, element, .2f - n * .045f, tailPosition, 0, .4f - n * .1f);
                        if (element == 2) FrostGlint(root.transform, glow, tailPosition, .12f - n * .025f);
                    }
                    var trail = root.AddComponent<TrailRenderer>();
                    trail.sharedMaterial = ink; trail.time = .09f + tier * .025f; trail.minVertexDistance = .07f;
                    trail.widthMultiplier = .07f + tier * .015f; trail.numCapVertices = 2;
                    trail.startColor = new Color(Colors[element].r, Colors[element].g, Colors[element].b, .45f);
                    trail.endColor = new Color(Colors[element].r, Colors[element].g, Colors[element].b, 0);
                    trail.sortingOrder = -2; trail.shadowCastingMode = ShadowCastingMode.Off;
                }
                if (tier >= 2)
                {
                    Effect(root.transform, element, .22f, new Vector2(-.1f,.2f), 30, .75f);
                    Effect(root.transform, element, .22f, new Vector2(-.1f,-.2f), -30, .75f);
                    if (element == 2)
                    {
                        FrostGlint(root.transform, glow, new Vector2(-.1f,.2f), .14f);
                        FrostGlint(root.transform, glow, new Vector2(-.1f,-.2f), .14f);
                    }
                    var halo = new GameObject("Soft core glow", typeof(SpriteRenderer)); halo.transform.SetParent(root.transform, false);
                    var sprite = halo.GetComponent<SpriteRenderer>(); sprite.sprite = glow; sprite.color = new Color(Colors[element].r, Colors[element].g, Colors[element].b, tier == 3 ? .24f : .12f);
                    halo.transform.localScale = Vector3.one * ((tier == 3 ? .8f : .64f) / Mathf.Max(glow.bounds.size.x, glow.bounds.size.y));
                    sprite.sortingOrder = -3;
                }
                if (tier == 3)
                {
                    var orbit = new GameObject("Awakened satellites", typeof(MagicVisualOrbit)); orbit.transform.SetParent(root.transform, false);
                    orbit.GetComponent<MagicVisualOrbit>().degreesPerSecond = 90 + element * 15;
                    for (int n = 0; n < 4; n++)
                    {
                        float a = n * Mathf.PI * .5f;
                        Effect(orbit.transform, element, .14f, new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * .34f, n * 90, .7f);
                        if (element == 2) FrostGlint(orbit.transform, glow, new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * .34f, .1f);
                    }
                    var ring = orbit.AddComponent<LineRenderer>(); ring.useWorldSpace = false; ring.loop = true;
                    ring.sharedMaterial = ink; ring.widthMultiplier = .012f; ring.positionCount = 32;
                    ring.startColor = ring.endColor = new Color(Colors[element].r, Colors[element].g, Colors[element].b, .42f);
                    for (int n = 0; n < 32; n++) { float a = n * Mathf.PI * 2 / 32; ring.SetPosition(n, new Vector3(Mathf.Cos(a)*.4f, Mathf.Sin(a)*.4f,0)); }
                    ring.shadowCastingMode = ShadowCastingMode.Off;
                }
                return PrefabUtility.SaveAsPrefabAsset(root, Root + "/Visuals/" + name + ".prefab");
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
        private static void FrostGlint(Transform parent, Sprite glow, Vector2 position, float size)
        {
            var glint = new GameObject("Ice glint", typeof(SpriteRenderer));
            glint.transform.SetParent(parent, false); glint.transform.localPosition = position;
            glint.transform.localScale = Vector3.one * (size / Mathf.Max(glow.bounds.size.x, glow.bounds.size.y));
            var renderer = glint.GetComponent<SpriteRenderer>(); renderer.sprite = glow;
            renderer.color = new Color(.5f,1,1,.8f);
        }
        private static void Effect(Transform parent, int element, float size, Vector2 position, float angle, float alpha)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Effects[element]);
            if (prefab == null) throw new Exception("Missing effect " + Effects[element]);
            var model = UnityEngine.Object.Instantiate(prefab, parent, false);
            foreach (var body in model.GetComponentsInChildren<Rigidbody2D>(true)) UnityEngine.Object.DestroyImmediate(body);
            foreach (var collider in model.GetComponentsInChildren<Collider2D>(true)) UnityEngine.Object.DestroyImmediate(collider);
            foreach (var animator in model.GetComponentsInChildren<Animator>(true)) { animator.applyRootMotion = false; animator.fireEvents = false; animator.keepAnimatorStateOnDisable = false; animator.updateMode = AnimatorUpdateMode.Normal; }
            // Small decorative copies keep the authored silhouette; only the core runs
            // the flipbook. This avoids multiplying Animator cost at awakened level.
            if (size < .3f) foreach (var animator in model.GetComponentsInChildren<Animator>(true)) UnityEngine.Object.DestroyImmediate(animator);
            foreach (var particle in model.GetComponentsInChildren<ParticleSystem>(true)) { var main = particle.main; main.stopAction = ParticleSystemStopAction.None; main.simulationSpace = ParticleSystemSimulationSpace.Local; }
            var sprites = model.GetComponentsInChildren<SpriteRenderer>(true).Where(s => s.sprite != null).ToArray();
            if (sprites.Length == 0) throw new Exception("No sprite in " + model.name);
            Bounds bounds = sprites[0].bounds; foreach (var sprite in sprites) bounds.Encapsulate(sprite.bounds);
            model.transform.localScale *= size / Mathf.Max(.01f, Mathf.Max(bounds.size.x, bounds.size.y));
            bounds = sprites[0].bounds; foreach (var sprite in sprites) bounds.Encapsulate(sprite.bounds);
            model.transform.position += parent.position - bounds.center;
            model.transform.localPosition += (Vector3)position;
            model.transform.localRotation = Quaternion.Euler(0,0,angle);
            foreach (var sprite in sprites) { var color = sprite.color; color.a *= alpha; sprite.color = color; }
        }
        public static void ValidateStats(ProjectileMagicDefinition a, ProjectileMagicDefinition b)
        {
            if (a.Element != b.Element || a.MagicId != b.MagicId || a.Range != b.Range || a.Speed != b.Speed || a.MaxDistance != b.MaxDistance || a.HitRadius != b.HitRadius)
                throw new Exception("Visual asset changed combat stats: " + a.name);
        }
    }
}
