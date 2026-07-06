namespace ProjectBlood
{
    public static class CameraUtils
    {
        // 缓存主相机控制器，避免重复GetComponent
        private static CameraController _mainCameraController;

        public static CameraController MainCameraController()
        {
            if (_mainCameraController == null)
            {
                _mainCameraController = UnityEngine.Camera.main.GetComponent<CameraController>();
            }
            return _mainCameraController;
        }

        public static void ShakeMainCamera(float intensity = 0.08f, float duration = 3)
        {
            MainCameraController()?.ShakeCamera(intensity, duration);
        }
    }
}