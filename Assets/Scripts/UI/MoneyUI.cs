using TMPro;
using UnityEngine;

/// <summary>
/// 우측 상단에 플레이어 보유 돈을 실시간으로 표시하는 UI.
/// - 돈을 얻기 전에도 0으로 표시됩니다.
/// </summary>
public class MoneyUI : MonoBehaviour
{
    [Header("참조")]
    [Tooltip("돈 보유량을 읽어올 PlayerCarrier")]
    [SerializeField] private PlayerCarrier playerCarrier;

    [Tooltip("돈을 표시할 TMP_Text 컴포넌트")]
    [SerializeField] private TMP_Text moneyText;

    [Header("표시 형식")]
    [Tooltip("텍스트 앞에 붙일 접두사 (예: \"💰 \", \"돈: \")")]
    [SerializeField] private string prefix = "💰 ";

    // 이전 값 캐시 — 매 프레임 setText 를 막아 GC 절감
    private int _lastMoney = -1;

    private void Update()
    {
        if (playerCarrier == null || moneyText == null) return;

        int current = playerCarrier.currentMoney;
        if (current == _lastMoney) return;

        _lastMoney = current;
        moneyText.text = prefix + current.ToString();
    }
}
