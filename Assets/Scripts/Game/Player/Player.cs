using System.Collections;
using System.Collections.Generic;
using System.Linq;
using QFramework;
using UnityEngine;

namespace ProjectBlood
{
    public partial class Player : ViewController
    {
        public float moveSpeed = 3.5f;
        public static Player player1;
        public PlayerBullet playerBullet;
        public WeaponBase currentWeapon; // 当前装备的武器
        // private List<WeaponBase> weapons = new List<WeaponBase>(); // 武器列表

        // public BloodBank bloodBank = new BloodBank(); // 血液银行组件，特殊资源，用于弹药管理和血量管理
        private ShieldState shieldState = new ShieldState(); // 护盾状态
        private Vector2 smoothAimDir; // 平滑过渡后的瞄准方向（单位向量）
        private float firstReloadTime; // 首次按下R键的时间
        private bool isSpecialReloadTriggered; // 是否已经触发了特殊换弹
        private Coroutine specialReloadCoroutine; // 特殊换弹协程
        private const float specialReloadWindow = 2f; // 双击R的时间窗口（秒）
        private const float specialReloadDelay = 3f; // 特殊换弹延迟时间（秒）
        private int specialReloadBloodCost = 20; // 特殊换弹消耗的血库资源
        private const float aimSmoothSpeed = 20f; // 瞄准平滑速度，值越大过渡越快
        private const float aimAngle = 35f; // 自动锁敌的角度范围（度）
        [SerializeField] private float SpecialReloadVolume = 0.7f;
        private bool recorded = false;
        Vector2 lastMoveDir;

        // faceLeft: true=朝左，false=朝右
        // 当玩家朝左时，翻转整个玩家对象，包括武器，文字提示单独再翻转一次
        private void SetFlipX(bool faceLeft)
        {
            if (SelfPlayerState.GetState() == PlayerState.State.Rolling && !recorded)
            {
                recorded = true;
                lastMoveDir = SelfSkillManager.GetFacingDirection();
            }
            if (SelfPlayerState.GetState() != PlayerState.State.Rolling && recorded)
            {
                recorded = false;
                // transform.localRotation = Quaternion.Euler(0, 0, 0);
            }

            if (recorded && lastMoveDir.x > 0 && faceLeft ||
                recorded && lastMoveDir.x < 0 && !faceLeft)
            {
                transform.localRotation = Quaternion.Euler(0, 180, 0);
            }
            else if (!recorded && SelfPlayerState.GetState() != PlayerState.State.Rolling)
            {
                transform.localRotation = Quaternion.Euler(0, 0, 0);
            }
            float scaleX = faceLeft ? -1f : 1f;
            transform.localScale = new Vector3(1.2f * scaleX, 1.2f, 1f);
            NoticeText.transform.localScale = new Vector3(0.0005f * scaleX, 0.0005f, 1f);
        }

        // 根据瞄准方向更新武器朝向和角色朝向
        private void UpdateWeaponAim(Vector2 aimDir)
        {
            float angle = Mathf.Atan2(aimDir.y, aimDir.x) * Mathf.Rad2Deg;

            if (aimDir.x < 0)
            {
                // 朝左：武器X轴翻转 + 旋转180度补偿Player镜像的影响
                // 玩家对象整体翻转导致武器依旧朝右，所以需要水平翻转武器Sprite
                SetFlipX(true);
                Arm.localScale = new Vector3(-1, -1, 1);
            }
            else
            {
                // 朝右：武器保持默认朝向
                Arm.localScale = new Vector3(1, 1, 1);
                SetFlipX(false);
            }
            Arm.eulerAngles = new Vector3(0, 0, angle);
        }

        // 显示跟随玩家的提示文本（换弹提示，购买提示）
        public static void DisplayText(string text)
        {
            player1.StartCoroutine(player1.ShowText(text, 2.0f));
        }

        public static void HideText()
        {
            player1.NoticeText.Hide();
        }

