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
        // 主菜单场景标记：主菜单中的玩家仅作互动展示，禁用切枪/特殊换弹/死亡等局内逻辑
        private static bool IsMainMenuScene => UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "GameStart";
        public PlayerBullet playerBullet;
        public WeaponBase currentWeapon; // 当前装备的武器
        // private List<WeaponBase> weapons = new List<WeaponBase>(); // 武器列表

        // public BloodBank bloodBank = new BloodBank(); // 血液银行组件,特殊资源,用于弹药管理和血量管理
        private ShieldState shieldState = new ShieldState(); // 护盾状态
        private Vector2 smoothAimDir; // 平滑过渡后的瞄准方向(单位向量)
        private float firstReloadTime; // 首次按下R键的时间
        private bool isSpecialReloadTriggered; // 是否已经触发了特殊换弹
        private Coroutine specialReloadCoroutine; // 特殊换弹协程
        private const float specialReloadWindow = 2f; // 双击R的时间窗口(秒)
        private const float specialReloadDelay = 3f; // 特殊换弹延迟时间(秒)
        private int specialReloadBloodCost = 20; // 特殊换弹消耗的血库资源
        private const float aimSmoothSpeed = 20f; // 瞄准平滑速度,值越大过渡越快
        private const float aimAngle = 35f; // 自动锁敌的角度范围(度)

        // 自动瞄准锁敌开关：默认关闭（已从基础能力移除），由血印结算效果在激活/结束时置位。
        // 置 true 后锁敌逻辑与原基础能力完全一致（角度/遮挡检测/瞄准标记/精度不变）。
        public bool AutoAimLockEnabled { get; set; }

        [SerializeField] private float SpecialReloadVolume = 0.7f;
        private bool recorded = false;
        Vector2 lastMoveDir;

        // faceLeft: true=朝左,false=朝右
        // 当玩家朝左时,翻转整个玩家对象,包括武器,文字提示单独再翻转一次
        private void SetFlipX(bool faceLeft)
        {
            float scaleX = faceLeft ? -1f : 1f;

            if (IsRollOpposite(faceLeft))
            {
                scaleX = -scaleX;
            }
            else
            {
                scaleX = faceLeft ? -1f : 1f;
            }

            transform.localScale = new Vector3(1.2f * scaleX, 1.2f, 1f);
            NoticeText.transform.localScale = new Vector3(0.0005f * scaleX, 0.0005f, 1f);
        }

        // 判断玩家是否在朝瞄准的反方向翻滚
        public bool IsRollOpposite(bool faceLeft)
        {
            if (SelfPlayerState.GetState() == PlayerState.State.Rolling && !recorded)
            {
                recorded = true;
                lastMoveDir = SelfSkillManager.GetFacingDirection();
            }
            if (SelfPlayerState.GetState() != PlayerState.State.Rolling && recorded)
            {
                recorded = false;
            }

            if (recorded && lastMoveDir.x > 0 && faceLeft ||
                recorded && lastMoveDir.x < 0 && !faceLeft)
            {
                return true;
            }
            else if (!recorded && SelfPlayerState.GetState() != PlayerState.State.Rolling)
            {
                return false;
            }
            return false;
        }

        // 根据瞄准方向更新武器朝向和角色朝向
        private void UpdateWeaponAim(Vector2 aimDir)
        {
            float angle = Mathf.Atan2(aimDir.y, aimDir.x) * Mathf.Rad2Deg;

            if (aimDir.x < 0)
            {
                // 朝左:武器X轴翻转 + 旋转180度补偿Player镜像的影响
                // 玩家对象整体翻转导致武器依旧朝右,所以需要水平翻转武器Sprite
                // 反向翻滚时，玩家会水平翻转，所以只需要再竖直翻转一次
                SetFlipX(true);
                if (!IsRollOpposite(true))
                {
                    Arm.localScale = new Vector3(-1, -1, 1);
                }
                else
                {
                    Arm.localScale = new Vector3(1, -1, 1);
                }
            }
            else
            {
                // 朝右:武器保持默认朝向
                // 反向翻滚时，玩家会水平翻转，所以需要再水平翻转一次抵消翻滚的影响
                if (!IsRollOpposite(false))
                {
                    Arm.localScale = new Vector3(1, 1, 1);
                }
                else
                {
                    Arm.localScale = new Vector3(-1, 1, 1);
                }
                SetFlipX(false);
            }
            Arm.eulerAngles = new Vector3(0, 0, angle);
        }

        // 自动瞄准锁敌逻辑：从 Player.Update 中抽出，供 AutoAimLock 开关复用。
        // 行为与原基础能力完全一致：角度范围 aimAngle(35°) → 距离鼠标最近排序 →
        // 墙射线检测 → 标记瞄准 → 平滑插值仍由 Update 内统一处理。
        private Vector2 ApplyAutoAimLock(Vector2 currentShootDir, Vector2 mouseWorldPos)
        {
            if (Global.currentRoom == null)
            {
                AimMark.Hide();
                return currentShootDir;
            }

            var enemies = Global.currentRoom.GetEnemies();
            var enemiesList = enemies.Where(enemy => enemy != null).ToList();
            if (enemiesList.Count == 0)
            {
                AimMark.Hide();
                return currentShootDir;
            }

            var sortedEnemies = enemiesList.OrderBy(enemy =>
                Vector2.Distance(enemy.GameObject.transform.position, mouseWorldPos)
            ).ToList();

            int wallLayer = LayerMask.GetMask("Wall");

            foreach (var enemy in sortedEnemies)
            {
                if (enemy == null) continue;

                Vector2 playerPos = transform.position;
                Vector2 enemyPos = enemy.GameObject.transform.position;
                Vector2 dirToEnemy = (enemyPos - playerPos).normalized;
                float angleToEnemy = Vector2.Angle(currentShootDir, dirToEnemy);
                if (angleToEnemy > aimAngle) continue;

                RaycastHit2D hit = Physics2D.Linecast(playerPos, enemyPos, wallLayer);
                if (hit.collider == null)
                {
                    AimMark.Position2D(enemyPos);
                    AimMark.Show();
                    return dirToEnemy; // 锁定成功，直接返回敌人方向
                }
            }

            // 未命中任何可锁目标，保持鼠标方向并隐藏标记
            AimMark.Hide();
            return currentShootDir;
        }

        // 显示跟随玩家的提示文本(换弹提示,购买提示)
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
            // 设置帧率为60,确保游戏和逻辑稳定运行
            Application.targetFrameRate = 300;
            // 依次添加武器到武器列表,后续可能会改成根据游戏进度逐步获取,比如从宝箱中获取
            player1 = this;
            PlayerUpgradeState.OnPlayerSpawned(); // 补回累计移速加成(Player 不跨场景,强化加成存在静态状态中)
            UseWeapon(0); // 默认装备第一把武器
            // 场景重载后武器实例全部重建,但静态 WeaponData 跨场景存活。
            // 立即为其余已拥有武器静默补加载数据(含尚未激活、Awake 未执行的隐藏武器),
            // 恢复“所有已拥有武器 Data 齐备”不变量,供特殊换弹等遍历全部武器的逻辑使用
            foreach (var weaponData in WeaponDataSystem.weaponDataList)
            {
                if (weaponData == null)
                {
                    continue;
                }
                var weapon = GetWeaponFromName(weaponData.weaponName);
                if (weapon == null)
                {
                    Debug.LogWarning($"场景加载:找不到武器 {weaponData.weaponName} 的实例,跳过其数据加载");
                    continue;
                }
                if (weapon == currentWeapon)
                {
                    continue; // 当前武器已由 UseWeapon(0) 加载并刷新过 UI
                }
                weapon.LoadWeaponData(weaponData, updateUI: false);
            }
            NoticeText.Hide();
            specialReloadBloodCost = (WeaponDataSystem.weaponDataList.Count - 1) * 3;   // 根据武器数量动态调整特殊换弹消耗的血库资源
            // 护盾一直挂载在玩家对象上,初始化护盾状态,捡到道具后才会激活
            shieldState.Initialize(ShieldSprite, this);
        }

        public WeaponBase GetWeaponFromName(string weaponName)
        {
            return GetWeapon(WeaponTypeExtensions.FromName(weaponName));
        }

        // 按武器类型枚举获取武器实例(强化系统/武器进化使用)
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
            // 立即将新武器对准当前瞄准方向,避免切枪时的一帧延迟
            if (smoothAimDir != Vector2.zero)
            {
                UpdateWeaponAim(smoothAimDir);
            }

            // 播放切换音效(独立播放,不需要等待)
            AudioKitManager.Instance.PlayOneShot(WeaponSwitchSound, volume: 0.3f);
            // 更新相机大小
            Global.WeaponAdditionalCameraSize = currentWeapon.AdditionalCameraSize;

            // 强化系统:切枪钩子(武器进化等)(被动增伤已迁移至血印系统)
            // 血印系统:切枪钩子(限时增伤/连射重置等效果)
            BloodSigilState.NotifyWeaponSwitched(currentWeapon.WeaponType);
        }

        void Start()
        {
            Global.currentHP.RegisterWithInitValue(currentHP =>
            {
                // 主菜单不触发死亡：从游戏失败返回主菜单时 currentHP 残留为 0，
                // 若此处死亡会误开 UIGameOverPanel 并销毁菜单展示玩家
                if (currentHP <= 0 && !IsMainMenuScene)
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
            // 暂停/场景加载期间免伤：timeScale=0 已冻结物理碰撞，
            // 这里作为最后一道防线拦住不经过物理步进的即时伤害
            // （如近战敌人攻击协程启动当帧的 MakeDamage、激光持续伤害协程）
            if (Global.IsGamePaused) return;

            // 检查护盾是否抵挡伤害
            if (shieldState.HandleDamage(transform.Position2D()))
            {
                return;
            }

            // 血印：优先消耗"免疫下一次伤害"充能（由伤害免疫类结算效果预先充入）
            if (BloodSigilState.ConsumeNextDamageImmunity())
            {
                // 本次伤害完全免疫：不扣血、不死亡
                return;
            }

            // 血印致命拦截：若本次伤害将导致死亡，询问血印模块是否处理
            // （触发模块的结算如"免疫本次伤害 + 获得护盾"在 Fire 中立即生效）
            if (Global.currentHP.Value - damage <= 0f
                && BloodSigilState.TryHandleLethalDamage(damage))
            {
                return;
            }

            FxManager.PlayPlayerHurtFX(transform.Position2D());
            FxManager.DrawPlayerBlood(transform.Position2D());
            Global.currentHP.Value -= damage;
            BloodBank.Instance.AddBlood((int)Mathf.Round(damage));
            if (Global.currentHP.Value < 0) Global.currentHP.Value = 0;

            // 血印：实际受伤事件（在扣血后通知，供受伤触发类模块结算）
            BloodSigilState.NotifyDamageTaken(damage);
            // 血印：血量变化事件（驱动血量阈值类触发，边沿锁存避免区间内重复触发）
            BloodSigilState.NotifyHealthChanged();

            if (Global.currentHP.Value > 0)
            {
                AudioKitManager.Instance.PlayOneShot("Hurt", volume: 0.5f);
            }
        }

        public void ActivateShield(int blockCount, float duration)
        {
            shieldState.Activate(blockCount, duration);
        }

        private void Death()
        {
            LegacyUpgradeState.SettleFromRun(Global.Level.Value);
            RunSaveService.DeleteSave();    // 永久死亡：删除本局存档
            AudioKitManager.Instance.PlayOneShot("WilhelmScream");
            this.DestroyGameObjGracefully();
            UIKit.OpenPanel<UIGameOverPanel>();
        }

        void Update()
        {
            bool inMainMenu = IsMainMenuScene;
            // 主菜单养成面板打开期间(IsGamePaused=true)：冻结玩家全部操作(移动/瞄准/射击/换弹)
            if (inMainMenu && Global.IsGamePaused)
            {
                SelfRigidbody2D.velocity = Vector2.zero;
                PlayerAnimator.SetBool("isMoving", false);
                AimMark.Hide();
                currentWeapon.StopAttacking();
                return;
            }

            float horizontal = Input.GetAxis("Horizontal"); // A/D
            float vertical = Input.GetAxis("Vertical");     // W/S
            // 暂停(加载进入下一关/结算等)期间冻结玩家移动输入
            if (Global.IsGamePaused)
            {
                horizontal = 0f;
                vertical = 0f;
            }
            // 设置移动动画状态
            bool isMoving = horizontal != 0 || vertical != 0;
            PlayerAnimator.SetBool("isMoving", isMoving);

            // 保持任意方向速度一致
            var direction = new Vector2(horizontal, vertical).normalized;
            SelfRigidbody2D.velocity = direction * moveSpeed;

            // 暂停/场景加载期间冻结整条瞄准链路：不读鼠标、不锁敌、不旋转武器，准星隐藏。
            // Esc 暂停(timeScale=0)时插值本已冻结，加载页(timeScale=0)同理；
            // 这里显式跳过，保证任何暂停语义下武器都保持最后朝向、准星不闪烁
            if (!Global.IsGamePaused)
            {
                // 获取鼠标在屏幕上的位置
                Vector3 mouseScreenPos = Input.mousePosition;
                // 转成世界坐标,Z 要设成 0(2D 游戏)
                mouseScreenPos.z = 0;
                Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);

                // 计算从玩家指向鼠标的方向
                Vector2 shootDir = (mouseWorldPos - transform.position).normalized;

                // 自动瞄准锁敌：已不再是基础能力，仅当血印结算效果激活开关后才执行
                if (AutoAimLockEnabled)
                {
                    shootDir = ApplyAutoAimLock(shootDir, mouseWorldPos);
                }
                else
                {
                    AimMark.Hide();
                }

                // 平滑过渡瞄准方向
                // 使用线性插值使武器旋转更自然,避免方向突变
                // 速度由aimSmoothSpeed控制插值速度,值越大过渡越快
                smoothAimDir = Vector2.Lerp(smoothAimDir, shootDir, Time.deltaTime * aimSmoothSpeed);
                smoothAimDir.Normalize();
                // 更新武器朝向和角色朝向
                UpdateWeaponAim(smoothAimDir);
            }
            else
            {
                AimMark.Hide();
            }

            // 主菜单中指针悬停在 UI 上时不触发射击，避免点击菜单按钮误开火
            bool pointerOverUI = inMainMenu
                && UnityEngine.EventSystems.EventSystem.current != null
                && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();

            //鼠标左键射击(朝平滑后的瞄准方向)
            if (Input.GetMouseButtonDown(0) && playerBullet != null && !Global.IsGamePaused && !pointerOverUI)
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
            if (Input.GetMouseButton(0) && playerBullet != null && !Global.IsGamePaused && !pointerOverUI)
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
                // 特殊换弹不要求血库存量充足:血量不足也允许触发,
                // 但补装的其他武器子弹不会被强化(具体处理见 SpecialReloadCoroutine)
                else if (!inMainMenu && currentTime - firstReloadTime <= specialReloadWindow &&
                WeaponDataSystem.weaponDataList.Count > 2)
                {
                    isSpecialReloadTriggered = true;
                    // 快速连按可能重复触发,先停止上一个等待中的特殊换弹协程,避免重复扣血/补弹
                    if (specialReloadCoroutine != null)
                    {
                        StopCoroutine(specialReloadCoroutine);
                    }
                    specialReloadCoroutine = StartCoroutine(SpecialReloadCoroutine());
                }
            }
            GameUI.UpdateBloodText();

            // 切枪（主菜单仅展示初始武器 DE，禁用一切切枪输入）
            if (!inMainMenu)
            {
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
            }

            // 血印系统:每帧驱动限时效果计时（被动计时已迁移至血印系统）
            BloodSigilState.Tick(Time.deltaTime);

            // 通关耗时累计：暂停（加载/升级/暂停页）与主菜单展示期间不计入
            if (!Global.IsGamePaused && !inMainMenu)
            {
                Global.RunElapsedSeconds += Time.deltaTime;
            }
        }

        // 特殊换弹协程, 双击换弹触发,为所有武器补充弹药并播放音效
        private IEnumerator SpecialReloadCoroutine()
        {
            yield return new WaitForSeconds(specialReloadDelay);

            // 3 秒等待期间可能发生关卡切换等销毁流程,等待结束后重新校验关键引用
            if (currentWeapon == null || BloodBank.Instance == null)
            {
                isSpecialReloadTriggered = false;
                specialReloadCoroutine = null;
                yield break;
            }

            if (isSpecialReloadTriggered)
            {
                // 血库足以支付特殊换弹代价时才扣血,并让其他武器补装强化子弹;
                // 血量不足时特殊换弹照常执行(补满其他武器弹夹),但不扣血、补装的子弹不强化
                bool canEnhance = BloodBank.Instance.CurrentBloodAmount >= specialReloadBloodCost;
                if (canEnhance)
                {
                    BloodBank.Instance.RemoveBlood(specialReloadBloodCost);
                }

                foreach (var weaponData in WeaponDataSystem.weaponDataList)
                {
                    if (weaponData == null || weaponData == currentWeapon.Data)
                    {
                        continue;
                    }

                    var weapon = GetWeaponFromName(weaponData.weaponName);
                    if (weapon == null)
                    {
                        // 武器数据存在但场景中找不到对应武器实例(如未在 Inspector 赋值),跳过而不是中断整个补弹流程
                        Debug.LogWarning($"特殊换弹:找不到武器 {weaponData.weaponName} 的实例,已跳过");
                        continue;
                    }

                    // 场景重载后未切换过的武器尚未执行 LoadWeaponData(Data 为空),
                    // 先补加载,否则 SaveWeaponData 无法持久化,之后切枪时补弹结果也会被旧数据覆盖
                    if (weapon.Data == null)
                    {
                        weapon.LoadWeaponData(weaponData);
                    }

                    weapon.FillClipDirectly(canEnhance);
                    weapon.SaveWeaponData();
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
            // 仅当自己仍是当前 player1 时才清空，避免场景切换时旧 Player 的 OnDestroy
            // 在新 Player 的 Awake 之后执行，把新 Player 已赋值的 player1 误清空。
            // 一旦 player1 被误清空，WeaponBase.Attack 中 fireFlash.Flash 等访问
            // Player.player1 的代码会抛 NullReferenceException，导致 KeepAttacking
            // 中的 RecordAttackTime / gunClip.Shoot 被跳过 → 无冷却且不扣弹的每帧连发。
            if (player1 == this)
            {
                player1 = null;
            }
            if (specialReloadCoroutine != null)
            {
                StopCoroutine(specialReloadCoroutine);
            }
        }
    }
}