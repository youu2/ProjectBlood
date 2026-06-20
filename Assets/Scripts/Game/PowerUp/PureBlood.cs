using UnityEngine;
using QFramework;

namespace ProjectBlood
{
    // 纯血道具（Pure Blood，简称 PB）
    // 视觉表现：吸血触发时从敌人死亡位置向四周飞射，
    // 经历短暂的惯性飞射阶段后平滑转弯追踪玩家，被玩家拾取后恢复固定治疗量。
    public partial class PureBlood : DropItem
    {
        [Header("飞射阶段")]

        [Tooltip("初始飞射速度")]
        public float ejectSpeed = 7.5f;
        [Tooltip("飞射阶段阻尼系数（越大减速越快）")]
        public float ejectDrag = 5f;
        [Tooltip("飞射阶段持续时间（秒）")]
        public float flyDuration = 0.15f;

        [Header("追踪阶段")]
        
        [Tooltip("追踪阶段目标速度")]
        public float chaseSpeed = 16f;
        [Tooltip("方向平滑插值系数（越大转向越快）")]
        public float turnSmoothness = 7f;

        [System.NonSerialized]
        public float healAmount = 1f;

        private enum State
        {
            Flying,     // 飞射阶段：惯性运动 + 阻尼减速
            Chasing     // 追踪阶段：平滑转向 + 加速追踪
        }

        private State currentState;
        private Vector2 velocity;
        private Vector2 ejectDir;
        private float stateStartTime;
        private bool collected;

        void Awake()
        {
            autoCollectOnRoomFinish = false;
            price = 1;
        }

        void OnEnable()
        {
            collected = false;
            velocity = Vector2.zero;
            currentState = State.Flying;

            // 随机飞射方向
            float angle = Random.Range(0f, Mathf.PI * 2f);
            ejectDir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

            stateStartTime = Time.time;
            velocity = ejectDir * ejectSpeed;
        }

        void Update()
        {
            if (collected) return;
            if (Player.player1 == null) return;

            switch (currentState)
            {
                case State.Flying:
                    UpdateFlying();
                    break;
                case State.Chasing:
                    UpdateChasing();
                    break;
            }

            // 移动
            transform.Translate(velocity * Time.deltaTime, Space.World);
        }

        private void UpdateFlying()
        {
            // 阻尼减速
            float speed = velocity.magnitude;
            if (speed > 0.001f)
            {
                velocity -= velocity / speed * ejectDrag * Time.deltaTime;
            }

            // 飞射持续时间结束，进入追踪阶段
            if (Time.time >= stateStartTime + flyDuration)
            {
                TransitionTo(State.Chasing);
            }
        }

        private void UpdateChasing()
        {
            Vector2 toPlayer = (Vector2)Player.player1.transform.position - (Vector2)transform.position;
            float dist = toPlayer.magnitude;
            Vector2 dirToPlayer = dist > 0.001f ? toPlayer / dist : Vector2.zero;

            // 平滑转向
            Vector2 currentDir = velocity.sqrMagnitude > 0.001f ? velocity.normalized : ejectDir;
            Vector2 newDir = Vector2.Lerp(currentDir, dirToPlayer, Time.deltaTime * turnSmoothness);
            if (newDir.sqrMagnitude > 0.0001f) newDir.Normalize();

            // 加速追踪
            float speed = Mathf.Max(velocity.magnitude, chaseSpeed);
            velocity = newDir * speed;
        }

        private void TransitionTo(State newState)
        {
            currentState = newState;
            stateStartTime = Time.time;
        }

        public void Initialize(float heal)
        {
            healAmount = heal;
        }

        protected override void Collect()
        {
            if (collected) return;
            collected = true;

            if (Player.player1 != null)
            {
                Global.AddHP(healAmount);
                AudioKitManager.Instance.PlayOneShot("PureBloodPickUp");
            }

            this.DestroyGameObjGracefully();
        }
    }
}
