//using Unity.Burst;
//using Unity.Entities;
//using Unity.Collections;
//using Debug = UnityEngine.Debug;
//using Unity.Mathematics;
//using Unity.Transforms;
//using Unity.Jobs;

//public partial struct SeekSystem : ISystem, ISystemStartStop
//{
//    NativeArray<Entity> seekersArray;
//    NativeArray<Entity> targetsArray;
//    NativeArray<float3> nearestTargetArray;

//    NativeArray<float3> seekerPos;
//    NativeArray<float3> targetPos;
//    [BurstCompile]
//    public void OnCreate(ref SystemState state)
//    {
//        state.RequireForUpdate<SeekerTag>();
//        state.RequireForUpdate<TargetTag>();

//    }

//    [BurstCompile]
//    public void OnStartRunning(ref SystemState state)
//    {
//        seekersArray = state.GetEntityQuery(ComponentType.ReadOnly<SeekerTag>()).ToEntityArray(Allocator.Persistent);

//        targetsArray = state.GetEntityQuery(ComponentType.ReadOnly<TargetTag>()).ToEntityArray(Allocator.Persistent);

//        nearestTargetArray = new NativeArray<float3>(seekersArray.Length, Allocator.Persistent);

//        seekerPos = new NativeArray<float3>(seekersArray.Length, Allocator.Persistent);

//        targetPos = new NativeArray<float3>(targetsArray.Length, Allocator.Persistent);


//    }

//    [BurstCompile]
//    public void OnStopRunning(ref SystemState state) { }


//    [BurstCompile]
//    public void OnUpdate(ref SystemState state)
//    {


//        //
//        for (int i = 0; i < seekersArray.Length; i++)
//        {
//            seekerPos[i] = SystemAPI.GetComponentRO<LocalTransform>(seekersArray[i]).ValueRO.Position;
//        }


//        //
//        for (int i = 0; i < targetsArray.Length; i++)
//        {
//            targetPos[i] = SystemAPI.GetComponentRO<LocalTransform>(targetsArray[i]).ValueRO.Position;
//        }

//        var job = new SeekJob
//        {
//            NearestTargetPositions = nearestTargetArray,
//            SeekerPositions = seekerPos,
//            TargetPositions = targetPos
//        };

//        var handle = job.Schedule(seekerPos.Length,50);
//        handle.Complete();

//        for (int i = 0; i < seekerPos.Length; i++)
//        {
//            Debug.DrawLine(seekerPos[i], nearestTargetArray[i]);
//            // Debug.Log(nearestTargetArray[i]);
//        }




//    }

//    [BurstCompile]
//    public void OnDestroy(ref SystemState state)
//    {
//        seekersArray.Dispose();
//        targetsArray.Dispose();
//        nearestTargetArray.Dispose();
//        seekerPos.Dispose();
//        targetPos.Dispose();
//    }
//}

//[BurstCompile]
//public partial struct SeekJob : IJobParallelFor
//{
//    [ReadOnly] public NativeArray<float3> TargetPositions;
//    [ReadOnly] public NativeArray<float3> SeekerPositions;
//    public NativeArray<float3> NearestTargetPositions;
//    public void Execute(int index)
//    {
      
//            float3 seekerPos = SeekerPositions[index];
//            float nearestDistSq = float.MaxValue;
//            for (int j = 0; j < TargetPositions.Length; j++)
//            {
//                float3 targetPos = TargetPositions[j];
//                float distSq = math.distancesq(seekerPos, targetPos);
//                if (distSq < nearestDistSq)
//                {
//                    nearestDistSq = distSq;
//                    NearestTargetPositions[index] = targetPos;
//                }
//            }
        
//    }
//}



///*
// * 
// public void OnUpdate(ref SystemState state)
//    {
//        EntityQuery seekerQuery = state.GetEntityQuery(ComponentType.ReadOnly<SeekerTag>());
//        NativeArray<Entity> seekersArray = seekerQuery.ToEntityArray(Allocator.TempJob);

//        NativeArray<float3> seekerPos = new NativeArray<float3>(seekersArray.Length, Allocator.TempJob);
//        //
//        for(int i = 0; i < seekersArray.Length; i++)
//        {
//            seekerPos[i] = SystemAPI.GetComponentRO<LocalTransform>(seekersArray[i]).ValueRO.Position;
//        }

//        EntityQuery targetQuery = state.GetEntityQuery(ComponentType.ReadOnly<TargetTag>());
//        NativeArray<Entity> targetsArray = targetQuery.ToEntityArray(Allocator.TempJob);

//        NativeArray<float3> targetPos = new NativeArray<float3>(targetsArray.Length, Allocator.TempJob);
//        //
//        for(int i = 0; i < targetsArray.Length; i++)
//        {
//            targetPos[i] = SystemAPI.GetComponentRO<LocalTransform>(targetsArray[i]).ValueRO.Position;
//        }


//        NativeArray<float3> nearestTargetArray = new NativeArray<float3>(seekersArray.Length, Allocator.TempJob);

//        var job = new SeekJob
//        {
//            NearestTargetPositions = nearestTargetArray,
//            SeekerPositions = seekerPos,
//            TargetPositions = targetPos
//        };

//        var handle = job.Schedule();
//        handle.Complete();

//        for(int i=0;i<seekerPos.Length;i++)
//        {
//            Debug.DrawLine(seekerPos[i], nearestTargetArray[i]);
//           // Debug.Log(nearestTargetArray[i]);
//        }

//        seekersArray.Dispose();
//        targetsArray.Dispose();
//        seekerPos.Dispose();
//        targetPos.Dispose();
//        nearestTargetArray.Dispose();
//    }
//}
// * 
// * 
// */