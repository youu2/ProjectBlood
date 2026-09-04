using System.Collections.Generic;
using QFramework;
using UnityEngine;
using UnityEngine.Pool;

namespace ProjectBlood
{
    public partial class ShotGun : SemiAutomaticWeapon
    {
        [SerializeField] private int bulletCount = 5; // 每次攻击生成的子弹数量
        public override void Attack(Vector2 shootDir)
        {
            // 基准方向（准心瞄准方向）
            Vector2 baseDirection = shootDir.normalized;

            // 生成5发子弹
            for (int i = 0; i < bulletCount; i++)
            {
                // 在圆锥范围内随机生成一个偏转角度
                float randomAngle = Random.Range(-spreadAngle / 2f, spreadAngle / 2f);
                // 构造一个旋转，围绕Z轴旋转randomAngle度
                Quaternion randomRotation = Quaternion.AngleAxis(randomAngle, Vector3.forward);
                // 将基准方向转换为方向向量，并应用旋转，得到最终发射方向
                Vector2 finalDirection = randomRotation * baseDirection;

                // 生成子弹，并设置其飞行方向
                // var bullet = Instantiate(BulletPrefab, BulletSpawnPoint.position, randomRotation);
                var bullet = PlayerBulletPool.Instance.Get(BulletPrefab);
                bullet.transform.SetPositionAndRotation(BulletSpawnPoint.position, randomRotation);
                bullet.GetComponent<PlayerBullet>().direction = finalDirection;
                bullet.GetComponent<PlayerBullet>().weaponType = WeaponType; // 标记子弹来源武器，供强化伤害计算
                bullet.SetActive(true);
                ApplyLifestealToBullet(bullet.GetComponent<PlayerBullet>());
            }
            // 播放射击声音，随机选择一个音效
            int randomIndex = Random.Range(0, ShootSounds.Count);
            AudioKitManager.Instance.PlayOneShot(ShootSounds[randomIndex], volume: FireVolume);
            fireFlash.Flash(BulletSpawnPoint.position, shootDir); // 显示枪口火焰特效
            CameraUtils.ShakeMainCamera(0.15f, 7);
            WeaponAnimator.SetTrigger("SingleShoot");
            CreateShell(baseDirection);
            TriggerWeaponFired(); // 触发父类武器射击事件
        }

        private void CreateShell(Vector2 baseDirection)
        {
            if (ShellPool.instance == null) return;
            GameObject shellObj = ShellPool.instance.shellPool.Get();
            shellObj.transform.SetPositionAndRotation(transform.position, transform.rotation);
            shellObj.GetComponent<ShellManager>().PlayShellAnimation(baseDirection, transform);
        }
    }
}
