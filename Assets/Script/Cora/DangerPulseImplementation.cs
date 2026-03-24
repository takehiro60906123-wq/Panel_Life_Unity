// =============================================================
// 敵攻撃予兆演出 — 実装ガイド
//
// 概要:
//   敵の残りクールダウンが 1 以下になった瞬間、
//   敵スプライトが赤く脈動するループ演出を開始する。
//   攻撃後やクールダウンリセットで自動停止。
//
// 変更ファイル:
//   1. EnemyTweenPresenter.cs — 脈動メソッド追加
//   2. BattleUnitView.cs — RefreshCooldown で開始/停止制御
//   3. ScreenShakeController.cs — 画面端の赤ヴィネット（任意）
// =============================================================


// #############################################################
// 1. EnemyTweenPresenter.cs
// #############################################################

// --- 変更 1-A: フィールド追加 ---
// 既存の [Header("スキル演出")] の直前あたりに追加:
//
//     [Header("攻撃予兆パルス")]
//     [SerializeField] private Color dangerPulseColor = new Color(1f, 0.25f, 0.18f, 1f);
//     [SerializeField] private float dangerPulseInterval = 0.55f;
//     [SerializeField] private float dangerPulseScaleAmount = 1.06f;
//     [SerializeField] private float dangerPulseBobY = 0.025f;
//
//     private Sequence dangerPulseSequence;
//     private bool isDangerPulsePlaying;


// --- 変更 1-B: メソッド追加 ---
// PlayHealTween() の直後あたりに以下の2メソッドを追加:

/*

    // =============================================================
    // 攻撃予兆パルス — クールダウン1以下で開始するループ演出
    // 赤く脈動 + 微かに前傾する → 「次に殴ってくる」が映像で伝わる
    // =============================================================

    public void PlayDangerPulseTween()
    {
        if (isDangerPulsePlaying) return;

        EnsureSetup();

        // 他の一回きり演出（攻撃・被弾等）は止めない。
        // パルスは別 Sequence で並行再生する。
        StopDangerPulseInternal();

        isDangerPulsePlaying = true;

        dangerPulseSequence = DOTween.Sequence();

        float halfInterval = dangerPulseInterval * 0.5f;

        // ── 前半: 赤く光りながら少し膨張 + 前傾 ──
        dangerPulseSequence.Append(
            visualRoot.DOScale(baseLocalScale * dangerPulseScaleAmount, halfInterval)
                .SetEase(Ease.InOutSine));

        dangerPulseSequence.Join(
            visualRoot.DOLocalMove(
                baseLocalPos + new Vector3(attackDirectionX * dangerPulseBobY, dangerPulseBobY * 0.5f, 0f),
                halfInterval)
                .SetEase(Ease.InOutSine));

        // ── 後半: 元に戻る ──
        dangerPulseSequence.Append(
            visualRoot.DOScale(baseLocalScale, halfInterval)
                .SetEase(Ease.InOutSine));

        dangerPulseSequence.Join(
            visualRoot.DOLocalMove(baseLocalPos, halfInterval)
                .SetEase(Ease.InOutSine));

        // ── 色パルス（スケールとは独立で SpriteRenderer を点滅） ──
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            SpriteRenderer sr = spriteRenderers[i];
            if (sr == null) continue;

            Color original = baseColors[i];
            Color pulse = Color.Lerp(original, dangerPulseColor, 0.45f);

            // 赤方向にブレンドして戻す（ループ内で毎回）
            Sequence colorSeq = DOTween.Sequence();
            colorSeq.Append(sr.DOColor(pulse, halfInterval).SetEase(Ease.InOutSine));
            colorSeq.Append(sr.DOColor(original, halfInterval).SetEase(Ease.InOutSine));
            colorSeq.SetLoops(-1, LoopType.Restart);

            // dangerPulseSequence と同期して Kill されるよう紐付け
            colorSeq.SetLink(sr.gameObject, LinkBehaviour.KillOnDestroy);

            // カラー Sequence を保持して StopDangerPulse で Kill する
            // → dangerPulseSequence.OnKill で一括処理するのが簡潔
        }

        // 無限ループ
        dangerPulseSequence.SetLoops(-1, LoopType.Restart);
        dangerPulseSequence.SetLink(gameObject, LinkBehaviour.KillOnDestroy);
    }

    public void StopDangerPulse()
    {
        if (!isDangerPulsePlaying) return;
        StopDangerPulseInternal();
    }

    private void StopDangerPulseInternal()
    {
        isDangerPulsePlaying = false;

        if (dangerPulseSequence != null && dangerPulseSequence.IsActive())
        {
            dangerPulseSequence.Kill();
            dangerPulseSequence = null;
        }

        // スプライトの色パルスも確実にリセット
        if (spriteRenderers != null && baseColors != null)
        {
            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                SpriteRenderer sr = spriteRenderers[i];
                if (sr == null) continue;

                sr.DOKill();
                sr.color = baseColors[i];
            }
        }

        // 位置・スケールを基準に戻す
        if (visualRoot != null)
        {
            visualRoot.DOKill();
            visualRoot.localPosition = baseLocalPos;
            visualRoot.localScale = baseLocalScale;
            visualRoot.localRotation = baseLocalRotation;
        }
    }

*/


