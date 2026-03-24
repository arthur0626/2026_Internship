using UnityEngine;

/// <summary>
/// 게임 시작 시 지정한 구역 안에 Mineral 프리팹을 자동으로 배치하는 스포너.
/// 씬에 Mineral 오브젝트를 수동으로 배치하는 대신 이 스크립트 하나로 관리.
/// </summary>
public class MineralSpawner : MonoBehaviour
{
    [Header("프리팹")]
    [Tooltip("스폰할 Mineral 프리팹")]
    [SerializeField] private GameObject mineralPrefab;

    [Header("스폰 설정")]
    [Tooltip("총 스폰 개수")]
    [SerializeField] private int count = 30;

    [Tooltip("스폰 구역 너비/깊이 (중심 기준 ±절반)")]
    [SerializeField] private Vector2 areaSize = new Vector2(10f, 20f);

    [Tooltip("스폰될 Y 위치")]
    [SerializeField] private float spawnY = 0f;

    [Tooltip("미네랄 간 최소 간격 (0이면 겹침 방지 비활성화)")]
    [SerializeField] private float minSpacing = 0f;

    [Tooltip("간격 조건이 맞지 않을 때 재시도 횟수 (minSpacing > 0 일 때만 사용)")]
    [SerializeField] private int maxRetries = 50;

    private void Start()
    {
        SpawnMinerals();
    }

    private void SpawnMinerals()
    {
        if (mineralPrefab == null) return;

        bool useSpacing = minSpacing > 0f;

        for (int i = 0; i < count; i++)
        {
            Vector3 pos;

            if (useSpacing)
            {
                pos = GetRandomPos();
                for (int retry = 0; retry < maxRetries; retry++)
                {
                    var candidate = GetRandomPos();
                    if (IsPositionValid(candidate)) { pos = candidate; break; }
                }
            }
            else
            {
                pos = GetRandomPos();
            }

            Instantiate(mineralPrefab, pos, Quaternion.identity, transform);
        }
    }

    private Vector3 GetRandomPos()
    {
        return new Vector3(
            transform.position.x + Random.Range(-areaSize.x * 0.5f, areaSize.x * 0.5f),
            spawnY,
            transform.position.z + Random.Range(-areaSize.y * 0.5f, areaSize.y * 0.5f)
        );
    }

    /// <summary>
    /// 이미 배치된 자식 미네랄들과 minSpacing 이상 떨어져 있는지 검사.
    /// </summary>
    private bool IsPositionValid(Vector3 pos)
    {
        float minSqr = minSpacing * minSpacing;

        foreach (Transform child in transform)
        {
            if ((child.position - pos).sqrMagnitude < minSqr)
                return false;
        }

        return true;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.25f);
        Gizmos.DrawCube(
            new Vector3(transform.position.x, spawnY, transform.position.z),
            new Vector3(areaSize.x, 0.1f, areaSize.y)
        );

        Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.8f);
        Gizmos.DrawWireCube(
            new Vector3(transform.position.x, spawnY, transform.position.z),
            new Vector3(areaSize.x, 0.1f, areaSize.y)
        );
    }
#endif
}
