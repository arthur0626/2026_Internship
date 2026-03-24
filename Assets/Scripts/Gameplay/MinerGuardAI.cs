using System.Collections;
using UnityEngine;

/// <summary>
/// 채굴 가드 AI.
/// 두 순찰 지점을 왕복하며 미네랄을 채굴하고,
/// 최대 보유량 도달 시 Machine 에 자동 납입한다.
///
/// 동작 흐름
/// 1. 비활성 상태로 시작 (NPCBase.Awake 에서 렌더러 숨김).
/// 2. MinerGuardUpgrade 에서 Activate() 호출 → 렌더러 표시 + 루틴 시작.
/// 3. PATROL : 두 지점(A ↔ B) 왕복, 이동 중 채굴 시도.
/// 4. 보유량 = maxOre → DELIVER : Machine Area 로 이동 후 납입 → PATROL 복귀.
/// </summary>
public class MinerGuardAI : NPCBase
{
    [Header("참조")]
    [Tooltip("광물을 납입할 Machine 컴포넌트")]
    [SerializeField] private Machine machine;

    [Tooltip("납입 시 이동할 Machine Area Transform")]
    [SerializeField] private Transform machineAreaTarget;

    [Header("순찰 경로")]
    [Tooltip("순찰 왕복 지점 A")]
    [SerializeField] private Transform patrolPointA;

    [Tooltip("순찰 왕복 지점 B")]
    [SerializeField] private Transform patrolPointB;

    [Header("채굴 설정")]
    [Tooltip("채굴 시도 간격 (초)")]
    [SerializeField] private float mineInterval = 1f;

    [Tooltip("채굴 가능 반경")]
    [SerializeField] private float mineRange = 3f;

    [Tooltip("최대 보유 광물 수 — 도달 시 즉시 납입 이동")]
    [SerializeField] private int maxOre = 5;

    [Tooltip("미네랄 레이어 마스크 (0 이면 전체 레이어 검색)")]
    [SerializeField] private LayerMask mineralLayer;

    [Header("타이밍")]
    [Tooltip("납입 완료 후 잠시 대기 시간 (초)")]
    [SerializeField] private float deliverPause = 0.4f;

    private int   _currentOre;
    private float _mineTimer;

    // ─────────────────────────────────────────────────────────────────────
    public override void Activate()
    {
        base.Activate();
        StartCoroutine(MinerRoutine());
    }

    private IEnumerator MinerRoutine()
    {
        Transform[] waypoints   = { patrolPointA, patrolPointB };
        int         waypointIdx = 0;

        while (true)
        {
            bool delivering = _currentOre >= maxOre;

            if (delivering)
            {
                // 납입: NPCBase.MoveTo 활용
                Vector3 dest = machineAreaTarget != null
                    ? machineAreaTarget.position
                    : machine.transform.position;
                yield return StartCoroutine(MoveTo(dest));

                if (machine != null && _currentOre > 0)
                {
                    machine.DeliverOre(_currentOre);
                    _currentOre = 0;
                }
                yield return new WaitForSeconds(deliverPause);
            }
            else
            {
                // 순찰: 이동 중 채굴 시도 (도중에 만광 시 루프 탈출 → 다음 루프에서 납입)
                Vector3 dest = waypoints[waypointIdx] != null
                    ? waypoints[waypointIdx].position
                    : transform.position;

                while (true)
                {
                    // 만광이 되면 순찰 중단
                    if (_currentOre >= maxOre) break;

                    Vector3 delta = dest - transform.position;
                    delta.y = 0f;
                    if (delta.magnitude <= arriveDistance) break;

                    transform.position = Vector3.MoveTowards(
                        transform.position,
                        new Vector3(dest.x, transform.position.y, dest.z),
                        moveSpeed * Time.deltaTime);

                    if (delta.sqrMagnitude > 0.001f)
                        transform.rotation = Quaternion.Slerp(
                            transform.rotation,
                            Quaternion.LookRotation(delta.normalized),
                            Time.deltaTime * 10f);

                    _mineTimer += Time.deltaTime;
                    if (_mineTimer >= mineInterval)
                    {
                        _mineTimer -= mineInterval;
                        TryMine();
                    }

                    yield return null;
                }

                // 만광이 아닌 경우에만 다음 순찰 지점으로 전환
                if (_currentOre < maxOre)
                    waypointIdx = (waypointIdx + 1) % waypoints.Length;
            }
        }
    }

    private void TryMine()
    {
        Collider[] hits = mineralLayer.value != 0
            ? Physics.OverlapSphere(transform.position, mineRange, mineralLayer, QueryTriggerInteraction.Collide)
            : Physics.OverlapSphere(transform.position, mineRange, ~0,           QueryTriggerInteraction.Collide);

        if (hits == null || hits.Length == 0) return;

        Mineral nearest    = null;
        float   minSqrDist = float.MaxValue;

        foreach (var hit in hits)
        {
            if (hit == null || !hit.gameObject.activeInHierarchy) continue;
            var node = hit.GetComponent<Mineral>();
            if (node == null) continue;
            float d = (hit.transform.position - transform.position).sqrMagnitude;
            if (d < minSqrDist) { minSqrDist = d; nearest = node; }
        }

        if (nearest != null && nearest.Mine())
            _currentOre = Mathf.Min(_currentOre + 1, maxOre);
    }

#if UNITY_EDITOR
    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();

        Gizmos.color = new Color(0f, 0.8f, 0.2f, 0.6f);
        if (patrolPointA != null) Gizmos.DrawWireSphere(patrolPointA.position, 0.3f);
        if (patrolPointB != null) Gizmos.DrawWireSphere(patrolPointB.position, 0.3f);
        if (patrolPointA != null && patrolPointB != null)
            Gizmos.DrawLine(patrolPointA.position, patrolPointB.position);

        Gizmos.color = new Color(1f, 0.5f, 0f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, mineRange);

        if (machineAreaTarget != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(machineAreaTarget.position, arriveDistance);
        }
    }
#endif
}
