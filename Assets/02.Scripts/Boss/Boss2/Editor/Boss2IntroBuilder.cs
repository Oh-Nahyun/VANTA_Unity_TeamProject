using System.Collections.Generic;
using System.IO;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.Timeline;
using UnityEngine.UI;

/// <summary>
/// 보스2 등장 시네마틱(Timeline·시그널·베기 스프라이트·씬 오브젝트)을 만들어 현재 씬에 배치한다.
/// 다시 실행하면 Timeline 커브·시그널을 새로 만들고 씬 오브젝트는 지우고 다시 만든다.
/// </summary>
public static class Boss2IntroBuilder
{
    private const string IntroAnimFolder = "Assets/05.Animations/Boss2/Intro";
    private const string SignalFolder = IntroAnimFolder + "/Signals";
    private const string TimelinePath = IntroAnimFolder + "/Boss2Intro.playable";
    private const string VfxFolder = "Assets/09.VFX/Boss2/Intro";
    private const string CoreTexturePath = VfxFolder + "/T_Boss2IntroSlashCore.png";
    private const string GlowTexturePath = VfxFolder + "/T_Boss2IntroSlashGlow.png";
    private const string GlowMaterialPath = VfxFolder + "/M_Boss2IntroSlashGlow.mat";
    private const string UnlitSpriteMaterialPath = "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat";
    private const string GasPrefabPath = Boss2DarkBurstVfxBuilder.PrefabPath;
    private const string CloudPrefabPath = "Assets/03.Prefabs/Boss2/Effect/CloudFogVFX.prefab";
    private const string BlinkSpritePath = "Assets/04.Sprites/Boss2/HandMadeSprite/Boss2 Blink.png";
    private const string VignetteSpritePath = "Assets/04.Sprites/Boss2/Intro/T_HorrorVignette.png";
    private const string RootName = "Boss2IntroCinematic";

    // Timeline 시간표 (초)
    public const float SlashFirstTime = 3.0f;
    public const float SlashSecondTime = 3.5f;
    public const float ExplosionTime = 3.95f;
    public const float BlackoutTime = 4.0f;
    public const float RevealTime = 4.3f;
    public const float LightsOnTime = 4.85f;
    public const float DropTime = 5.25f;
    public const float DropDuration = 0.08f;
    public const float ShakeSettle = 0.05f;
    public const float EndCameraTime = 5.8f;
    public const float StartBattleTime = 7.0f;

    private const float SlashLength = 7f;

    [MenuItem("Tools/Boss2/Build Intro Cinematic")]
    public static void Build()
    {
        EnsureFolder(IntroAnimFolder);
        EnsureFolder(SignalFolder);
        EnsureFolder(VfxFolder);

        Sprite coreSprite = CreateSlashSprite(CoreTexturePath, 512, 32, false);
        Sprite glowSprite = CreateSlashSprite(GlowTexturePath, 512, 128, true);
        Material glowMaterial = CreateGlowMaterial();

        var signals = new Dictionary<string, SignalAsset>
        {
            { nameof(Boss2IntroCinematic.SlashFirst), GetOrCreateSignal("Boss2Intro_SlashFirst") },
            { nameof(Boss2IntroCinematic.SlashSecond), GetOrCreateSignal("Boss2Intro_SlashSecond") },
            { nameof(Boss2IntroCinematic.RequestBoss1Explosion), GetOrCreateSignal("Boss2Intro_Boss1Explosion") },
            { nameof(Boss2IntroCinematic.RevealBoss2), GetOrCreateSignal("Boss2Intro_RevealBoss2") },
            { nameof(Boss2IntroCinematic.EndIntroCamera), GetOrCreateSignal("Boss2Intro_EndIntroCamera") },
            { nameof(Boss2IntroCinematic.StartBattle), GetOrCreateSignal("Boss2Intro_StartBattle") },
        };

        TimelineAsset timeline = CreateTimeline(signals, out AnimationTrack valueTrack, out SignalTrack signalTrack);

        BuildSceneObjects(SceneManager.GetActiveScene(), timeline, valueTrack, signalTrack, signals, coreSprite, glowSprite, glowMaterial);

        AssetDatabase.SaveAssets();
        Debug.Log("[Boss2IntroBuilder] 보스2 인트로 시네마틱 생성 완료");
    }

