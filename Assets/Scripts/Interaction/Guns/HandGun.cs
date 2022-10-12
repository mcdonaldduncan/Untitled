using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class HandGun : MonoBehaviour, IGun
{
    private void OnEnable()
    {
        GunHandler.weaponSwitched += OnWeaponSwitch;
    }
    private void OnDisable()
    {
        GunHandler.weaponSwitched -= OnWeaponSwitch;
    }

    public static void Shoot(Camera fpsCam, Transform shootFrom, GameObject gameObject, LayerMask layerToIgnore, float bulletDamage, float verticalSpread, float horizontalSpread)
    {
        RaycastHit hitInfo;

        GameObject lineDrawer = new GameObject();
        LineRenderer lineRenderer = lineDrawer.AddComponent<LineRenderer>();
        lineRenderer.startWidth = 0.1f;
        lineRenderer.endWidth = 0.1f;

        Vector3 aimSpot = fpsCam.transform.position;
        aimSpot += fpsCam.transform.forward * 30f;
        shootFrom.LookAt(aimSpot);

        Vector3 direction = shootFrom.transform.forward; // your initial aim.
        Vector3 spread = Vector3.zero;
        spread += shootFrom.transform.up * Random.Range(-verticalSpread, verticalSpread);
        spread += shootFrom.transform.right * Random.Range(-horizontalSpread, horizontalSpread);
        direction += spread.normalized; //* Random.Range(0f, 0.2f);

        if (Physics.Raycast(shootFrom.transform.position, direction, out hitInfo, float.MaxValue, ~layerToIgnore))
        {
            lineRenderer.material.color = Color.green;
            lineRenderer.SetPosition(0, shootFrom.transform.position);
            lineRenderer.SetPosition(1, hitInfo.point);

            Debug.DrawLine(shootFrom.transform.position, hitInfo.point, Color.green, 1f);
            try
            {
                IDamageable damageableTarget = hitInfo.transform.GetComponent<IDamageable>();
                Vector3 targetPosition = hitInfo.transform.position;

                float distance = Vector3.Distance(targetPosition, gameObject.transform.position);
                damageableTarget.TakeDamage(bulletDamage / (Mathf.Abs(distance / 2)));

                Debug.Log($"{hitInfo.transform.name}: {damageableTarget.Health}");
            }
            catch
            {
                Debug.Log($"Hit {hitInfo.transform.name}");
                Debug.Log("Not an IDamageable");
            }
        }
        else
        {
            Debug.DrawLine(shootFrom.transform.position, shootFrom.transform.forward + direction * 10, Color.red, 1f);
            lineRenderer.material.color = Color.red;
            lineRenderer.SetPosition(0, shootFrom.transform.position);
            lineRenderer.SetPosition(1, shootFrom.transform.position + direction * 10);
        }
    }

    public static void StartReload(GunHandler instance, HandGun handGun, WaitForSeconds reloadWait)
    {
        instance.StartCoroutine(handGun.Reload(instance, reloadWait));
    }

    public IEnumerator Reload(GunHandler instance, WaitForSeconds reloadWait)
    {
        instance.Reloading = true;
        yield return reloadWait;
        if (instance.Reloading)
        {
            instance.Reloading = false;
            instance.HandGunCurrentAmmo = instance.HandGunMaxAmmo;
        }
    }

    private void OnWeaponSwitch(GunHandler instance, IGun handGun, WaitForSeconds reloadWait)
    {
        Debug.Log("Stopping Reload");
        instance.Reloading = false;
        StopCoroutine(handGun.Reload(instance, reloadWait));
    }
}
