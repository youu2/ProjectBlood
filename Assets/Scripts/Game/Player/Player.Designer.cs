// Generate Id:8bf73a38-c39a-40e5-acf5-47e537a7684c
using UnityEngine;

namespace ProjectBlood
{
	public partial class Player
	{
		public CircleCollider2D HurtBox;
		
		public TMPro.TextMeshProUGUI NoticeText;
		
		public SpriteRenderer AimMark;
		
		public Animator PlayerAnimator;
		
		public Transform Arm;
		
		public UnityEngine.Transform Weapon;
		
		public ProjectBlood.SemiAutomaticWeapon DE;
		
		public ProjectBlood.AutomaticWeapon MP5;
		
		public ProjectBlood.ShotGun ShotGun;
		
		public ProjectBlood.AutomaticWeapon AK;
		
		public ProjectBlood.SemiAutomaticWeapon AWP;
		
		public PenetratingBullet AWPBullet;
		
		public ProjectBlood.Laser Laser;
		
		public UnityEngine.SpriteRenderer FireFlash;
		
		public SpriteRenderer ShieldSprite;
		
		public UnityEngine.Rigidbody2D SelfRigidbody2D;
		
		public UnityEngine.AudioClip WeaponSwitchSound;
		
		public UnityEngine.AudioSource SelfAudioSource;
		
	}
}
