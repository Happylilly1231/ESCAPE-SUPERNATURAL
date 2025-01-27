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
    int curBulletCnt;

    IEnumerator curCoroutine;
    public bool canFire;

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
                // curCoroutine = Fire();
                canFire = false;
                StartCoroutine(Fire(user));
            }
        }
    }

    IEnumerator Fire(GameObject user)
    {
        gameObject.transform.localRotation = Quaternion.Euler(fireRotation); // 총 각도 변경(발사 각도)

        yield return new WaitForSeconds(0.3f);

        Ray ray;
        if (user == GameManager.instance.characters[GameManager.instance.selectCharacterId])
        {
            Debug.Log("현재 플레이 중인 플레이어의 공격입니다.");
            ray = GameManager.instance.mainCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2)); // 카메라 중앙 방향 계산
        }
        else
        {
            Debug.Log("현재 플레이 중이지 않은 적/클론/나머지 캐릭터의 공격입니다.");
            Vector3 rayDirection = user.transform.forward;
            ray = new Ray(bulletPos.position, rayDirection);
        }

        Vector3 targetPoint;
        if (Physics.Raycast(ray, out RaycastHit hit, 10f, GameManager.instance.enemyLayerMask | GameManager.instance.mapLayerMask))
        {
            targetPoint = hit.point; // 충돌 지점
            // Debug.Log(hit.collider.gameObject.name + "와(과) 총알이 충돌했습니다.");
            if (hit.collider.gameObject.tag == "Enemy")
            {
                hit.collider.gameObject.GetComponent<EnemyController>().Damage(damage);
                Debug.Log("적이 총알에 맞았습니다!");
            }
        }
        else
        {
            targetPoint = ray.GetPoint(fireDistance); // 충돌하지 않으면 먼 거리로
        }

        // 총알 시각적 효과 생성
        GameObject firedBullet = Instantiate(bullet, bulletPos.position, Quaternion.identity);

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

        // Debug.Log("*** " + gameObject.transform.root.GetChild(0).GetChild(0).GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).IsName("Fire"));
        // if (!gameObject.transform.root.GetChild(0).GetChild(0).GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).IsName("Fire"))
        // {
        //     gameObject.transform.localRotation = Quaternion.Euler(originalRotation); // 총 각도 원래대로 변경(들고 있는 각도)
        // }

        // curCoroutine = null;
        canFire = true;
    }
}
