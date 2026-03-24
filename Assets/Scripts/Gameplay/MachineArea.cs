using UnityEngine;

/// <summary>
/// Machine/Area 오브젝트에 부착하는 감지 스크립트.
/// 플레이어가 범위 안에 있으면 매 프레임 납입을 시도한다.
/// (currentOre <= 0 이면 Machine.OnPlayerEnter 내부에서 자동으로 무시)
/// </summary>
public class MachineArea : MonoBehaviour
{
    [Tooltip("연결할 Machine 컴포넌트 (Inspector에서 직접 드래그)")]
    [SerializeField] private Machine machine;

    [Tooltip("플레이어 감지 반경")]
    [SerializeField] private float interactRadius = 3f;

    private PlayerMiner _miner;

    private void Start()
    {
        var player = GameObject.Find("Player");
        if (player != null)
            _miner = player.GetComponent<PlayerMiner>();
        else
            Debug.LogError("[MachineArea] 'Player' 오브젝트를 찾지 못했습니다.");

        // Inspector 연결이 없으면 부모 계층에서 자동 탐색
        if (machine == null && transform.parent != null)
            machine = transform.parent.GetComponentInChildren<Machine>();

        if (machine == null)
            Debug.LogError("[MachineArea] Machine을 찾지 못했습니다. Inspector에서 직접 연결하세요.");
    }

    private void Update()
    {
        if (machine == null || _miner == null)
            return;

        // XZ 평면 거리 (Y 무시)
        float dx = transform.position.x - _miner.transform.position.x;
        float dz = transform.position.z - _miner.transform.position.z;
        float sqrDist = dx * dx + dz * dz;

        if (sqrDist <= interactRadius * interactRadius)
            machine.OnPlayerEnter(_miner);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 1f, 0.2f);
        Gizmos.DrawSphere(transform.position, interactRadius);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactRadius);
    }
#endif
}
