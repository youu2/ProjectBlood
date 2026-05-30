using UnityEngine;
using QFramework;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

namespace ProjectBlood
{
	public partial class Player : ViewController
	{
		public float moveSpeed = 3.5f;
		public static Player player1;
		public PlayerBullet playerBullet;
		public SpriteRenderer spriteRenderer;
		public Transform weaponTransform;
		public WeaponBase currentWeapon;
		private List<WeaponBase> weapons = new List<WeaponBase>();
		private List<AudioClip> weaponSwitchSounds = new List<AudioClip>();
		private AudioSource temporaryAudioSource; // 用于播放切换武器时的shootEnd音效
		private WeaponBase weaponToHide = null; // 待隐藏的武器引用（用于半自动武器延迟隐藏）
		public BloodBank bloodBank = new BloodBank(); // 血液银行组件，特殊资源，用于弹药管理和血量管理
		private Vector2 smoothAimDir; // 平滑过渡后的瞄准方向
		private const float aimSmoothSpeed = 20f; // 瞄准平滑速度
		private const float aimAngle = 35f; // 锁定角度
		// 显示跟随玩家的提示文本
		public static void DisplayText(string text){
			player1.StartCoroutine(player1.ShowText(text, 2.0f));
		}

		public static void HideText(){
			player1.NoticeText.Hide();
		}
		
		IEnumerator ShowText(string text, float duration)
		{
			player1.NoticeText.text = text;
			player1.NoticeText.Show();
			yield return new WaitForSeconds(duration);
			player1.NoticeText.Hide();
		}
		private void Awake()
		{
			Application.targetFrameRate = 60;
			player1 = this;
			weapons.Add(DE);
			weapons.Add(MP5);
			weapons.Add(ShotGun);
			weapons.Add(AWP);
			weapons.Add(AK);
			weapons.Add(Laser);
			// weapons.Add(Bow);
			weaponSwitchSounds.Add(WeaponSwitchSound);
			UseWeapon(0); // 默认装备第一把武器
			NoticeText.Hide();
			// 为所有武器设置血液银行引用
			foreach (var weapon in weapons)
			{
				weapon.BloodBank = bloodBank;
			}
			
			// 创建临时的 AudioSource 用于播放切换武器时的 shootEnd 音效
			temporaryAudioSource = gameObject.AddComponent<AudioSource>();
		}
		
		void UseWeapon(int index)
		{
			var previousWeapon = currentWeapon;
			
			// 停止上一把武器的所有状态（音效已在 WeaponBase 中处理）
			previousWeapon?.StopAttacking();
			previousWeapon?.StopReload();
			previousWeapon?.Hide();
			
			// 切换到新武器
			currentWeapon = weapons[index];
			currentWeapon.BloodBank = bloodBank;
			currentWeapon.SwitchToSet();
			currentWeapon.Show();
			
			// 播放切换音效（独立播放，不需要等待）
			AudioKit.PlaySound(WeaponSwitchSound);
		}
		
		void HidePreviousWeapon()
		{
			if (weaponToHide != null)
			{
				weaponToHide.Hide();
				weaponToHide = null;
			}
		}

		void Start()
		{
			HurtBox.OnTriggerEnter2DEvent((Collider2D col)=>
			{
				var hitBox = col.GetComponent<HitBox>();
				// 如果撞到的东西没有伤害能力，就跳过受伤流程
				if (hitBox == null) return; 
				Global.currentHP.Value -= col.GetComponent<HitBox>().owner.GetComponent<IDamageable>().HitDamage;
				if (Global.currentHP.Value < 0) Global.currentHP.Value = 0; // 避免HP变成负数
				if (Global.currentHP.Value > 0)
				{
					AudioKit.PlaySound("Hurt");
					return;
				} 
				// 死亡时根据当前等级获得传承点数
				Global.SettleLegacyPoints();
				AudioKit.PlaySound("WilhelmScream");
				this.DestroyGameObjGracefully();
				UIKit.OpenPanel<UIGameOverPanel>();
			}).UnRegisterWhenGameObjectDestroyed(gameObject);
		}
		
		void Update()
		{
			float horizontal = Input.GetAxis("Horizontal"); // A/D
			float vertical = Input.GetAxis("Vertical");     // W/S
			weaponTransform = currentWeapon.transform;
			if (horizontal != 0 || vertical != 0)
			{
				spriteRenderer.flipX = horizontal < 0; // 根据输入方向调整角色朝向
			}

			// 保持任意方向速度一致
			var direction = new Vector2(horizontal, vertical).normalized;
			SelfRigidbody2D.velocity = direction * moveSpeed;

			// 获取鼠标在屏幕上的位置
			Vector3 mouseScreenPos = Input.mousePosition;
			// 转成世界坐标，Z 要设成 0（2D 游戏）
			mouseScreenPos.z = 0;
			Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);
			
			// 计算从玩家指向鼠标的方向
			Vector2 shootDir = (mouseWorldPos - transform.position).normalized;

