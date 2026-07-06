namespace ProjectBlood
{
    public class LifestealFeature
    {
        public int Level { get; private set; } = 1;
        public float BaseLifestealPercent => 1f; // 基础吸血比例1%
        public float LifestealPercent => BaseLifestealPercent + (Level - 1); // 每级+1%

        public void LevelUp()
        {
            Level++;
        }

        public float GetLifestealAmount(float damage)
        {
            return damage * (LifestealPercent / 100f);
        }
    }
}