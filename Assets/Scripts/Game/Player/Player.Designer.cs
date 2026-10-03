// Generate Id:41230c6e-ac03-42b8-a5ee-3c94953340bc
using UnityEngine;

namespace ProjectBlood
{
	public partial class Player
	{
		public UnityEngine.GameObject CollisionBox;
		
		public CircleCollider2D HurtBox;
		
		public SpriteRenderer AimMark;
		
		public Animator PlayerAnimator;
		
		public Transform PlayerSelfLight2D;
		
		public Transform Arm;
		
		public UnityEngine.Transform Weapon;
		
		public ProjectBlood.SemiAutomaticWeapon DE;
		
		public ProjectBlood.AutomaticWeapon MP5;
		
		public ProjectBlood.ShotGun ShotGun;
		
		public ProjectBlood.AutomaticWeapon AK;
		
		public ProjectBlood.SemiAutomaticWeapon AWP;
		
		public ProjectBlood.Laser Laser;
		
		public UnityEngine.SpriteRenderer FireFlash;
		
		public SpriteRenderer ShieldSprite;
		
		public TMPro.TextMeshProUGUI NoticeText;
		
		public UnityEngine.Rigidbody2D SelfRigidbody2D;
		
		public UnityEngine.AudioClip WeaponSwitchSound;
		
		public UnityEngine.AudioSource SelfAudioSource;
		
		public SkillManager SelfSkillManager;
		
		public PlayerState SelfPlayerState;
		
	}
}
