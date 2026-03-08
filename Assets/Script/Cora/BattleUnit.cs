using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class BattleUnit : MonoBehaviour
{
    [Header("ステータス")]
    public int maxHP = 15; // シレン風に初期値15
    private int currentHP;

    // ▼ 新規追加：敵が落とす経験値（プレイヤーの場合は使わないので0でOK）
    public int expYield = 2;

    [Header("レベルシステム（プレイヤー専用）")]
    public int level = 1;
    public int currentExp = 0;
    public TextMeshProUGUI levelText; // 「Lv 1」と表示するUI
    // レベルアップに必要な経験値のテーブル（10でLv2, 30でLv3...）
    private int[] expTable = { 0, 10, 30, 60, 100, 150, 220, 300, 400, 500 };

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

    // ★重複しないように、UpdateUIはこの1つだけにします！
    public void UpdateUI()
    {
        if (hpSlider != null)
        {
            hpSlider.maxValue = maxHP;
            hpSlider.value = currentHP;
        }
        if (hpText != null)
        {
            hpText.text = $"{currentHP} / {maxHP}";
        }

        // ▼ 追加：レベルUIの更新
        if (levelText != null)
        {
            levelText.text = $"Lv {level}";
        }
    }

    // ▼ 復活：ターン表示の更新処理
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

    // ▼ 修正：経験値を獲得し、レベルアップしたかどうかを返す
    public bool AddExp(int amount)
    {
        currentExp += amount;
        bool isLeveledUp = false; // レベルアップしたかのフラグ

        // レベルアップ判定
        while (level < expTable.Length && currentExp >= expTable[level])
        {
            LevelUp();
            isLeveledUp = true;
        }

        return isLeveledUp; // 結果をマネージャーに報告！
    }

    // ▼ 新規追加：レベルアップ処理
    private void LevelUp()
    {
        level++;

        // 最大HPが 4 または 5 上がる
        int hpIncrease = Random.Range(4, 6);
        maxHP += hpIncrease;

        // ★ローグライクの醍醐味：レベルアップでHP全回復！
        currentHP = maxHP;

        // 演出（アニメーションがあれば再生）
        if (animator != null) animator.SetTrigger("6_Other"); // 例としてHealと同じアニメ

        // もしレベルアップ用のエフェクトがあればここでInstantiate(またはプールから取得)しても良いです
        Debug.Log($"レベルアップ！ Lv{level} になった！ 最大HPが {hpIncrease} 上がった！");

        UpdateUI();
    }
}