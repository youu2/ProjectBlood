using QFramework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ProjectBlood
{
    /// <summary>
    /// 玩家鼠标指针管理器（手动挂载的 uGUI Image 方案）：
    ///   - InGame 游玩时（无论是否战斗中）显示瞄准准星 Image
    ///   - 主菜单 / 暂停界面（含音量设置等面板）显示指针 Image
    ///
    /// 状态切换纯事件驱动（场景加载 + IsGamePaused 变更事件）；
    /// Image 走相机渲染目标，可被 URP Bloom 等后处理泛光。
    /// </summary>
    public class CursorManager : ViewController
    {
        public static CursorManager Instance;

        [Header("=== 指针 Image（手动挂载） ===")]
        [Tooltip("指针 Image：主菜单 / 暂停等 UI 交互场景显示。RectTransform pivot 设 (0,1) 作为热点")]
        public Image uiCursor;

        [Tooltip("瞄准 Image：InGame 游玩时显示。RectTransform pivot 设 (0.5,0.5) 作为热点")]
        public Image combatCursor;

        [SerializeField] private Canvas _canvas;
        [SerializeField] private RectTransform _canvasRect;
        [SerializeField] private RectTransform _activeRect;
        // 加载页显示期间隐藏指针
        private bool _loadingPageActive;

        // 主菜单场景判定（与 Player.IsMainMenuScene 同一标准）
        private static bool IsMainMenuScene => SceneManager.GetActiveScene().name == "GameStart";

        void Awake()
        {
            // 跨场景单例：整个根物体 DontDestroyOnLoad，Canvas 与 Image 引用随之跨场景存活
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // 指针 Image 绝不能拦截 UGUI 射线，否则全屏跟随时挡死所有按钮点击
            if (uiCursor != null) uiCursor.raycastTarget = false;
            if (combatCursor != null) combatCursor.raycastTarget = false;
        }

        void OnEnable()
        {
            Global.OnGamePausedChanged += OnGamePausedChanged;
            SceneManager.sceneLoaded += OnSceneLoaded;
            GameUI.OnLoadingPageStateChanged += OnLoadingPageStateChanged;
            Cursor.visible = false; // 由 Image 指针接管
        }

        void OnDisable()
        {
            Global.OnGamePausedChanged -= OnGamePausedChanged;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            GameUI.OnLoadingPageStateChanged -= OnLoadingPageStateChanged;
            Cursor.visible = true;
        }

        void Start()
        {
            BindCanvasCamera();
            RefreshCursor();
        }

        void Update()
        {
            // 暂停时（timeScale=0）Update 仍执行，指针在暂停界面照常跟随
            if (_canvasRect == null || _activeRect == null) return;

            // 统一换算：自动处理 CanvasScaler 缩放与 ScreenSpaceCamera 投影，
            // 与 Image 嵌套层级无关，窗口化/全屏/任意分辨率均精准对应鼠标位置
            Vector3 worldPoint;
            RectTransformUtility.ScreenPointToWorldPointInRectangle(
                _canvasRect, Input.mousePosition, _canvas.worldCamera, out worldPoint);
            _activeRect.position = worldPoint;
        }

        void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
            Cursor.visible = true; // 恢复系统默认光标
        }

        private void OnGamePausedChanged(bool paused)
        {
            RefreshCursor();
        }

        private void OnLoadingPageStateChanged(bool active)
        {
            _loadingPageActive = active;
            RefreshCursor();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // 相机是场景内对象且不 DDOL，场景切换后 ScreenSpaceCamera Canvas 的引用失效，必须重绑
            BindCanvasCamera();
            RefreshCursor();
        }

        /// <summary>
        /// 重新查找 Canvas 引用并绑定当前场景主相机。
        /// 每次 sceneLoaded 都重新查找：旧场景相机销毁后残留引用仍 != null，
        /// 不重新查找会导致误判为新相机已绑定而跳过。
        /// </summary>
        private void BindCanvasCamera()
        {
            if (_canvas == null) return;
            _canvas.worldCamera = Camera.main;
        }

        /// <summary>
        /// 按当前状态显示对应 Image（互斥）：加载页显示中 → 隐藏；主菜单或暂停中 → 指针；其余 → 准星。
        /// 当前态对应 Image 未挂载时回退显示系统默认光标。
        /// </summary>
        private void RefreshCursor()
        {
            // 加载页期间隐藏指针（同时隐藏系统光标，避免双光标）
            if (_loadingPageActive)
            {
                if (uiCursor != null) uiCursor.gameObject.SetActive(false);
                if (combatCursor != null) combatCursor.gameObject.SetActive(false);
                _activeRect = null;
                Cursor.visible = false;
                return;
            }

            bool useUiCursor = Global.IsGamePaused || IsMainMenuScene;
            Image active = useUiCursor ? uiCursor : combatCursor;

            if (uiCursor != null) uiCursor.gameObject.SetActive(active == uiCursor);
            if (combatCursor != null) combatCursor.gameObject.SetActive(active == combatCursor);

            if (active != null)
            {
                _activeRect = active.rectTransform;
                Cursor.visible = false;
            }
            else
            {
                // 引用未拖：隐藏 Image 指针，回退系统默认光标
                _activeRect = null;
                Cursor.visible = true;
            }
        }
    }
}