// --- 変更 1-C: KillTweens() を修正 ---
// 既存の KillTweens() の末尾に1行追加して、
// 攻撃や被弾の演出開始時にパルスも止まるようにする:
//
// 【変更前】
//
//     private void KillTweens()
//     {
//         if (visualRoot != null)
//         {
//             visualRoot.DOKill();
//         }
//
//         if (spriteRenderers == null) return;
//
//         for (int i = 0; i < spriteRenderers.Length; i++)
//         {
//             if (spriteRenderers[i] == null) continue;
//             spriteRenderers[i].DOKill();
//         }
//     }
//
// 【変更後】
//
//     private void KillTweens()
//     {
//         // ★ 攻撃予兆パルスも停止
//         StopDangerPulseInternal();
//
//         if (visualRoot != null)
//         {
//             visualRoot.DOKill();
//         }
//
//         if (spriteRenderers == null) return;
//
//         for (int i = 0; i < spriteRenderers.Length; i++)
//         {
//             if (spriteRenderers[i] == null) continue;
//             spriteRenderers[i].DOKill();
//         }
//     }


// --- 変更 1-D: ResetVisualsImmediate() に追加 ---
// 既存の ResetVisualsImmediate() の先頭に1行追加:
//
//     public void ResetVisualsImmediate()
//     {
//         EnsureSetup();
//         StopDangerPulseInternal();  // ★ 追加
//         KillTweens();
//         // ... 既存の処理 ...
//     }



// #############################################################
// 2. BattleUnitView.cs
// #############################################################

// --- 変更 2-A: フィールド追加 ---
// 既存の private bool hintInitialized; の直後あたりに追加:
//
//     private bool wasDangerLastFrame;


// --- 変更 2-B: RefreshCooldown() を修正 ---
//
// 【変更前】
//
//     public void RefreshCooldown(int cooldown)
//     {
//         if (turnText == null) return;
//
//         if (cooldown > 0)
//         {
//             turnText.text = cooldown.ToString();
//         }
//         else
//         {
//             turnText.text = "!";
//         }
//
//         RefreshTurnHintFromCurrentState(force: true);
//     }
//
// 【変更後】
//
//     public void RefreshCooldown(int cooldown)
//     {
//         if (turnText == null) return;
//
//         if (cooldown > 0)
//         {
//             turnText.text = cooldown.ToString();
//         }
//         else
//         {
//             turnText.text = "!";
//         }
//
//         // ★ 攻撃予兆パルスの開始/停止
//         bool isDangerNow = cooldown <= 1;
//
//         if (isDangerNow && !wasDangerLastFrame)
//         {
//             // danger に入った瞬間 → パルス開始
//             if (tweenPresenter != null)
//             {
//                 tweenPresenter.PlayDangerPulseTween();
//             }
//         }
//         else if (!isDangerNow && wasDangerLastFrame)
//         {
//             // danger から抜けた（攻撃後リセット等）→ パルス停止
//             if (tweenPresenter != null)
//             {
//                 tweenPresenter.StopDangerPulse();
//             }
//         }
//
//         wasDangerLastFrame = isDangerNow;
//
//         RefreshTurnHintFromCurrentState(force: true);
//     }


// --- 変更 2-C: PlayIdle() に安全停止を追加 ---
// 敵 Respawn 時などにパルスが残らないよう、PlayIdle 内で停止。
//
// 【変更前】
//
//     public void PlayIdle()
//     {
//         if (playerAnimationPresenter != null)
//         {
//             playerAnimationPresenter.PlayIdle();
//             return;
//         }
//
//         tweenPresenter?.PlayIdleReset();
//     }
//
// 【変更後】
//
//     public void PlayIdle()
//     {
//         wasDangerLastFrame = false;  // ★ 追加
//
//         if (playerAnimationPresenter != null)
//         {
//             playerAnimationPresenter.PlayIdle();
//             return;
//         }
//
//         tweenPresenter?.PlayIdleReset();
//     }



// #############################################################
// 3. ScreenShakeController.cs（任意 — 画面端の赤ヴィネット）
// #############################################################
//
// 以下は任意の追加演出。敵スプライトのパルスだけでも十分機能するが、
// 画面端にうっすら赤いヴィネットを出すと「盤面全体が危険」という空気が出る。
//
// ScreenShakeController に DangerVignette 機能を足す場合:

