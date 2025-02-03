using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

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
    public int curBulletCnt;

    public bool canFire;
    public bool canFireBullet;
    public bool weaponDirection;
    public bool isFiring;

    void Start()
    {
        curBulletCnt = maxBulletCnt;
        canFire = true;
    }

    public void Use(GameObject user)
    {
        if (curBulletCnt > 0)
        {
            if (canFire)
            {
                canFire = false;
                StartCoroutine(Fire(user));
            }
        }
    }

    void OnEnable()
    {
        gameObject.transform.localRotation = Quaternion.Euler(originalRotation);
    }

    IEnumerator Fire(GameObject user)
    {
        isFiring = true;

        while (!canFireBullet)
        {
            yield return null;
        }
        yield return new WaitForSeconds(0.1f);

        Ray ray;
        int layerMask = layerMask = GameManager.instance.enemyLayerMask | GameManager.instance.mapLayerMask;
        if (user == GameManager.instance.Characters[GameManager.instance.selectCharacterId])
        {
            Debug.Log("현재 플레이 중인 플레이어의 공격입니다.");
            ray = GameManager.instance.mainCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2)); // 카메라 중앙 방향 계산
        }
        else
        {
            Debug.Log("현재 플레이 중이지 않은 적/클론/나머지 캐릭터의 공격입니다.");
            ray = new Ray(bulletPos.position, user.transform.forward);
            if (user.tag == "Enemy")
            {
                layerMask = GameManager.instance.playerLayerMask | GameManager.instance.mapLayerMask;
            }
        }

        Vector3 targetPoint;
        if (Physics.Raycast(ray, out RaycastHit hit, 10f, layerMask))
        {
            targetPoint = hit.point; // 충돌 지점
            if (user.tag != "Enemy" && hit.collider.gameObject.tag == "Enemy")
            {
                hit.collider.gameObject.GetComponent<EnemyController>().Damage(damage);
                Debug.Log("적이 총알에 맞았습니다!");
            }
            else if (user.tag == "Enemy")
            {
                if (hit.collider.gameObject.tag == "Player")
                {
                    hit.collider.gameObject.GetComponent<PlayerController>().Damage(damage);
                    Debug.Log("플레이어가 총알에 맞았습니다!");
                }
                else if (hit.collider.gameObject.tag == "Clone")
                {
                    hit.collider.gameObject.GetComponent<CloneController>().Damage(damage);
                    Debug.Log("분신이 총알에 맞았습니다!");
                }

            }
        }
        else
        {
            targetPoint = ray.GetPoint(fireDistance); // 충돌하지 않으면 먼 거리로
        }

        // 총알 시각적 효과 생성
        GameObject firedBullet = Instantiate(bullet, bulletPos.position, Quaternion.identity);

        // // 총이 충돌 지점을 향하도록 회전 설정
        // gameObject.transform.LookAt(targetPoint);

        // 총알이 충돌 지점을 향하도록 회전 설정
        firedBullet.transform.LookAt(targetPoint);

        // // Rigidbody를 사용하여 총알 이동
        // Rigidbody rb = firedBullet.GetComponent<Rigidbody>();
        // Vector3 direction = (targetPoint - bulletPos.position).normalized;
        // rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        // rb.velocity = direction * bulletSpeed; // bulletSpeed는 속도 값

        Debug.DrawRay(ray.origin, ray.direction, Color.cyan, 3f);

        while (true)
        {
            if (firedBullet.transform.position == targetPoint)
            {
                Destroy(firedBullet);
                break;
            }

            firedBullet.transform.position = Vector3.MoveTowards(firedBullet.transform.position, targetPoint, bulletSpeed * Time.deltaTime);
            yield return null;
        }
        curBulletCnt--;
        UIManager.instance.bulletCntTxt.text = curBulletCnt.ToString() + " / " + maxBulletCnt.ToString();

        canFire = true;

        isFiring = false;
    }
}
