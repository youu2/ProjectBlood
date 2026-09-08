using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ProjectBlood;
using QFramework;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    private bool isShaking = false;
    private float intensity = 0;
    private float duration = 0;
    private Camera mCamera = null;
    // 背景颜色渐变
    public List<Color> Colors = new();
    private Color currentBgColor;
    private Color targetBgColor;
    [SerializeField] private float colorLerpSpeed = 2.0f; // 过渡速度

    [Header("=== 鼠标动态偏移设置 ===")]
    [Tooltip("是否启用基于鼠标位置的摄像机动态偏移")]
    [SerializeField] private bool enableMouseOffset = true;
    [Tooltip("偏移强度：归一化鼠标距离(0~1)乘以此系数得到世界单位偏移量，值越大偏移越明显")]
    [SerializeField] private float mouseOffsetStrength = 2.0f;
    [Tooltip("最大偏移量(世界单位)，防止摄像机过度偏离玩家")]
    [SerializeField] private float maxMouseOffset = 2.5f;
    [Tooltip("偏移缓动速度，值越大鼠标偏移响应越快，越小越柔和")]
    [SerializeField] private float mouseOffsetLerpSpeed = 6.0f;
    // 当前实际生效的鼠标偏移（经缓动平滑后）
    private Vector3 currentMouseOffset = Vector3.zero;

    void Awake()
    {
        mCamera = GetComponent<Camera>();
        currentBgColor = mCamera.backgroundColor;
    }
    void OnEnable()
    {
        // 订阅玩家进入房间事件
        Room.OnPlayerEnteredRoom += OnPlayerEnteredRoom;
    }
    void Update()
    {
        // 更新相机大小
        UpdateCameraSize();
        // 更新背景颜色

        if (currentBgColor != targetBgColor)
        {
            float t = 1.0f - Mathf.Exp(-colorLerpSpeed * Time.deltaTime);
            currentBgColor = Color.Lerp(currentBgColor, targetBgColor, t);
            mCamera.backgroundColor = currentBgColor;
        }
    }
    void LateUpdate()
    {
        if (Player.player1 == null)
        {
            return;
        }
        // 先更新鼠标动态偏移（双摇杆射击：镜头向鼠标瞄准方向小幅偏移）
        UpdateMouseOffset();
        // 当前玩家移动方向
        Vector2 moveDirection = new Vector2(Player.player1.transform.position.x, Player.player1.transform.position.y);
        // 获取当前摄像机位置
        Vector3 currentCameraPosition = transform.position;
        Vector3 targetPosition;
        // 摄像机缓动目标位置：玩家位置 + 鼠标偏移 必须共同作为缓动目标。
        // 注意：偏移若在缓动结果之外每帧叠加，跟随缓动每帧只回拉 k 比例，
        // 稳态误差会被放大 1/k 倍（k≈0.05 时约20倍），表现为相机朝鼠标方向
        // 过冲、鼠标静止后仍持续爬行、反向甩鼠标时来回抖动。
        Vector3 followTarget = new Vector3(moveDirection.x, moveDirection.y, -10) + currentMouseOffset;
        // 摄像机缓动(调整e的系数越大越慢跟随)
        targetPosition = Vector3.Lerp(currentCameraPosition,
        followTarget,
        1.0f - Mathf.Exp(-3.0f * Time.deltaTime));
        if (isShaking)
        {
            var shakeIntensity = (duration / 60).Lerp(intensity, 0);
            targetPosition.x += Random.Range(-shakeIntensity, shakeIntensity);
            targetPosition.y += Random.Range(-shakeIntensity, shakeIntensity);
            duration--;
            if (duration <= 0) isShaking = false;
        }

        targetPosition.z = -10; // 保持摄像机在正确的深度位置
        // 摄像机跟随玩家移动
        transform.position = targetPosition;

        if (Global.currentRoom)
        {
            var direction = Player.player1.Direction2DFrom(Global.currentRoom);
            var width = (float)Global.currentRoom.roomConfig.Width;
            var height = (float)Global.currentRoom.roomConfig.Height;
            var originalAngleZ = transform.rotation.eulerAngles.z;
            float targetAngleZ = Mathf.Lerp(-2.0f, 2.0f, 0.5f + direction.x / (2 * width) + direction.y / (2 * height));
            if (originalAngleZ >= 2.6f) originalAngleZ -= 360;
            transform.rotation = Quaternion.Euler(0, 0, Mathf.Lerp(originalAngleZ, targetAngleZ, 1.0f - Mathf.Exp(-5.0f * Time.deltaTime)));
        }
    }

    // 根据鼠标相对屏幕中心的位置计算摄像机动态偏移
    private void UpdateMouseOffset()
    {
        Vector3 targetOffset = Vector3.zero;
        if (enableMouseOffset)
        {
            // 屏幕中心像素坐标
            Vector2 screenCenter = new Vector2(Screen.width, Screen.height) * 0.5f;
            // 鼠标指针相对屏幕中心的实时像素偏移（方向即鼠标偏移方向）
            Vector2 mouseScreenOffset = (Vector2)Input.mousePosition - screenCenter;
            // 按半屏宽高归一化到约[-1,1]，保证不同分辨率下手感一致；屏幕角落处限制模长不超过1
            Vector2 normalizedOffset = new Vector2(
                mouseScreenOffset.x / screenCenter.x,
                mouseScreenOffset.y / screenCenter.y);
            normalizedOffset = Vector2.ClampMagnitude(normalizedOffset, 1.0f);
            // 偏移量与鼠标到屏幕中心的距离成正比，并限制最大偏移阈值
            float offsetMagnitude = Mathf.Min(normalizedOffset.magnitude * mouseOffsetStrength, maxMouseOffset);
            // 屏幕空间方向转换为世界空间方向（随相机Z轴旋转保持一致）；
            // 先归一化方向再乘偏移量，避免模长(m)与偏移量(m*strength)相乘造成m²二次缩放
            Vector3 worldOffset = transform.TransformDirection(
                new Vector3(normalizedOffset.x, normalizedOffset.y, 0f));
            targetOffset = worldOffset.sqrMagnitude > 1e-8f
                ? worldOffset.normalized * offsetMagnitude
                : Vector3.zero;
        }
        // 指数缓动平滑偏移量（与跟随缓动同风格），暂停时deltaTime为0自动冻结
        currentMouseOffset = Vector3.Lerp(currentMouseOffset, targetOffset,
            1.0f - Mathf.Exp(-mouseOffsetLerpSpeed * Time.deltaTime));
    }

    public void ShakeCamera(float i, float d)
    {
        isShaking = true;
        intensity = i;
        duration = d;
    }

    public void UpdateCameraSize()
    {
        mCamera.orthographicSize =
        (1.0f - Mathf.Exp(-Time.deltaTime * 3.0f))
        .Lerp(mCamera.orthographicSize, Global.WeaponAdditionalCameraSize + 7);
    }

    public void OnPlayerEnteredRoom(Room room)
    {
        if (room.colorIndex == -1)
        {
            room.colorIndex = Random.Range(0, Colors.Count);
        }
        targetBgColor = Colors[room.colorIndex];
    }
    private void OnDisable()
    {
        Room.OnPlayerEnteredRoom -= OnPlayerEnteredRoom;
    }
}
