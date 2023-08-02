//using Unity.Burst;
//using Unity.Entities;
//using Unity.Collections;
//using Debug = UnityEngine.Debug;
//using Unity.Mathematics;
//using Unity.Transforms;
//using Unity.Jobs;
//using System.Collections.Generic;
//using System;
//using Unity.Collections.LowLevel.Unsafe;
//using UnityEditor.Timeline.Actions;


//public partial struct BetterSeekSystem : ISystem, ISystemStartStop
//{

//    NativeArray<float3> nearestTargetArray;

//    NativeList<float3> seekerPos;
//    NativeList<float3> targetPos;
//    [BurstCompile]
//    public void OnCreate(ref SystemState state)
//    {
//        state.RequireForUpdate<SeekerTag>();
//        state.RequireForUpdate<TargetTag>();

//    }

//    [BurstCompile]
//    public void OnStartRunning(ref SystemState state)
//    {




//    }

//    [BurstCompile]
//    public void OnStopRunning(ref SystemState state) { }


//    //[BurstCompile]
//    unsafe public void OnUpdate(ref SystemState state)
//    {


//        seekerPos = new NativeList<float3>(5000, Allocator.TempJob);
//        targetPos = new NativeList<float3>(5000, Allocator.TempJob);


//        //Debug.Break();
//        var tjob = new TargetArrayJob
//        {
//            targetPos = targetPos
//        };

//        tjob.Schedule();


//        foreach ((RefRO<SeekerTag> SeekerTag, RefRO<LocalTransform> transform) in SystemAPI.Query<RefRO<SeekerTag>, RefRO<LocalTransform>>())
//        {

//            seekerPos.Add(transform.ValueRO.Position);

//        }

//        nearestTargetArray = new NativeArray<float3>(seekerPos.Length, Allocator.TempJob);
//        state.Dependency.Complete();


//        SortJob<float3, AxisXComparer> sortJob = targetPos.SortJob(new AxisXComparer { });

//        var seekJob = new SeekJob
//        {
//            NearestTargetPositions = nearestTargetArray,
//            SeekerPositions = seekerPos,
//            TargetPositions = targetPos
//        };

//        JobHandle sortHandle = sortFunction(sortJob);
//        //JobHandle sortHandle = sortJob.Schedule();
//        int size = seekerPos.Length / 15;
//        JobHandle seekHandle = seekJob.Schedule(seekerPos.Length, size, sortHandle);
//        seekHandle.Complete();

//        seekerPos.Dispose();
//        targetPos.Dispose();
//        nearestTargetArray.Dispose();

//        //JobHandle sortHandle = sortJob.Schedule();
//        //int size = seekerPos.Length / 15;
//        //JobHandle seekHandle = seekJob.Schedule(seekerPos.Length, size, sortHandle);
//        //seekHandle.Complete();

//        //nearestTargetArray.Dispose();
//        //seekerPos.Dispose();
//        //targetPos.Dispose();

//    }

//    JobHandle sortFunction(SortJob<float3, AxisXComparer> sortJob)
//    {
//        return sortJob.Schedule();
//    }


//    [BurstCompile]
//    public void OnDestroy(ref SystemState state)
//    {
//        //seekersArray.Dispose();
//        //targetsArray.Dispose();

//    }
//}


//[BurstCompile]
//public struct AxisXComparer : IComparer<float3>
//{
//    public int Compare(float3 a, float3 b)
//    {
//        return a.x.CompareTo(b.x);
//    }
//}

//[BurstCompile]
//public partial struct TargetArrayJob : IJobEntity
//{
//    public NativeList<float3> targetPos;
//    void Execute(in TargetTag tag, in LocalTransform transfrom)
//    {
//        targetPos.Add(transfrom.Position);
//        //Debug.Log("A: "+ DateTime.Now);
//    }
//}

//[BurstCompile]
//public partial struct SeekersArrayJob : IJobEntity
//{
//    public NativeList<float3> seekerPos;
//    void Execute(in SeekerTag tag, in LocalTransform transform)
//    {
//        seekerPos.Add(transform.Position);
//        //Debug.Log("B: "+DateTime.Now);
//    }
//}


//[BurstCompile]
//public partial struct SeekJob : IJobParallelFor
//{
//    [ReadOnly] public NativeList<float3> TargetPositions;
//    [ReadOnly] public NativeList<float3> SeekerPositions;
//    public NativeArray<float3> NearestTargetPositions;

//    public void Execute(int index)
//    {
//        float3 seekerPos = SeekerPositions[index];

//        // Find the target with the closest X coord.
//        int startIdx = TargetPositions.BinarySearch(seekerPos, new AxisXComparer { });

//        // When no precise match is found, BinarySearch returns the bitwise negation of the last-searched offset.
//        // So when startIdx is negative, we flip the bits again, but we then must ensure the index is within bounds.
//        if (startIdx < 0) startIdx = ~startIdx;
//        if (startIdx >= TargetPositions.Length) startIdx = TargetPositions.Length - 1;

//        // The position of the target with the closest X coord.
//        float3 nearestTargetPos = TargetPositions[startIdx];
//        float nearestDistSq = math.distancesq(seekerPos, nearestTargetPos);

//        // Searching upwards through the array for a closer target.
//        Search(seekerPos, startIdx + 1, TargetPositions.Length, +1, ref nearestTargetPos, ref nearestDistSq);

//        // Search downwards through the array for a closer target.
//        Search(seekerPos, startIdx - 1, -1, -1, ref nearestTargetPos, ref nearestDistSq);

//        NearestTargetPositions[index] = nearestTargetPos;

//        Debug.DrawLine(seekerPos, nearestTargetPos);
//    }

//    void Search(float3 seekerPos, int startIdx, int endIdx, int step,
//            ref float3 nearestTargetPos, ref float nearestDistSq)
//    {
//        for (int i = startIdx; i != endIdx; i += step)
//        {
//            float3 targetPos = TargetPositions[i];
//            float xdiff = seekerPos.x - targetPos.x;

//            // If the square of the x distance is greater than the current nearest, we can stop searching.
//            if ((xdiff * xdiff) > nearestDistSq) break;

//            float distSq = math.distancesq(targetPos, seekerPos);

//            if (distSq < nearestDistSq)
//            {
//                nearestDistSq = distSq;
//                nearestTargetPos = targetPos;
//            }
//        }
//    }
//}




