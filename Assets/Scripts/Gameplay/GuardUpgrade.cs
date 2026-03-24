using UnityEngine;

/// <summary>
/// 수갑 운반 가드 고용 업그레이드 발판 (Env/Queue/Upgrade (2)) — 1회성.
/// </summary>
public class GuardUpgrade : UpgradeBase
{
    [Header("가드 업그레이드 설정")]
    [Tooltip("활성화할 GuardAI 컴포넌트 (Entity/Guard)")]
    [SerializeField] private GuardAI guardAI;

    protected override void OnUpgradeApplied()
    {
        if (guardAI != null)
            guardAI.Activate();
    }
}
