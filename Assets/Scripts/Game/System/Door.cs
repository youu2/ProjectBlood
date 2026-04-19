using UnityEngine;
using QFramework;

namespace ProjectBlood
{
	public partial class Door : ViewController
	{
		public MapController.Direction direction{get;set;}
		void Start()
		{
			// Code Here
		}

		public Door WithDirection(MapController.Direction direction)
		{
			this.direction = direction;
			return this;
		}
	}
}
