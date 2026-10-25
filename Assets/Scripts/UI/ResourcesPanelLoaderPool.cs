using System;
using QFramework;
using UnityEngine;

namespace ProjectBlood
{
    /// <summary>
    /// 打包后 ResKit 需要预先构建的 AssetBundle；UI 面板改为从 Resources 加载，
    /// 保证编辑器与播放器行为一致（面板 prefab 位于 Assets/Resources 下）。
    /// </summary>
    public class ResourcesPanelLoaderPool : AbstractPanelLoaderPool
    {
        private class ResourcesPanelLoader : IPanelLoader
        {
            private static string GetPanelName(PanelSearchKeys keys)
            {
                return string.IsNullOrEmpty(keys.GameObjName) ? keys.PanelType.Name : keys.GameObjName;
            }

            public GameObject LoadPanelPrefab(PanelSearchKeys panelSearchKeys)
            {
                return Resources.Load<GameObject>(GetPanelName(panelSearchKeys));
            }

            public void LoadPanelPrefabAsync(PanelSearchKeys panelSearchKeys, Action<GameObject> onPanelPrefabLoad)
            {
                var request = Resources.LoadAsync<GameObject>(GetPanelName(panelSearchKeys));
                request.completed += _ => onPanelPrefabLoad(request.asset as GameObject);
            }

            public void Unload()
            {
            }
        }

        protected override IPanelLoader CreatePanelLoader()
        {
            return new ResourcesPanelLoader();
        }
    }
}
