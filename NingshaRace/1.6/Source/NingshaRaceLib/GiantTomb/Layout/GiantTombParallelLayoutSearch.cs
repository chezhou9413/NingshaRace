using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace NingshaRaceLib.GiantTomb.Layout
{
    //类职责：在独立任务中按确定性批次并行求解纯墓葬布局，并向主线程公开只读进度。
    internal sealed class GiantTombParallelLayoutSearch
    {
        private const int MaximumWorkerCount = 8;
        private readonly GiantTombLayoutSearchAttempt[] attempts;
        private readonly GiantTombModule entrance;
        private readonly GiantTombModule[] terminalModules;
        private readonly int mapWidth;
        private readonly int mapHeight;
        private readonly int borderMargin;
        private int completedAttempts;
        private long totalEvaluations;
        private int deepestPlacementCount;

        public Task<GiantTombLayoutSearchResult> Completion { get; private set; }
        public int CompletedAttempts => Volatile.Read(ref completedAttempts);
        public long TotalEvaluations => Interlocked.Read(ref totalEvaluations);
        public int AttemptCount => attempts.Length;

        //函数职责：接收主线程冻结的全部输入，不在后台读取地图、Def数据库或全局随机状态。
        public GiantTombParallelLayoutSearch(GiantTombLayoutSearchAttempt[] attempts, GiantTombModule entrance,
            GiantTombModule[] terminalModules, int mapWidth, int mapHeight, int borderMargin)
        {
            this.attempts = attempts ?? throw new ArgumentNullException(nameof(attempts));
            this.entrance = entrance ?? throw new ArgumentNullException(nameof(entrance));
            this.terminalModules = terminalModules ?? throw new ArgumentNullException(nameof(terminalModules));
            this.mapWidth = mapWidth;
            this.mapHeight = mapHeight;
            this.borderMargin = borderMargin;
        }

        //函数职责：启动唯一后台任务，使全部并行工作在该任务完成前收束。
        public void Start()
        {
            if (Completion != null) throw new InvalidOperationException("巨型墓葬并行布局搜索不能重复启动");
            Completion = Task.Run(Execute);
        }

        //函数职责：按CPU数量分批并行求解，并稳定选择索引最小的成功尝试作为最终结果。
        private GiantTombLayoutSearchResult Execute()
        {
            Stopwatch timer = Stopwatch.StartNew();
            int workerCount = Math.Max(1, Math.Min(MaximumWorkerCount, Environment.ProcessorCount - 1));
            for (int batchStart = 0; batchStart < attempts.Length; batchStart += workerCount)
            {
                int batchEnd = Math.Min(attempts.Length, batchStart + workerCount);
                int bestSuccessIndex = int.MaxValue;
                ConcurrentBag<GiantTombLayoutSearchResult> batchResults = new ConcurrentBag<GiantTombLayoutSearchResult>();
                Parallel.For(batchStart, batchEnd, new ParallelOptions { MaxDegreeOfParallelism = workerCount }, attemptIndex =>
                {
                    GiantTombLayoutSearchAttempt attempt = attempts[attemptIndex];
                    GiantTombLayoutSolver solver = new GiantTombLayoutSolver(mapWidth, mapHeight, borderMargin, terminalModules,
                        attempt.RandomSeed, () => Volatile.Read(ref bestSuccessIndex) < attempt.Index);
                    bool success = solver.TrySolve(attempt.Pool, entrance, attempt.CandidateBudget,
                        out var placements, out var connections);
                    if (success)
                    {
                        UpdateMinimum(ref bestSuccessIndex, attempt.Index);
                    }
                    Interlocked.Add(ref totalEvaluations, solver.Evaluations);
                    UpdateMaximum(ref deepestPlacementCount, solver.DeepestPlacementCount);
                    batchResults.Add(new GiantTombLayoutSearchResult
                    {
                        Success = success,
                        Attempt = attempt,
                        Placements = placements,
                        Connections = connections
                    });
                    Interlocked.Increment(ref completedAttempts);
                });

                GiantTombLayoutSearchResult selected = SelectFirstSuccess(batchResults);
                if (selected != null)
                {
                    timer.Stop();
                    CompleteStatistics(selected, timer.ElapsedMilliseconds);
                    return selected;
                }
            }

            timer.Stop();
            GiantTombLayoutSearchResult failure = new GiantTombLayoutSearchResult { Success = false };
            CompleteStatistics(failure, timer.ElapsedMilliseconds);
            return failure;
        }

        //函数职责：从完整批次结果中选择尝试索引最小的成功结果，保证并发调度不改变地图种子结果。
        private static GiantTombLayoutSearchResult SelectFirstSuccess(ConcurrentBag<GiantTombLayoutSearchResult> results)
        {
            GiantTombLayoutSearchResult selected = null;
            foreach (GiantTombLayoutSearchResult result in results)
            {
                if (!result.Success) continue;
                if (selected == null || result.Attempt.Index < selected.Attempt.Index) selected = result;
            }
            return selected;
        }

        //函数职责：在后台任务收束后写入只包含数值的最终诊断统计。
        private void CompleteStatistics(GiantTombLayoutSearchResult result, long elapsedMilliseconds)
        {
            result.TotalEvaluations = Interlocked.Read(ref totalEvaluations);
            result.DeepestPlacementCount = Volatile.Read(ref deepestPlacementCount);
            result.ElapsedMilliseconds = elapsedMilliseconds;
        }

        //函数职责：使用无锁比较交换记录所有并行尝试达到的最大模块深度。
        private static void UpdateMaximum(ref int target, int value)
        {
            int current = Volatile.Read(ref target);
            while (value > current)
            {
                int observed = Interlocked.CompareExchange(ref target, value, current);
                if (observed == current) return;
                current = observed;
            }
        }

        //函数职责：使用无锁比较交换记录当前批次最小成功索引，允许更高索引尝试尽早停止。
        private static void UpdateMinimum(ref int target, int value)
        {
            int current = Volatile.Read(ref target);
            while (value < current)
            {
                int observed = Interlocked.CompareExchange(ref target, value, current);
                if (observed == current) return;
                current = observed;
            }
        }
    }
}
