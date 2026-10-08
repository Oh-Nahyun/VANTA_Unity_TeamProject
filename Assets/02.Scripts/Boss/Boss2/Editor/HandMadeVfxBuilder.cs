using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 발 먼지 / 공격 불꽃 파티클 프리팹을 만든다. 메뉴: Tools / Boss2 VFX / Build Foot Dust + Hit Spark
///   - FootDustVFX : 캐릭터 자식으로 붙이면 이동한 거리만큼 발밑에서 먼지가 퐁퐁 피어오른다 (Rate over Distance)
///   - HitSparkVFX : 공격 순간에 생성(또는 Play)하면 불티가 매번 다른 랜덤 방향으로 튄다. 다 튀면 스스로 파괴
/// 값은 절대값으로 넣으므로 여러 번 실행해도 결과가 같다. 텍스처는 같은 폴더의 T_FootDust / T_HitSpark / T_HitFlash.
/// </summary>
public static class HandMadeVfxBuilder
{
    private const string Folder = "Assets/09.VFX/Boss2/HandMade";

    [MenuItem("Tools/Boss2 VFX/Build Foot Dust + Hit Spark")]
    public static void BuildAll()
    {
        BuildFootDust();
        BuildHitSpark();
        AssetDatabase.SaveAssets();
    }

    // ---------------------------------------------------------------- 발 먼지

