using UnityEngine;
using QFramework;
using System.Collections.Generic;

namespace ProjectBlood
{
	public partial class AimMark : ViewController
	{
		private readonly List<Sprite> aimMarkFrameList = new List<Sprite>();
		private int currentAimMarkFrame = 0;
		private const float aimMarkFrameTime = 0.2f;
		private float aimMarkTimer = 0f;
		private SpriteRenderer aimSpriteRenderer;
		void Awake()
		{
			aimSpriteRenderer = GetComponent<SpriteRenderer>();
			aimMarkFrameList.Add(Aim1);
			aimMarkFrameList.Add(Aim2);
			aimMarkFrameList.Add(Aim3);
		}

		void Start()
		{
			updateAimMarkFrame();
		}
		void Update()
		{
			// 累加时间
			aimMarkTimer += Time.deltaTime;
			
			// 每 aimMarkFrameTime 时间切换一次 sprite
			if(aimMarkTimer >= aimMarkFrameTime)
			{
				aimMarkTimer = 0f;
				currentAimMarkFrame++;
				
				if(currentAimMarkFrame >= aimMarkFrameList.Count)
				{
					currentAimMarkFrame = 0;
				}
				
				updateAimMarkFrame();
			}
		}
		void updateAimMarkFrame()
		{
			aimSpriteRenderer.sprite = aimMarkFrameList[currentAimMarkFrame];
		}
	}
}
