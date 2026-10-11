using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 보스2 연출 파티클을 코드로 만들고 고친다. 메뉴: Tools / Boss2 VFX
///   - DarkEngulfVFX2 : 흡수 반경 2배 + 블랙홀처럼 빨려 드는 움직임 (기존 프리팹 수정)
///   - DarkFogVFX     : 검은 안개가 서서히 깔림 (새 프리팹)
///   - MoonlightFogVFX: 검은 안개 + 오른쪽 위에서 왼쪽 아래로 떨어지는 달빛, 반시계 30° 회전 (새 프리팹)
/// 값은 모두 절대값으로 넣으므로 여러 번 실행해도 결과가 같다. 사용법은 Docs/Boss2VFX.md 참고.
/// </summary>
public static class Boss2VfxBuilder
{
    private const string EffectFolder = "Assets/03.Prefabs/Boss2/Effect";
    private const string MaterialFolder = "Assets/08.Materials/Boss2";
    private const string TextureFolder = "Assets/04.Sprites/Boss2/VFX";

    private const string EngulfPath = EffectFolder + "/DarkEngulfVFX2.prefab";
    private const string FogPath = EffectFolder + "/DarkFogVFX.prefab";
    private const string MoonlightPath = EffectFolder + "/MoonlightFogVFX.prefab";

    private const string SmokeMaterialPath = MaterialFolder + "/M_Boss2SmokeAlpha.mat";
    private const string DarkMaterialPath = MaterialFolder + "/M_Boss2DarkParticle.mat";
    private const string BeamMaterialPath = MaterialFolder + "/M_Boss2MoonBeamAdditive.mat";
    private const string DustMaterialPath = MaterialFolder + "/M_Boss2MoonDustAdditive.mat";
    private const string BeamTexturePath = TextureFolder + "/T_Boss2MoonBeam.png";

    // 달빛 설정
    private const float BeamLength = 16f;
    private const float BeamDuration = 6f;

    [MenuItem("Tools/Boss2 VFX/Build All")]
    public static void BuildAll()
    {
        UpdateDarkEngulf();
        BuildDarkFog();
        BuildMoonlightFog();
    }

    // ---------------------------------------------------------------- DarkEngulfVFX2