        IEnumerator ShowText(string text, float duration)
        {
            player1.NoticeText.text = text;
            player1.NoticeText.Show();
            yield return new WaitForSeconds(duration);
            player1.NoticeText.Hide();
        }
        private void Awake()
        {
            // 设置帧率为60，确保游戏和逻辑稳定运行
            Application.targetFrameRate = 60;
            // 依次添加武器到武器列表，后续可能会改成根据游戏进度逐步获取，比如从宝箱中获取
            player1 = this;
            PlayerUpgradeState.OnPlayerSpawned(); // 补回累计移速加成（Player 不跨场景，强化加成存在静态状态中）
            UseWeapon(0); // 默认装备第一把武器
            NoticeText.Hide();
            specialReloadBloodCost = (WeaponDataSystem.weaponDataList.Count - 1) * 3;   // 根据武器数量动态调整特殊换弹消耗的血库资源
            // 护盾一直挂载在玩家对象上，初始化护盾状态，捡到道具后才会激活
            shieldState.Initialize(ShieldSprite, this);
        }

        public WeaponBase GetWeaponFromName(string weaponName)
        {
            return GetWeapon(WeaponTypeExtensions.FromName(weaponName));
        }

        // 按武器类型枚举获取武器实例（强化系统/武器进化使用）
        public WeaponBase GetWeapon(WeaponType type)
        {
            switch (type)
            {
                case WeaponType.DE: return DE;
                case WeaponType.MP5: return MP5;
                case WeaponType.ShotGun: return ShotGun;
                case WeaponType.AK: return AK;
                case WeaponType.AWP: return AWP;
                case WeaponType.Laser: return Laser;
                default: return null;
            }
        }

        void UseWeapon(int index)
        {
            var weaponData = WeaponDataSystem.weaponDataList[index];

            var previousWeapon = currentWeapon;

            // 停止上一把武器的所有状态
            if (previousWeapon != null)
            {
                if (WeaponDataSystem.weaponDataList.Count > 1)
                {
                    previousWeapon.SwitchFromSet();
                    previousWeapon.Hide();
                }

                previousWeapon.SaveWeaponData();
            }

            // 切换到新武器
            currentWeapon = GetWeaponFromName(weaponData.weaponName);

            // weaponTransform = currentWeapon.transform;
            if (WeaponDataSystem.weaponDataList.Count > 1) currentWeapon.SwitchToSet();
            currentWeapon.Show();
            currentWeapon.LoadWeaponData(weaponData);
            GameUI.UpdateClipText(currentWeapon.GetGunClip());
            // 立即将新武器对准当前瞄准方向，避免切枪时的一帧延迟
            if (smoothAimDir != Vector2.zero)
            {
                UpdateWeaponAim(smoothAimDir);
            }

            // 播放切换音效（独立播放，不需要等待）
            AudioKitManager.Instance.PlayOneShot(WeaponSwitchSound, volume: 0.3f);
            // 更新相机大小
            Global.WeaponAdditionalCameraSize = currentWeapon.AdditionalCameraSize;

            // 强化系统：切枪钩子（重置单武器持续叠加、尝试激活切枪增益被动）
            PlayerUpgradeState.OnWeaponSwitched(currentWeapon.WeaponType);
        }

        void Start()
        {
            Global.currentHP.RegisterWithInitValue(currentHP =>
            {
                if (currentHP <= 0)
                {
                    Death();
                }
            }).UnRegisterWhenGameObjectDestroyed(gameObject);

            var weaponData = WeaponDataSystem.weaponDataList[0];
            if (weaponData.weaponName == WeaponConfig.DE.weaponName)
            {
                UseWeapon(0);
            }
        }

        public void TakeDamage(float damage)    // 玩家受到伤害
        {
            // 检查护盾是否抵挡伤害
            if (shieldState.HandleDamage(transform.Position2D()))
            {
                return;
            }

            FxManager.PlayPlayerHurtFX(transform.Position2D());
            FxManager.DrawPlayerBlood(transform.Position2D());
            Global.currentHP.Value -= damage;
            BloodBank.Instance.AddBlood((int)Mathf.Round(damage));
            if (Global.currentHP.Value < 0) Global.currentHP.Value = 0;

            if (Global.currentHP.Value > 0)
            {
                AudioKitManager.Instance.PlayOneShot("Hurt", volume: 0.5f);
            }
            else
            {
                Death();
            }
        }

        public void ActivateShield(int blockCount, float duration)
        {
            shieldState.Activate(blockCount, duration);
        }

        private void Death()
        {
            Global.SettleLegacyPoints();
            AudioKitManager.Instance.PlayOneShot("WilhelmScream");
            this.DestroyGameObjGracefully();
            UIKit.OpenPanel<UIGameOverPanel>();
        }

