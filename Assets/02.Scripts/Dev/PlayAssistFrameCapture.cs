#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections;
using System.IO;
using UnityEngine;

/// <summary>
/// 촬영용 프레임 캡처 (개발용). UI 까지 포함한 게임 화면을 매 프레임 끝에 PNG 로 저장한다.
/// Time.captureFramerate 와 함께 쓰면 게임 시간이 프레임마다 일정하게 흘러 GIF 간격이 정확하다.
/// 사용: PlayAssistFrameCapture.Begin(폴더, 가로 크기) → PlayAssistFrameCapture.End()
/// </summary>
public class PlayAssistFrameCapture : MonoBehaviour
{
    private static PlayAssistFrameCapture _instance;

    public static int FrameCount { get; private set; }    public static bool IsCapturing => _instance != null && _instance._directory != null;

    private string _directory;
    private int _width = 960;
    private RenderTexture _scaled;
    private Texture2D _readback;

    public static void Begin(string directory, int width)
    {
        if (_instance == null)
        {
            GameObject go = new GameObject("[PlayAssistFrameCapture]");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<PlayAssistFrameCapture>();
        }
        Directory.CreateDirectory(directory);
        _instance._directory = directory;
        _instance._width = width;
        FrameCount = 0;
    }

    public static void End()
    {
        if (_instance != null)
        {
            _instance._directory = null;
        }
    }

    private void OnEnable()
    {
        StartCoroutine(CaptureLoop());
    }

    private IEnumerator CaptureLoop()
    {
        WaitForEndOfFrame endOfFrame = new WaitForEndOfFrame();
        while (true)
        {
            yield return endOfFrame;
            if (_directory != null)
            {
                CaptureFrame();
            }
        }
    }

    private void CaptureFrame()
    {
        Texture2D screen = ScreenCapture.CaptureScreenshotAsTexture();
        int height = Mathf.RoundToInt(_width * screen.height / (float)screen.width);
        if (_scaled == null || _scaled.width != _width || _scaled.height != height)
        {
            if (_scaled != null) _scaled.Release();
            // 캡처 텍스처는 이미 화면 색(sRGB) 값이라, 리니어 RT 로 그대로 옮겨야 감마가 두 번 들어가 뿌옇게 되지 않는다
            _scaled = new RenderTexture(_width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            _readback = new Texture2D(_width, height, TextureFormat.RGB24, false);
        }
        // 픽셀아트가 번지지 않도록 점 샘플링으로 축소한다
        screen.filterMode = FilterMode.Point;
        Graphics.Blit(screen, _scaled);
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = _scaled;
        _readback.ReadPixels(new Rect(0, 0, _width, height), 0, 0);
        _readback.Apply();
        RenderTexture.active = previous;
        Destroy(screen);

        File.WriteAllBytes(Path.Combine(_directory, $"f{FrameCount:D4}.png"), _readback.EncodeToPNG());
        FrameCount++;
    }

    private void OnDestroy()
    {
        if (_scaled != null) _scaled.Release();
        if (_instance == this) _instance = null;
    }
}
#endif
