using UnityEditor;
using UnityEngine;

/// <summary>
/// 보스1 중심에서 검은 기운이 사방으로 뿜어져 나오는 파티클(DarkBurstVFX)을 만든다. 메뉴: Tools / Boss2 VFX
/// 참고: 센과 치히로의 행방불명 '오물신' 장면 — 막혀 있던 오염된 기운이 한꺼번에 터지며 뭉게뭉게 방 안을 채우는 느낌.
///   - Haze    : 덩어리 사이를 메우며 넓게 번지는 부드러운 안개 (바탕)
///   - Core    : 중심에서 끓어오르듯 꿈틀대는 짙은 덩어리 (근원)
///   - Billow  : 사방으로 터져 나가며 감속하고 부풀어 오르는 검은 구름 (주된 몸통)
///   - Streaks : 빠르게 뻗어 나가는 가는 검은 줄기 (분출 속도감)
///   - Embers  : 함께 튀어 나가는 보랏빛 잔불 (어두운 배경에서도 형태가 읽히게)
/// 모든 층은 루프로 계속 방출한다. 시네마틱(Boss2IntroCinematic)이 방출량·속도를 커브로 조절한다.
/// 값은 모두 절대값으로 넣으므로 여러 번 실행해도 결과가 같다.
/// </summary>
public static class Boss2DarkBurstVfxBuilder
{
    public const string PrefabPath = "Assets/03.Prefabs/Boss2/Effect/DarkBurstVFX.prefab";

    private const string CloudMaterialPath = "Assets/09.VFX/Boss2/BlackCloud/M_BlackCloud.mat";
    private const string SmokeMaterialPath = "Assets/08.Materials/Boss2/M_Boss2SmokeAlpha.mat";
    private const string EmberMaterialPath = "Assets/08.Materials/Boss2/M_Boss2MoonDustAdditive.mat";
    private const string SortingLayer = "VfxBack";

    [MenuItem("Tools/Boss2 VFX/Build DarkBurstVFX (검은 기운 분출)")]
    public static void Build()
    {
        var root = new GameObject("DarkBurstVFX");
        try
        {
            // 루트는 방출하지 않는 빈 시스템 (자식들을 한 번에 Play/Stop 하기 위함)
            var rootPs = root.AddComponent<ParticleSystem>();
            var rootMain = rootPs.main;
            rootMain.loop = true;
            rootMain.playOnAwake = false;
            var rootEmission = rootPs.emission;
            rootEmission.enabled = false;
            rootPs.GetComponent<ParticleSystemRenderer>().enabled = false;

            Material cloud = AssetDatabase.LoadAssetAtPath<Material>(CloudMaterialPath);
            Material smoke = AssetDatabase.LoadAssetAtPath<Material>(SmokeMaterialPath);
            Material ember = AssetDatabase.LoadAssetAtPath<Material>(EmberMaterialPath);

            SetupHaze(CreateChild(root.transform, "Haze"), smoke);
            SetupCore(CreateChild(root.transform, "Core"), cloud);
            SetupBillow(CreateChild(root.transform, "Billow"), cloud);
            SetupStreaks(CreateChild(root.transform, "Streaks"), smoke);
            SetupEmbers(CreateChild(root.transform, "Embers"), ember);

            // 루트는 그리지 않는다. 머티리얼이 없어 입자가 생기면 마젠타 박스로 보이므로 저장 직전에 다시 확실히 끈다.
            var rootRenderer = rootPs.GetComponent<ParticleSystemRenderer>();
            rootRenderer.enabled = false;
            rootRenderer.sharedMaterial = null;

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            if (saved.GetComponent<ParticleSystemRenderer>().enabled)
            {
                Debug.LogError("[Boss2DarkBurstVfxBuilder] 루트 렌더러가 켜진 채로 저장됐습니다: " + PrefabPath);
            }
            Debug.Log("[Boss2DarkBurstVfxBuilder] DarkBurstVFX 생성 완료: " + PrefabPath);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    // 바탕: 구름 덩어리 사이를 메우며 넓게 번지는 부드러운 검은 안개
    private static void SetupHaze(ParticleSystem ps, Material material)
    {
        BaseMain(ps, 2.2f, 3.2f, 1.2f, 2.8f, 1.6f, 2.6f, 120);

        var emission = ps.emission;
        emission.rateOverTime = 24f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.3f;
        shape.radiusThickness = 1f;

        var limit = ps.limitVelocityOverLifetime;
        limit.enabled = true;
        limit.limit = new ParticleSystem.MinMaxCurve(1f, Curve((0f, 2.8f), (0.4f, 0.5f), (1f, 0.2f)));
        limit.dampen = 0.15f;

        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, Curve((0f, 0.5f), (1f, 2.4f)));

        var color = ps.colorOverLifetime;
        color.enabled = true;
        color.color = Gradient(new Color(0.05f, 0.03f, 0.08f), (0f, 0f), (0.15f, 0.75f), (0.7f, 0.55f), (1f, 0f));

        Renderer(ps, material, 0);
    }

    // 근원: 거의 제자리에서 끓어오르는 짙은 덩어리
    private static void SetupCore(ParticleSystem ps, Material material)
    {
        BaseMain(ps, 1.2f, 1.8f, 0.2f, 0.7f, 1.2f, 1.8f, 50);

        var emission = ps.emission;
        emission.rateOverTime = 14f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.2f;
        shape.radiusThickness = 1f;

        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, Curve((0f, 0.5f), (0.4f, 1f), (1f, 1.3f)));

        var rotation = ps.rotationOverLifetime;
        rotation.enabled = true;
        rotation.z = new ParticleSystem.MinMaxCurve(-60f * Mathf.Deg2Rad, 60f * Mathf.Deg2Rad);

        var color = ps.colorOverLifetime;
        color.enabled = true;
        color.color = Gradient(new Color(0.42f, 0.34f, 0.5f), (0f, 0f), (0.15f, 0.85f), (0.7f, 0.7f), (1f, 0f));

        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.35f;
        noise.frequency = 0.8f;
        noise.scrollSpeed = 0.6f;

        CloudSheet(ps);
        Renderer(ps, material, 2);
    }