    /// <summary>TestInGameScene(Scenes/Boss2) 을 (다른 씬이 열려 있으면 추가로) 열어 인트로를 만들고 저장한다.</summary>
    [MenuItem("Tools/Boss2/Build Intro Cinematic In TestInGameScene")]
    public static void BuildInTestScene()
    {
        const string scenePath = "Assets/01.Scenes/Boss2/TestInGameScene.unity";
        Scene previousActive = SceneManager.GetActiveScene();
        Scene scene = SceneManager.GetSceneByPath(scenePath);
        bool openedHere = scene.IsValid() == false || scene.isLoaded == false;
        if (openedHere)
        {
            scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
        }

        SceneManager.SetActiveScene(scene);
        Build();
        EditorSceneManager.SaveScene(scene);

        if (previousActive.IsValid() && previousActive != scene)
        {
            SceneManager.SetActiveScene(previousActive);
        }
        if (openedHere && SceneManager.sceneCount > 1)
        {
            EditorSceneManager.CloseScene(scene, true);
        }
    }

    #region 에셋

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }

    // 가운데가 가장 두껍고 양 끝이 뾰족한 바늘 모양. glow 는 같은 길이에 두께를 부드럽게 퍼뜨린 모양.
    private static Sprite CreateSlashSprite(string path, int width, int height, bool glow)
    {
        var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        var pixels = new Color32[width * height];
        float centerY = (height - 1) * 0.5f;
        for (int x = 0; x < width; x++)
        {
            float u = (x + 0.5f) / width;
            // 앞쪽(벤 끝)이 조금 더 길게 뾰족한 비대칭 프로파일
            float profile = Mathf.Pow(Mathf.Sin(Mathf.PI * Mathf.Pow(u, 0.8f)), 0.7f);
            for (int y = 0; y < height; y++)
            {
                float d = Mathf.Abs(y - centerY);
                float a;
                if (glow)
                {
                    float radius = Mathf.Max(0.001f, profile * height * 0.42f);
                    float n = d / radius;
                    a = Mathf.Exp(-n * n * 2.2f) * Mathf.Clamp01(profile * 1.4f);
                }
                else
                {
                    float halfThickness = profile * height * 0.32f;
                    a = Mathf.Clamp01(halfThickness - d + 0.5f);
                }
                pixels[y * width + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
            }
        }
        tex.SetPixels32(pixels);
        tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 100;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteAlignment = (int)SpriteAlignment.Custom;
        settings.spritePivot = new Vector2(0f, 0.5f);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static Material CreateGlowMaterial()
    {
        var material = AssetDatabase.LoadAssetAtPath<Material>(GlowMaterialPath);
        if (material == null)
        {
            material = new Material(Shader.Find("Boss2/SpriteAdditive"));
            AssetDatabase.CreateAsset(material, GlowMaterialPath);
        }
        material.shader = Shader.Find("Boss2/SpriteAdditive");
        material.SetFloat("_Intensity", 1.6f);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static SignalAsset GetOrCreateSignal(string name)
    {
        string path = $"{SignalFolder}/{name}.signal";
        var signal = AssetDatabase.LoadAssetAtPath<SignalAsset>(path);
        if (signal == null)
        {
            signal = ScriptableObject.CreateInstance<SignalAsset>();
            AssetDatabase.CreateAsset(signal, path);
        }
        return signal;
    }

    private static TimelineAsset CreateTimeline(Dictionary<string, SignalAsset> signals, out AnimationTrack valueTrack, out SignalTrack signalTrack)
    {
        // 에셋을 지우고 새로 만들면 GUID 가 바뀌어 프리팹·씬의 Director 가 Timeline 을 잃을 수 있으므로,
        // 기존 에셋은 그대로 두고 안의 트랙·커브 클립만 비운 뒤 다시 채운다.
        // (트랙 ID 는 새로 발급되지만 Boss2IntroCinematic 이 재생 직전에 트랙을 다시 연결한다)
        var timeline = AssetDatabase.LoadAssetAtPath<TimelineAsset>(TimelinePath);
        if (timeline == null)
        {
            timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            AssetDatabase.CreateAsset(timeline, TimelinePath);
        }
        else
        {
            foreach (TrackAsset oldTrack in new List<TrackAsset>(timeline.GetRootTracks()))
            {
                timeline.DeleteTrack(oldTrack);
            }
            foreach (Object sub in AssetDatabase.LoadAllAssetsAtPath(TimelinePath))
            {
                if (sub is AnimationClip)
                {
                    Object.DestroyImmediate(sub, true);
                }
            }
        }
        timeline.editorSettings.frameRate = 60;

        // 1) 연출 값 커브 (Boss2IntroCinematic 의 직렬화 필드를 움직인다)
        valueTrack = timeline.CreateTrack<AnimationTrack>(null, "Intro Values");
        var clip = new AnimationClip { name = "Boss2IntroValues", frameRate = 60 };
        SetCurve(clip, "_gasAmount", GasCurve());
        SetCurve(clip, "_darkness", DarknessCurve());
        SetCurve(clip, "_cloudAlpha", CloudCurve());
        SetCurve(clip, "_flashAlpha", FlashCurve());
        SetCurve(clip, "_blackout", Linear(
            (0f, 0f), (BlackoutTime, 0f), (BlackoutTime + 0.02f, 1f),
            (LightsOnTime, 1f), (LightsOnTime + 0.01f, 0f), (StartBattleTime, 0f)));
        SetCurve(clip, "_cameraDrop", DropCurve());
        SetCurve(clip, "_cameraShake", Linear(
            (0f, 0f), (DropTime + DropDuration - 0.01f, 0f), (DropTime + DropDuration, 1f),
            (DropTime + DropDuration + ShakeSettle, 0f), (StartBattleTime, 0f)));
        AssetDatabase.AddObjectToAsset(clip, timeline);

        TimelineClip timelineClip = valueTrack.CreateClip(clip);
        timelineClip.start = 0;
        timelineClip.duration = StartBattleTime + 0.05f;
        timelineClip.displayName = "Boss2 Intro Values";

        // 2) 시그널
        signalTrack = timeline.CreateTrack<SignalTrack>(null, "Intro Signals");
        AddSignal(signalTrack, SlashFirstTime, signals[nameof(Boss2IntroCinematic.SlashFirst)]);
        AddSignal(signalTrack, SlashSecondTime, signals[nameof(Boss2IntroCinematic.SlashSecond)]);
        AddSignal(signalTrack, ExplosionTime, signals[nameof(Boss2IntroCinematic.RequestBoss1Explosion)]);
        AddSignal(signalTrack, RevealTime, signals[nameof(Boss2IntroCinematic.RevealBoss2)]);
        AddSignal(signalTrack, EndCameraTime, signals[nameof(Boss2IntroCinematic.EndIntroCamera)]);
        AddSignal(signalTrack, StartBattleTime, signals[nameof(Boss2IntroCinematic.StartBattle)]);

        EditorUtility.SetDirty(timeline);
        return timeline;
    }

    private static void AddSignal(SignalTrack track, double time, SignalAsset signal)
    {
        var emitter = track.CreateMarker<SignalEmitter>(time);
        emitter.asset = signal;
        emitter.retroactive = false;
        emitter.emitOnce = true;
    }

    private static void SetCurve(AnimationClip clip, string field, AnimationCurve curve)
    {
        var binding = EditorCurveBinding.FloatCurve("", typeof(Boss2IntroCinematic), field);
        AnimationUtility.SetEditorCurve(clip, binding, curve);
    }

    // 시작 순간 가장 많이·빠르게 뿜고(시작 분출 + 최대 값), 앞쪽은 빠르게·뒤로 갈수록 완만하게 줄어든다.
    private const float GasStartAmount = 1f;
    private const float GasMidAmount = 0.45f;
    private const float GasEndAmount = 0.2f;

    private static AnimationCurve GasCurve()
    {
        var curve = new AnimationCurve(
            new Keyframe(0f, GasStartAmount, 0f, -0.45f),
            new Keyframe(SlashFirstTime * 0.5f, GasMidAmount, -0.15f, -0.15f),
            new Keyframe(BlackoutTime, GasEndAmount, -0.02f, 0f),
            new Keyframe(BlackoutTime + 0.02f, 0f, 0f, 0f),
            new Keyframe(StartBattleTime, 0f, 0f, 0f));
        SetLinear(curve, 2, 3);
        return curve;
    }

    private static AnimationCurve DarknessCurve()
    {
        var curve = new AnimationCurve(
            new Keyframe(0f, 0f, 0f, 0f),
            new Keyframe(SlashFirstTime, 1f, 0f, 0f),
            new Keyframe(RevealTime, 1f, 0f, 0f),
            new Keyframe(RevealTime + 0.01f, 0f, 0f, 0f),
            new Keyframe(StartBattleTime, 0f, 0f, 0f));
        SetLinear(curve, 2, 3);
        return curve;
    }

    private static AnimationCurve CloudCurve()
    {
        var curve = new AnimationCurve(
            new Keyframe(0f, 0f, 0f, 0f),
            new Keyframe(0.4f, 0f, 0f, 0f),
            new Keyframe(SlashFirstTime, 1f, 0f, 0f),
            new Keyframe(BlackoutTime, 1f, 0f, 0f),
            new Keyframe(BlackoutTime + 0.02f, 0f, 0f, 0f),
            new Keyframe(StartBattleTime, 0f, 0f, 0f));
        SetLinear(curve, 3, 4);
        return curve;
    }

    // 베기마다 화면이 번쩍
    private static AnimationCurve FlashCurve()
    {
        return Linear(
            (0f, 0f),
            (SlashFirstTime - 0.01f, 0f), (SlashFirstTime + 0.02f, 0.3f), (SlashFirstTime + 0.22f, 0f),
            (SlashSecondTime - 0.01f, 0f), (SlashSecondTime + 0.02f, 0.35f), (SlashSecondTime + 0.25f, 0f),
            (StartBattleTime, 0f));
    }

    // 뚝 떨어지듯 가속하며 이동
    private static AnimationCurve DropCurve()
    {
        var curve = new AnimationCurve(
            new Keyframe(0f, 0f, 0f, 0f),
            new Keyframe(DropTime, 0f, 0f, 0f),
            new Keyframe(DropTime + DropDuration, 1f, 2f / DropDuration, 0f),
            new Keyframe(StartBattleTime, 1f, 0f, 0f));
        return curve;
    }

    private static AnimationCurve Linear(params (float t, float v)[] points)
    {
        var curve = new AnimationCurve();
        foreach (var p in points)
        {
            curve.AddKey(new Keyframe(p.t, p.v));
        }
        for (int i = 0; i < curve.length; i++)
        {
            AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.Linear);
            AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.Linear);
        }
        return curve;
    }

    private static void SetLinear(AnimationCurve curve, int from, int to)
    {
        AnimationUtility.SetKeyRightTangentMode(curve, from, AnimationUtility.TangentMode.Linear);
        AnimationUtility.SetKeyLeftTangentMode(curve, to, AnimationUtility.TangentMode.Linear);
    }

    #endregion

    #region 씬

    private static void BuildSceneObjects(Scene scene, TimelineAsset timeline, AnimationTrack valueTrack, SignalTrack signalTrack,
        Dictionary<string, SignalAsset> signals, Sprite coreSprite, Sprite glowSprite, Material glowMaterial)
    {
        GameObject old = System.Array.Find(scene.GetRootGameObjects(), go => go.name == RootName);
        if (old != null)
        {
            Object.DestroyImmediate(old);
        }

        var root = new GameObject(RootName);
        SceneManager.MoveGameObjectToScene(root, scene);
        root.transform.position = Vector3.zero;

        var director = root.AddComponent<PlayableDirector>();
        var animator = root.AddComponent<Animator>();
        var receiver = root.AddComponent<SignalReceiver>();
        var impulse = root.AddComponent<CinemachineImpulseSource>();
        var cinematic = root.AddComponent<Boss2IntroCinematic>();

        director.playableAsset = timeline;
        director.playOnAwake = false;
        director.extrapolationMode = DirectorWrapMode.None;
        director.timeUpdateMode = DirectorUpdateMode.UnscaledGameTime;
        director.SetGenericBinding(valueTrack, animator);
        director.SetGenericBinding(signalTrack, receiver);

        foreach (var pair in signals)
        {
            var reaction = new UnityEvent();
            var method = (UnityAction)System.Delegate.CreateDelegate(typeof(UnityAction), cinematic, pair.Key);
            UnityEventTools.AddPersistentListener(reaction, method);
            receiver.AddReaction(pair.Value, reaction);
        }

        impulse.DefaultVelocity = new Vector3(0.12f, -0.12f, 0f);
        impulse.ImpulseDefinition.ImpulseShape = CinemachineImpulseDefinition.ImpulseShapes.Bump;
        impulse.ImpulseDefinition.ImpulseDuration = 0.15f;

        // 가스·구름
        ParticleSystem gas = InstantiateEffect(GasPrefabPath, "Gas", root.transform);
        ParticleSystem cloud = InstantiateEffect(CloudPrefabPath, "Cloud", root.transform);

        // X 베기
        var slashRoot = new GameObject("Slashes").transform;
        slashRoot.SetParent(root.transform, false);
        Boss2IntroSlash first = CreateSlash("Slash_1", slashRoot, new Vector2(1f, -1f), coreSprite, glowSprite, glowMaterial);
        Boss2IntroSlash second = CreateSlash("Slash_2", slashRoot, new Vector2(-1f, -1f), coreSprite, glowSprite, glowMaterial);

        // 화면 오버레이 (번쩍임·암전)
        var canvasObject = new GameObject("IntroOverlay", typeof(Canvas));
        canvasObject.transform.SetParent(root.transform, false);
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;
        // 공포 테두리는 번쩍임·암전 아래에 깔린다 (먼저 만든 것이 아래)
        Image vignette = CreateOverlayImage("HorrorVignette", canvasObject.transform, new Color(1f, 1f, 1f, 0f));
        vignette.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(VignetteSpritePath);
        Image flash = CreateOverlayImage("Flash", canvasObject.transform, new Color(1f, 1f, 1f, 0f));
        Image black = CreateOverlayImage("Blackout", canvasObject.transform, new Color(0f, 0f, 0f, 0f));

        // 얼굴 줌인 카메라
        var cameraObject = new GameObject("Boss2IntroCamera");
        cameraObject.transform.SetParent(root.transform, false);
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);
        var introCamera = cameraObject.AddComponent<CinemachineCamera>();
        introCamera.Priority.Enabled = true;
        introCamera.Priority.Value = 100;
        var lens = introCamera.Lens;
        lens.OrthographicSize = 1.1f;
        introCamera.Lens = lens;
        cameraObject.SetActive(false);

        // 대상
        Boss2 boss2 = FindInScene<Boss2>(scene);
        Boss1DeathController boss1 = FindInScene<Boss1DeathController>(scene);

        var so = new SerializedObject(cinematic);
        so.FindProperty("_boss2").objectReferenceValue = boss2;
        so.FindProperty("_boss1").objectReferenceValue = boss1 != null ? boss1.transform : null;
        so.FindProperty("_gas").objectReferenceValue = gas;
        so.FindProperty("_cloud").objectReferenceValue = cloud;
        var slashes = so.FindProperty("_slashes");
        slashes.arraySize = 2;
        slashes.GetArrayElementAtIndex(0).objectReferenceValue = first;
        slashes.GetArrayElementAtIndex(1).objectReferenceValue = second;
        so.FindProperty("_slashRoot").objectReferenceValue = slashRoot;
        so.FindProperty("_slashImpulse").objectReferenceValue = impulse;
        so.FindProperty("_flashOverlay").objectReferenceValue = flash;
        so.FindProperty("_blackOverlay").objectReferenceValue = black;
        so.FindProperty("_vignetteOverlay").objectReferenceValue = vignette;
        so.FindProperty("_introCamera").objectReferenceValue = introCamera;
        so.FindProperty("_boss2EyesClosedSprite").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(BlinkSpritePath);
        so.ApplyModifiedPropertiesWithoutUndo();

        // 보스2 FSM 은 프리팹의 _runOnStart = false 로 꺼 두고, 인트로가 끝난 뒤 시작한다.
        if (boss2 == null)
        {
            Debug.LogWarning("[Boss2IntroBuilder] 씬에서 Boss2 를 찾지 못했습니다.");
        }

        EditorSceneManager.MarkSceneDirty(root.scene);
        Selection.activeGameObject = root;
    }

    private static ParticleSystem InstantiateEffect(string prefabPath, string name, Transform parent)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        instance.name = name;
        instance.transform.localPosition = Vector3.zero;
        var ps = instance.GetComponent<ParticleSystem>();
        var main = ps.main;
        main.playOnAwake = false;
        return ps;
    }

    private static Boss2IntroSlash CreateSlash(string name, Transform parent, Vector2 direction, Sprite core, Sprite glow, Material glowMaterial)
    {
        direction.Normalize();
        var slashObject = new GameObject(name);
        slashObject.transform.SetParent(parent, false);
        // 피벗이 왼쪽 끝이므로 시작점을 중심에서 반 길이만큼 뒤로
        slashObject.transform.localPosition = -(Vector3)direction * (SlashLength * 0.5f);
        slashObject.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);

        float scaleX = SlashLength / core.bounds.size.x;

        var glowObject = new GameObject("Glow");
        glowObject.transform.SetParent(slashObject.transform, false);
        glowObject.transform.localScale = new Vector3(scaleX, 1f, 1f);
        var glowRenderer = glowObject.AddComponent<SpriteRenderer>();
        glowRenderer.sprite = glow;
        glowRenderer.sharedMaterial = glowMaterial;
        glowRenderer.color = new Color(1f, 0.25f, 0.3f, 1f);
        glowRenderer.sortingLayerName = "Boss";
        glowRenderer.sortingOrder = 200;

        var coreObject = new GameObject("Core");
        coreObject.transform.SetParent(slashObject.transform, false);
        coreObject.transform.localScale = new Vector3(scaleX, 1f, 1f);
        var coreRenderer = coreObject.AddComponent<SpriteRenderer>();
        coreRenderer.sprite = core;
        coreRenderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(UnlitSpriteMaterialPath);
        coreRenderer.color = new Color(1f, 0.96f, 0.96f, 1f);
        coreRenderer.sortingLayerName = "Boss";
        coreRenderer.sortingOrder = 201;

        var slash = slashObject.AddComponent<Boss2IntroSlash>();
        var so = new SerializedObject(slash);
        so.FindProperty("_core").objectReferenceValue = coreRenderer;
        so.FindProperty("_glow").objectReferenceValue = glowRenderer;
        so.ApplyModifiedPropertiesWithoutUndo();
        return slash;
    }

    private static Image CreateOverlayImage(string name, Transform parent, Color color)
    {
        var imageObject = new GameObject(name, typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(parent, false);
        var rect = imageObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        var image = imageObject.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        image.enabled = false;
        return image;
    }

    private static T FindInScene<T>(Scene scene) where T : Component
    {
        T[] found = Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        return System.Array.Find(found, x => x.gameObject.scene == scene);
    }

    #endregion
}
