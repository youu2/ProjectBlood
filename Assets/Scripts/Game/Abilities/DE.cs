using UnityEngine;
using ProjectBlood;

public class DE : MonoBehaviour, IWeapon
{
    // Start is called before the first frame update
    public PlayerBullet Bullet;
    public float HitDamage => 0.5f;

    public void Attack(Vector2 shootDir)
    {
        // 计算旋转：根据 shootDir 向量创建对应的 Quaternion 朝向
        Quaternion bulletRotation = Quaternion.FromToRotation(Vector2.right, shootDir);
        var bullet = Instantiate(Bullet, Bullet.transform.position, bulletRotation);
        bullet.direction = shootDir;
        bullet.gameObject.SetActive(true);
    }
}