using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class BattleUnit : MonoBehaviour
{
    [Header("ステータス")]
    public int maxHP = 50;
    private int currentHP;

    [Header("UI連携")]
    public Slider hpSlider;
    public TextMeshProUGUI hpText;

    [Header("アニメーター")]
    public Animator animator;

    [Header("行動ターン（敵専用）")]
    public int attackInterval = 1; // 基本の行動間隔（1なら毎ターン攻撃）
    [HideInInspector] public int currentCooldown; // 攻撃までの残りターン
    public TextMeshProUGUI turnText; // 「あと〇ターン」を表示するテキスト

    void Awake()
    {
        currentHP = maxHP;
        UpdateUI();
    }

    // ▼ 新規追加：戦闘開始時にターン数をセットする処理
    public void InitializeTurn()
    {
        currentCooldown = attackInterval;
        UpdateTurnUI();
    }

    // ▼ 新規追加：ターン表示の更新
    public void UpdateTurnUI()
    {
        if (turnText != null)
        {
            if (currentCooldown > 0)
            {
                turnText.text = $"あと {currentCooldown}";
            }
            else
            {
                turnText.text = "ATTACK!";
            }
        }
    }

    public void TakeDamage(int damage)
    {
        currentHP -= damage;
        if (currentHP < 0) currentHP = 0;

        if (animator != null)
        {
            if (currentHP > 0) animator.Play("DAMAGED", 0, 0f);
            else animator.Play("DEATH", 0, 0f);
        }

        UpdateUI();
    }

    public void Heal(int amount)
    {
        currentHP += amount;
        if (currentHP > maxHP) currentHP = maxHP;

        if (animator != null) animator.SetTrigger("6_Other");
        UpdateUI();
    }

    public void UpdateUI()
    {
        if (hpSlider != null)
        {
            hpSlider.maxValue = maxHP;
            hpSlider.DOValue(currentHP, 0.3f);
        }
        if (hpText != null)
        {
            hpText.text = $"{currentHP} / {maxHP}";
        }
    }

    public bool IsDead() => currentHP <= 0;

    public void Respawn()
    {
        currentHP = maxHP;
        UpdateUI();
        if (animator != null) animator.Play("IDLE", 0, 0f);
    }

    // --- BattleUnit.cs の末尾あたりに追加 ---

    // UIの表示/非表示を切り替えるメソッド
    public void SetUIActive(bool isActive)
    {
        if (hpSlider != null) hpSlider.gameObject.SetActive(isActive);
        if (hpText != null) hpText.gameObject.SetActive(isActive);
        if (turnText != null) turnText.gameObject.SetActive(isActive); // ★追加
    }
}