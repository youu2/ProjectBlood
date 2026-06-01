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
			FxManager.Instance.EnemyHurt.Instantiate()
			.Position2D(pos)
			.Show()
			.Self(self =>
			{
				ActionKit.Delay(self.main.duration + 0.3f, self.DestroyGameObjGracefully).StartCurrentScene();
				// StartCurrentScene用于在非MonoBehaviour类中启动协程，
				// QFramework 在场景加载时会自动创建一个隐藏的 SceneCoroutineRunner GameObject，专门用来管理这些协程。
			}).Play();
		}
		public static void PlayPlayerHurtFX(Vector2 pos)
		{
			FxManager.Instance.PlayerHurt.Instantiate()
			.Position2D(pos)
			.Show()
			.Self(self =>
			{
				ActionKit.Delay(self.main.duration + 0.3f, self.DestroyGameObjGracefully).StartCurrentScene();
				// StartCurrentScene用于在非MonoBehaviour类中启动协程，
				// QFramework 在场景加载时会自动创建一个隐藏的 SceneCoroutineRunner GameObject，专门用来管理这些协程。
			}).Play();
		}
		void OnDestroy()
		{
			Instance = null;
		}
	}
}