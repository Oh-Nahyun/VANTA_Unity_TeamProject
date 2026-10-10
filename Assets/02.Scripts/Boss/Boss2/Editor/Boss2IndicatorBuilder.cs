using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 보스2 공격 인디케이터 준비. 메뉴: Tools / Boss2 / Build Attack Indicators
///   - 직선 인디케이터 스프라이트 2장(테두리·채움)을 만든다. 광역기 인디케이터(T_Orb_*)와 같은 붉은 톤, 9-slice, 피벗 = 왼쪽 가운데.
///   - Boss2 프리팹에 Boss2IndicatorSpawner 를 붙이고 직선·원 스프라이트를 연결한다.
///   - "돌진 강타" 행동의 이동을 추적 달리기(ChaseRun) → 직선 돌진(DashLine)으로 바꾼다.
/// 여러 번 실행해도 결과가 같다.
/// </summary>
public static class Boss2IndicatorBuilder
{
    private const string IndicatorFolder = "Assets/04.Sprites/Boss2/Indicator";
    private const string LineBorderPath = IndicatorFolder + "/T_Line_Border.png";
    private const string LineFillPath = IndicatorFolder + "/T_Line_Fill.png";
    private const string OrbBorderPath = IndicatorFolder + "/T_Orb_Border.png";
    private const string OrbFillPath = IndicatorFolder + "/T_Orb_Fill.png";
    private const string Boss2PrefabPath = "Assets/03.Prefabs/Boss2/Boss/Boss2.prefab";
    // 광역기 인디케이터와 같은 재질 (조명 영향 없음)
    private const string UnlitSpriteMaterialPath = "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat";

    private const int Width = 64;
    private const int Height = 32;
    private const int PixelsPerUnit = 32;
    // 9-slice 테두리 (픽셀). 늘려도 가장자리 무늬가 찌그러지지 않는다.
    private const int Slice = 8;

    [MenuItem("Tools/Boss2/Build Attack Indicators")]
    public static void Build()
    {
        Sprite lineBorder = CreateLineSprite(LineBorderPath, true);
        Sprite lineFill = CreateLineSprite(LineFillPath, false);
        Sprite orbBorder = AssetDatabase.LoadAssetAtPath<Sprite>(OrbBorderPath);
        Sprite orbFill = FindSprite(AssetDatabase.LoadAllAssetsAtPath(OrbFillPath));

        GameObject root = PrefabUtility.LoadPrefabContents(Boss2PrefabPath);
        try
        {
            Boss2IndicatorSpawner spawner = root.GetComponent<Boss2IndicatorSpawner>();
            if (spawner == null)
            {
                spawner = root.AddComponent<Boss2IndicatorSpawner>();
            }

            var so = new SerializedObject(spawner);
            so.FindProperty("_lineBorderSprite").objectReferenceValue = lineBorder;
            so.FindProperty("_lineFillSprite").objectReferenceValue = lineFill;
            so.FindProperty("_circleBorderSprite").objectReferenceValue = orbBorder;
            so.FindProperty("_circleFillSprite").objectReferenceValue = orbFill;
            so.FindProperty("_material").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Material>(UnlitSpriteMaterialPath);
            so.ApplyModifiedPropertiesWithoutUndo();

            SwitchDashActionToLine(root.GetComponent<Boss2Controller>());

            PrefabUtility.SaveAsPrefabAsset(root, Boss2PrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        Debug.Log("[Boss2IndicatorBuilder] 공격 인디케이터 준비 완료");
    }

    // 강타 앞에서 달려가는(ChaseRun) 행동을 직선 돌진(DashLine)으로 바꾼다.
    private static void SwitchDashActionToLine(Boss2Controller controller)
    {
        if (controller == null)
            return;

        var so = new SerializedObject(controller);
        SerializedProperty actions = so.FindProperty("_actions");
        for (int i = 0; i < actions.arraySize; i++)
        {
            SerializedProperty action = actions.GetArrayElementAtIndex(i);
            SerializedProperty move = action.FindPropertyRelative("Move");
            SerializedProperty attack = action.FindPropertyRelative("Attack");
            if (move.intValue == (int)Boss2MovePatternType.ChaseRun && attack.intValue == (int)Boss2AttackPatternType.SmashAttack)
            {
                move.intValue = (int)Boss2MovePatternType.DashLine;
            }
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Sprite FindSprite(Object[] assets)
    {
        foreach (Object asset in assets)
        {
            if (asset is Sprite sprite)
                return sprite;
        }
        return null;
    }

    // 픽셀아트 톤의 직선 인디케이터 텍스처. 가로로 늘려 쓰므로 9-slice 로 가장자리를 지킨다.
    //   테두리: 안쪽은 비우고, 위아래·좌우 가장자리에 밝은 붉은 선 + 어두운 바깥 픽셀 + 듬성듬성한 파편
    //   채움  : 어두운 붉은 연기 무늬, 위아래 가장자리와 앞쪽(오른쪽) 끝이 밝다
    private static Sprite CreateLineSprite(string path, bool isBorder)
    {
        var texture = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
        var random = new System.Random(isBorder ? 7 : 13);
        Color bright = new Color(1f, 0.13f, 0.22f, 1f);
        Color mid = new Color(0.62f, 0.05f, 0.12f, 1f);
        Color dark = new Color(0.25f, 0.02f, 0.06f, 1f);

        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                int edgeY = Mathf.Min(y, Height - 1 - y);
                int edgeX = Mathf.Min(x, Width - 1 - x);
                Color c = Color.clear;

                if (isBorder)
                {
                    int edge = Mathf.Min(edgeX, edgeY);
                    if (edge == 2 || edge == 3)
                    {
                        c = bright;
                    }
                    else if (edge == 1 || edge == 4)
                    {
                        c = random.NextDouble() < 0.75 ? mid : Color.clear;
                    }
                    else if (edge == 0 || edge == 5)
                    {
                        c = random.NextDouble() < 0.35 ? dark : Color.clear;
                    }
                }
                else
                {
                    float noise = Mathf.PerlinNoise(x * 0.18f, y * 0.25f);
                    c = Color.Lerp(dark, mid, noise);
                    c.a = 0.85f;
                    if (edgeY <= 2)
                    {
                        c = Color.Lerp(mid, bright, 0.6f);
                    }
                    if (Width - 1 - x <= 2)
                    {
                        c = bright;
                    }
                }

                texture.SetPixel(x, y, c);
            }
        }

        texture.Apply();
        File.WriteAllBytes(path, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = PixelsPerUnit;
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.spriteBorder = new Vector4(Slice, Slice, Slice, Slice);
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteAlignment = (int)SpriteAlignment.Custom;
        settings.spritePivot = new Vector2(0f, 0.5f);
        // 9-slice 는 Full Rect 메시여야 한다.
        settings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
