using Unity.Burst;
using Unity.Entities;
using Unity.Collections;
using Debug = UnityEngine.Debug;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Jobs;
using System.Collections.Generic;
using System;
using Unity.Collections.LowLevel.Unsafe;
using UnityEditor.Timeline.Actions;
using static Unity.Burst.Intrinsics.X86.Avx;
using Unity.Jobs.LowLevel.Unsafe;
using UnityEngine.UIElements;

public partial struct BetterSeekSystem : ISystem, ISystemStartStop
{
  
    NativeArray<float3> nearestTargetArray;

    NativeList<float3> seekerPos;
    NativeList<float3> targetPos;
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<SeekerTag>();
        state.RequireForUpdate<TargetTag>();

    }

    [BurstCompile]
    public void OnStartRunning(ref SystemState state)
    {
  
    }

    [BurstCompile]
    public void OnStopRunning(ref SystemState state) { }


    [BurstCompile]
    unsafe public void OnUpdate(ref SystemState state)
    {

        seekerPos = new NativeList<float3>(5000, Allocator.TempJob);
        targetPos = new NativeList<float3>(5000, Allocator.TempJob);

        var tjob = new TargetArrayJob
        {
            targetPos = targetPos
        };

        tjob.Schedule();
       
        foreach ((RefRO<SeekerTag> SeekerTag, RefRO<LocalTransform> transform) in SystemAPI.Query<RefRO<SeekerTag>, RefRO<LocalTransform>>())
        {
            seekerPos.Add(transform.ValueRO.Position);
        }

        nearestTargetArray = new NativeArray<float3>(seekerPos.Length, Allocator.TempJob);
        state.Dependency.Complete();

        var seekJob = new SeekJob
        {
            NearestTargetPositions = nearestTargetArray,
            SeekerPositions = seekerPos,
            TargetPositions = targetPos
        };

        JobHandle sortHandle = sortFunction(targetPos.Length, targetPos.GetUnsafePtr(),new AxisXComparer { });
        int size = seekerPos.Length / 15;
        JobHandle seekHandle = seekJob.Schedule(seekerPos.Length, size, sortHandle );
        seekHandle.Complete();

        seekerPos.Dispose();
        targetPos.Dispose();
        nearestTargetArray.Dispose();

    }

    [BurstCompile]
    unsafe JobHandle sortFunction(int Length, float3* Data,AxisXComparer Comp)
    {
        var segmentCount = (Length + 1023) / 1024;

#if UNITY_2022_2_14F1_OR_NEWER
                int maxThreadCount = JobsUtility.ThreadIndexCount;
#else
        int maxThreadCount = JobsUtility.MaxJobThreadCount;
#endif
        var workerCount = Unity.Mathematics.math.max(1, maxThreadCount);
        var workerSegmentCount = segmentCount / workerCount;
        var segmentSortJob = new SegmentSort { Data = Data, Comp = Comp, Length = Length, SegmentWidth = 1024 };
        var segmentSortJobHandle = segmentSortJob.Schedule(segmentCount, workerSegmentCount);
        var segmentSortMergeJob = new SegmentSortMerge { Data = Data, Comp = Comp, Length = Length, SegmentWidth = 1024 };
        return  segmentSortMergeJob.Schedule(segmentSortJobHandle);
        // return segmentSortMergeJobHandle;
    }


    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        //seekersArray.Dispose();
        //targetsArray.Dispose();
 
    }
}


[BurstCompile]
struct SegmentSort : IJobParallelFor
{
    [NativeDisableUnsafePtrRestriction]
    unsafe public float3* Data;
    public AxisXComparer Comp;

    public int Length;
    public int SegmentWidth;

    unsafe public void Execute(int index)
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
    unsafe public float3* Data;
    public AxisXComparer Comp;

    public int Length;
    public int SegmentWidth;

    unsafe public void Execute()
    {
        var segmentCount = (Length + (SegmentWidth - 1)) / SegmentWidth;
        var segmentIndex = stackalloc int[segmentCount];

     
        var resultCopy = (float3*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<float3>() * Length, 16, Allocator.Temp);

        for (int sortIndex = 0; sortIndex < Length; sortIndex++)
        {
            // find next best
            int bestSegmentIndex = -1;
            float3 bestValue = default(float3);

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

        UnsafeUtility.MemCpy(Data, resultCopy, UnsafeUtility.SizeOf<float3>() * Length);
    }
}

[BurstCompile]
public struct AxisXComparer : IComparer<float3>
{
    public int Compare(float3 a, float3 b)
    {
        return a.x.CompareTo(b.x);
    }
}

[BurstCompile]
public partial struct TargetArrayJob : IJobEntity
{
    public NativeList<float3> targetPos;
    void Execute(in TargetTag tag, in LocalTransform transfrom)
    {
        targetPos.Add(transfrom.Position);
        //Debug.Log("A: "+ DateTime.Now);
    }
}

[BurstCompile]
public partial struct SeekersArrayJob : IJobEntity
{
    public NativeList<float3> seekerPos;
    void Execute(in SeekerTag tag,  in LocalTransform transform)
    {
        seekerPos.Add(transform.Position);
        //Debug.Log("B: "+DateTime.Now);
    }
}


[BurstCompile]
public partial struct SeekJob : IJobParallelFor
{
    [ReadOnly] public NativeList<float3> TargetPositions;
    [ReadOnly] public NativeList<float3> SeekerPositions;
    public NativeArray<float3> NearestTargetPositions;

    public void Execute(int index)
    {
        float3 seekerPos = SeekerPositions[index];

        // Find the target with the closest X coord.
        int startIdx = TargetPositions.BinarySearch(seekerPos, new AxisXComparer { });

        // When no precise match is found, BinarySearch returns the bitwise negation of the last-searched offset.
        // So when startIdx is negative, we flip the bits again, but we then must ensure the index is within bounds.
        if (startIdx < 0) startIdx = ~startIdx;
        if (startIdx >= TargetPositions.Length) startIdx = TargetPositions.Length - 1;

        // The position of the target with the closest X coord.
        float3 nearestTargetPos = TargetPositions[startIdx];
        float nearestDistSq = math.distancesq(seekerPos, nearestTargetPos);

        // Searching upwards through the array for a closer target.
        Search(seekerPos, startIdx + 1, TargetPositions.Length, +1, ref nearestTargetPos, ref nearestDistSq);

        // Search downwards through the array for a closer target.
        Search(seekerPos, startIdx - 1, -1, -1, ref nearestTargetPos, ref nearestDistSq);

        NearestTargetPositions[index] = nearestTargetPos;

        Debug.DrawLine(seekerPos, nearestTargetPos);
    }

        void Search(float3 seekerPos, int startIdx, int endIdx, int step,
                ref float3 nearestTargetPos, ref float nearestDistSq)
    {
        for (int i = startIdx; i != endIdx; i += step)
        {
            float3 targetPos = TargetPositions[i];
            float xdiff = seekerPos.x - targetPos.x;

            // If the square of the x distance is greater than the current nearest, we can stop searching.
            if ((xdiff * xdiff) > nearestDistSq) break;

            float distSq = math.distancesq(targetPos, seekerPos);

            if (distSq < nearestDistSq)
            {
                nearestDistSq = distSq;
                nearestTargetPos = targetPos;
            }
        }
    }
}




