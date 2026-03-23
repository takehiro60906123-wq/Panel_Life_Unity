// =============================================================
// ScreenShakeController.cs
// カメラシェイク・ヒットストップ・スクリーンフラッシュの一元管理
//
// 修正版ポイント:
// - カメラの基準位置を Awake 時固定ではなく、毎回のシェイク直前の現在位置にする
// - シェイクしていない間は基準位置を追従更新する
// - 部屋移動/戦闘遷移後に古い位置へ戻ってしまう不具合を防ぐ
// =============================================================
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public enum ShakePreset
{
    Light,
    Medium,
    Heavy,
    Boss,
    GunPistol,
    GunMachineGun,
    GunShotgun,
    GunRifle,
    EnemyDefeat,
    EnemyDefeatDanger,
    PlayerHit,
}

public class ScreenShakeController : MonoBehaviour
{
    private static ScreenShakeController instance;
    public static ScreenShakeController Instance => instance;

    [Header("対象カメラ（空なら Main Camera）")]
    [SerializeField] private Camera targetCamera;

    [Header("フラッシュ用オーバーレイ（Canvas > Image, RaycastTarget=OFF）")]
    [SerializeField] private Image flashOverlay;

    [Header("プリセット: Light")]
    [SerializeField] private float lightIntensity = 0.04f;
    [SerializeField] private float lightDuration = 0.08f;
    [SerializeField] private int lightVibrato = 14;

    [Header("プリセット: Medium")]
    [SerializeField] private float mediumIntensity = 0.08f;
    [SerializeField] private float mediumDuration = 0.10f;
    [SerializeField] private int mediumVibrato = 18;

    [Header("プリセット: Heavy")]
    [SerializeField] private float heavyIntensity = 0.14f;
    [SerializeField] private float heavyDuration = 0.13f;
    [SerializeField] private int heavyVibrato = 22;

    [Header("プリセット: Boss")]
    [SerializeField] private float bossIntensity = 0.22f;
    [SerializeField] private float bossDuration = 0.20f;
    [SerializeField] private int bossVibrato = 26;

    [Header("銃: ピストル")]
    [SerializeField] private float pistolShakeIntensity = 0.05f;
    [SerializeField] private float pistolShakeDuration = 0.07f;

    [Header("銃: マシンガン")]
    [SerializeField] private float mgShakeIntensity = 0.03f;
    [SerializeField] private float mgShakeDuration = 0.04f;

    [Header("銃: ショットガン")]
    [SerializeField] private float sgShakeIntensity = 0.16f;
    [SerializeField] private float sgShakeDuration = 0.12f;

    [Header("銃: ライフル")]
    [SerializeField] private float rifleShakeIntensity = 0.10f;
    [SerializeField] private float rifleShakeDuration = 0.09f;

    [Header("ヒットストップ")]
    [SerializeField] private float defaultHitStopDuration = 0.045f;
    [SerializeField] private float hitStopTimeScale = 0.02f;

    [Header("スクリーンフラッシュ")]
    [SerializeField] private float defaultFlashAlpha = 0.35f;
    [SerializeField] private float defaultFlashFadeDuration = 0.12f;

