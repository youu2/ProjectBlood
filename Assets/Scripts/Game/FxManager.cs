using UnityEngine;
using QFramework;

namespace ProjectBlood
{
	public partial class FxManager : ViewController
	{
		public static FxManager Instance;
		void Awake()
		{
			Instance = this;
		}
		public static void PlayEnemyHurtFX(Vector2 pos)
		{
			Instance.EnemyHurt.Instantiate()
			.Position2D(pos)
			.Show()
			.Self(self =>
			{
				ActionKit.Delay(self.main.duration + 0.3f, () => self.gameObject.SetActive(false)).StartCurrentScene();
				// StartCurrentScene用于在非MonoBehaviour类中启动协程，
				// QFramework 在场景加载时会自动创建一个隐藏的 SceneCoroutineRunner GameObject，专门用来管理这些协程。
			}).Play();
		}
		public static void PlayPlayerHurtFX(Vector2 pos)
		{
			Instance.PlayerHurt.Instantiate()
			.Position2D(pos)
			.Show()
			.Self(self =>
			{
				ActionKit.Delay(self.main.duration + 0.3f, () => self.gameObject.SetActive(false)).StartCurrentScene();
			}).Play();
		}


		public static void DrawBlood(Vector2 originPos, SpriteRenderer bloodSource)
		{
			var blood = bloodSource.Instantiate()
			.Position2D(originPos)
			.EulerAnglesZ(Random.Range(0,360f))
			.LocalScale(0.1f)
			.Show();

			// 血液随机向一个地方飞溅
			var angle = Random.Range(0, 360);
			var radius = Random.Range(0.2f, 1.5f);
			var movePos = angle.AngleToDirection2D() * radius;
			var scaleTo = Random.Range(0.2f, 3.0f);
			ActionKit.Lerp(0, 1, Random.Range(0.1f, 0.3f),(p) =>
			{
				p = EaseUtility.InCubic(0, 1, p);
				blood.Position2D(originPos + movePos * p);
				blood.LocalScale(scaleTo * p);
			}).StartCurrentScene();
		}

		public static void DrawPlayerBlood(Vector2 originPos)
		{
			DrawBlood(originPos, Instance.PlayerBlood);
		}

		public static void DrawEnemyBlood(Vector2 originPos)
		{
			DrawBlood(originPos, Instance.EnemyBlood);
		}

		public static void PlayShieldBlockFX(Vector2 pos)
		{
			Instance.ShieldBlock.Instantiate()
			.Position2D(pos)
			.Show()
			.Self(self =>
			{
				ActionKit.Delay(self.main.duration + 0.3f, () => self.gameObject.SetActive(false)).StartCurrentScene();
			}).Play();
		}

		public static void PlayShieldBreakFX(Vector2 pos)
		{
			Instance.ShieldBlock.Instantiate()
			.Position2D(pos)
			.Show()
			.Self(self =>
			{
				ActionKit.Delay(self.main.duration + 0.3f, () => self.gameObject.SetActive(false)).StartCurrentScene();
			}).Play();
		}

		void OnDestroy()
		{
			Instance = null;
		}
	}
}