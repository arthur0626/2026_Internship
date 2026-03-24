using UnityEngine;

/// <summary>
/// 교도소 확장 업그레이드 발판 (Env/Prison/Upgrade) — 1회성.
/// 수용 인원 증가 + 뒷벽 이동 + 사이드 벽 Z 스케일 확장.
/// </summary>
public class PrisonUpgrade : UpgradeBase
{
    [Header("교도소 확장 설정")]
    [Tooltip("수용 인원을 늘릴 PrisonerQueue 컴포넌트")]
    [SerializeField] private PrisonerQueue prisonerQueue;

    [Tooltip("수용 인원 증가량")]
    [SerializeField] private int capacityIncrease = 20;

    [Header("벽 확장")]
    [Tooltip("뒤쪽 벽 (Wall) Transform")]
    [SerializeField] private Transform backWall;

    [Tooltip("사이드 벽 A (Wall (1)) Transform")]
    [SerializeField] private Transform sideWallA;

    [Tooltip("사이드 벽 B (Wall (2)) Transform")]
    [SerializeField] private Transform sideWallB;

    [Tooltip("뒷벽 Z 위치 증가량 (월드 단위)")]
    [SerializeField] private float backWallZOffset = 6f;

    [Tooltip("사이드 벽 LocalScale.z 증가량")]
    [SerializeField] private float sideWallZScaleAdd = 4f;

    [Tooltip("사이드 벽 피벗이 중앙일 때 늘어나는 방향 (+1 또는 -1)")]
    [SerializeField] private float sideWallGrowDirection = 1f;

    protected override void OnUpgradeApplied()
    {
        if (prisonerQueue != null)
            prisonerQueue.IncreasePrisonCapacity(capacityIncrease);

        if (backWall != null)
        {
            Vector3 pos = backWall.position;
            pos.z += backWallZOffset;
            backWall.position = pos;
        }

        ExpandSideWall(sideWallA);
        ExpandSideWall(sideWallB);
    }

    private void ExpandSideWall(Transform wall)
    {
        if (wall == null) return;

        float localToWorld = (wall.localScale.z > 0f)
            ? wall.lossyScale.z / wall.localScale.z
            : 1f;

        Vector3 scale = wall.localScale;
        scale.z += sideWallZScaleAdd;
        wall.localScale = scale;

        float worldHalfOffset = sideWallZScaleAdd * localToWorld * 0.5f;
        wall.position += wall.forward * (worldHalfOffset * sideWallGrowDirection);
    }
}
