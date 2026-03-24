using System.Collections;
using UnityEngine;

/// <summary>
/// 수갑 운반 가드 AI.
/// Machine/Plate ↔ Queue/Area 를 반복 이동하며 완성된 수갑을 자동 운반한다.
///
/// 동작 흐름
/// 1. 비활성 상태로 시작 (NPCBase.Awake 에서 렌더러 숨김).
/// 2. GuardUpgrade 에서 Activate() 호출 → 렌더러 표시 + 순찰 시작.
/// 3. Plate 도착 → 수갑 생성 대기 → 전량 수령
/// 4. Area 도착 → 전량 납입
/// 5. 무한 반복
/// </summary>
public class GuardAI : NPCBase
{
    [Header("참조")]
    [Tooltip("수갑을 픽업할 Machine 컴포넌트 (Env/Machine)")]
    [SerializeField] private Machine machine;

    [Tooltip("수갑을 납입할 PrisonerQueue 컴포넌트 (Env/Queue)")]
    [SerializeField] private PrisonerQueue prisonerQueue;

    [Tooltip("이동 목적지 — Plate (Env/Machine/Plate)")]
    [SerializeField] private Transform plateTarget;

    [Tooltip("이동 목적지 — Area (Env/Queue/Area)")]
    [SerializeField] private Transform areaTarget;

    [Header("타이밍")]
    [Tooltip("수갑 픽업/납입 후 잠시 대기 시간 (초)")]
    [SerializeField] private float actionPause = 0.3f;

    [Tooltip("Plate에 수갑이 없을 때 재확인 간격 (초)")]
    [SerializeField] private float waitCheckInterval = 0.5f;

    private int _carried;

    // ─────────────────────────────────────────────────────────────────────
    public override void Activate()
    {
        base.Activate();
        StartCoroutine(PatrolRoutine());
    }

    private IEnumerator PatrolRoutine()
    {
        while (true)
        {
            // 1. Plate 로 이동
            if (plateTarget != null)
                yield return StartCoroutine(MoveTo(plateTarget.position));

            // 2. 수갑이 생길 때까지 대기 후 전량 수령
            if (machine != null)
            {
                while (!machine.HasHandcuffs)
                    yield return new WaitForSeconds(waitCheckInterval);

                _carried += machine.TakeAllHandcuffs();
                yield return new WaitForSeconds(actionPause);
            }

            // 3. Area 로 이동
            if (areaTarget != null)
                yield return StartCoroutine(MoveTo(areaTarget.position));

            // 4. 수갑 전량 납입
            if (prisonerQueue != null && _carried > 0)
            {
                prisonerQueue.DepositHandcuffs(_carried);
                _carried = 0;
                yield return new WaitForSeconds(actionPause);
            }
        }
    }

#if UNITY_EDITOR
    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
        if (plateTarget != null)
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.8f);
            Gizmos.DrawWireSphere(plateTarget.position, arriveDistance);
            Gizmos.DrawLine(transform.position, plateTarget.position);
        }
        if (areaTarget != null)
        {
            Gizmos.color = new Color(0.5f, 0f, 1f, 0.8f);
            Gizmos.DrawWireSphere(areaTarget.position, arriveDistance);
            Gizmos.DrawLine(transform.position, areaTarget.position);
        }
    }
#endif
}
