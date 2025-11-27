using System.Diagnostics;

namespace website.updater.Utils
{
    public static class HardwareUtils
    {
        /// <summary>
        /// 取得CPU使用率
        /// </summary>
        /// <returns></returns>
        public static float GetCpuUsage()
        {
            using var cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total", true);
            _ = cpuCounter.NextValue();
            Thread.Sleep(1000); // 必須暫停 1 秒才能取得準確值
            return (float)Math.Round(cpuCounter.NextValue(), 2);
        }

        /// <summary>
        /// 取得記憶體使用率
        /// </summary>
        /// <returns></returns>
        public static (float usedMb, float availableMb) GetMemoryInfo()
        {
            using var totalCounter = new PerformanceCounter("Memory", "% Committed Bytes in Use");
            using var availableCounter = new PerformanceCounter("Memory", "Available MBytes");

            float availableMb = availableCounter.NextValue();
            float committedBytes = totalCounter.NextValue();
            float committedMb = committedBytes;

            return (committedMb, availableMb);
        }
    }
}
