using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DocumentCollectObject : MonoBehaviour
{
    public enum DocumentCollectObjectType { Computer, LockedCabinet, Obstacle };
    public DocumentCollectObjectType documentCollectObjectType;
    public int interactiveCharacterId;
    public Collider triggerCollider;
    public int documentId;
    public DocumentCollectObject[] computers; // 같이 만져야 하는 컴퓨터 배열
    public Canvas hackingCanvas;
    public Slider hackingBar;
    public TextMeshProUGUI hackingTxt;
    public bool[] isExists;
    public SecretPassageGate secretPassageGate;
    public Vector3 moveAmount;
    public int canMoveObstacleCharacterCnt;

    float hackingTime = 5f;

    void Start()
    {
        isExists = new bool[3];
    }

    public void Exist(int characterId, bool canUIUpdate)
    {
        isExists[characterId] = true;
        if (canUIUpdate)
        {
            string text = "";
            switch (documentCollectObjectType)
            {
                case DocumentCollectObjectType.Computer:
                    text = "분신술 능력을 사용하여 여러 대의 컴퓨터로 빠르게 해킹할 수 있습니다. 소요 시간 : 5초";
                    break;
                case DocumentCollectObjectType.LockedCabinet:
                    text = "물질통과 능력을 사용하여 증거를 꺼낼 수 있습니다.";
                    break;
                case DocumentCollectObjectType.Obstacle:
                    text = "장애물을 이동시키기 위해, 최소 2명이 필요합니다.";
                    break;
            }
            if (UIManager.instance.curGuideCoroutine == null)
                UIManager.instance.ShowGuide(text);
        }
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
        hackingCanvas.gameObject.SetActive(true);

        float timer = hackingTime;
        while (timer > 0)
        {
            for (int i = 0; i < computers.Length; i++)
            {
                if (!computers[i].isExists[interactiveCharacterId])
                {
                    hackingCanvas.gameObject.SetActive(false);
                    UIManager.instance.ShowGuide("해킹에 실패했습니다.");
                    hackingCanvas.gameObject.SetActive(false);
                    yield break;
                }
            }

            // 해킹바 플레이어 화면 바라보게 하기
            hackingCanvas.transform.LookAt(hackingCanvas.transform.position + GameManager.instance.mainCamera.transform.rotation * Vector3.forward, GameManager.instance.mainCamera.transform.rotation * Vector3.up);

            hackingBar.value = (hackingTime - timer) / hackingTime;

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
            QuestManager.instance.havingDocuments[documentId] = true; // 문서 획득

            if (documentId == 0)
            {
                UIManager.instance.ShowGuide("제2실험실에서 비인간적인 실험 내용에 관한 증거를 획득했습니다.");
                QuestManager.instance.QuestClear(1, 1); // 스테이지 1의 세번째 퀘스트 완료
            }
        }

        for (int i = 0; i < computers.Length; i++)
        {
            computers[i].gameObject.GetComponent<SphereCollider>().enabled = false;
        }

        hackingCanvas.gameObject.SetActive(false);
    }

    IEnumerator OpenLockedCabinet()
    {
        while (isExists[interactiveCharacterId])
        {
            Debug.Log(GameManager.instance.Characters[interactiveCharacterId].GetComponent<PhasingAbility>().isPhasing + " | " + (GameManager.instance.selectCharacterId == interactiveCharacterId));
            if (GameManager.instance.Characters[interactiveCharacterId].GetComponent<PhasingAbility>().isPhasing && GameManager.instance.selectCharacterId == interactiveCharacterId)
            {
                Debug.Log("획득 완료!");
                QuestManager.instance.havingDocuments[documentId] = true;

                if (documentId == 1)
                {
                    UIManager.instance.ShowGuide("샘플저장소에서 불법적인 약물 실험에 관한 증거를 획득했습니다.");
                    QuestManager.instance.QuestClear(1, 2); // 스테이지 1의 세번째 퀘스트 완료
                }
                else if (documentId == 2)
                {
                    UIManager.instance.ShowGuide("제2훈련실에서 불법적인 무기들에 대한 증거를 획득했습니다.");
                    QuestManager.instance.QuestClear(1, 3); // 스테이지 1의 네번째 퀘스트 완료
                }

                gameObject.GetComponent<SphereCollider>().enabled = false;

                break;
            }
            yield return null;
        }
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
            if (UIManager.instance.curGuideCoroutine != null)
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