    public static void BuildFootDust()
    {
        Material material = GetOrCreateSpriteMaterial(Folder + "/M_FootDust.mat", Folder + "/T_FootDust.png");

        GameObject root = new GameObject("FootDustVFX");
        try
        {
            ParticleSystem ps = root.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = ps.main;
            main.duration = 1f;
            main.loop = true;
            main.playOnAwake = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.55f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 0.8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.25f, 0.4f);
            main.gravitySource = ParticleSystemGravitySource.Physics2D;
            main.gravityModifier = -0.05f;                      // 살짝 떠오른다
            main.simulationSpace = ParticleSystemSimulationSpace.World;   // 지나간 자리에 남는다
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.maxParticles = 50;

            // 걸을 때만: 1 유닛 이동마다 4개 (가만히 있으면 안 나온다)
            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.rateOverDistance = 4f;

            // 발밑 반원(위쪽)으로 퍼진다
            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.08f;
            shape.radiusThickness = 1f;
            shape.arc = 180f;
            shape.rotation = Vector3.zero;

            ParticleSystem.VelocityOverLifetimeModule velocity = ps.velocityOverLifetime;
            velocity.enabled = true;
            velocity.speedModifier = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0.15f));

            ParticleSystem.SizeOverLifetimeModule size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.7f, 1f, 1.2f));

            ParticleSystem.ColorOverLifetimeModule color = ps.colorOverLifetime;
            color.enabled = true;
            color.color = Gradient(new[] { Color.white, Color.white }, new[] { 0f, 1f }, new[] { 1f, 1f, 0f }, new[] { 0f, 0.6f, 1f });

            // 4프레임 퐁 → 고리 → 흩어짐
            ParticleSystem.TextureSheetAnimationModule sheet = ps.textureSheetAnimation;
            sheet.enabled = true;
            sheet.mode = ParticleSystemAnimationMode.Grid;
            sheet.numTilesX = 4;
            sheet.numTilesY = 1;
            sheet.animation = ParticleSystemAnimationType.WholeSheet;
            sheet.timeMode = ParticleSystemAnimationTimeMode.Lifetime;
            sheet.frameOverTime = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0f, 1f, 1f));

            ParticleSystemRenderer renderer = root.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sharedMaterial = material;
            renderer.sortingLayerName = "VfxBack";              // 캐릭터 발 뒤
            renderer.sortingOrder = 0;
            renderer.flip = new Vector3(0.5f, 0f, 0f);          // 절반은 좌우 반전 → 같은 모양 반복이 덜 보인다

            Save(root, Folder + "/FootDustVFX.prefab");
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    // ---------------------------------------------------------------- 공격 불꽃

    public static void BuildHitSpark()
    {
        Material sparkMaterial = GetOrCreateAdditiveMaterial(Folder + "/M_HitSpark.mat", Folder + "/T_HitSpark.png");
        Material flashMaterial = GetOrCreateAdditiveMaterial(Folder + "/M_HitFlash.mat", Folder + "/T_HitFlash.png");

        GameObject root = new GameObject("HitSparkVFX");
        try
        {
            ParticleSystem ps = root.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = ps.main;
            main.duration = 0.4f;
            main.loop = false;
            main.playOnAwake = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.15f, 0.35f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(4f, 9f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.12f);
            main.startColor = new ParticleSystem.MinMaxGradient(Color.white, new Color(1f, 0.95f, 0.6f));
            main.gravitySource = ParticleSystemGravitySource.Physics2D;
            main.gravityModifier = 0.6f;                        // 튀었다가 아래로 떨어진다
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.maxParticles = 60;
            main.stopAction = ParticleSystemStopAction.Destroy; // 생성해서 쓰고 버리는 용도

            // 한 번에 10~16개
            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, new ParticleSystem.MinMaxCurve(10f, 16f)) });

            // 360도 아무 방향으로 + 방향을 조금씩 더 흔들어 매번 다른 모양으로 튄다
            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.05f;
            shape.radiusThickness = 1f;
            shape.arc = 360f;
            shape.arcMode = ParticleSystemShapeMultiModeValue.Random;
            shape.randomDirectionAmount = 0.2f;

            // 빠르게 튀었다가 급히 감속
            ParticleSystem.LimitVelocityOverLifetimeModule limit = ps.limitVelocityOverLifetime;
            limit.enabled = true;
            limit.drag = 6f;
            limit.multiplyDragByParticleVelocity = true;

            ParticleSystem.SizeOverLifetimeModule size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.3f));

            // 흰색 → 노랑 → 주황 → 빨강으로 식으며 사라진다 (HDR 로 Bloom)
            ParticleSystem.ColorOverLifetimeModule color = ps.colorOverLifetime;
            color.enabled = true;
            color.color = Gradient(
                new[] { new Color(1f, 1f, 1f), new Color(1f, 0.85f, 0.3f), new Color(1f, 0.45f, 0.1f), new Color(0.9f, 0.15f, 0.1f) },
                new[] { 0f, 0.25f, 0.6f, 1f },
                new[] { 1f, 1f, 0f }, new[] { 0f, 0.7f, 1f });

            // 살짝 지그재그로 흔들리는 불티
            ParticleSystem.NoiseModule noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.6f;
            noise.frequency = 2.5f;
            noise.scrollSpeed = 1f;

            ParticleSystemRenderer renderer = root.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch; // 날아가는 방향으로 길쭉하게
            renderer.velocityScale = 0.05f;
            renderer.lengthScale = 1.5f;
            renderer.sharedMaterial = sparkMaterial;
            renderer.sortingLayerName = "Vfx";
            renderer.sortingOrder = 10;

            // 가운데 번쩍임 (한 개, 아주 짧게)
            GameObject flashObj = new GameObject("Flash");
            flashObj.transform.SetParent(root.transform, false);
            ParticleSystem flash = flashObj.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule fm = flash.main;
            fm.duration = 0.2f;
            fm.loop = false;
            fm.playOnAwake = true;
            fm.startLifetime = 0.1f;
            fm.startSpeed = 0f;
            fm.startSize = new ParticleSystem.MinMaxCurve(0.6f, 0.8f);
            fm.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 0.5f);   // 십자가 매번 조금 돌아간다
            fm.startColor = new Color(1f, 0.95f, 0.75f);
            fm.simulationSpace = ParticleSystemSimulationSpace.World;
            fm.scalingMode = ParticleSystemScalingMode.Hierarchy;
            fm.maxParticles = 1;
            ParticleSystem.EmissionModule fe = flash.emission;
            fe.rateOverTime = 0f;
            fe.SetBursts(new[] { new ParticleSystem.Burst(0f, 1) });
            ParticleSystem.ShapeModule fs = flash.shape;
            fs.enabled = false;
            ParticleSystem.SizeOverLifetimeModule fsz = flash.sizeOverLifetime;
            fsz.enabled = true;
            fsz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 1.2f));
            ParticleSystem.ColorOverLifetimeModule fc = flash.colorOverLifetime;
            fc.enabled = true;
            fc.color = Gradient(new[] { Color.white, Color.white }, new[] { 0f, 1f }, new[] { 1f, 0f }, new[] { 0f, 1f });
            ParticleSystemRenderer fr = flashObj.GetComponent<ParticleSystemRenderer>();
            fr.renderMode = ParticleSystemRenderMode.Billboard;
            fr.sharedMaterial = flashMaterial;
            fr.sortingLayerName = "Vfx";
            fr.sortingOrder = 11;

            Save(root, Folder + "/HitSparkVFX.prefab");
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    // ---------------------------------------------------------------- 공용

    private static void Save(GameObject root, string path)
    {
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Debug.Log($"[HandMadeVfxBuilder] {path} 저장 완료");
    }

    private static ParticleSystem.MinMaxGradient Gradient(Color[] colors, float[] colorTimes, float[] alphas, float[] alphaTimes)
    {
        GradientColorKey[] ck = new GradientColorKey[colors.Length];
        for (int i = 0; i < colors.Length; i++) ck[i] = new GradientColorKey(colors[i], colorTimes[i]);
        GradientAlphaKey[] ak = new GradientAlphaKey[alphas.Length];
        for (int i = 0; i < alphas.Length; i++) ak[i] = new GradientAlphaKey(alphas[i], alphaTimes[i]);
        Gradient g = new Gradient();
        g.SetKeys(ck, ak);
        return new ParticleSystem.MinMaxGradient(g);
    }

    // 먼지: 보통 반투명 스프라이트 셰이더 (2D 라이트 영향 없음)
    private static Material GetOrCreateSpriteMaterial(string path, string texturePath)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }
        material.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        EditorUtility.SetDirty(material);
        return material;
    }

    // 불꽃·번쩍임: URP Particles/Unlit 가산(Additive)
    private static Material GetOrCreateAdditiveMaterial(string path, string texturePath)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            AssetDatabase.CreateAsset(material, path);
        }
        material.SetFloat("_Surface", 1f);
        material.SetFloat("_Blend", 2f);
        material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        material.SetFloat("_DstBlend", (float)BlendMode.One);
        material.SetFloat("_ZWrite", 0f);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.renderQueue = (int)RenderQueue.Transparent;
        material.SetOverrideTag("RenderType", "Transparent");
        material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));
        material.SetColor("_BaseColor", Color.white);
        EditorUtility.SetDirty(material);
        return material;
    }
}
