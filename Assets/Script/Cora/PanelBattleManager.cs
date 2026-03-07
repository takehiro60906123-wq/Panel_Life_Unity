using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;

public enum PanelType { Sword, Magic, Coin, Heal, LvUp, Chick, Diamond, None }

[System.Serializable]
public class PanelSetting
{
    public string label;
    public PanelType type;
    public Sprite panelImage;
    public int weight;
}

public class PanelBattleManager : MonoBehaviour
{
    [Header("UIの設定")]
    public GameObject panelPrefab;
    public Transform boardParent;
    public List<PanelSetting> panelSettings;

    [Header("バトルユニット連携")]
    public BattleUnit playerUnit;
    public BattleUnit enemyUnit;

    [Header("グローバルUI")]
    public TextMeshProUGUI coinText;
    private int currentCoins = 0;

    [Header("バトル演出")]
    public GameObject damageTextPrefab;
    public GameObject hitEffectPrefab;
    public GameObject magicBulletPrefab;
    public GameObject energyOrbPrefab;
    public GameObject absorbEffectPrefab;

    private bool isPlayerTurn = true;
    private CanvasGroup boardCanvasGroup;

    private int rows = 6;
    private int cols = 6;
    private PanelType[,] gridData;
    private GameObject[,] panelObjects;

    [Header("ステージ進行設定")]
    public Transform battlePosition;
    public Vector3 waitOffset = new Vector3(2.5f, 0, 0); // ★X軸（右）に2.5ずつズラす
    public List<BattleUnit> stageEnemyPrefabs;

    private Queue<BattleUnit> upcomingEnemies = new Queue<BattleUnit>(); // 待機列
    private List<BattleUnit> spawnedEnemies = new List<BattleUnit>(); // 実際にシーンに出した敵のリスト
    // ★追加：敵が交代している最中かどうかを判定するフラグ
    private bool isEnemySpawning = false;
    private bool isEnemyDefeatedThisTurn = false;

    // ▼ 変更＆追加：100階層（100体）進むための設定
    public int maxFloors = 100;            // 全何体まで進むか
    public int maxVisibleEnemies = 3;      // 画面内に見せておく待機列の最大数
    public List<BattleUnit> enemyPrefabs;  // ランダムに出現させる敵のリスト（スライム、ゴブリンなど複数登録）

    // ▼ 追加：進行度を管理する変数
    private int currentFloor = 1;          // 現在何体目と戦っているか
    private int spawnedEnemyCount = 0;     // 今までに生成した敵の合計数

    void Start()
    {
        DOTween.Init();
        gridData = new PanelType[rows, cols];
        panelObjects = new GameObject[rows, cols];

        // ▼ 追加！ 盤面（PanelBoard）にCanvasGroupを自動でくっつけて設定する
        boardCanvasGroup = boardParent.gameObject.AddComponent<CanvasGroup>();
        SetBoardInteractable(true); // プレイヤーのターンから開始
        UpdateCoinUI();
        GenerateBoard();

        SetupStage();
    }

    void SetupStage()
    {
        // 最初に maxVisibleEnemies（3体）の数だけ敵を生成する
        int initialSpawnCount = Mathf.Min(maxVisibleEnemies, maxFloors);
        for (int i = 0; i < initialSpawnCount; i++)
        {
            SpawnNextEnemy();
        }

        // 先頭の敵をセット
        enemyUnit = upcomingEnemies.Dequeue();
        enemyUnit.transform.localScale = Vector3.one;
        enemyUnit.SetUIActive(true);

        enemyUnit.InitializeTurn();
    }

