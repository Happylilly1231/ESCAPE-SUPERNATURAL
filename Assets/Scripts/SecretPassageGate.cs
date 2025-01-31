using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SecretPassageGate : MonoBehaviour
{
    public Vector3 moveAmount;

    public void Activate()
    {
        StartCoroutine(GateOpen());
    }

    IEnumerator GateOpen()
    {
        Debug.Log("비밀 통로 오픈");
        Vector3 targetPos = transform.position + moveAmount;
        while (true)
        {
            if (transform.position == targetPos)
            {
                Destroy(gameObject);
                break;
            }

            transform.position = Vector3.MoveTowards(transform.position, targetPos, 5f * Time.deltaTime);
            yield return null;
        }
    }
}
