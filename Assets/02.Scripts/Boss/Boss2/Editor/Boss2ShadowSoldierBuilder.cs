using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

/// <summary>
/// 그림자 병사 시트(T_ShadowSoldier_*.png)를 64x64 칸으로 잘라 병사 프리팹을 만들고,
/// ShadowThrone 프리팹에 ShadowSoldierTroop 을 붙여 연결한다. 다시 실행해도 같은 스프라이트 ID 를 유지한다.
/// 시트 구성: 0행 나타남, 1행 무기 idle, 2행 흩어짐. 칸 수는 같은 이름의 .txt (appear/idle/vanish) 에 적혀 있다.
/// </summary>
public static class Boss2ShadowSoldierBuilder
{
    private const string SpriteFolder = "Assets/04.Sprites/Boss2/ShadowSoldier";
    private const string PrefabFolder = "Assets/03.Prefabs/Boss2/Intro/ShadowSoldiers";
    private const string ThronePrefabPath = "Assets/03.Prefabs/Boss2/Intro/ShadowThrone.prefab";
    private const int Cell = 64;
    private const int Feet = 56;
    private const float PixelsPerUnit = 18f;

    [MenuItem("Tools/Boss2/Build Shadow Soldiers")]
    public static void Build()
    {
        if (AssetDatabase.IsValidFolder(PrefabFolder) == false)
        {
            Directory.CreateDirectory(PrefabFolder);
            AssetDatabase.Refresh();
        }

        Material material = LoadThroneMaterial();
        var prefabs = new List<ShadowSoldier>();
        foreach (string pngPath in Directory.GetFiles(SpriteFolder, "T_ShadowSoldier_*.png"))
        {
            string path = pngPath.Replace('\\', '/');
            string txtPath = Path.ChangeExtension(path, ".txt");
            if (File.Exists(txtPath) == false)
            {
                Debug.LogWarning($"[ShadowSoldier] {txtPath} 가 없어 건너뜁니다.");
                continue;
            }

            Dictionary<string, int> counts = ReadCounts(txtPath);
            string soldierName = Path.GetFileNameWithoutExtension(path).Replace("T_ShadowSoldier_", "");
            SliceSheet(path, soldierName, counts);

            Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
            ShadowSoldier prefab = CreatePrefab(soldierName, material,
                Pick(sprites, soldierName + "_Appear_", counts["appear"]),
                Pick(sprites, soldierName + "_Idle_", counts["idle"]),
                Pick(sprites, soldierName + "_Vanish_", counts["vanish"]));
            prefabs.Add(prefab);
        }

        LinkToThrone(prefabs);
        AssetDatabase.SaveAssets();
        Debug.Log($"[ShadowSoldier] 병사 {prefabs.Count}종을 만들고 ShadowThrone 에 연결했습니다.");
    }

    private static Dictionary<string, int> ReadCounts(string txtPath)
    {
        var counts = new Dictionary<string, int>();
        foreach (string line in File.ReadAllLines(txtPath))
        {
            string[] pair = line.Split('=');
            if (pair.Length == 2 && int.TryParse(pair[1].Trim(), out int value))
            {
                counts[pair[0].Trim()] = value;
            }
        }
        return counts;
    }

    private static void SliceSheet(string path, string soldierName, Dictionary<string, int> counts)
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = PixelsPerUnit;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();

        var factory = new SpriteDataProviderFactories();
        factory.Init();
        ISpriteEditorDataProvider provider = factory.GetSpriteEditorDataProviderFromObject(importer);
        provider.InitSpriteEditorDataProvider();

        // 다시 만들 때 같은 이름이면 기존 ID 를 그대로 써서 프리팹 참조가 끊기지 않게 한다.
        var oldIds = new Dictionary<string, GUID>();
        foreach (SpriteRect old in provider.GetSpriteRects())
        {
            oldIds[old.name] = old.spriteID;
        }

