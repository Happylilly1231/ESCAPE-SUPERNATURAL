using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    public int damage;

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Enemy")
        {
            Debug.Log("적을 맞췄습니다.");
            other.gameObject.GetComponent<EnemyController>().Damage(damage);
        }
        Destroy(gameObject);
    }
}
