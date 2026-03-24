using TMPro;
using UnityEngine;

/// <summary>
/// Env/Prison/prisonwall 에 부착.
/// PrisonerQueue 의 수용 인원 수를 실시간으로 텍스트에 반영한다.
/// </summary>
public class PrisonCountDisplay : MonoBehaviour
{
    [Header("참조")]
    [Tooltip("수용 인원을 제공할 PrisonerQueue 컴포넌트")]
    [SerializeField] private PrisonerQueue prisonerQueue;

    [Tooltip("표시할 TMP_Text (prisonwall 의 자식 3D Text)")]
    [SerializeField] private TMP_Text countText;

    [Header("표시 형식")]
    [Tooltip("숫자 앞에 붙는 문자열 (비워두면 숫자만 표시)")]
    [SerializeField] private string prefix = "";

    [Tooltip("현재/최대 사이 구분자 (기본: \"/\")")]
    [SerializeField] private string separator = "/";

    private int _lastCount = -1;
    private int _lastMax   = -1;

    private void Update()
    {
        if (prisonerQueue == null || countText == null) return;

        int count = prisonerQueue.CurrentPrisonCount;
        int max   = prisonerQueue.MaxPrisonCapacity;

        // 둘 다 변경 없으면 갱신 생략
        if (count == _lastCount && max == _lastMax) return;

        _lastCount = count;
        _lastMax   = max;
        countText.text = prefix + count + separator + max;
    }
}
