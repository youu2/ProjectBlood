using System.Collections;
using System.Collections.Generic;
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
    private void Awake()
    {
        mCamera = GetComponent<Camera>();
        currentBgColor = mCamera.backgroundColor;
    }
    public void OnEnable()
    {
        // 订阅玩家进入房间事件
        Room.OnPlayerEnteredRoom += OnPlayerEnteredRoom;
    }
    void LateUpdate()
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

        if (Player.player1 == null)
        {
            return;
        }
        // 当前玩家移动方向
        Vector2 moveDirection = new Vector2(Player.player1.transform.position.x, Player.player1.transform.position.y);
        // 获取当前摄像机位置
        Vector3 currentCameraPosition = transform.position;
        Vector3 targetPosition;
        // 摄像机缓动目标位置(调整e的系数越大越慢跟随)
        targetPosition = Vector3.Lerp(currentCameraPosition,
        new Vector3(moveDirection.x, moveDirection.y, -10),
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
