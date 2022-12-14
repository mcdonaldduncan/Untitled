using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public interface IGun
{
    GunHandler GunManager { get; set; }
    Transform ShootFrom { get; set; }
    LayerMask LayerToIgnore { get; set; }
    float FireRate { get; set; }
    float BulletDamage { get; set; }
    float DamageDrop { get; set; }
    float VerticalSpread { get; set; }
    float HorizontalSpread { get; set; }
    float AimOffset { get; set; }
    GameObject HitEnemy { get; set; }
    GameObject HitNonEnemy { get; set; }
    int CurrentAmmo { get; set; }
    int CurrentMaxAmmo { get; }
    CanvasGroup GunReticle { get; set; }
    TrailRenderer BulletTrail { get; set; }
    AudioClip GunShotAudio { get; set; }
    GameObject GunModel { get; set; }
    WaitForSeconds ReloadWait { get; set; }

    void ShootTriggered(InputAction.CallbackContext context);
    void StartReload();
    IEnumerator Reload(WaitForSeconds reloadWait);
}
