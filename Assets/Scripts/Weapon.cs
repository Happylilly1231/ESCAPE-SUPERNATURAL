using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Weapon : MonoBehaviour
{
    public enum Type { PrimaryWeapon, SecondaryWeapon };
    public Type type;
    public int damage;
    public GameObject bullet;
    public Transform bulletPos;
    public Vector3 fireRotation;
    public Vector3 originalRotation;
    public int maxBulletCnt;
    public int weaponId;
    public float bulletSpeed;
    public float fireDistance;
    int curBulletCnt;
    int enemyLayer = 9;

    void Start()
    {
        curBulletCnt = maxBulletCnt;
    }

    public void Use()
    {
        if (curBulletCnt > 0)
        {
            StartCoroutine(Fire());
        }
    }

    IEnumerator Fire()
    {
        yield return new WaitForSeconds(0.3f);

        // 카메라 중앙 방향 계산
        Ray ray = GameManager.instance.mainCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2));

        Vector3 targetPoint;
        if (Physics.Raycast(ray, out RaycastHit hit, 10f, enemyLayer))
        {
            if (hit.collider.gameObject != gameObject)
                targetPoint = hit.point; // 충돌 지점
            else
                targetPoint = ray.GetPoint(fireDistance); // 충돌하지 않으면 먼 거리로
        }
        else
        {
            targetPoint = ray.GetPoint(fireDistance); // 충돌하지 않으면 먼 거리로
        }

        // 총구에서 목표 지점으로 발사
        Vector3 dir = (targetPoint - bulletPos.position).normalized;
        GameObject firedBullet = Instantiate(bullet, bulletPos.position, Quaternion.LookRotation(dir));
        Rigidbody bulletRigid = firedBullet.GetComponent<Rigidbody>();
        bulletRigid.velocity = dir * bulletSpeed;
        curBulletCnt--;

        Debug.Log(firedBullet);
        // if (firedBullet != null)
        //     Destroy(firedBullet, 3f);
    }
}
