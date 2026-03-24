using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("이동 설정")]
    [SerializeField] private float moveSpeed     = 7f;
    [SerializeField] private float rotationSpeed = 15f;
    [SerializeField] private float gravity       = 20f;

    [Header("조이스틱")]
    [Tooltip("Joystick 컴포넌트 연결 (없으면 키보드만 사용)")]
    [SerializeField] private Joystick joystick;

    private CharacterController _controller;
    private Vector3 _velocity;

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();
    }

    private void Update()
    {
        // ── 입력 취합: 조이스틱 우선, 없으면 키보드 ─────────────────────────
        Vector2 input = Vector2.zero;

        if (joystick != null && joystick.InputDirection.magnitude > 0.01f)
        {
            input = joystick.InputDirection;
        }
        else
        {
            input.x = Input.GetAxisRaw("Horizontal");
            input.y = Input.GetAxisRaw("Vertical");
        }

        Vector3 moveDir = new Vector3(input.x, 0f, input.y).normalized;

        // ── 회전 ─────────────────────────────────────────────────────────────
        if (moveDir.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.Slerp(
                transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        // ── 중력 + 이동 ───────────────────────────────────────────────────────
        if (_controller.isGrounded)
            _velocity.y = -0.5f;
        else
            _velocity.y -= gravity * Time.deltaTime;

        _controller.Move((moveDir * moveSpeed + _velocity) * Time.deltaTime);
    }
}