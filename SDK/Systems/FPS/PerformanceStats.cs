using UnityEngine;
using TMPro;
using UnityEngine.Profiling;

public class PerformanceStats : MonoBehaviour
{
    public TextMeshProUGUI statsLabel;
    public float updateInterval = 0.5f;

    private float deltaTime;
    private float timer;

    void Update()
    {
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
        timer += Time.unscaledDeltaTime;

        if (timer >= updateInterval)
        {
            timer = 0f;
            if (statsLabel == null) return;

            float fps = 1.0f / deltaTime;
            float ms = deltaTime * 1000f;

            long totalMem = Profiler.GetTotalReservedMemoryLong() / (1024 * 1024);
            long allocMem = Profiler.GetTotalAllocatedMemoryLong() / (1024 * 1024);
            long monoMem = Profiler.GetMonoUsedSizeLong() / (1024 * 1024);

            statsLabel.text = string.Format(
                "<b>FPS:</b> {0:0} ({1:0.0} ms)\n" +
                "<b>Memory:</b> {2} MB reserved / {3} MB alloc\n" +
                "<b>Mono/GC:</b> {4} MB",
                fps, ms, totalMem, allocMem, monoMem
            );
        }
    }
}