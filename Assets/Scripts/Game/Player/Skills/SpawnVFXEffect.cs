using UnityEngine;

[CreateAssetMenu(fileName = "SpawnVFXEffect", menuName = "技能系统/效果/生成特效效果")]
public class SpawnVFXEffect : SkillEffect
{
    [Header("特效设置")]
    [Tooltip("要生成的特效预制体")]
    public GameObject vfxPrefab;

    [Tooltip("特效存活时间, 0表示不自动销毁")]
    public float lifetime = 1f;

    [Tooltip("是否将特效作为施法者的子物体（跟随移动）")]
    public bool parentToCaster = false;

    public override void OnStart(EffectContext context)
    {
        if (vfxPrefab == null)
        {
            Debug.LogWarning("未设置特效预制体");
            return;
        }

        GameObject vfxInstance;
        float angle = Mathf.Atan2(context.direction.y, context.direction.x) * Mathf.Rad2Deg;

        vfxInstance = Instantiate(vfxPrefab, context.caster.transform.position, Quaternion.Euler(0, 0, angle));

        if (parentToCaster)
        {
            vfxInstance.transform.SetParent(context.caster.transform);
        }

        // 如果设置了存活时间，自动销毁
        if (lifetime > 0f)
        {
            Destroy(vfxInstance, lifetime);
        }
    }
}