using UnityEngine;
using QFramework;
using UnityEditor.Rendering;
using Unity.VisualScripting;
using JetBrains.Annotations;

namespace ProjectBlood
{
	public partial class Player : ViewController
	{
		public float moveSpeed = 3.5f;
		public static Player player1;
		public PlayerBullet playerBullet;
		public SpriteRenderer spriteRenderer;
		public Transform weaponTransform;
		public IWeapon currentWeapon;
		

		
		private void Awake()
		{
			player1 = this;
		}
		
		void Start()
		{
			HitBox.OnTriggerEnter2DEvent((Collider2D col)=>
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

			// 让武器朝向鼠标方向
			float angle = Mathf.Atan2(shootDir.y, shootDir.x) * Mathf.Rad2Deg;
        	weaponTransform.eulerAngles = new Vector3(0, 0, angle);
			// 当瞄准左边时翻转武器（假设武器默认朝右）
			if(shootDir.x < 0)
			{
				weaponTransform.localScale = new Vector3(1, -1, 1);
			}
			else
			{
				weaponTransform.localScale = new Vector3(1, 1, 1);
			}

			//鼠标左键射击（朝鼠标方向）
			if (Input.GetMouseButtonDown(0) && playerBullet != null)
			{				
				currentWeapon.StartAttacking(shootDir);
			}
			//限制为固定射速
			if(Input.GetMouseButton(0) && playerBullet != null)
			{
				currentWeapon.keepAttacking(shootDir);
			}
			if(Input.GetMouseButtonUp(0) && playerBullet != null)
			{
				currentWeapon.StopAttacking(shootDir);
			}

			// 按R键换弹
            if (Input.GetKeyDown(KeyCode.R))
            {
                currentWeapon.Reload();
            }
		}

        private void OnDestroy()
        {
			player1 = null;
        }
	}
}