// --- フィールド追加 ---
//
//     [Header("危険ヴィネット（任意）")]
//     [SerializeField] private Image dangerVignetteOverlay;
//     [SerializeField] private Color dangerVignetteColor = new Color(0.8f, 0.1f, 0.05f, 0.12f);
//     [SerializeField] private float dangerVignetteFadeIn = 0.3f;
//     [SerializeField] private float dangerVignetteFadeOut = 0.25f;
//     [SerializeField] private float dangerVignettePulseMin = 0.06f;
//     [SerializeField] private float dangerVignettePulseMax = 0.14f;
//     [SerializeField] private float dangerVignettePulseSpeed = 1.2f;
//
//     private Tween dangerVignetteTween;
//     private bool dangerVignetteActive;

// --- メソッド追加 ---

/*

    /// <summary>
    /// 画面端の赤ヴィネットを開始する。
    /// dangerVignetteOverlay に「中央が透明、端が赤い」グラデーション画像を設定しておく。
    /// </summary>
    public void StartDangerVignette()
    {
        if (dangerVignetteOverlay == null) return;
        if (dangerVignetteActive) return;

        dangerVignetteActive = true;
        dangerVignetteOverlay.gameObject.SetActive(true);
        dangerVignetteOverlay.raycastTarget = false;

        Color c = dangerVignetteColor;
        c.a = 0f;
        dangerVignetteOverlay.color = c;

        // フェードインしてからパルスループ開始
        dangerVignetteOverlay.DOFade(dangerVignettePulseMax, dangerVignetteFadeIn)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                if (!dangerVignetteActive) return;

                dangerVignetteTween = dangerVignetteOverlay
                    .DOFade(dangerVignettePulseMin, dangerVignettePulseSpeed)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetEase(Ease.InOutSine);
            });
    }

    /// <summary>
    /// 赤ヴィネットを停止してフェードアウトする。
    /// </summary>
    public void StopDangerVignette()
    {
        if (dangerVignetteOverlay == null) return;
        if (!dangerVignetteActive) return;

        dangerVignetteActive = false;

        if (dangerVignetteTween != null && dangerVignetteTween.IsActive())
        {
            dangerVignetteTween.Kill();
            dangerVignetteTween = null;
        }

        dangerVignetteOverlay.DOKill();
        dangerVignetteOverlay.DOFade(0f, dangerVignetteFadeOut)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                dangerVignetteOverlay.gameObject.SetActive(false);
            });
    }

    // 静的便利メソッド
    public static void TryStartDangerVignette()
    {
        if (instance != null) instance.StartDangerVignette();
    }

    public static void TryStopDangerVignette()
    {
        if (instance != null) instance.StopDangerVignette();
    }

*/

// ヴィネットを有効にしたい場合、BattleUnitView.RefreshCooldown() の
// パルス開始/停止の箇所にそれぞれ1行追加する:
//
//     if (isDangerNow && !wasDangerLastFrame)
//     {
//         tweenPresenter?.PlayDangerPulseTween();
//         ScreenShakeController.TryStartDangerVignette();   // ★
//     }
//     else if (!isDangerNow && wasDangerLastFrame)
//     {
//         tweenPresenter?.StopDangerPulse();
//         ScreenShakeController.TryStopDangerVignette();    // ★
//     }
//
// ヴィネット用の Image アセットは、中央が完全透明で
// 四辺が赤いグラデーションの PNG を使う。
// Unity の Radial Fill や 9-slice でも作れる。


// #############################################################
// まとめ
// #############################################################
//
// 変更量:
//
// | ファイル | 変更 | 行数 |
// |---------|------|------|
// | EnemyTweenPresenter.cs | フィールド6個 + メソッド3個 + KillTweens修正 | +約80行 |
// | BattleUnitView.cs | フィールド1個 + RefreshCooldown修正 + PlayIdle修正 | +15行 |
// | ScreenShakeController.cs | ヴィネット機能（任意） | +約40行 |
//
// 動作フロー:
//
//   1. 敵ターン終了 → TickCooldown() → UpdateTurnUI()
//   2. → view.RefreshCooldown(cooldown)
//   3. cooldown が 1 以下になった → PlayDangerPulseTween() 開始
//   4. 敵スプライトが赤く脈動 + 微かに前傾ループ
//   5. プレイヤーが行動 → 敵が攻撃 → ResetCooldown()
//   6. → UpdateTurnUI() → RefreshCooldown(新しいcooldown)
//   7. cooldown > 1 → StopDangerPulse() → 元の色・位置に戻る
//
// KillTweens() に StopDangerPulseInternal() を入れたので、
// 攻撃演出・被弾演出・死亡演出の開始時にパルスは自動で止まる。
// つまり「パルス中に敵が殴ってきた」「パルス中に撃破した」でも
// 演出が干渉することはない。
//
// ヴィネットは敵スプライトのパルスだけでは物足りない場合に追加する。
// 入れなくてもゲームとしては十分機能する。
