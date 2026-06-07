using QFramework;
using UnityEngine;

namespace ProjectBlood
{
    public partial class GameUI : ViewController
    {
        public static GameUI GUIInstance;
        private void Awake()
        {
            GUIInstance = this;
        }
        public static void UpdateClipText(GunClip gunClip)
        {
            if (GUIInstance != null && GUIInstance.ClipText != null)
            {
                GUIInstance.ClipText.text = $"Ammo: {gunClip.currentAmmo} / {gunClip.maxAmmo}\n([R] to reload)";
            }
        }
        public static void UpdateBloodText(BloodBank bloodBank)
        {
            if (GUIInstance != null && GUIInstance.BloodText != null)
            {
                GUIInstance.BloodText.text = $"Blood: {bloodBank.CurrentBloodAmount} / {bloodBank.MaxBloodAmount}";
            }
        }

        
    }
}