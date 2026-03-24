using System.Collections;
using UnityEngine;

/// <summary>
/// GuardAI · MinerGuardAI 공통 베이스 클래스.
///
/// 공통 제공 기능
/// - 비활성 시작 / Activate() 로 표시 전환
/// - SetVisible(bool) — 하위 Renderer 일괄 제어
/// - MoveTo(Vector3) 코루틴 — XZ 평면 이동 + 회전
/// </summary>
public abstract class NPCBase : MonoBehaviour
{
    [Header("이동 설정")]
    [Tooltip("이동 속도 (유닛/초)")]
    [SerializeField] protected float moveSpeed = 4f;

    [Tooltip("목적지 도착 판정 거리 (XZ 기준)")]
    [SerializeField] protected float arriveDistance = 0.4f;

    [Header("비주얼")]
    [Tooltip("표시/숨김을 제어할 루트 Transform (null 이면 자신 포함 하위 전체)")]
    [SerializeField] protected Transform visualRoot;

    protected Renderer[] _renderers;

    protected virtual void Awake()
    {
        Transform root = visualRoot != null ? visualRoot : transform;
        _renderers = root.GetComponentsInChildren<Renderer>(true);
        SetVisible(false);
    }

    /// <summary>업그레이드 구매 후 호출. 렌더러를 표시하고 AI 루틴을 시작한다.</summary>
    public virtual void Activate()
    {
        SetVisible(true);
    }

    protected void SetVisible(bool visible)
    {
        if (_renderers == null) return;
        foreach (var r in _renderers)
            r.enabled = visible;
    }

    /// <summary>
    /// XZ 평면에서 destination 으로 이동하는 코루틴.
    /// 도착(arriveDistance 이내) 시 XZ 위치를 스냅하고 종료.
    /// </summary>
    protected IEnumerator MoveTo(Vector3 destination)
    {
        while (true)
        {
            Vector3 delta = destination - transform.position;
            delta.y = 0f;
            if (delta.magnitude <= arriveDistance) break;

            transform.position = Vector3.MoveTowards(
                transform.position,
                new Vector3(destination.x, transform.position.y, destination.z),
                moveSpeed * Time.deltaTime);

            if (delta.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    Quaternion.LookRotation(delta.normalized),
                    Time.deltaTime * 10f);

            yield return null;
        }

        transform.position = new Vector3(destination.x, transform.position.y, destination.z);
    }

#if UNITY_EDITOR
    protected virtual void OnDrawGizmosSelected() { }
#endif
}