        provider.GetDataProvider<ITextureDataProvider>().GetTextureActualWidthAndHeight(out int _, out int textureHeight);
        var rects = new List<SpriteRect>();
        AddRow(rects, oldIds, soldierName + "_Appear_", 0, counts["appear"], textureHeight);
        AddRow(rects, oldIds, soldierName + "_Idle_", 1, counts["idle"], textureHeight);
        AddRow(rects, oldIds, soldierName + "_Vanish_", 2, counts["vanish"], textureHeight);
        provider.SetSpriteRects(rects.ToArray());

        var nameIds = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
        nameIds.SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)));

        provider.Apply();
        importer.SaveAndReimport();
    }

    private static void AddRow(List<SpriteRect> rects, Dictionary<string, GUID> oldIds, string prefix, int row, int count, int textureHeight)
    {
        for (int i = 0; i < count; i++)
        {
            string name = prefix + i;
            rects.Add(new SpriteRect
            {
                name = name,
                // 텍스처 좌표는 아래에서 위로 센다: 0행(나타남)이 맨 위.
                rect = new Rect(i * Cell, textureHeight - (row + 1) * Cell, Cell, Cell),
                alignment = SpriteAlignment.Custom,
                pivot = new Vector2(0.5f, (Cell - Feet) / (float)Cell),
                spriteID = oldIds.TryGetValue(name, out GUID id) ? id : GUID.Generate()
            });
        }
    }

    private static Sprite[] Pick(Sprite[] sprites, string prefix, int count)
    {
        var result = new Sprite[count];
        for (int i = 0; i < count; i++)
        {
            result[i] = sprites.FirstOrDefault(s => s.name == prefix + i);
        }
        return result;
    }

    private static ShadowSoldier CreatePrefab(string soldierName, Material material, Sprite[] appear, Sprite[] idle, Sprite[] vanish)
    {
        var go = new GameObject("ShadowSoldier_" + soldierName);
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = idle.Length > 0 ? idle[0] : null;
        if (material != null)
        {
            renderer.sharedMaterial = material;
        }
        var soldier = go.AddComponent<ShadowSoldier>();

        var so = new SerializedObject(soldier);
        SetSprites(so.FindProperty("_appearFrames"), appear);
        SetSprites(so.FindProperty("_idleFrames"), idle);
        SetSprites(so.FindProperty("_vanishFrames"), vanish);
        so.ApplyModifiedPropertiesWithoutUndo();

        string path = $"{PrefabFolder}/ShadowSoldier_{soldierName}.prefab";
        GameObject saved = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return saved.GetComponent<ShadowSoldier>();
    }

    private static void SetSprites(SerializedProperty property, Sprite[] sprites)
    {
        property.arraySize = sprites.Length;
        for (int i = 0; i < sprites.Length; i++)
        {
            property.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
        }
    }

    // 병사도 왕좌와 같은 머티리얼로 그린다.
    private static Material LoadThroneMaterial()
    {
        var throne = AssetDatabase.LoadAssetAtPath<GameObject>(ThronePrefabPath);
        SpriteRenderer renderer = throne != null ? throne.GetComponent<SpriteRenderer>() : null;
        return renderer != null ? renderer.sharedMaterial : null;
    }

    private static void LinkToThrone(List<ShadowSoldier> prefabs)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(ThronePrefabPath);
        try
        {
            ShadowThrone throne = root.GetComponent<ShadowThrone>();
            ShadowSoldierTroop troop = root.GetComponent<ShadowSoldierTroop>();
            if (troop == null)
            {
                troop = root.AddComponent<ShadowSoldierTroop>();
            }

            var troopSo = new SerializedObject(troop);
            SerializedProperty list = troopSo.FindProperty("_soldierPrefabs");
            list.arraySize = prefabs.Count;
            for (int i = 0; i < prefabs.Count; i++)
            {
                list.GetArrayElementAtIndex(i).objectReferenceValue = prefabs[i];
            }
            troopSo.ApplyModifiedPropertiesWithoutUndo();

            if (throne != null)
            {
                var throneSo = new SerializedObject(throne);
                throneSo.FindProperty("_troop").objectReferenceValue = troop;
                throneSo.ApplyModifiedPropertiesWithoutUndo();
            }

            PrefabUtility.SaveAsPrefabAsset(root, ThronePrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }
}
