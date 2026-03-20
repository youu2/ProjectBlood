using UnityEngine;
using QFramework;
using System.Collections.Generic;

namespace ProjectBlood
{
	public partial class ShotGun : IWeapon
	{
		// public override float HitDamage => 0.5f;

        public float attackInterval = 0.5f; // 攻击间隔
        private float lastAttackTime = 0f; // 上次攻击时间

        public List<AudioClip> ShootSounds = new List<AudioClip>();
        // public AudioSource shootAudioSource; 
        // 被QF架构other bind功能生成的SelfAudioSource替代，可在designer中直接绑定

		[SerializeField] private float spreadAngle = 30f; // 圆锥散射角度，例如30度，可调节
		[SerializeField] private int bulletCount = 5; // 每次攻击生成的子弹数量

        public override void Attack(Vector2 shootDir)
		{
			// 基准方向（准心瞄准方向）
			Vector2 baseDirection = shootDir.normalized;

			// 生成5发子弹
			for (int i = 0; i < bulletCount; i++)
			{
				// 在圆锥范围内随机生成一个偏转角度 [-spreadAngle/2, spreadAngle/2]
				float randomAngle = Random.Range(-spreadAngle / 2f, spreadAngle / 2f);

				// 构造一个旋转，围绕Z轴旋转randomAngle度
				Quaternion randomRotation = Quaternion.AngleAxis(randomAngle, Vector3.forward);

				// 将基准方向转换为方向向量，并应用旋转，得到最终发射方向
				Vector2 finalDirection = randomRotation * baseDirection;

				// 计算飞行方向的角度，并生成对应的旋转
				float angle = Mathf.Atan2(finalDirection.y, finalDirection.x) * Mathf.Rad2Deg;
				Quaternion bulletRotation = Quaternion.AngleAxis(angle, Vector3.forward);

				// 生成子弹，并设置其飞行方向
				var bullet = Instantiate(DEBullet, DEBullet.transform.position, bulletRotation);
				bullet.direction = finalDirection;
				bullet.gameObject.SetActive(true);
			}
		}
        public override void keepAttacking(Vector2 shootDir)
        {
            if (Time.time - lastAttackTime >= attackInterval)
            {
                int randomIndex = Random.Range(0, ShootSounds.Count);
                SelfAudioSource.clip = ShootSounds[randomIndex];
                SelfAudioSource.Play();
                
                Attack(shootDir);
                lastAttackTime = Time.time;
            }
        }

        public override void StopAttacking(Vector2 shootDir)
        {
			// 喷子射速较慢(音频是单段射击，时长很短，可以播放完全)，停止攻击时不需要额外逻辑
        }
	}
}
