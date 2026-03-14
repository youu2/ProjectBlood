using UnityEngine;
using ProjectBlood;
using System.Collections.Generic;

public class DE : MonoBehaviour, IWeapon
{
    // Start is called before the first frame update
    public PlayerBullet Bullet;
    public float HitDamage => 0.5f;

    public float attackInterval = 0.5f; // 攻击间隔
    private float lastAttackTime = 0f; // 上次攻击时间

    public List<AudioClip> ShootSounds = new List<AudioClip>();
    public AudioSource shootAudioSource;

    public void Attack(Vector2 shootDir)
    {
        // 计算旋转：根据 shootDir 向量创建对应的 Quaternion 朝向
        Quaternion bulletRotation = Quaternion.FromToRotation(Vector2.right, shootDir);
        var bullet = Instantiate(Bullet, Bullet.transform.position, bulletRotation);
        bullet.direction = shootDir;
        bullet.gameObject.SetActive(true);
    }
    public void keepAttacking(Vector2 shootDir)
    {
        //Attack(shootDir);
        if (Time.time - lastAttackTime >= attackInterval)
        {
            int randomIndex = Random.Range(0, ShootSounds.Count);
            shootAudioSource.clip = ShootSounds[randomIndex];
            shootAudioSource.Play();
            Attack(shootDir);
            lastAttackTime = Time.time;
        }
    }
}