using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;

namespace Seondong.IntegrationGame
{
    // Explicit diagnostic scene only. This is art/performance evidence, never a game clear.
    public sealed class IntegrationGraphicsPreview : MonoBehaviour
    {
        public MagicVisualProfile[] profiles;
        public GameObject[] oldVisuals;
        public GameObject actorVisual;
        public Material groundMaterial;
        public TMP_FontAsset font;
        public Action<bool> completed;
        private Camera cameraView;
        private RenderTexture target;
        private readonly List<GameObject> objects = new List<GameObject>();
        private readonly List<string> failures = new List<string>();
        private string output;
        [Serializable] private class Timing { public string label; public int frames; public double meanMs, p95Ms; }
        private readonly List<Timing> timings = new List<Timing>();
        private IEnumerator Start()
        {
            output = Path.GetFullPath("Logs/GraphicsGrowth"); Directory.CreateDirectory(output);
            Application.logMessageReceived += Log;
            Time.timeScale = 1; QualitySettings.vSyncCount = 0; Application.targetFrameRate = -1; Application.runInBackground = true;
            cameraView = new GameObject("Graphics Preview Camera", typeof(Camera)).GetComponent<Camera>();
            cameraView.orthographic = true; cameraView.orthographicSize = 7.4f;
            cameraView.transform.position = new Vector3(0,0,-10); cameraView.clearFlags = CameraClearFlags.SolidColor;
            cameraView.backgroundColor = new Color(.04f,.06f,.08f);
            SetTarget(1920,1440);
            for (int e = 0; e < 5; e++)
            {
                Label(profiles[e].element.ToString(), new Vector2(-8.2f, 5-e*2.5f), 3.6f);
                for (int t = 0; t < 4; t++)
                {
                    var model = Instantiate(profiles[e].tiers[t]); objects.Add(model);
                    model.transform.position = new Vector3(-5.6f+t*4, 5-e*2.5f, 0); model.transform.localScale = Vector3.one*2.5f;
                }
            }
            for (int t = 0; t < 4; t++) Label(new[] { "Lv 1-2", "Lv 3-4", "Lv 5-7", "Lv 8" }[t],new Vector2(-5.6f+t*4,6.6f),3.6f);
            var galleryModels=objects.Where(o=>o.GetComponent<AssetVisualLifecycle>()!=null).ToArray();
            var anchors=galleryModels.Select(o=>o.transform.position).ToArray();
            float galleryStart=Time.realtimeSinceStartup;
            while(Time.realtimeSinceStartup-galleryStart<.5f)
            {
                float offset=Mathf.Sin((Time.realtimeSinceStartup-galleryStart)*8)*.25f;
                for(int i=0;i<galleryModels.Length;i++)galleryModels[i].transform.position=anchors[i]+Vector3.right*offset;
                yield return null;
            }
            yield return null;
            Capture("20-magic-tiers.png");
            // Re-enable each visual and verify reset contracts independently of combat.
            foreach (var model in objects.Where(o => o.GetComponent<AssetVisualLifecycle>() != null))
            {
                model.SetActive(false); model.SetActive(true);
                foreach (var trail in model.GetComponentsInChildren<TrailRenderer>())
                    if (trail.positionCount != 0) failures.Add("Trail did not reset: " + model.name);
                foreach (var orbit in model.GetComponentsInChildren<MagicVisualOrbit>())
                    if (Quaternion.Angle(orbit.transform.localRotation, Quaternion.identity) > .01f) failures.Add("Orbit did not reset: " + model.name);
            }
            var rotations = objects.SelectMany(o => o.GetComponentsInChildren<MagicVisualOrbit>()).ToArray();
            // Let the pause take effect at the next frame boundary before measuring.
            Time.timeScale = 0; yield return null;
            var beforePause = rotations.Select(o => o.transform.localRotation).ToArray();
            var animations = objects.SelectMany(o => o.GetComponentsInChildren<Animator>()).Where(a=>a.isActiveAndEnabled).ToArray();
            var pausedAnimations = animations.Select(a=>a.GetCurrentAnimatorStateInfo(0).normalizedTime).ToArray();
            yield return new WaitForSecondsRealtime(.2f);
            for (int i=0;i<rotations.Length;i++) if (Quaternion.Angle(beforePause[i],rotations[i].transform.localRotation)>.01f) failures.Add("Visual moved during pause.");
            for (int i=0;i<animations.Length;i++) if (!Mathf.Approximately(pausedAnimations[i],animations[i].GetCurrentAnimatorStateInfo(0).normalizedTime)) failures.Add("Animator advanced during pause.");
            Time.timeScale = 1;
            Clear(); yield return null;
            SetTarget(1920,1080); cameraView.orthographicSize = 5;
            // Ground follows the view while the shader samples world coordinates.
            var floor = GameObject.CreatePrimitive(PrimitiveType.Quad); objects.Add(floor); Destroy(floor.GetComponent<Collider>());
            floor.GetComponent<MeshRenderer>().sharedMaterial = groundMaterial;
            floor.AddComponent<IntegrationGround>().followCamera = cameraView;
            for (int i=0;i<3;i++)
            {
                cameraView.transform.position = new Vector3(i==0?0:i==1?1000:-1000, i==0?0:i==1?-800:800,-10);
                yield return null; yield return null; Capture("ground-position-"+i+".png");
            }
            Clear(); yield return null; cameraView.transform.position = new Vector3(0,0,-10);
            for (int pass = 0; pass < 2; pass++)
            {
                var moving = new List<Transform>();
                if (pass == 1)
                {
                    floor = GameObject.CreatePrimitive(PrimitiveType.Quad); objects.Add(floor); Destroy(floor.GetComponent<Collider>());
                    floor.GetComponent<MeshRenderer>().sharedMaterial=groundMaterial; floor.AddComponent<IntegrationGround>().followCamera=cameraView;
                }
                for (int i=0;i<100;i++)
                {
                    var actor = Instantiate(actorVisual); objects.Add(actor); actor.transform.position = new Vector3(-7+(i%10)*1.5f,-4+(i/10)*.85f,0);
                    var life = actor.AddComponent<AssetVisualLifecycle>(); life.visual=actor.transform; life.faceMovement=true;
                }
                for (int i=0;i<64;i++)
                {
                    var visual = Instantiate(pass==0?oldVisuals[i%5]:profiles[i%5].tiers[3]); objects.Add(visual); moving.Add(visual.transform);
                    if (pass==0) { var life=visual.AddComponent<AssetVisualLifecycle>(); life.visual=visual.transform; life.repeatAnimation=true; }
                }
                var samples = new List<double>(); float start=Time.realtimeSinceStartup;
                while (Time.realtimeSinceStartup-start < 25)
                {
                    float elapsed=Time.realtimeSinceStartup-start;
                    for (int i=0;i<moving.Count;i++) moving[i].position=new Vector3(-8+Mathf.Repeat(elapsed*4+i*.25f,16),-4+(i%8)*1.1f,0);
                    if (elapsed>5) samples.Add(Time.unscaledDeltaTime*1000d);
                    yield return null;
                }
                samples.Sort(); timings.Add(new Timing {label=pass==0?"before":"after-tier8",frames=samples.Count,meanMs=samples.Average(),p95Ms=samples[Math.Min(samples.Count-1,(int)(samples.Count*.95f))]});
                Capture("performance-"+(pass==0?"before":"after")+".png"); Clear(); yield return null;
            }
            File.WriteAllText(Path.Combine(output,"visual-performance.json"),"["+string.Join(",",timings.Select(t=>JsonUtility.ToJson(t,true)))+"]");
            File.WriteAllText(Path.Combine(output,"preview-check.txt"),"Synthetic visual fixture; NOT a gameplay completion.\n20 tier appearances; pooled visual reset; pause; world ground at 0/+1000/-1000.\n1920x1080 RenderTexture; 100 same actor visuals + 64 moving projectile visuals; 5s warmup/20s sample; VSync off.\n"+SystemInfo.graphicsDeviceName+"\n"+(failures.Count==0?"PASS":string.Join("\n",failures)));
            completed?.Invoke(failures.Count==0);
        }
        private void Label(string text, Vector2 position, float size)
        {
            var go=new GameObject(text,typeof(TextMeshPro)); objects.Add(go); go.transform.position=position;
            var label=go.GetComponent<TextMeshPro>(); label.text=text; label.font=font; label.fontSize=size; label.color=Color.white; label.alignment=TextAlignmentOptions.Center; label.rectTransform.sizeDelta=new Vector2(3,1);
        }
        private void SetTarget(int width,int height)
        {
            if(target!=null){cameraView.targetTexture=null;target.Release();Destroy(target);}
            target=new RenderTexture(width,height,24); target.Create();cameraView.targetTexture=target;
        }
        private void Capture(string name)
        {
            var previous=RenderTexture.active;RenderTexture.active=target;
            var texture=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,target.width,target.height),0,0);texture.Apply();
            File.WriteAllBytes(Path.Combine(output,name),texture.EncodeToPNG());Destroy(texture);RenderTexture.active=previous;
        }
        private void Clear(){foreach(var go in objects)if(go!=null)Destroy(go);objects.Clear();}
        private void Log(string message,string stack,LogType kind){if(kind==LogType.Error||kind==LogType.Exception||kind==LogType.Assert)failures.Add(message);}
        private void OnDestroy(){Application.logMessageReceived-=Log;if(target!=null){target.Release();Destroy(target);}}
    }
}
