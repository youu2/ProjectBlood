using QFramework;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectBlood
{
    /// <summary>
    /// 玩家鼠标指针管理器：
    ///   - InGame 游玩时（无论是否战斗中）显示瞄准准星
    ///   - 主菜单 / 暂停界面（含音量设置等面板）切换回指针光标
    /// 纯事件驱动（场景加载 + IsGamePaused 变更事件），无轮询、无每帧开销。
    /// </summary>
    public class CursorManager : ViewController
    {
        public static CursorManager Instance;

        [Header("=== 指针贴图（Inspector 挂载） ===")]
        [Tooltip("指针光标：主菜单 / 暂停等 UI 交互场景使用。未挂载时回退系统默认光标")]
        public Texture2D uiCursor;

        [Tooltip("瞄准光标：InGame 游玩时使用。未挂载时回退系统默认光标")]
        public Texture2D combatCursor;

        [Header("=== 热点（相对贴图左上角的像素偏移） ===")]
        [Tooltip("指针光标热点，箭头类贴图通常填 (0, 0)")]
        public Vector2 uiHotspot = Vector2.zero;

        [Tooltip("瞄准光标热点，十字准星类贴图通常填 (宽/2, 高/2)")]
        public Vector2 combatHotspot = Vector2.zero;

        // 主菜单场景判定（与 Player.IsMainMenuScene 同一标准）
        private static bool IsMainMenuScene => SceneManager.GetActiveScene().name == "GameStart";

        void Awake()
        {
            // 跨场景单例：挂在 GameStart 场景，DontDestroyOnLoad 存活到 InGame
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void OnEnable()
        {
            Global.OnGamePausedChanged += OnGamePausedChanged;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnDisable()
        {
            Global.OnGamePausedChanged -= OnGamePausedChanged;
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        void Start()
        {
            RefreshCursor();
        }

        void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
            // 恢复系统默认光标，避免退出后残留自定义贴图
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }

        private void OnGamePausedChanged(bool paused)
        {
            RefreshCursor();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            RefreshCursor();
        }

        /// <summary>
        /// 按当前状态应用指针：主菜单或暂停中 → 指针光标；其余（InGame 游玩）→ 瞄准光标。
        /// </summary>
        private void RefreshCursor()
        {
            bool useUiCursor = Global.IsGamePaused || IsMainMenuScene;
            var tex = useUiCursor ? uiCursor : combatCursor;

            if (tex != null)
            {
                Cursor.SetCursor(tex, useUiCursor ? uiHotspot : combatHotspot, CursorMode.Auto);
            }
            else
            {
                // 贴图未挂载时回退系统默认光标
                Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            }
        }
    }
}
