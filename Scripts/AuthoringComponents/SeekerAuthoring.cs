
using Unity.Entities;
using UnityEngine;

public class SeekerAuthoring : MonoBehaviour
{
    public class Baker : Baker<SeekerAuthoring>
    {
        public override void Bake(SeekerAuthoring authoring)
        {
            Entity e = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(e, new SeekerTag { });
        }
    }
}