    // ▼ 新規追加：奥に新しい敵を1体補充するメソッド
    void SpawnNextEnemy()
    {
        // すでに最大数（100体）まで生成済みなら何もしない
        if (spawnedEnemyCount >= maxFloors) return;

        // リストの中からランダムに敵のプレハブを1つ選ぶ
        BattleUnit prefabToSpawn = enemyPrefabs[Random.Range(0, enemyPrefabs.Count)];

        // 生成位置は「最初の戦闘位置 + (ズラす距離 × 今までに生成した数)」
        // プレイヤーがどれだけ右に進んでも、この計算なら常に正しい奥の位置に生成されます。
        Vector3 spawnPos = battlePosition.position + (waitOffset * spawnedEnemyCount);

        BattleUnit newEnemy = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);

        // 奥の敵なので少し小さく、UIはオフ
        newEnemy.transform.localScale = Vector3.one * 0.8f;
        newEnemy.SetUIActive(false);

        // 少し暗くして遠近感を出す
        SpriteRenderer sr = newEnemy.GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = Color.gray;

        upcomingEnemies.Enqueue(newEnemy);
        spawnedEnemyCount++; // 生成数を+1
    }

    // ==========================================
    // ▼ 新規追加！ 盤面のオン/オフと、暗くする処理
    // ==========================================
    void SetBoardInteractable(bool isInteractable)
    {
        isPlayerTurn = isInteractable;
        if (boardCanvasGroup != null)
        {
            boardCanvasGroup.interactable = isInteractable;  // タッチ可否
            boardCanvasGroup.blocksRaycasts = isInteractable;// タッチのブロック
            // 操作できない時は 0.6f (少し暗く/半透明に)、できる時は 1.0f (元通り)
            boardCanvasGroup.DOFade(isInteractable ? 1.0f : 0.6f, 0.3f);
        }
    }

    void UpdateCoinUI()
    {
        if (coinText != null) coinText.text = currentCoins.ToString();
    }

    void GenerateBoard()
    {
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                GameObject newPanel = Instantiate(panelPrefab, boardParent);
                newPanel.name = $"Panel_{r}_{c}";
                PanelType randomType = GetRandomPanelType();
                gridData[r, c] = randomType;
                panelObjects[r, c] = newPanel;
                Image img = newPanel.transform.Find("IconImage").GetComponent<Image>();
                if (img != null) img.sprite = GetSpriteForType(randomType);
                int row = r; int col = c;
                Button btn = newPanel.GetComponent<Button>();
                if (btn != null) btn.onClick.AddListener(() => OnPanelClicked(row, col));
            }
        }
    }

    PanelType GetRandomPanelType()
    {
        int totalWeight = 0;
        foreach (var setting in panelSettings) totalWeight += setting.weight;
        int randomVal = Random.Range(0, totalWeight);
        foreach (var setting in panelSettings)
        {
            randomVal -= setting.weight;
            if (randomVal < 0) return setting.type;
        }
        return PanelType.Sword;
    }

    Sprite GetSpriteForType(PanelType type)
    {
        foreach (var setting in panelSettings) { if (setting.type == type) return setting.panelImage; }
        return null;
    }

    void OnPanelClicked(int row, int col)
    {
        // ▼ 追加！ 自分のターンじゃなければ何もしない
        if (!isPlayerTurn) return;

        PanelType clickedType = gridData[row, col];
        if (clickedType == PanelType.None) return;

        SetBoardInteractable(false);

        List<Vector2Int> chain = FindChain(row, col, clickedType);
        if (clickedType == PanelType.Sword || clickedType == PanelType.Magic)
        {
            chain.AddRange(GetInvolvedMonsters(chain));
        }

        StartCoroutine(CollectEnergyAndAttack(clickedType, chain));

        foreach (Vector2Int pos in chain)
        {
            gridData[pos.x, pos.y] = PanelType.None;
            Transform iconTransform = panelObjects[pos.x, pos.y].transform.Find("IconImage");
            iconTransform.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack).OnComplete(() => {
                iconTransform.GetComponent<Image>().sprite = null;
            });
        }
        DOVirtual.DelayedCall(0.25f, () => DropAndFillPanels());
    }

    IEnumerator CollectEnergyAndAttack(PanelType type, List<Vector2Int> chain)
    {
        if (playerUnit == null || enemyUnit == null) yield break;

        Vector3 targetPos = (type == PanelType.LvUp) ? enemyUnit.transform.position : playerUnit.transform.position;
        targetPos.y += 0.5f;

        float flyDuration = 1.0f;
        float delay = 0f;
        foreach (Vector2Int pos in chain)
        {
            SpawnEnergyOrb(panelObjects[pos.x, pos.y].transform.position, targetPos, flyDuration, delay);
            delay += 0.05f;
        }
        yield return new WaitForSeconds(flyDuration + delay + 0.3f);
        ExecutePanelAction(type, chain.Count);
    }

    void SpawnEnergyOrb(Vector3 startPos, Vector3 target, float duration, float delay)
    {
        Canvas canvas = boardParent.GetComponentInParent<Canvas>();
        if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            Vector3 screenPos = startPos; screenPos.z = Mathf.Abs(Camera.main.transform.position.z);
            startPos = Camera.main.ScreenToWorldPoint(screenPos);
        }
        if (energyOrbPrefab != null)
        {
            GameObject orb = Instantiate(energyOrbPrefab, startPos, Quaternion.identity);
            orb.transform.DOMove(target, duration).SetDelay(delay).SetEase(Ease.InBack).OnComplete(() => {
                Destroy(orb);
                if (absorbEffectPrefab) Instantiate(absorbEffectPrefab, target, Quaternion.identity).transform.localScale = Vector3.one * 1.5f;
            });
        }
    }

    void ExecutePanelAction(PanelType type, int chainCount)
    {
        switch (type)
        {
            case PanelType.Sword: StartCoroutine(PlayMeleeAttack(chainCount)); break;
            case PanelType.Magic: StartCoroutine(PlayMagicAttack(chainCount)); break;
            case PanelType.Heal:
                playerUnit.Heal(chainCount * 5);
                StartCoroutine(EndPlayerTurn()); // すぐ終了処理へ
                break;
            case PanelType.Coin:
                currentCoins += chainCount * 10; UpdateCoinUI();
                StartCoroutine(EndPlayerTurn()); // すぐ終了処理へ
                break;
            default:
                StartCoroutine(EndPlayerTurn()); // その他もターン終了へ
                break;
        }
    }

    IEnumerator PlayMeleeAttack(int count)
    {
        for (int i = 0; i < count; i++)
        {
            // ★追加：敵がすでに死んでいたら残りの連撃をやめる
            if (enemyUnit == null || enemyUnit.IsDead()) break;

            if (playerUnit.animator != null) playerUnit.animator.Play("ATTACK", 0, 0f);
            yield return new WaitForSeconds(0.15f);
            DamageEnemy(1);
            yield return new WaitForSeconds(0.1f);
        }
        StartCoroutine(EndPlayerTurn());
    }
    IEnumerator PlayMagicAttack(int count)
    {
        for (int i = 0; i < count; i++)
        {
            // ★追加：敵がすでに死んでいたらやめる
            if (enemyUnit == null || enemyUnit.IsDead()) break;

            if (playerUnit.animator != null) playerUnit.animator.Play("ATTACK", 0, 0f);
            yield return new WaitForSeconds(0.1f);
            SpawnMagicBullet();
            yield return new WaitForSeconds(0.15f);
        }
        StartCoroutine(EndPlayerTurn());
    }

    void SpawnMagicBullet()
    {
        if (magicBulletPrefab == null) return;
        Vector3 start = playerUnit.transform.position + new Vector3(0.5f, 0.5f, 0);
        GameObject bullet = Instantiate(magicBulletPrefab, start, Quaternion.identity);

        bullet.transform.DOMove(enemyUnit.transform.position + Vector3.up * 0.5f, 0.15f).OnComplete(() => {
            Destroy(bullet);

            // ★修正：弾が着弾した瞬間に、すでに敵の交代が始まっていたらダメージを与えない
            if (!isEnemySpawning)
            {
                DamageEnemy(1);
            }
        });
    }

    IEnumerator EndPlayerTurn()
    {
        yield return new WaitForSeconds(0.6f); // パネル落下や演出が落ち着くまで待つ

        // 敵の交代処理が終わるまで待つ
        while (isEnemySpawning)
        {
            yield return null;
        }

        // 敵がいなければステージクリア
        if (enemyUnit == null)
        {
            Debug.Log("ステージクリア！リザルトへ");
            yield break;
        }

        // ★追加：もし敵を倒して交代した直後なら、敵のターンをスキップ！
        if (isEnemyDefeatedThisTurn)
        {
            isEnemyDefeatedThisTurn = false; // フラグを元に戻す
            SetBoardInteractable(true);      // 盤面を明るくしてプレイヤーのターン開始
            yield break;                     // 敵のターンには行かずにここで終了
        }

        // 倒していなければ通常通り敵のターンへ
        yield return StartCoroutine(EnemyTurnRoutine());
    }

    IEnumerator EnemyTurnRoutine()
    {
        // ★追加：敵の残りターンを減らす
        enemyUnit.currentCooldown--;
        enemyUnit.UpdateTurnUI();

        // ★追加：ターンが0以下になったら攻撃実行！
        if (enemyUnit.currentCooldown <= 0)
        {
            // 敵の攻撃モーション
            if (enemyUnit.animator != null) enemyUnit.animator.Play("ATTACK", 0, 0f);
            yield return new WaitForSeconds(0.2f); // 攻撃が当たるまでの絶妙な間合い

            // 敵の攻撃の回避・クリティカル判定
            bool isEvasion = Random.Range(0, 100) < 15;
            bool isCritical = Random.Range(0, 100) < 10;

            Vector3 pos = playerUnit.transform.position;

            if (isEvasion)
            {
                SpawnDamageText("Miss", pos + Vector3.up * 1.5f, Color.gray);
            }
            else
            {
                int damage = isCritical ? 3 : 1;
                playerUnit.TakeDamage(damage);

                if (hitEffectPrefab) Destroy(Instantiate(hitEffectPrefab, pos + Vector3.up * 0.5f, Quaternion.identity), 1f);

                Color textColor = isCritical ? Color.yellow : Color.red;
                string textStr = isCritical ? $"CRITICAL!\n{damage}" : damage.ToString();
                SpawnDamageText(textStr, pos + Vector3.up * 1.5f, textColor);
            }

            // ★追加：攻撃が終わったらターン数をリセット
            enemyUnit.currentCooldown = enemyUnit.attackInterval;
            enemyUnit.UpdateTurnUI();

            yield return new WaitForSeconds(0.6f); // ターン終了の余韻
        }
        else
        {
            // ★追加：まだ攻撃しないターン（少しだけ待機してプレイヤーのターンへ）
            // 必要であればここで敵が「様子を見ている」ような小さいアニメーションやテキストを出しても面白いです
            yield return new WaitForSeconds(0.3f);
        }

        // プレイヤーのターンに戻し、盤面を明るくして操作可能にする！
        SetBoardInteractable(true);
    }

    void DamageEnemy(int baseDamage)
    {
        // ★修正：敵が死んでいる、または「交代中（isEnemySpawning）」ならダメージ処理を完全にストップ！
        if (enemyUnit == null || enemyUnit.IsDead() || isEnemySpawning) return;

        bool isEvasion = Random.Range(0, 100) < 10;
        bool isCritical = Random.Range(0, 100) < 20;

        Vector3 pos = enemyUnit.transform.position;

        if (isEvasion)
        {
            SpawnDamageText("Miss", pos + Vector3.up * 1.5f, Color.gray);
            return;
        }

        int finalDamage = isCritical ? baseDamage * 2 : baseDamage;

        enemyUnit.TakeDamage(finalDamage);
        if (hitEffectPrefab) Destroy(Instantiate(hitEffectPrefab, pos + Vector3.up * 0.5f, Quaternion.identity), 1f);

        Color textColor = isCritical ? Color.yellow : Color.white;
        string textStr = isCritical ? $"CRITICAL!\n{finalDamage}" : finalDamage.ToString();
        SpawnDamageText(textStr, pos + Vector3.up * 1.5f, textColor);

        if (enemyUnit.IsDead())
        {
            isEnemyDefeatedThisTurn = true;
            StartCoroutine(EnemyRespawnRoutine());
        }
    }

    IEnumerator EnemyRespawnRoutine()
    {
        isEnemySpawning = true;
        yield return new WaitForSeconds(1.0f);

        // 倒れた敵は消す
        Destroy(enemyUnit.gameObject);

        // ★追加：階層（倒した数）を進める
        currentFloor++;

        // ★追加：待機列の奥に、新しい敵を1体補充する
        SpawnNextEnemy();

        // 待機列に敵がいれば次へ
        if (upcomingEnemies.Count > 0)
        {
            enemyUnit = upcomingEnemies.Dequeue();

            MovePlayerForward();

            yield return new WaitForSeconds(0.6f);

            enemyUnit.SetUIActive(true);
            SpriteRenderer sr = enemyUnit.GetComponent<SpriteRenderer>();
            if (sr != null) sr.DOColor(Color.white, 0.5f);
        }
        else
        {
            enemyUnit = null;
            // 100体すべて倒しきった時の処理！
            Debug.Log("全" + maxFloors + "体撃破！完全クリア！");
        }

        isEnemySpawning = false;
    }



    // 新規追加：プレイヤーが次の敵に向かって右へ前進する
    void MovePlayerForward()
    {
        Vector3 targetPos = playerUnit.transform.position + waitOffset;

        // ★追加：移動開始時に歩行アニメーションを再生
        // ※ "WALK" の部分は、UnityのAnimatorで設定している移動アニメーションのステート名に合わせてください（"MOVE" や "RUN" など）
        if (playerUnit.animator != null)
        {
            playerUnit.animator.Play("MOVE", 0, 0f);
        }

        // プレイヤーの移動処理
        playerUnit.transform.DOMove(targetPos, 0.5f).SetEase(Ease.OutCubic).OnComplete(() =>
        {
            // ★追加：目的地に到着（DOMoveが完了）したら、待機アニメーションに戻す
            if (playerUnit.animator != null)
            {
                playerUnit.animator.Play("IDLE", 0, 0f);
            }
        });

        // 新しい敵のサイズを戦闘用の大きさに戻す
        enemyUnit.transform.DOScale(Vector3.one, 0.5f);

        // カメラも一緒に移動させる
        Camera.main.transform.DOMoveX(Camera.main.transform.position.x + waitOffset.x, 0.5f).SetEase(Ease.OutCubic);

        enemyUnit.InitializeTurn();
    }

    // ※前回追加した MoveEnemiesForward() はもう使わないので削除してOKです！

    // ▼ 新規追加：待機列を前に進める処理
    void MoveEnemiesForward()
    {
        enemyUnit.transform.DOMove(battlePosition.position, 0.5f).SetEase(Ease.OutCubic);
        enemyUnit.transform.DOScale(Vector3.one, 0.5f);

        // 暗くしていた色を元に戻す
        SpriteRenderer frontSr = enemyUnit.GetComponent<SpriteRenderer>();
        if (frontSr != null) frontSr.DOColor(Color.white, 0.5f);

        int waitIndex = 1;
        foreach (var enemy in upcomingEnemies)
        {
            Vector3 targetPos = battlePosition.position + (waitOffset * waitIndex);
            enemy.transform.DOMove(targetPos, 0.5f).SetEase(Ease.OutCubic);
            waitIndex++;
        }
    }

    void SpawnDamageText(string text, Vector3 position, Color color)
    {
        if (damageTextPrefab == null) return;
        GameObject textObj = Instantiate(damageTextPrefab, position, Quaternion.identity);
        TextMeshPro tmpro = textObj.GetComponent<TextMeshPro>();
        if (tmpro != null)
        {
            tmpro.text = text;
            tmpro.color = color; // 色を設定
            float randomX = Random.Range(-0.5f, 0.5f);
            textObj.transform.DOMove(new Vector3(position.x + randomX, position.y + 1.5f, position.z), 0.8f).SetEase(Ease.OutCirc);
            tmpro.DOFade(0, 0.8f).SetDelay(0.2f);
        }
        Destroy(textObj, 1.2f);
    }

    List<Vector2Int> FindChain(int startRow, int startCol, PanelType targetType)
    {
        List<Vector2Int> chain = new List<Vector2Int>();
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
        Vector2Int startNode = new Vector2Int(startRow, startCol);
        queue.Enqueue(startNode); visited.Add(startNode);
        Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue(); chain.Add(current);
            foreach (Vector2Int dir in directions)
            {
                int nr = current.x + dir.x; int nc = current.y + dir.y;
                if (nr >= 0 && nr < rows && nc >= 0 && nc < cols)
                {
                    Vector2Int nextNode = new Vector2Int(nr, nc);
                    if (!visited.Contains(nextNode) && gridData[nr, nc] == targetType)
                    {
                        visited.Add(nextNode); queue.Enqueue(nextNode);
                    }
                }
            }
        }
        return chain;
    }

    void DropAndFillPanels()
    {
        float cellSize = 150f;
        for (int c = 0; c < cols; c++)
        {
            int writeRow = rows - 1;
            for (int r = rows - 1; r >= 0; r--)
            {
                if (gridData[r, c] != PanelType.None)
                {
                    if (r != writeRow)
                    {
                        gridData[writeRow, c] = gridData[r, c]; gridData[r, c] = PanelType.None;
                        Transform writeIcon = panelObjects[writeRow, c].transform.Find("IconImage");
                        Transform readIcon = panelObjects[r, c].transform.Find("IconImage");
                        writeIcon.GetComponent<Image>().sprite = readIcon.GetComponent<Image>().sprite;
                        writeIcon.localScale = Vector3.one; readIcon.GetComponent<Image>().sprite = null;
                        int dropDistance = writeRow - r;
                        writeIcon.localPosition = new Vector3(0, dropDistance * cellSize, 0);
                        writeIcon.DOLocalMoveY(0, 0.4f).SetEase(Ease.OutBounce);
                    }
                    writeRow--;
                }
            }
            for (int r = writeRow; r >= 0; r--)
            {
                PanelType newType = GetRandomPanelType(); gridData[r, c] = newType;
                Transform iconTransform = panelObjects[r, c].transform.Find("IconImage");
                iconTransform.GetComponent<Image>().sprite = GetSpriteForType(newType);
                iconTransform.localScale = Vector3.one;
                iconTransform.localPosition = new Vector3(0, (r + 1) * cellSize, 0);
                int dropOrder = writeRow - r;
                iconTransform.DOLocalMoveY(0, 0.4f).SetDelay(0.08f * dropOrder).SetEase(Ease.OutBounce);
            }
        }
    }

    List<Vector2Int> GetInvolvedMonsters(List<Vector2Int> attackChain)
    {
        HashSet<Vector2Int> monsters = new HashSet<Vector2Int>();
        Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        foreach (Vector2Int pos in attackChain)
        {
            foreach (Vector2Int dir in directions)
            {
                int nr = pos.x + dir.x; int nc = pos.y + dir.y;
                if (nr >= 0 && nr < rows && nc >= 0 && nc < cols)
                {
                    if (gridData[nr, nc] == PanelType.LvUp) monsters.Add(new Vector2Int(nr, nc));
                }
            }
        }
        return new List<Vector2Int>(monsters);
    }
}