        void Update()
        {
            float horizontal = Input.GetAxis("Horizontal"); // A/D
            float vertical = Input.GetAxis("Vertical");     // W/S										
            // 设置移动动画状态
            bool isMoving = horizontal != 0 || vertical != 0;
            PlayerAnimator.SetBool("isMoving", isMoving);

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

            // 检查敌人自动锁敌功能
            if (Global.currentRoom && Global.currentRoom.GetEnemies().Count > 0)
            {
                var enemies = Global.currentRoom.GetEnemies();

                // 将 HashSet 转换为 List 并过滤掉已销毁或正在死亡的敌人
                var enemiesList = enemies.Where(enemy => enemy != null).ToList();

                if (enemiesList.Count > 0)
                {
                    // 将敌人按离鼠标指针的距离从近到远排序
                    var sortedEnemies = enemiesList.OrderBy(enemy =>
                        Vector2.Distance(enemy.GameObject.transform.position, mouseWorldPos)
                    ).ToList();

                    // 获取 Wall Layer 的掩码
                    int wallLayer = LayerMask.GetMask("Wall");

                    // 标记是否找到了可瞄准的敌人
                    bool foundTarget = false;

                    // 遍历排序后的敌人，找到第一个没有障碍物的
                    foreach (var enemy in sortedEnemies)
                    {
                        // 再次检查敌人是否还存在且没有在死亡过程中
                        if (enemy == null)
                        {
                            continue;
                        }

                        // 检查玩家到敌人之间是否有墙壁障碍物
                        Vector2 playerPos = transform.position;
                        Vector2 enemyPos = enemy.GameObject.transform.position;
                        Vector2 dirToEnemy = (enemyPos - playerPos).normalized;

                        // 检查敌人是否在鼠标方向的30度范围内
                        float angleToEnemy = Vector2.Angle(shootDir, dirToEnemy);
                        if (angleToEnemy > aimAngle)
                        {
                            continue;
                        }

                        // 使用射线检测，只检测 Wall 层的物体
                        RaycastHit2D hit = Physics2D.Linecast(playerPos, enemyPos, wallLayer);

                        // 如果没有碰到墙壁
                        if (hit.collider == null)
                        {
                            // 瞄准这个敌人
                            shootDir = dirToEnemy;
                            AimMark.Position2D(enemyPos);
                            AimMark.Show(); // 显示瞄准标记
                            foundTarget = true;
                            break;
                        }
                    }

                    // 如果没有找到可瞄准的敌人，隐藏瞄准标记
                    if (!foundTarget)
                    {
                        AimMark.Hide();
                    }
                }
                else
                {
                    // 如果过滤后没有敌人，隐藏瞄准标记（保持瞄准鼠标方向）
                    AimMark.Hide();
                }
            }
            else
            {
                // 如果没有敌人，隐藏瞄准标记
                AimMark.Hide();
            }

            // 平滑过渡瞄准方向
            // 使用线性插值使武器旋转更自然，避免方向突变
            // 速度由aimSmoothSpeed控制插值速度，值越大过渡越快
            smoothAimDir = Vector2.Lerp(smoothAimDir, shootDir, Time.deltaTime * aimSmoothSpeed);
            smoothAimDir.Normalize();
            // 更新武器朝向和角色朝向
            UpdateWeaponAim(smoothAimDir);

            //鼠标左键射击（朝平滑后的瞄准方向）
            if (Input.GetMouseButtonDown(0) && playerBullet != null && !Global.IsGamePaused)
            {
                if (isSpecialReloadTriggered && specialReloadCoroutine != null)
                {
                    StopCoroutine(specialReloadCoroutine);
                    isSpecialReloadTriggered = false;
                    specialReloadCoroutine = null;
                }
                currentWeapon.StartAttacking();
            }
            //限制为固定射速
            if (Input.GetMouseButton(0) && playerBullet != null && !Global.IsGamePaused)
            {
                if (isSpecialReloadTriggered && specialReloadCoroutine != null)
                {
                    StopCoroutine(specialReloadCoroutine);
                    isSpecialReloadTriggered = false;
                    specialReloadCoroutine = null;
                }
                currentWeapon.KeepAttacking(smoothAimDir);
            }
            if (Input.GetMouseButtonUp(0) && playerBullet != null)
            {
                currentWeapon.StopAttacking();
            }

            // 按R键换弹
            if (Input.GetKeyDown(KeyCode.R) && !Global.IsGamePaused)
            {
                float currentTime = Time.time;
                if (currentWeapon.GetGunClip().CanReload())
                {
                    currentWeapon.Reload();
                    firstReloadTime = currentTime;
                    isSpecialReloadTriggered = false;
                }
                else if (currentTime - firstReloadTime <= specialReloadWindow &&
                BloodBank.Instance.CurrentBloodAmount >= specialReloadBloodCost &&
                WeaponDataSystem.weaponDataList.Count > 2)
                {
                    isSpecialReloadTriggered = true;
                    specialReloadCoroutine = StartCoroutine(SpecialReloadCoroutine());
                }
            }
            GameUI.UpdateBloodText();

            // 切枪
            if (Input.GetKeyDown(KeyCode.Alpha1) && !Global.IsGamePaused)
            {
                UseWeapon(0);
            }
            if (Input.GetKeyDown(KeyCode.Alpha2) && !Global.IsGamePaused)
            {
                UseWeapon(1);
            }
            if (Input.GetKeyDown(KeyCode.Alpha3) && !Global.IsGamePaused)
            {
                UseWeapon(2);
            }
            if (Input.GetKeyDown(KeyCode.Alpha4) && !Global.IsGamePaused)
            {
                UseWeapon(3);
            }
            if (Input.GetKeyDown(KeyCode.Alpha5) && !Global.IsGamePaused)
            {
                UseWeapon(4);
            }
            if (Input.GetKeyDown(KeyCode.Alpha6) && !Global.IsGamePaused)
            {
                UseWeapon(5);
            }
            if ((Input.mouseScrollDelta.y > 0 || Input.GetKeyDown(KeyCode.Q)) && !Global.IsGamePaused) // 鼠标滚轮向上滚动切换到上一个武器
            {
                // 使用模运算实现循环切换武器
                UseWeapon((WeaponDataSystem.weaponDataList.IndexOf(currentWeapon.Data) - 1 + WeaponDataSystem.weaponDataList.Count) % WeaponDataSystem.weaponDataList.Count);
            }
            else if ((Input.mouseScrollDelta.y < 0 || Input.GetKeyDown(KeyCode.E)) && !Global.IsGamePaused) // 鼠标滚轮向下滚动切换到下一个武器
            {
                UseWeapon((WeaponDataSystem.weaponDataList.IndexOf(currentWeapon.Data) + 1) % WeaponDataSystem.weaponDataList.Count);
            }

            // 强化系统：被动增益计时（暂停时 deltaTime 为 0，不会误走时）
            PlayerUpgradeState.TickPassives(Time.deltaTime);
        }

