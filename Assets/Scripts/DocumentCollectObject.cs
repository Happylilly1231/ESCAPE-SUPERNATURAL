using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DocumentCollectObject : MonoBehaviour
{
    public enum DocumentCollectObjectType { Computer, LockedCabinet, Obstacle };
    public DocumentCollectObjectType documentCollectObjectType;
    public int interactiveCharacterId;
    public Collider triggerCollider;
    public int documentId;
    public DocumentCollectObject[] computers; // 같이 만져야 하는 컴퓨터 배열
    public bool[] isExists;
    public SecretPassageGate secretPassageGate;
    public Vector3 moveAmount;
    public int canMoveObstacleCharacterCnt;

    float hackingTime = 5f;
    float openTime = 2f;

    void Start()
    {
        isExists = new bool[3];
    }

    public void Activate()
    {
        switch (documentCollectObjectType)
        {
            case DocumentCollectObjectType.Computer:
                StartCoroutine(Hacking());
                break;
            case DocumentCollectObjectType.LockedCabinet:
                StartCoroutine(OpenLockedCabinet());
                break;
            case DocumentCollectObjectType.Obstacle:
                StartCoroutine(MoveObstacle());
                break;
        }
    }

    IEnumerator Hacking()
    {
        float timer = hackingTime;
        while (timer > 0)
        {
            for (int i = 0; i < computers.Length; i++)
            {
                if (!computers[i].isExists[interactiveCharacterId])
                    yield break;
            }

            Debug.Log("해킹 중...");
            timer -= Time.deltaTime;
            yield return null;
        }

        Debug.Log("해킹 완료!" + secretPassageGate);

        if (secretPassageGate != null)
        {
            secretPassageGate.Activate(); // 비밀 통로 오픈
        }
        else
        {
            GameManager.instance.havingDocuments[documentId] = true; // 문서 획득
        }
        for (int i = 0; i < computers.Length; i++)
        {
            computers[i].gameObject.GetComponent<SphereCollider>().enabled = false;
        }
    }

    IEnumerator OpenLockedCabinet()
    {
        float timer = openTime;
        while (timer > 0)
        {
            if (!isExists[interactiveCharacterId])
                yield break;

            Debug.Log("가져오는 중...");
            timer -= Time.deltaTime;
            yield return null;
        }

        Debug.Log("획득 완료!");
        GameManager.instance.havingDocuments[documentId] = true;
        gameObject.GetComponent<SphereCollider>().enabled = false;
    }

    IEnumerator MoveObstacle()
    {
        int existCnt = 0;
        for (int i = 0; i < 3; i++)
        {
            if (isExists[i])
            {
                existCnt++;
            }
        }

        if (existCnt < canMoveObstacleCharacterCnt)
        {
            Debug.Log("장애물을 이동시킬 만큼 사람이 충분하지 않습니다.");
        }
        else
        {
            Vector3 targetPos = transform.position + moveAmount;
            while (true)
            {
                if (transform.position == targetPos)
                {
                    break;
                }

                transform.position = Vector3.MoveTowards(transform.position, targetPos, 5f * Time.deltaTime);
                yield return null;
            }

            Debug.Log("장애물 이동 완료!");
            gameObject.GetComponent<SphereCollider>().enabled = false;
        }
    }
}
