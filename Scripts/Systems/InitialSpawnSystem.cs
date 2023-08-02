using Unity.Entities;
using Unity.Burst;
using Unity.Transforms;
using Unity.Mathematics;
using Debug = UnityEngine.Debug;

//[UpdateInGroup(typeof(Unity.Entities.SimulationSystemGroup))]
//[UpdateAfter(typeof(BeginInitializationEntityCommandBufferSystem))]
public partial struct InitialSpawnSystem : ISystem
{ 

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        //state.RequireForUpdate<Config>();
       // state.RequireForUpdate<BeginInitializationEntityCommandBufferSystem>();
        //state.RequireForUpdate<RandomComponent>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        //Debug.Log("hey");
        EntityCommandBuffer ecb = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);

        if (SystemAPI.TryGetSingleton<Config>(out var config))
        {
            for (int i = 0; i < config.seekers; i++)
            {
                new InitialSpawnJob
                {
                    ecb = ecb,
                    prefab = config.seekerPrefab
                }.Schedule();
            }

            for (int i = 0; i < config.targets; i++)
            {
                new InitialSpawnJob
                {
                    ecb = ecb,
                    prefab = config.targetsPrefab
                }.Schedule();
            }
        }
        state.Enabled = false;
    }
}

[BurstCompile]
public partial struct InitialSpawnJob : IJobEntity
{
    public EntityCommandBuffer ecb;
    public Entity prefab;
    void Execute(in Config config, ref RandomComponent rand)
    {
        float halfX = config.fieldDimension.x / 2f;
        float halfY = config.fieldDimension.y / 2f;

        Entity e = ecb.Instantiate(prefab);
        ecb.SetComponent(e, new LocalTransform
        {
            Position = new Unity.Mathematics.float3(
                rand.Value.NextFloat(-halfX, halfX),
                0,
                rand.Value.NextFloat(-halfY, halfY)
                ),
            Rotation = quaternion.identity,
            Scale = 1f
        }) ;
    }

    
}