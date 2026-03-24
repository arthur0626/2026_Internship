using UnityEngine;

/// <summary>
/// Machine/Plate 오브젝트에 부착하는 수갑 픽업 스크립트.
/// 플레이어가 Plate에 접근하면 쌓인 수갑 전량을 즉시 획득.
/// </summary>
public class MachinePlate : MonoBehaviour
{
    [Tooltip("연결할 Machine 컴포넌트")]
    [SerializeField] private Machine machine;

    [Tooltip("픽업 감지 반경")]
    [SerializeField] private float pickupRadius = 2f;

    private PlayerCarrier _carrier;

    private void Start()
    {
        var player = GameObject.Find("Player");
        if (player == null)
        {
            Debug.LogError("[MachinePlate] 'Player' 오브젝트를 찾지 못했습니다.");
            return;
        }

        _carrier = player.GetComponent<PlayerCarrier>();
        if (_carrier == null)
            Debug.LogError("[MachinePlate] Player에 PlayerCarrier 컴포넌트가 없습니다!");

        // Inspector 연결이 없으면 부모 계층에서 자동 탐색
        if (machine == null && transform.parent != null)
            machine = transform.parent.GetComponentInChildren<Machine>();

        if (machine == null)
            Debug.LogError("[MachinePlate] Machine을 찾지 못했습니다. Inspector에서 직접 연결하세요.");
    }

    private void Update()
    {
        if (machine == null || _carrier == null)
            return;

        // XZ 평면 거리 (Y 무시)
        float dx = transform.position.x - _carrier.transform.position.x;
        float dz = transform.position.z - _carrier.transform.position.z;
        float sqrDist = dx * dx + dz * dz;

        if (sqrDist <= pickupRadius * pickupRadius && machine.HasHandcuffs)
            machine.PickupHandcuffs(_carrier);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.2f);
        Gizmos.DrawSphere(transform.position, pickupRadius);
        Gizmos.color = new Color(1f, 0.5f, 0f, 1f);
        Gizmos.DrawWireSphere(transform.position, pickupRadius);
    }
#endif
}
