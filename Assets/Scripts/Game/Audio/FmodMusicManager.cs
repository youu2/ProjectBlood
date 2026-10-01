using FMOD.Studio;
using FMODUnity;
using QFramework;
using UnityEngine;

namespace ProjectBlood
{
    // BGM 播放状态，与 FMOD 全局参数 GameStages 的标签一一对应
    public enum GameStage
    {
        MainMenu = 0,
        Normal = 1,
        BossPhase1 = 2,
        BossPhase2 = 3,
    }

    // FMOD 侧 BGM 管理器：与 AudioKitManager 共存，只负责音乐
    // 整个 BGM 由 GameMusics 单事件承载，切换通过全局参数 GameStages 完成
    public class FmodMusicManager
    {
        private static readonly FmodMusicManager instance = new FmodMusicManager();
        public static FmodMusicManager Instance => instance;

        private const string GameMusicEventPath = "event:/Music/GameMusics";
        private const string GameStagesParam = "GameStages";

        private EventInstance _musicInstance;
        private bool _hasInstance;
        private bool _initialized;
        private GameStage _currentStage = GameStage.MainMenu;

        private FmodMusicManager() { }

        // 创建并启动 GameMusics 实例，进入主菜单阶段；幂等注册，每次调用都淡出旧音乐后重新播放
        public void Init()
        {
            if (!_initialized)
            {
                _initialized = true;
                // 音量沿用现有设置：音乐比例 × 全局比例，并跟随变化
                AudioKitManager.MusicVolumeRatio.Register(_ => ApplyVolume());
                AudioKitManager.GlobalVolumeRatio.Register(_ => ApplyVolume());
            }

            // 加载结束统一行为：先淡出停止当前音乐，再开始播放目标音乐
            StartMusic(GameStage.MainMenu);
        }

        // 切换游戏阶段（FMOD 内部用 AHDSR 做淡入淡出）
        public void SetStage(GameStage stage)
        {
            _currentStage = stage;
            if (!_hasInstance) return;
            _musicInstance.setParameterByName(GameStagesParam, (float)stage);
        }

        // 触碰传送门时调用：带淡出的停止并释放实例
        public void StopMusic()
        {
            if (!_hasInstance) return;
            _musicInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            _musicInstance.release();
            _hasInstance = false;
        }

        // 下一关加载完成后调用：重新以指定阶段开始播放
        public void StartMusic(GameStage stage)
        {
            if (_hasInstance)
            {
                StopMusic();
            }
            _musicInstance = RuntimeManager.CreateInstance(GameMusicEventPath);
            _musicInstance.setParameterByName(GameStagesParam, (float)stage);
            _currentStage = stage;
            _musicInstance.start();
            _hasInstance = true;
            ApplyVolume();
        }

        public GameStage CurrentStage => _currentStage;

        private void ApplyVolume()
        {
            if (!_hasInstance) return;
            float volume = AudioKitManager.MusicVolumeRatio.Value * AudioKitManager.GlobalVolumeRatio.Value;
            _musicInstance.setVolume(volume);
        }
    }
}