    private Transform cameraTransform;
    private Vector3 cameraBaseLocalPos;
    private Tween currentShakeTween;
    private Coroutine hitStopCoroutine;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(this);
            return;
        }

        ResolveCamera();
        RefreshCameraBasePosition();
    }

    private void LateUpdate()
    {
        if (!IsShakePlaying())
        {
            RefreshCameraBasePosition();
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }

        if (Time.timeScale < 0.5f)
        {
            Time.timeScale = 1f;
        }
    }

    private void ResolveCamera()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (targetCamera != null)
        {
            cameraTransform = targetCamera.transform;
        }
    }

    private void RefreshCameraBasePosition()
    {
        if (cameraTransform == null)
        {
            ResolveCamera();
        }

        if (cameraTransform != null)
        {
            cameraBaseLocalPos = cameraTransform.localPosition;
        }
    }

    private bool IsShakePlaying()
    {
        return currentShakeTween != null && currentShakeTween.IsActive() && currentShakeTween.IsPlaying();
    }

    public void Shake(ShakePreset preset)
    {
        ResolvePreset(preset, out float intensity, out float duration, out int vibrato);
        ShakeCamera(intensity, duration, vibrato);
    }

    public void Shake(float intensity, float duration, int vibrato = 18)
    {
        ShakeCamera(intensity, duration, vibrato);
    }

    public void HitStop(float duration = -1f)
    {
        float d = duration > 0f ? duration : defaultHitStopDuration;

        if (hitStopCoroutine != null)
        {
            StopCoroutine(hitStopCoroutine);
            Time.timeScale = 1f;
        }

        hitStopCoroutine = StartCoroutine(HitStopRoutine(d));
    }

    public void Flash(Color color, float fadeDuration = -1f, float peakAlpha = -1f)
    {
        if (flashOverlay == null) return;

        float alpha = peakAlpha > 0f ? peakAlpha : defaultFlashAlpha;
        float fade = fadeDuration > 0f ? fadeDuration : defaultFlashFadeDuration;

        flashOverlay.DOKill();
        Color c = color;
        c.a = alpha;
        flashOverlay.color = c;
        flashOverlay.enabled = true;
        flashOverlay.DOFade(0f, fade).SetEase(Ease.OutQuad).SetUpdate(true)
            .OnComplete(() => flashOverlay.enabled = false);
    }

    public void Impact(ShakePreset preset, Color flashColor, float hitStopDuration = -1f)
    {
        Shake(preset);
        Flash(flashColor);

        if (hitStopDuration > 0f)
        {
            HitStop(hitStopDuration);
        }
    }

    public void GunImpact(GunType gunType)
    {
        switch (gunType)
        {
            case GunType.Pistol:
                Shake(ShakePreset.GunPistol);
                break;
            case GunType.MachineGun:
                Shake(ShakePreset.GunMachineGun);
                break;
            case GunType.Shotgun:
                Shake(ShakePreset.GunShotgun);
                Flash(new Color(1f, 0.97f, 0.88f), 0.08f, 0.25f);
                HitStop(0.04f);
                break;
            case GunType.Rifle:
                Shake(ShakePreset.GunRifle);
                Flash(new Color(1f, 1f, 0.92f), 0.06f, 0.18f);
                HitStop(0.05f);
                break;
        }
    }

    public void PanelClearImpact(int chainSize, int bigThreshold, int maxThreshold)
    {
        if (chainSize < bigThreshold) return;

        if (chainSize >= maxThreshold)
        {
            Shake(ShakePreset.Medium);
            HitStop(0.035f);
        }
        else
        {
            Shake(ShakePreset.Light);
        }
    }

    public void EnemyDefeatImpact(bool isDangerEnemy)
    {
        if (isDangerEnemy)
        {
            Shake(ShakePreset.EnemyDefeatDanger);
            Flash(new Color(1f, 0.92f, 0.5f), 0.14f, 0.20f);
            HitStop(0.05f);
        }
        else
        {
            Shake(ShakePreset.EnemyDefeat);
        }
    }

    public static void TryShake(ShakePreset preset)
    {
        if (instance != null) instance.Shake(preset);
    }

    public static void TryHitStop(float duration = -1f)
    {
        if (instance != null) instance.HitStop(duration);
    }

    public static void TryFlash(Color color, float fadeDuration = -1f, float peakAlpha = -1f)
    {
        if (instance != null) instance.Flash(color, fadeDuration, peakAlpha);
    }

    public static void TryGunImpact(GunType gunType)
    {
        if (instance != null) instance.GunImpact(gunType);
    }

    private void ShakeCamera(float intensity, float duration, int vibrato)
    {
        if (cameraTransform == null)
        {
            ResolveCamera();
            if (cameraTransform == null) return;
        }

        // 直前の本来位置を基準にする。部屋移動・戦闘遷移後でも古い位置へ戻さない。
        Vector3 shakeBasePos = cameraTransform.localPosition;

        if (currentShakeTween != null && currentShakeTween.IsActive())
        {
            currentShakeTween.Kill();
        }

        cameraBaseLocalPos = shakeBasePos;
        cameraTransform.localPosition = cameraBaseLocalPos;

        currentShakeTween = cameraTransform
            .DOShakePosition(duration, intensity, vibrato, 90f, false, true)
            .SetUpdate(true)
            .OnKill(() =>
            {
                if (cameraTransform != null)
                {
                    cameraTransform.localPosition = cameraBaseLocalPos;
                }
            })
            .OnComplete(() =>
            {
                if (cameraTransform != null)
                {
                    cameraTransform.localPosition = cameraBaseLocalPos;
                }
            });
    }

    private IEnumerator HitStopRoutine(float duration)
    {
        Time.timeScale = hitStopTimeScale;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        Time.timeScale = 1f;
        hitStopCoroutine = null;
    }

    private void ResolvePreset(ShakePreset preset, out float intensity, out float duration, out int vibrato)
    {
        switch (preset)
        {
            case ShakePreset.Light:
                intensity = lightIntensity;
                duration = lightDuration;
                vibrato = lightVibrato;
                break;
            case ShakePreset.Medium:
                intensity = mediumIntensity;
                duration = mediumDuration;
                vibrato = mediumVibrato;
                break;
            case ShakePreset.Heavy:
            case ShakePreset.GunShotgun:
                intensity = preset == ShakePreset.GunShotgun ? sgShakeIntensity : heavyIntensity;
                duration = preset == ShakePreset.GunShotgun ? sgShakeDuration : heavyDuration;
                vibrato = heavyVibrato;
                break;
            case ShakePreset.Boss:
                intensity = bossIntensity;
                duration = bossDuration;
                vibrato = bossVibrato;
                break;
            case ShakePreset.GunPistol:
                intensity = pistolShakeIntensity;
                duration = pistolShakeDuration;
                vibrato = mediumVibrato;
                break;
            case ShakePreset.GunMachineGun:
                intensity = mgShakeIntensity;
                duration = mgShakeDuration;
                vibrato = lightVibrato;
                break;
            case ShakePreset.GunRifle:
                intensity = rifleShakeIntensity;
                duration = rifleShakeDuration;
                vibrato = mediumVibrato;
                break;
            case ShakePreset.EnemyDefeat:
                intensity = lightIntensity * 1.2f;
                duration = lightDuration;
                vibrato = lightVibrato;
                break;
            case ShakePreset.EnemyDefeatDanger:
                intensity = mediumIntensity * 1.3f;
                duration = mediumDuration * 1.2f;
                vibrato = heavyVibrato;
                break;
            case ShakePreset.PlayerHit:
                intensity = mediumIntensity * 0.8f;
                duration = mediumDuration * 0.9f;
                vibrato = mediumVibrato;
                break;
            default:
                intensity = lightIntensity;
                duration = lightDuration;
                vibrato = lightVibrato;
                break;
        }
    }
}
