using Unity.Burst;
using Unity.Entities;
using Unity.Physics;
using UnityEngine;
using Random = Unity.Mathematics.Random;
using Unity.Mathematics;
public partial struct RandomWalkStraightSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<Config>();
        state.RequireForUpdate<SeekerTag>();
    }
    //[BurstCompile]
    //public void OnStartRunning(ref SystemState state)
    //{
   
    //}

    //[BurstCompile]
    //public void OnStopRunning(ref SystemState state)
    //{

    //}

    [BurstCompile]
    public void OnUpdate(ref SystemState state) 
    {
       
        Entity e = SystemAPI.GetSingletonEntity<Config>();
        RandomComponent randComponent = SystemAPI.GetComponent<RandomComponent>(e);
 
        var job = new RandomStraightWalkJob
        {
            rand = randComponent.Value
        };
        job.Schedule();
        state.Enabled = false;
    }
}

[BurstCompile]
public partial struct RandomStraightWalkJob : IJobEntity
{
    public Random rand;
    void Execute(ref PhysicsVelocity velocity)
    {
        //Random rand = new Random();
        float3 dir = new float3(
            rand.NextFloat(-1,1),
            0,
            rand.NextFloat(-1,1)
            );
        //rsqrt give inverse magnitude
        float3 normDir = dir * math.rsqrt(math.lengthsq(dir));
        velocity.Linear = normDir * 1f;
     //   Debug.Log("shit");
    }
}