        // 特殊换弹协程, 双击换弹触发，为所有武器补充弹药并播放音效
        private IEnumerator SpecialReloadCoroutine()
        {
            yield return new WaitForSeconds(specialReloadDelay);
            if (isSpecialReloadTriggered && BloodBank.Instance.CurrentBloodAmount >= specialReloadBloodCost)
            {
                BloodBank.Instance.RemoveBlood(specialReloadBloodCost);

                foreach (var weaponData in WeaponDataSystem.weaponDataList)
                {
                    if (weaponData != currentWeapon.Data)
                    {
                        var weapon = GetWeaponFromName(weaponData.weaponName);
                        weapon.FillClipDirectly();
                        weapon.SaveWeaponData();
                    }
                }

                AudioKitManager.Instance.PlayOneShot("SpecialReload", volume: SpecialReloadVolume);
            }
            GameUI.UpdateClipText(currentWeapon.GetGunClip());
            isSpecialReloadTriggered = false;
            specialReloadCoroutine = null;
        }

        public void UpdateSpecialReloadCost()
        {
            specialReloadBloodCost = (WeaponDataSystem.weaponDataList.Count - 1) * 3;
        }

        public void UpdateRollAnimationDirection()
        {
            if (SelfPlayerState.GetState() == PlayerState.State.Rolling)
            {
                var facingDirection = SelfSkillManager.GetFacingDirection();
                float angle = Mathf.Atan2(facingDirection.y, facingDirection.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0, 0, angle);
            }
            else
            {
                transform.rotation = Quaternion.identity;
            }
        }

        private void OnDestroy()
        {
            player1 = null;
            if (specialReloadCoroutine != null)
            {
                StopCoroutine(specialReloadCoroutine);
            }
        }
    }
}