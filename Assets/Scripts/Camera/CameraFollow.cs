using UnityEngine;

/// <summary>
/// 플레이어를 쿼터뷰로 따라가는 기본 카메라 스크립트.
/// - target을 기준으로 positionOffset 만큼 떨어진 위치에서 바라봄
/// - 고정된 회전(쿼터뷰 각도) 유지
/// </summary>
[RequireComponent(typeof(Camera))]
public class CameraFollow : MonoBehaviour
{
    [Header("타겟")]
    [Tooltip("따라갈 플레이어 Transform")]
    public Transform target;

    [Header("위치 오프셋")]
    [Tooltip("타겟 기준 카메라 위치 오프셋 (월드 기준)")]
    public Vector3 positionOffset = new Vector3(-3f, 8f, -5f);

    [Header("회전 설정")]
    [Tooltip("쿼터뷰 각도 (Pitch, Yaw, Roll 순)")]
    public Vector3 eulerAngles = new Vector3(52f, 45f, 0f);

    [Header("부드러운 따라가기")]
    [Range(0f, 20f)]
    public float followSpeed = 8f;

    private void LateUpdate()
    {
        if (target == null)
            return;

        // 목표 위치와 회전 계산
        Vector3 desiredPosition = target.position + positionOffset;
        Quaternion desiredRotation = Quaternion.Euler(eulerAngles);

        // 부드럽게 위치/회전 보간
        transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime * followSpeed);
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, Time.deltaTime * followSpeed);
    }
}