			// 检查敌人自动锁敌功能
			if (Global.currentRoom && Global.currentRoom.GetEnemies().Count > 0)
			{
				var enemies = Global.currentRoom.GetEnemies();
				
				// 将 HashSet 转换为 List 并过滤掉已销毁或正在死亡的敌人
				var enemiesList = enemies.Where(enemy => enemy != null && !enemy.IsDying).ToList();
				
				if (enemiesList.Count > 0)
				{
					// 将敌人按离鼠标指针的距离从近到远排序
					var sortedEnemies = enemiesList.OrderBy(enemy => 
						Vector2.Distance(enemy.GameObject.transform.position, mouseWorldPos)
					).ToList();
					
					// 获取 Wall Layer 的掩码
					int wallLayer = LayerMask.GetMask("Wall");
					
					// 标记是否找到了可瞄准的敌人
					bool foundTarget = false;
					
					// 遍历排序后的敌人，找到第一个没有障碍物的
					foreach (var enemy in sortedEnemies)
					{
						// 再次检查敌人是否还存在且没有在死亡过程中
						if (enemy == null || enemy.IsDying)
						{
							continue;
						}
						
						// 检查玩家到敌人之间是否有墙壁障碍物
						Vector2 playerPos = transform.position;
						Vector2 enemyPos = enemy.GameObject.transform.position;
						Vector2 dirToEnemy = (enemyPos - playerPos).normalized;
						
						// 检查敌人是否在鼠标方向的30度范围内
						float angleToEnemy = Vector2.Angle(shootDir, dirToEnemy);
						if (angleToEnemy > aimAngle)
						{
							continue;
						}
						
						// 使用射线检测，只检测 Wall 层的物体
						RaycastHit2D hit = Physics2D.Linecast(playerPos, enemyPos, wallLayer);
						
						// 如果没有碰到墙壁
						if (hit.collider == null)
						{
							// 瞄准这个敌人
							shootDir = dirToEnemy;
							AimMark.Position2D(enemyPos);
							AimMark.Show();	// 显示瞄准标记
							foundTarget = true;
							break;
						}
					}
					
					// 如果没有找到可瞄准的敌人，隐藏瞄准标记
					if (!foundTarget)
					{
						AimMark.Hide();
					}
				}
				else
				{
					// 如果过滤后没有敌人，隐藏瞄准标记（保持瞄准鼠标方向）
					AimMark.Hide();
				}
			}
			else
			{
				// 如果没有敌人，隐藏瞄准标记
				AimMark.Hide();
			}
			
			// 平滑过渡瞄准方向
			smoothAimDir = Vector2.Lerp(smoothAimDir, shootDir, Time.deltaTime * aimSmoothSpeed);
			smoothAimDir.Normalize();

			// 让武器朝向平滑后的瞄准方向
			float angle = Mathf.Atan2(smoothAimDir.y, smoothAimDir.x) * Mathf.Rad2Deg;
        	weaponTransform.eulerAngles = new Vector3(0, 0, angle);
			// 当瞄准左边时翻转武器（假设武器默认朝右）
			if(smoothAimDir.x < 0)
			{
				weaponTransform.localScale = new Vector3(1, -1, 1);
				spriteRenderer.flipX = true;
			}
			else
			{
				weaponTransform.localScale = new Vector3(1, 1, 1);
				spriteRenderer.flipX = false;
			}

			//鼠标左键射击（朝平滑后的瞄准方向）
			if (Input.GetMouseButtonDown(0) && playerBullet != null)
			{				
				currentWeapon.StartAttacking(smoothAimDir);
			}
			//限制为固定射速
			if(Input.GetMouseButton(0) && playerBullet != null)
			{
				currentWeapon.KeepAttacking(smoothAimDir);
			}
			if(Input.GetMouseButtonUp(0) && playerBullet != null)
			{
				currentWeapon.StopAttacking();
			}

			// 按R键换弹
            if (Input.GetKeyDown(KeyCode.R) && bloodBank.CurrentBloodAmount >= currentWeapon.BloodRequired)
            {
                currentWeapon.Reload(); // 调用GunClip的reload方法进行换弹
                // GameUI.UpdateBloodText(bloodBank);
            }
			GameUI.UpdateBloodText(bloodBank);

			// 切枪
			// if(Input.GetKeyDown(KeyCode.Alpha1))
			// {
			// 	useWeapon(0);
			// }
			// if(Input.GetKeyDown(KeyCode.Alpha2))
			// {
			// 	useWeapon(1);
			// }
			if(Input.mouseScrollDelta.y > 0 || Input.GetKeyDown(KeyCode.Q)) // 鼠标滚轮向上滚动切换到上一个武器
			{
				// 使用模运算实现循环切换武器
				UseWeapon((weapons.IndexOf(currentWeapon) - 1 + weapons.Count) % weapons.Count);
			}
			else if(Input.mouseScrollDelta.y < 0 || Input.GetKeyDown(KeyCode.E)) // 鼠标滚轮向下滚动切换到下一个武器
			{				
				UseWeapon((weapons.IndexOf(currentWeapon) + 1) % weapons.Count);
			}
		}

        private void OnDestroy()
        {
			player1 = null;
        }
	}
}