    [MenuItem("Tools/Boss2 VFX/Update DarkEngulfVFX2 (블랙홀 흡수, 반경 2배)")]
    public static void UpdateDarkEngulf()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(EngulfPath);
        try
        {
            Transform suckIn = root.transform.Find("SuckIn");
            if (suckIn == null)
            {
                Debug.LogError("[Boss2VfxBuilder] DarkEngulfVFX2 에 SuckIn 자식이 없습니다.");
                return;
            }

            SetupSuckIn(suckIn.GetComponent<ParticleSystem>());
            SetupEngulfCore(GetOrCreateChild(root.transform, "Core"), suckIn.GetComponent<ParticleSystemRenderer>());

            PrefabUtility.SaveAsPrefabAsset(root, EngulfPath);
            Debug.Log("[Boss2VfxBuilder] DarkEngulfVFX2 수정 완료");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    // 가장자리(반경 4.4)에서 생겨 점점 빨라지며 소용돌이쳐 가운데로 빨려 든다.
    private static void SetupSuckIn(ParticleSystem ps)
    {
        ParticleSystem.MainModule main = ps.main;
        main.duration = 0.6f;
        main.startLifetime = 0.55f;
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.22f);
        main.maxParticles = 1000;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 4.4f;              // 기존 2.2 의 2배
        shape.radiusThickness = 0.15f;    // 가장자리 띠에서만 생성

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 280f;     // 둘레가 2배라 밀도를 맞추려고 2배

        // 반경 4.4 를 수명 0.55초 안에 지나도록 평균 -8, 처음엔 느리고 끝으로 갈수록 빨라진다.
        ParticleSystem.VelocityOverLifetimeModule velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.radial = new ParticleSystem.MinMaxCurve(-8f, Curve(0f, 0.4f, 1f, 1.6f));
        // 궤도 회전(라디안/초)도 가운데로 갈수록 빨라져 소용돌이처럼 보인다. 세 축은 같은 모드여야 한다.
        velocity.orbitalX = new ParticleSystem.MinMaxCurve(0f, Curve(0f, 0f, 1f, 0f));
        velocity.orbitalY = new ParticleSystem.MinMaxCurve(0f, Curve(0f, 0f, 1f, 0f));
        velocity.orbitalZ = new ParticleSystem.MinMaxCurve(1f, Curve(0f, 1f, 1f, 6f));

        // 빨려 들며 작아진다.
        ParticleSystem.SizeOverLifetimeModule size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, Curve(0f, 1f, 1f, 0.2f));
    }

    // 가운데에서 점점 커지는 검은 핵 (사건의 지평선 느낌)
    private static void SetupEngulfCore(GameObject go, ParticleSystemRenderer reference)
    {
        ParticleSystem ps = GetOrAdd<ParticleSystem>(go);
        ParticleSystem.MainModule main = ps.main;
        main.duration = 0.6f;
        main.loop = false;
        main.playOnAwake = true;
        main.startLifetime = 0.75f;
        main.startSpeed = 0f;
        main.startSize = 1.6f;
        main.startColor = new Color(0.02f, 0f, 0.04f, 0.9f);
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        main.maxParticles = 1;

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.enabled = false;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 1) });

        ParticleSystem.SizeOverLifetimeModule size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, Curve(0f, 0f, 0.8f, 1f));

        ParticleSystem.ColorOverLifetimeModule color = ps.colorOverLifetime;
        color.enabled = true;
        color.color = AlphaGradient(Color.white, (0f, 0f), (0.3f, 1f), (0.85f, 1f), (1f, 0f));

        ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = LoadMaterial(SmokeMaterialPath);
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.maxParticleSize = 1f;
        if (reference != null)
        {
            renderer.sortingLayerID = reference.sortingLayerID;
            renderer.sortingOrder = reference.sortingOrder - 1; // 빨려 드는 입자 뒤
        }
    }

    // ---------------------------------------------------------------- DarkFogVFX

    [MenuItem("Tools/Boss2 VFX/Build DarkFogVFX (검은 안개)")]
    public static void BuildDarkFog()
    {
        GameObject root = new GameObject("DarkFogVFX");
        try
        {
            SetupFog(root, new Vector2(16f, 7f));
            SavePrefab(root, FogPath);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    // 큰 연기 입자가 투명하게 생겨 천천히 짙어지고 흘러 다닌다. 수명이 길어서 처음 몇 초 동안 점점 깔린다.
    private static void SetupFog(GameObject go, Vector2 area)
    {
        ParticleSystem ps = GetOrAdd<ParticleSystem>(go);
        ParticleSystem.MainModule main = ps.main;
        main.duration = 6f;
        main.loop = true;
        main.prewarm = false;
        main.playOnAwake = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(4f, 6f);
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(2.5f, 4.5f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.03f, 0.02f, 0.06f, 0.55f),
            new Color(0.10f, 0.05f, 0.14f, 0.45f));
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        main.maxParticles = 200;

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.position = Vector3.zero;
        shape.rotation = Vector3.zero;
        shape.scale = new Vector3(area.x, area.y, 0f);

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 12f;

        ParticleSystem.VelocityOverLifetimeModule velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.x = new ParticleSystem.MinMaxCurve(-0.15f, 0.15f);
        velocity.y = new ParticleSystem.MinMaxCurve(-0.05f, 0.05f);
        velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);

        ParticleSystem.RotationOverLifetimeModule rotation = ps.rotationOverLifetime;
        rotation.enabled = true;
        rotation.z = new ParticleSystem.MinMaxCurve(-0.17f, 0.17f); // ±10°/s

        ParticleSystem.SizeOverLifetimeModule size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, Curve(0f, 0.8f, 1f, 1.15f));

        // 서서히 나타나 오래 머물다 사라진다.
        ParticleSystem.ColorOverLifetimeModule color = ps.colorOverLifetime;
        color.enabled = true;
        color.color = AlphaGradient(Color.white, (0f, 0f), (0.3f, 1f), (0.75f, 1f), (1f, 0f));

        ParticleSystem.NoiseModule noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.15f;
        noise.frequency = 0.3f;
        noise.scrollSpeed = 0.1f;

        ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = LoadMaterial(SmokeMaterialPath);
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortingLayerName = "VfxBack"; // 캐릭터 발밑
        renderer.sortingOrder = 0;
        renderer.maxParticleSize = 2f;
    }

    // ---------------------------------------------------------------- MoonlightFogVFX

    [MenuItem("Tools/Boss2 VFX/Build MoonlightFogVFX (검은 안개 + 달빛)")]
    public static void BuildMoonlightFog()
    {
        Material beamMaterial = GetOrCreateAdditiveMaterial(BeamMaterialPath, GetOrCreateBeamTexture());
        Material dustMaterial = GetOrCreateAdditiveMaterial(DustMaterialPath, LoadMaterial(SmokeMaterialPath).GetTexture("_BaseMap"));

        GameObject root = new GameObject("MoonlightFogVFX");
        try
        {
            SetupFog(GetOrCreateChild(root.transform, "Fog"), new Vector2(16f, 7f));

            // 오른쪽 위에 빛의 축을 두고, 축을 반시계로 30° 돌린다. 빛은 축의 아래쪽(-Y)으로 뻗는다.
            GameObject pivot = GetOrCreateChild(root.transform, "MoonBeamPivot");
            pivot.transform.localPosition = new Vector3(7f, 5f, 0f);
            pivot.transform.localRotation = Quaternion.Euler(0f, 0f, -45f);
            GetOrAdd<Boss2LightSweep>(pivot);

            Vector3 beamCenter = new Vector3(0f, -BeamLength * 0.5f, 0f);
            SetupBeam(GetOrCreateChild(pivot.transform, "Beam"), beamCenter, 2.4f, new Color(0.7f, 0.82f, 1f, 0.45f), beamMaterial, 0);
            SetupBeam(GetOrCreateChild(pivot.transform, "BeamCore"), beamCenter, 0.8f, new Color(0.9f, 0.95f, 1f, 0.6f), beamMaterial, 1);
            SetupMoonDust(GetOrCreateChild(pivot.transform, "MoonDust"), beamCenter, dustMaterial);

            SavePrefab(root, MoonlightPath);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    // ---------------------------------------------------------------- EndingBeamVFX

    private const string EndingBeamPath = EffectFolder + "/EndingBeamVFX.prefab";
    private const string EndingBeamWashTexturePath = TextureFolder + "/T_Boss2EndingBeamWash.png";
    private const string EndingBeamWashMaterialPath = MaterialFolder + "/M_Boss2EndingBeamWashAdditive.mat";
    private const string EndingBeamCoreTexturePath = TextureFolder + "/T_Boss2EndingBeamCore.png";
    private const string EndingBeamCoreMaterialPath = MaterialFolder + "/M_Boss2EndingBeamCoreAdditive.mat";
    // 바닥(Background·Default) 위, 보스(Boss) 뒤
    private const string EndingBeamSortingLayer = "VfxBack";
    // 엔딩 빛줄기: 루트가 빛의 윗끝이고 아래(-Y)로 뻗는다.
    // 두께 1 = 빛이 비추는 전체 폭 (Boss2EndingCinematic 이 포커스 타원의 가로 길이에 맞춰 X 스케일로 정한다)
    private const float EndingBeamLength = 11f;
    // 엔딩 내내 켜 두고, 끝나면 스크립트가 지운다.
    private const float EndingBeamDuration = 30f;

    /// <summary>
    /// 보스2 엔딩 빛줄기. 가운데는 강하게, 바깥은 은은하게 퍼지도록 폭이 다른 층을 겹친다.
    ///   - Wash    : 전체 폭(포커스 타원 가로 길이)을 은은하게 채우는 넓은 빛. 가장자리만 부드럽게 사그라든다
    ///   - Beam    : 가운데 쪽 중간 세기
    ///   - BeamCore: 가운데 강한 빛
    ///   - BeamHot : 한가운데 가장 밝은 심지
    /// 빛줄기 텍스처·재질은 엔딩 전용이다. (달빛 빛줄기 것은 건드리지 않는다)
    /// </summary>
    [MenuItem("Tools/Boss2 VFX/Build EndingBeamVFX (보스2 엔딩 빛줄기)")]
    public static void BuildEndingBeam()
    {
        // 달빛과 같이 쓰는 먼지 재질은 있으면 그대로 쓴다. (다시 설정하면 달빛 쪽에서 맞춰 둔 색이 덮인다)
        Material dustMaterial = AssetDatabase.LoadAssetAtPath<Material>(DustMaterialPath) ?? GetOrCreateAdditiveMaterial(DustMaterialPath, LoadMaterial(SmokeMaterialPath).GetTexture("_BaseMap"));
        // 빛줄기는 엔딩 전용 텍스처·재질을 쓴다.
        Material beamMaterial = GetOrCreateAdditiveMaterial(EndingBeamCoreMaterialPath, CreateEndingBeamTexture(EndingBeamCoreTexturePath, false));
        Material washMaterial = GetOrCreateAdditiveMaterial(EndingBeamWashMaterialPath, CreateEndingBeamTexture(EndingBeamWashTexturePath, true));

        GameObject root = new GameObject("EndingBeamVFX");
        try
        {
            Vector3 beamCenter = new Vector3(0f, -EndingBeamLength * 0.5f, 0f);
            SetupEndingBeamPart(GetOrCreateChild(root.transform, "Wash"), beamCenter, 1f, new Color(0.75f, 0.85f, 1f, 0.2f), washMaterial, 0);
            SetupEndingBeamPart(GetOrCreateChild(root.transform, "Beam"), beamCenter, 0.45f, new Color(0.8f, 0.9f, 1f, 0.45f), beamMaterial, 1);
            SetupEndingBeamPart(GetOrCreateChild(root.transform, "BeamCore"), beamCenter, 0.2f, new Color(0.95f, 0.98f, 1f, 1f), beamMaterial, 2);
            SetupEndingBeamPart(GetOrCreateChild(root.transform, "BeamHot"), beamCenter, 0.08f, Color.white, beamMaterial, 3);

            GameObject dust = GetOrCreateChild(root.transform, "MoonDust");
            SetupMoonDust(dust, beamCenter, dustMaterial);
            ParticleSystem dustPs = dust.GetComponent<ParticleSystem>();
            ParticleSystem.MainModule dustMain = dustPs.main;
            dustMain.duration = EndingBeamDuration;
            dustMain.loop = true;
            dustMain.startDelay = 0.6f;
            ParticleSystem.ShapeModule dustShape = dustPs.shape;
            dustShape.scale = new Vector3(0.6f, EndingBeamLength * 0.9f, 0f);
            dust.GetComponent<ParticleSystemRenderer>().sortingLayerName = EndingBeamSortingLayer;

            SavePrefab(root, EndingBeamPath);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    // ---------------------------------------------------------------- EyeFlameVFX

    private const string EyeFlamePath = EffectFolder + "/EyeFlameVFX.prefab";
    private const string FireMaterialPath = MaterialFolder + "/M_Boss2FireAdditive.mat";

    /// <summary>
    /// 보스2 엔딩에서 눈 위에 타오르는 작은 검은 불. 루프로 타오르고, Boss2EndingCinematic 이 멈추면 불이 꺼진다.
    ///   - Flame : 위로 날름거리며 사라지는 검은 불꽃 (알파)
    ///   - Glow  : 불꽃 가장자리의 보랏빛 (가산) — 어두운 몸 위에서도 불 모양이 읽히게
    /// </summary>
    [MenuItem("Tools/Boss2 VFX/Build EyeFlameVFX (보스2 엔딩 눈의 검은 불)")]
    public static void BuildEyeFlame()
    {
        Material smoke = LoadMaterial(SmokeMaterialPath);
        Material fire = LoadMaterial(FireMaterialPath);

        GameObject root = new GameObject("EyeFlameVFX");
        try
        {
            SetupEyeFlameLayer(GetOrCreateChild(root.transform, "Glow"), fire, new Color(0.6f, 0.2f, 0.95f, 1f), 0.16f, 0.3f, 40f, 1);
            SetupEyeFlameLayer(GetOrCreateChild(root.transform, "Flame"), smoke, new Color(0.03f, 0.01f, 0.06f, 1f), 0.14f, 0.26f, 60f, 2);
            SavePrefab(root, EyeFlamePath);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    private static void SetupEyeFlameLayer(GameObject go, Material material, Color color, float sizeMin, float sizeMax, float rate, int sortingOrder)
    {
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;

        ParticleSystem ps = GetOrAdd<ParticleSystem>(go);
        ParticleSystem.MainModule main = ps.main;
        main.duration = 1f;
        main.loop = true;
        main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.6f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.4f, 0.8f);
        main.startSize = new ParticleSystem.MinMaxCurve(sizeMin, sizeMax);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = color;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        main.maxParticles = 80;

        // 눈 너비만큼 가로로 퍼진 자리에서 위로 날름거린다.
        ParticleSystem.ShapeModule shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(0.28f, 0.06f, 0f);

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = rate;

        ParticleSystem.VelocityOverLifetimeModule velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.x = new ParticleSystem.MinMaxCurve(-0.08f, 0.08f);
        velocity.y = new ParticleSystem.MinMaxCurve(0.5f, 0.9f);
        velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);

        ParticleSystem.SizeOverLifetimeModule size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, Curve(0f, 1f, 1f, 0.1f));

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        colorOverLifetime.color = AlphaGradient(Color.white, (0f, 0f), (0.15f, 1f), (0.6f, 0.8f), (1f, 0f));

        ParticleSystem.NoiseModule noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.25f;
        noise.frequency = 2.5f;
        noise.scrollSpeed = 1.5f;

        ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = material;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        // 보스2 본체(Boss 레이어) 위에 그려지도록 한다. Boss2EndingCinematic 이 본체 순서에 맞춰 다시 정한다.
        renderer.sortingLayerName = "Boss";
        renderer.sortingOrder = sortingOrder;
    }

    // 달빛 빛줄기와 같은 입자를 쓰되, 엔딩 동안 계속 켜져 있도록 수명·길이·알파를 바꾼다.
    private static void SetupEndingBeamPart(GameObject go, Vector3 localPosition, float width, Color tint, Material material, int sortingOrder)
    {
        SetupBeam(go, localPosition, width, tint, material, sortingOrder);

        ParticleSystem ps = go.GetComponent<ParticleSystem>();
        ParticleSystem.MainModule main = ps.main;
        main.duration = EndingBeamDuration;
        main.startLifetime = EndingBeamDuration;
        main.startSizeY = EndingBeamLength;

        // 빛줄기가 내려오는 모습은 스크립트가 스케일로 보여 주므로, 알파는 바로 켜고 끝까지 유지한다.
        ParticleSystem.ColorOverLifetimeModule color = ps.colorOverLifetime;
        color.color = AlphaGradient(Color.white, (0f, 0f), (0.01f, 1f), (1f, 1f));

        ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.maxParticleSize = 20f;
        // 엔딩 빛줄기는 보스 뒤(바닥 위)에 그린다. 앞에 그리면 가운데 선 보스 몸이 빛에 덮인다.
        renderer.sortingLayerName = EndingBeamSortingLayer;
    }

    // 긴 빛줄기 입자 하나. 축(부모)을 따라 돌도록 로컬 공간·로컬 정렬로 그린다.
    private static void SetupBeam(GameObject go, Vector3 localPosition, float width, Color tint, Material material, int sortingOrder)
    {
        go.transform.localPosition = localPosition;
        go.transform.localRotation = Quaternion.identity;

        ParticleSystem ps = GetOrAdd<ParticleSystem>(go);
        ParticleSystem.MainModule main = ps.main;
        main.duration = BeamDuration;
        main.loop = false;
        main.playOnAwake = true;
        main.startLifetime = BeamDuration;
        main.startSpeed = 0f;
        main.startSize3D = true;
        main.startSizeX = width;
        main.startSizeY = BeamLength;
        main.startSizeZ = 1f;
        main.startRotation = 0f;
        main.startColor = tint;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        main.maxParticles = 1;

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.enabled = false;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 1) });

        // 안개가 깔린 뒤에 천천히 비추고, 끝에서 사라진다.
        ParticleSystem.ColorOverLifetimeModule color = ps.colorOverLifetime;
        color.enabled = true;
        color.color = AlphaGradient(Color.white, (0f, 0f), (0.2f, 1f), (0.85f, 1f), (1f, 0f));

        ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = material;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.alignment = ParticleSystemRenderSpace.Local;
        renderer.sortingLayerName = "Light";
        renderer.sortingOrder = sortingOrder;
        renderer.maxParticleSize = 5f;
    }

    // 빛줄기 안에서 반짝이며 떠다니는 먼지
    private static void SetupMoonDust(GameObject go, Vector3 localPosition, Material material)
    {
        go.transform.localPosition = localPosition;
        go.transform.localRotation = Quaternion.identity;

        ParticleSystem ps = GetOrAdd<ParticleSystem>(go);
        ParticleSystem.MainModule main = ps.main;
        main.duration = BeamDuration;
        main.loop = false;
        main.playOnAwake = true;
        main.startDelay = 0.8f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.5f, 3f);
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.1f);
        main.startColor = new Color(0.85f, 0.92f, 1f, 0.9f);
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        main.maxParticles = 150;

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.position = Vector3.zero;
        shape.rotation = Vector3.zero;
        shape.scale = new Vector3(1.4f, BeamLength * 0.9f, 0f);

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 25f;

        ParticleSystem.VelocityOverLifetimeModule velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.x = new ParticleSystem.MinMaxCurve(-0.1f, 0.1f);
        velocity.y = new ParticleSystem.MinMaxCurve(-0.15f, 0.05f);
        velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);

        ParticleSystem.ColorOverLifetimeModule color = ps.colorOverLifetime;
        color.enabled = true;
        color.color = AlphaGradient(Color.white, (0f, 0f), (0.3f, 1f), (0.7f, 1f), (1f, 0f));

        ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = material;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortingLayerName = "Light";
        renderer.sortingOrder = 2;
    }

    // ---------------------------------------------------------------- 머티리얼 · 텍스처

    // URP Particles/Unlit 가산(Additive) 머티리얼
    private static Material GetOrCreateAdditiveMaterial(string path, Texture texture)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }

        material.SetFloat("_Surface", 1f);   // Transparent
        material.SetFloat("_Blend", 2f);     // Additive
        material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        material.SetFloat("_DstBlend", (float)BlendMode.One);
        if (material.HasProperty("_SrcBlendAlpha")) material.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
        if (material.HasProperty("_DstBlendAlpha")) material.SetFloat("_DstBlendAlpha", (float)BlendMode.One);
        material.SetFloat("_ZWrite", 0f);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.renderQueue = (int)RenderQueue.Transparent;
        material.SetOverrideTag("RenderType", "Transparent");
        material.SetTexture("_BaseMap", texture);
        material.SetColor("_BaseColor", Color.white);

        EditorUtility.SetDirty(material);
        AssetDatabase.SaveAssets();
        return material;
    }

    // 가로로는 가운데가 밝고, 세로로는 위(광원)에서 아래로 갈수록 흐려지는 빛줄기 텍스처
    private static Texture2D GetOrCreateBeamTexture()
    {
        Texture2D existing = AssetDatabase.LoadAssetAtPath<Texture2D>(BeamTexturePath);
        if (existing != null)
            return existing;

        const int Width = 64;
        const int Height = 256;
        Texture2D texture = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
        for (int y = 0; y < Height; y++)
        {
            float v = (float)y / (Height - 1);   // 0 = 아래 끝, 1 = 위(광원)
            float vertical = Mathf.SmoothStep(0f, 1f, v / 0.6f) * Mathf.SmoothStep(0f, 1f, (1f - v) / 0.05f);
            for (int x = 0; x < Width; x++)
            {
                float u = ((float)x / (Width - 1) - 0.5f) / 0.5f; // -1 ~ 1
                float horizontal = Mathf.Exp(-u * u * 3.5f) * (1f - Mathf.Abs(u));
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(horizontal * vertical)));
            }
        }
        texture.Apply();

        Directory.CreateDirectory(TextureFolder);
        File.WriteAllBytes(BeamTexturePath, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(BeamTexturePath);

        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(BeamTexturePath);
        importer.textureType = TextureImporterType.Default;
        importer.alphaIsTransparency = true;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Texture2D>(BeamTexturePath);
    }

    // 엔딩 빛줄기 텍스처. 화면이 보스에 가깝게 확대돼 있어 보스 높이에서도 밝아야 하므로, 아래쪽은 짧게(15%)만 흐려진다.
    //   wash = true : 가로로 대부분 고르게 밝고 가장자리 25% 에서만 부드럽게 사그라든다 (텍스처 폭 = 비추는 폭)
    //   wash = false: 가운데만 밝고 바깥으로 빠르게 어두워진다 (달빛 빛줄기와 같은 가로 모양)
    // 실행할 때마다 다시 그린다.
    private static Texture2D CreateEndingBeamTexture(string path, bool wash)
    {
        const int Width = 64;
        const int Height = 256;
        Texture2D texture = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
        for (int y = 0; y < Height; y++)
        {
            float v = (float)y / (Height - 1);   // 0 = 아래 끝, 1 = 위(광원)
            float vertical = Mathf.SmoothStep(0f, 1f, v / 0.15f) * Mathf.SmoothStep(0f, 1f, (1f - v) / 0.05f);
            for (int x = 0; x < Width; x++)
            {
                float u = Mathf.Abs((float)x / (Width - 1) - 0.5f) / 0.5f; // 0 = 가운데, 1 = 가장자리
                float horizontal = wash
                    ? Mathf.SmoothStep(0f, 1f, (1f - u) / 0.25f) * Mathf.Lerp(1f, 0.75f, u)
                    : Mathf.Exp(-u * u * 3.5f) * (1f - u);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(horizontal * vertical)));
            }
        }
        texture.Apply();

        Directory.CreateDirectory(TextureFolder);
        File.WriteAllBytes(path, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Default;
        importer.alphaIsTransparency = true;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    // ---------------------------------------------------------------- 공용

    private static void SavePrefab(GameObject root, string path)
    {
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Debug.Log($"[Boss2VfxBuilder] {path} 저장 완료");
    }

    private static Material LoadMaterial(string path)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            Debug.LogWarning($"[Boss2VfxBuilder] 머티리얼을 찾지 못했습니다: {path}");
        }
        return material;
    }

    private static GameObject GetOrCreateChild(Transform parent, string name)
    {
        Transform child = parent.Find(name);
        if (child != null)
            return child.gameObject;

        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go;
    }

    private static T GetOrAdd<T>(GameObject go) where T : Component
    {
        T component = go.GetComponent<T>();
        return component != null ? component : go.AddComponent<T>();
    }

    private static AnimationCurve Curve(float t0, float v0, float t1, float v1)
    {
        return AnimationCurve.Linear(t0, v0, t1, v1);
    }

    private static ParticleSystem.MinMaxGradient AlphaGradient(Color color, params (float time, float alpha)[] alphaKeys)
    {
        GradientAlphaKey[] alphas = new GradientAlphaKey[alphaKeys.Length];
        for (int i = 0; i < alphaKeys.Length; i++)
        {
            alphas[i] = new GradientAlphaKey(alphaKeys[i].alpha, alphaKeys[i].time);
        }

        Gradient gradient = new Gradient();
        gradient.SetKeys(new[] { new GradientColorKey(color, 0f), new GradientColorKey(color, 1f) }, alphas);
        return new ParticleSystem.MinMaxGradient(gradient);
    }
}
