using System.Reflection;
using System.Text;
using Unity.Cinemachine;
using UnityEngine;

[DefaultExecutionOrder(10000)]
public class CameraStatusUpdater : MonoBehaviour
{
#if DEVELOPMENT_BUILD && !UNITY_EDITOR
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InstallBuildDiagnostics()
    {
        if (FindAnyObjectByType<CameraStatusUpdater>() == null)
        {
            new GameObject("Camera Build Diagnostics").AddComponent<CameraStatusUpdater>();
        }
    }
#endif

    [SerializeField] private CinemachineCamera _camera;
    [SerializeField] private CinemachineBrain _brain;
    [SerializeField] private bool _enableLogging = true;
    [SerializeField, Min(0.1f)] private float _logInterval = 1f;

    // GetCinemachineComponent rebuilds the cache. Read the existing field instead.
    private static readonly FieldInfo PipelineField = typeof(CinemachineCamera)
        .GetField("m_Pipeline", BindingFlags.Instance | BindingFlags.NonPublic);

    private float _nextLogTime;
    private int _startupSamples;

    private void Start()
    {
        ResolveReferences();
        if (_enableLogging)
        {
            LogStatus("Start");
            Debug.Log($"[CameraStatus] Diagnostics v2 scene={UnityEngine.SceneManagement.SceneManager.GetActiveScene().path} unity={Application.unityVersion} build={Application.buildGUID} editor={Application.isEditor} logPath={Application.consoleLogPath}", this);
        }
    }

    private void LateUpdate()
    {
        if (!_enableLogging)
        {
            return;
        }

        if (_startupSamples < 3 || Time.unscaledTime >= _nextLogTime)
        {
            ResolveReferences();
            LogStatus(_startupSamples < 3 ? "Startup LateUpdate" : "Sample");
            _startupSamples++;
            _nextLogTime = Time.unscaledTime + Mathf.Max(0.1f, _logInterval);
        }
    }

    private void ResolveReferences()
    {
        if (_camera == null)
        {
            _camera = GetComponentInChildren<CinemachineCamera>(true);
            if (_camera == null)
            {
                _camera = FindAnyObjectByType<CinemachineCamera>(FindObjectsInactive.Include);
            }
        }

        if (_brain == null)
        {
            _brain = GetComponentInChildren<CinemachineBrain>(true);
            if (_brain == null)
            {
                _brain = FindAnyObjectByType<CinemachineBrain>(FindObjectsInactive.Include);
            }
        }
    }

    private void LogStatus(string phase)
    {
        var log = new StringBuilder();
        log.AppendLine($"[CameraStatus] {phase} frame={Time.frameCount} time={Time.unscaledTime:F2} timeScale={Time.timeScale:F2}");

        if (_brain == null)
        {
            log.AppendLine("Brain=MISSING");
        }
        else
        {
            var active = _brain.ActiveVirtualCamera;
            log.AppendLine($"Brain={Describe(_brain)} activeEnabled={_brain.isActiveAndEnabled} activeCamera={active?.Name ?? "NONE"} selectedCameraMatches={ReferenceEquals(active, _camera)}");
            log.AppendLine($"Brain update={_brain.UpdateMethod} blendUpdate={_brain.BlendUpdateMethod} channelMask={_brain.ChannelMask} position={_brain.transform.position.ToString("F3")}");
            log.AppendLine($"BrainFinalPosition={_brain.State.GetFinalPosition().ToString("F3")} isBlending={_brain.IsBlending}");
        }

        if (_camera == null)
        {
            log.AppendLine("CinemachineCamera=MISSING");
        }
        else
        {
            var target = _camera.Follow;
            log.AppendLine($"Camera={Describe(_camera)} activeSelf={_camera.gameObject.activeSelf} activeHierarchy={_camera.gameObject.activeInHierarchy} enabled={_camera.enabled}");
            log.AppendLine($"OutputChannel={_camera.OutputChannel} priority={_camera.Priority.Value} previousStateValid={_camera.PreviousStateIsValid}");
            log.AppendLine($"Follow={Describe(target)} targetPosition={Position(target)} cameraPosition={Position(_camera.transform)} stateRawPosition={_camera.State.RawPosition.ToString("F3")}");
            var state = _camera.State;
            log.AppendLine($"RequestedSize={_camera.Lens.OrthographicSize:F3} stateAspect={state.Lens.Aspect:F3} orthographic={state.Lens.Orthographic}");
            var confiner = _camera.GetComponent<CinemachineConfiner2D>();
            if (confiner != null)
            {
                var shape = confiner.BoundingShape2D;
                log.AppendLine($"Confiner enabled={confiner.isActiveAndEnabled} shape={Describe(shape)} shapeEnabled={(shape != null && shape.isActiveAndEnabled)} bounds={(shape != null ? shape.bounds.ToString() : "NONE")} oversizeWindow={confiner.OversizeWindow.Enabled}");
            }
            else
            {
                log.AppendLine("Confiner=MISSING");
            }
            log.AppendLine($"CameraFinalPosition={state.GetFinalPosition().ToString("F3")} positionCorrection={state.PositionCorrection.ToString("F3")} orthographicSize={state.Lens.OrthographicSize:F3}");

            var follow = _camera.GetComponent<CinemachineFollow>();
            log.AppendLine($"AttachedFollow={Describe(follow)} enabled={(follow != null && follow.enabled)} hasTarget={target != null}");

            var pipeline = PipelineField?.GetValue(_camera) as CinemachineComponentBase[];
            if (PipelineField == null)
            {
                log.AppendLine("Pipeline=UNAVAILABLE (private field missing or stripped)");
            }
            else if (pipeline == null)
            {
                log.AppendLine("Pipeline=NULL (not built or invalidated)");
            }
            else
            {
                for (int i = 0; i < pipeline.Length; i++)
                {
                    var component = pipeline[i];
                    log.AppendLine($"Pipeline[{(CinemachineCore.Stage)i}]={Describe(component)} type={(component != null ? component.GetType().Name : "NONE")} enabled={(component != null && component.enabled)} matchesAttachedFollow={(follow != null && component == follow)}");
                }
            }
        }

       
    }

    private static string Describe(Object value)
    {
        return value != null ? $"{value.name}#{value.GetInstanceID()}" : "NONE";
    }

    private static string Position(Transform target)
    {
        return target != null ? target.position.ToString("F3") : "NONE";
    }
}