    // 몸통: 사방으로 터져 나가며 빠르게 감속하고 크게 부풀어 방을 채운다
    private static void SetupBillow(ParticleSystem ps, Material material)
    {
        BaseMain(ps, 1.8f, 2.8f, 2f, 4.5f, 0.6f, 1.1f, 260);

        var emission = ps.emission;
        emission.rateOverTime = 55f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.3f;
        shape.radiusThickness = 1f;
        shape.randomDirectionAmount = 0.15f;

        // 연기처럼 처음엔 세게 튀어 나가다 공기 저항으로 급격히 느려진다
        var limit = ps.limitVelocityOverLifetime;
        limit.enabled = true;
        limit.limit = new ParticleSystem.MinMaxCurve(1f, Curve((0f, 5f), (0.35f, 0.6f), (1f, 0.25f)));
        limit.dampen = 0.18f;

        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, Curve((0f, 0.4f), (0.3f, 1.6f), (1f, 3.4f)));

        var rotation = ps.rotationOverLifetime;
        rotation.enabled = true;
        rotation.z = new ParticleSystem.MinMaxCurve(-35f * Mathf.Deg2Rad, 35f * Mathf.Deg2Rad);

        var color = ps.colorOverLifetime;
        color.enabled = true;
        color.color = Gradient2(
            new Color(0.55f, 0.45f, 0.65f), new Color(0.3f, 0.25f, 0.38f),
            (0f, 0f), (0.06f, 0.95f), (0.55f, 0.75f), (1f, 0f));

        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = new ParticleSystem.MinMaxCurve(1f, Curve((0f, 0.1f), (1f, 0.6f)));
        noise.frequency = 0.5f;
        noise.scrollSpeed = 0.4f;
        noise.damping = true;

