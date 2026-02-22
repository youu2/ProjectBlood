using UnityEngine;
using QFramework;
using UnityEditor.Rendering;
using Unity.VisualScripting;

namespace ProjectBlood
{
	public partial class Player : ViewController
	{
		public float moveSpeed = 3.5f;
		public static Player player1;
		public PlayerBullet playerBullet;
		public SpriteRenderer spriteRenderer;
		
		private void Awake()
		{
			player1 = this;
		}
		
		void Start()
		{
			HitBox.OnTriggerEnter2DEvent((Collider2D col)=>
			{
				var hitBox = col.GetComponent<HitBox>();
				// 如果撞到的东西没有伤害能力，就跳过死亡流程
				if (hitBox == null) return; 
				Global.currentHP.Value -= col.GetComponent<HitBox>().owner.GetComponent<Enemy>().Damage;
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

			// 鼠标左键射击（朝鼠标方向）
			if (Input.GetMouseButtonDown(0) && playerBullet != null)
			{
				// 获取鼠标在屏幕上的位置
				Vector3 mouseScreenPos = Input.mousePosition;
				// 转成世界坐标，Z 要设成 0（2D 游戏）
				mouseScreenPos.z = 0;
				Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);
				
				// 计算从玩家指向鼠标的方向
				Vector2 shootDir = (mouseWorldPos - transform.position).normalized;
				
				// 生成子弹
				var bullet = Instantiate(playerBullet, transform.position, Quaternion.identity);
				bullet.direction = shootDir;
				bullet.gameObject.SetActive(true);
			}
		}

        private void OnDestroy()
        {
			player1 = null;
        }
	}
}