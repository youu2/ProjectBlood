using UnityEngine;
using QFramework;

namespace ProjectBlood
{
	public abstract class DropItem : ViewController
	{
		public float flySpeed = 12f;
		public bool autoCollectOnRoomFinish = true; // 房间完成后是否自动飞向玩家
		private bool isFlyingToPlayer = false;

		void Update()
		{
			if (autoCollectOnRoomFinish && !isFlyingToPlayer && Global.currentRoom != null && Global.currentRoom.roomState == Room.RoomState.Finished)
			{
				isFlyingToPlayer = true;
			}

			if (isFlyingToPlayer && Player.player1 != null)
			{
				transform.position = Vector3.MoveTowards(transform.position, Player.player1.transform.position, flySpeed * Time.deltaTime);

				if (Vector3.Distance(transform.position, Player.player1.transform.position) < 0.3f)
				{
					Collect();
				}
			}
		}

		private void OnTriggerEnter2D(Collider2D collider)
		{
			if (collider.GetComponent<CollectBox>() != null)
			{
				Collect();
			}
		}

		protected abstract void Collect();
	}
}
