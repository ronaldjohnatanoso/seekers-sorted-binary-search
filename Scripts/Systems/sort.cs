
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;


public unsafe struct SortJobMe<T, U>
    where T : unmanaged
    where U : IComparer<T>
{
    /// <summary>
    /// The data to sort.
    /// </summary>
    public T* Data;

    /// <summary>
    /// Comparison function.
    /// </summary>
    public U Comp;

    /// <summary>
    /// The length to sort.
    /// </summary>
    public int Length;

    [BurstCompile]
    struct SegmentSort : IJobParallelFor
    {
        [NativeDisableUnsafePtrRestriction]
        public T* Data;
        public U Comp;

        public int Length;
        public int SegmentWidth;

        public void Execute(int index)
        {
            var startIndex = index * SegmentWidth;
            var segmentLength = ((Length - startIndex) < SegmentWidth) ? (Length - startIndex) : SegmentWidth;
            NativeSortExtension.Sort(Data + startIndex, segmentLength, Comp);
        }
    }

    [BurstCompile]
    struct SegmentSortMerge : IJob
    {
        [NativeDisableUnsafePtrRestriction]
        public T* Data;
        public U Comp;

        public int Length;
        public int SegmentWidth;

        public void Execute()
        {
            var segmentCount = (Length + (SegmentWidth - 1)) / SegmentWidth;
            var segmentIndex = stackalloc int[segmentCount];

            // var resultCopy = (T*)Memory.Unmanaged.Allocate(UnsafeUtility.SizeOf<T>() * Length, 16, Allocator.Temp);
            var resultCopy = (T*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<T>() * Length, 16, Allocator.Temp);

            for (int sortIndex = 0; sortIndex < Length; sortIndex++)
            {
                // find next best
                int bestSegmentIndex = -1;
                T bestValue = default(T);

                for (int i = 0; i < segmentCount; i++)
                {
                    var startIndex = i * SegmentWidth;
                    var offset = segmentIndex[i];
                    var segmentLength = ((Length - startIndex) < SegmentWidth) ? (Length - startIndex) : SegmentWidth;
                    if (offset == segmentLength)
                        continue;

                    var nextValue = Data[startIndex + offset];
                    if (bestSegmentIndex != -1)
                    {
                        if (Comp.Compare(nextValue, bestValue) > 0)
                            continue;
                    }

                    bestValue = nextValue;
                    bestSegmentIndex = i;
                }

                segmentIndex[bestSegmentIndex]++;
                resultCopy[sortIndex] = bestValue;
            }

            UnsafeUtility.MemCpy(Data, resultCopy, UnsafeUtility.SizeOf<T>() * Length);
        }
    }

    /// <summary>
    /// Schedules this job.
    /// </summary>
    /// <param name="inputDeps">Handle of a job to depend upon.</param>
    /// <returns>The handle of this newly scheduled job.</returns>
    public JobHandle ScheduleMe(JobHandle inputDeps = default)
    {
        if (Length == 0)
            return inputDeps;
        var segmentCount = (Length + 1023) / 1024;

#if UNITY_2022_2_14F1_OR_NEWER
                int maxThreadCount = JobsUtility.ThreadIndexCount;
#else
        int maxThreadCount = JobsUtility.MaxJobThreadCount;
#endif
        var workerCount = Unity.Mathematics.math.max(1, maxThreadCount);
        var workerSegmentCount = segmentCount / workerCount;
        var segmentSortJob = new SegmentSort { Data = Data, Comp = Comp, Length = Length, SegmentWidth = 1024 };
        var segmentSortJobHandle = segmentSortJob.Schedule(segmentCount, workerSegmentCount, inputDeps);
        var segmentSortMergeJob = new SegmentSortMerge { Data = Data, Comp = Comp, Length = Length, SegmentWidth = 1024 };
        var segmentSortMergeJobHandle = segmentSortMergeJob.Schedule(segmentSortJobHandle);
        return segmentSortMergeJobHandle;
    }
}