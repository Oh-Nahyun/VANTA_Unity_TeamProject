using UnityEngine;
using UnityEngine.Video;

public class TimeController : MonoBehaviour
{
 
    public void StartTimeSlow(float time)
    {
        Time.timeScale = time;
    }

    public void StopTimeSlow()
    {
        Time.timeScale = 1f;
    }
}