        CloudSheet(ps);
        Renderer(ps, material, 1);
    }

    // 분출 속도감: 길게 늘어난 가는 검은 줄기
    private static void SetupStreaks(ParticleSystem ps, Material material)
    {
        var main = BaseMain(ps, 0.35f, 0.6f, 6f, 10f, 0.12f, 0.22f, 80);
        main.startRotation = 0f;

        var emission = ps.emission;
        emission.rateOverTime = 26f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.25f;
        shape.radiusThickness = 1f;

        var limit = ps.limitVelocityOverLifetime;
        limit.enabled = true;
        limit.limit = new ParticleSystem.MinMaxCurve(1f, Curve((0f, 10f), (1f, 2f)));
        limit.dampen = 0.12f;

        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, Curve((0f, 1f), (1f, 0.3f)));

        var color = ps.colorOverLifetime;
        color.enabled = true;
        color.color = Gradient(new Color(0.22f, 0.13f, 0.28f), (0f, 0f), (0.1f, 0.8f), (1f, 0f));

        var renderer = Renderer(ps, material, 3);
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.lengthScale = 2.5f;
        renderer.velocityScale = 0.12f;
    }

    // 가독성: 함께 튀어 나가는 보랏빛 잔불
    private static void SetupEmbers(ParticleSystem ps, Material material)
    {
        var main = BaseMain(ps, 0.8f, 1.6f, 1.5f, 4.5f, 0.05f, 0.1f, 80);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.55f, 0.22f, 0.85f, 0.9f), new Color(0.85f, 0.25f, 0.45f, 0.9f));

        var emission = ps.emission;
        emission.rateOverTime = 22f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.3f;
        shape.radiusThickness = 1f;

        var limit = ps.limitVelocityOverLifetime;
        limit.enabled = true;
        limit.limit = new ParticleSystem.MinMaxCurve(1f, Curve((0f, 4.5f), (1f, 0.5f)));
        limit.dampen = 0.1f;

        var color = ps.colorOverLifetime;
        color.enabled = true;
        color.color = Gradient(Color.white, (0f, 0f), (0.1f, 1f), (0.6f, 0.8f), (1f, 0f));

        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.8f;
        noise.frequency = 1.2f;
        noise.scrollSpeed = 1f;

        Renderer(ps, material, 4);
    }

    #region 공통

    private static ParticleSystem.MainModule BaseMain(ParticleSystem ps, float lifeMin, float lifeMax,
        float speedMin, float speedMax, float sizeMin, float sizeMax, int maxParticles)
    {
        var main = ps.main;
        main.duration = 5f;
        main.loop = true;
        main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(lifeMin, lifeMax);
        main.startSpeed = new ParticleSystem.MinMaxCurve(speedMin, speedMax);
        main.startSize = new ParticleSystem.MinMaxCurve(sizeMin, sizeMax);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = Color.white;
        main.gravityModifier = 0f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        main.maxParticles = maxParticles;
        return main;
    }

    // T_BlackClouds: 세로 6칸 구름 시트. 입자마다 무작위 한 칸을 고정으로 쓴다.
    private static void CloudSheet(ParticleSystem ps)
    {
        var sheet = ps.textureSheetAnimation;
        sheet.enabled = true;
        sheet.mode = ParticleSystemAnimationMode.Grid;
        sheet.numTilesX = 1;
        sheet.numTilesY = 6;
        sheet.animation = ParticleSystemAnimationType.WholeSheet;
        sheet.frameOverTime = new ParticleSystem.MinMaxCurve(0f);
        sheet.startFrame = new ParticleSystem.MinMaxCurve(0f, 5.99f);
        sheet.cycleCount = 1;
    }

    private static ParticleSystemRenderer Renderer(ParticleSystem ps, Material material, int order)
    {
        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sharedMaterial = material;
        renderer.sortingLayerName = SortingLayer;
        renderer.sortingOrder = order;
        renderer.alignment = ParticleSystemRenderSpace.View;
        return renderer;
    }

    private static ParticleSystem CreateChild(Transform parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.AddComponent<ParticleSystem>();
    }

    private static AnimationCurve Curve(params (float t, float v)[] keys)
    {
        var curve = new AnimationCurve();
        foreach (var k in keys)
        {
            curve.AddKey(k.t, k.v);
        }
        return curve;
    }

    private static ParticleSystem.MinMaxGradient Gradient(Color color, params (float t, float a)[] alphas)
    {
        return Gradient2(color, color, alphas);
    }

    // 색은 start → end 로, 알파는 키대로
    private static ParticleSystem.MinMaxGradient Gradient2(Color start, Color end, params (float t, float a)[] alphas)
    {
        var gradient = new Gradient();
        var alphaKeys = new GradientAlphaKey[alphas.Length];
        for (int i = 0; i < alphas.Length; i++)
        {
            alphaKeys[i] = new GradientAlphaKey(alphas[i].a, alphas[i].t);
        }
        gradient.SetKeys(new[] { new GradientColorKey(start, 0f), new GradientColorKey(end, 1f) }, alphaKeys);
        return new ParticleSystem.MinMaxGradient(gradient);
    }

    #endregion
}
