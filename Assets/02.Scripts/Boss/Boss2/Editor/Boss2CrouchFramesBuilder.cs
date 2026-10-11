using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 보스2 엔딩의 "서 있다가 서서히 주저앉는" 모션용 사이 프레임을 만든다. 메뉴: Tools / Boss2 / Build Crouch Frames
///   원본에는 서 있는 Idle_0(머리 y82) → 내려앉는 중 Death_0(y80~81) → 앉은 Death_1(y79) 뿐이라 3초 넘게 늘리면 뚝뚝 끊긴다.
///   그래서 각 프레임의 몸통 한 줄을 지워 1px 더 내려앉은 프레임을 만들어 사이에 끼운다.
///     Crouch_A = Idle_0 에서 1px 내려앉음,  Crouch_B = Death_0 에서 1px 내려앉음
///   Boss2 프리팹의 Boss2EndingCinematic 에 [Idle_0, Crouch_A, Death_0, Crouch_B, Death_1] 과 눈 감은 Death_2 를 연결한다.
/// 여러 번 실행해도 결과가 같다.
/// </summary>
public static class Boss2CrouchFramesBuilder
{
    private const string SheetFolder = "Assets/04.Sprites/Boss2/Luneblade - Little Reaper v1.1/Sprite Sheet";
    private const string IdleSheetPath = SheetFolder + "/Idle.png";
    private const string DeathSheetPath = SheetFolder + "/Death.png";
    private const string OutputFolder = "Assets/04.Sprites/Boss2/Ending";
    private const string CrouchAPath = OutputFolder + "/T_Boss2Crouch_A.png";
    private const string CrouchBPath = OutputFolder + "/T_Boss2Crouch_B.png";
    private const string Boss2PrefabPath = "Assets/03.Prefabs/Boss2/Boss/Boss2.prefab";

    // 원본 시트와 같은 규격
    private const int FrameSize = 144;
    private const int PixelsPerUnit = 18;
    // 지우는 몸통 줄 (프레임 기준, 다리 바로 위). 이 줄 위가 모두 1px 내려온다.
    private const int CutRow = 68;

    [MenuItem("Tools/Boss2/Build Crouch Frames")]
    public static void Build()
    {
        if (AssetDatabase.IsValidFolder(OutputFolder) == false)
        {
            AssetDatabase.CreateFolder("Assets/04.Sprites/Boss2", "Ending");
        }

        Sprite idle0 = LoadSprite(IdleSheetPath, "Idle_0");
        Sprite death0 = LoadSprite(DeathSheetPath, "Death_0");
        Sprite death1 = LoadSprite(DeathSheetPath, "Death_1");
        Sprite death2 = LoadSprite(DeathSheetPath, "Death_2");

        Sprite crouchA = CreateLoweredFrame(IdleSheetPath, idle0, CrouchAPath);
        Sprite crouchB = CreateLoweredFrame(DeathSheetPath, death0, CrouchBPath);

        GameObject root = PrefabUtility.LoadPrefabContents(Boss2PrefabPath);
        try
        {
            var ending = root.GetComponent<Boss2EndingCinematic>();
            var so = new SerializedObject(ending);
            Sprite[] frames = { idle0, crouchA, death0, crouchB, death1 };
            SerializedProperty frameProperty = so.FindProperty("_crouchFrames");
            frameProperty.arraySize = frames.Length;
            for (int i = 0; i < frames.Length; i++)
            {
                frameProperty.GetArrayElementAtIndex(i).objectReferenceValue = frames[i];
            }
            so.FindProperty("_eyesClosedSprite").objectReferenceValue = death2;
            so.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, Boss2PrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        Debug.Log("[Boss2CrouchFramesBuilder] 주저앉기 사이 프레임 생성 완료");
    }

    private static Sprite LoadSprite(string sheetPath, string spriteName)
    {
        foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(sheetPath))
        {
            if (asset is Sprite sprite && sprite.name == spriteName)
                return sprite;
        }
        Debug.LogError($"[Boss2CrouchFramesBuilder] {sheetPath} 에서 {spriteName} 을 찾지 못했습니다.");
        return null;
    }

    // 원본 프레임에서 CutRow 줄을 지우고 그 위를 1px 내린 프레임을 PNG 로 저장한다. (색 변환 없이 PNG 바이트를 그대로 읽는다)
    private static Sprite CreateLoweredFrame(string sheetPath, Sprite source, string outputPath)
    {
        var sheet = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        sheet.LoadImage(File.ReadAllBytes(sheetPath));

        int originX = (int)source.rect.x;
        int originY = (int)source.rect.y;
        var frame = new Texture2D(FrameSize, FrameSize, TextureFormat.RGBA32, false);
        for (int y = 0; y < FrameSize; y++)
        {
            int sourceY = y < CutRow ? y : y + 1;
            for (int x = 0; x < FrameSize; x++)
            {
                Color color = sourceY < FrameSize ? sheet.GetPixel(originX + x, originY + sourceY) : Color.clear;
                frame.SetPixel(x, y, color);
            }
        }
        frame.Apply();
        File.WriteAllBytes(outputPath, frame.EncodeToPNG());
        Object.DestroyImmediate(frame);
        Object.DestroyImmediate(sheet);

        AssetDatabase.ImportAsset(outputPath, ImportAssetOptions.ForceUpdate);
        var importer = (TextureImporter)AssetImporter.GetAtPath(outputPath);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = PixelsPerUnit;
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        // 원본 시트처럼 피벗 = 프레임 가운데
        settings.spriteAlignment = (int)SpriteAlignment.Center;
        settings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Sprite>(outputPath);
    }
}
