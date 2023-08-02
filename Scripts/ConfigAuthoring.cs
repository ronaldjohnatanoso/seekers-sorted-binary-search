using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

public class ConfigAuthoring: MonoBehaviour
{

    public int seekers;
    public int targets;
    public GameObject seekersPrefab;
    public GameObject targetsPrefab;
    public float2 fieldDimension;
    public uint Seed;

    public class Baker : Baker<ConfigAuthoring>
    {
        public override void Bake(ConfigAuthoring authoring)
        {
            Entity e = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(e, new Config
            {
                fieldDimension = authoring.fieldDimension,
                seekers = authoring.seekers,
                targets = authoring.targets,
                seekerPrefab = GetEntity(authoring.seekersPrefab, TransformUsageFlags.Dynamic),
                targetsPrefab =     GetEntity(authoring.targetsPrefab, TransformUsageFlags.Dynamic)
            });

            AddComponent(e, new RandomComponent()
            {
                Value = Random.CreateFromIndex(authoring.Seed)

            }) ;
        }
    }
}
public struct Config : IComponentData
{
    public int seekers;
    public int targets;
    public Entity seekerPrefab;
    public Entity targetsPrefab;
    public float2 fieldDimension;
}

public struct RandomComponent : IComponentData
{
    public Random Value;
}
