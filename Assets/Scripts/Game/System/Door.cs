using UnityEngine;
using QFramework;

namespace ProjectBlood
{
	public partial class Door : ViewController
	{
		public MapController.Direction direction{get;set;}
		void Start()
		{
			// 关门挡光：门格挂 1×1 隐形 caster，门隐藏(开)时随物体失活自动失效
			ShadowCaster2DGenerator.AttachCellCaster(transform);
		}

		public Door WithDirection(MapController.Direction direction)
		{
			this.direction = direction;
			return this;
		}
	}
}
