using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InteractiveObject : MonoBehaviour
{
    public enum InteractiveObjectType { Computer, LockedCabinet, Obstacle, Shelf, TimeBomb };
    public InteractiveObjectType interactiveObjectType;
    public int interactiveCharacterId;
    public Collider triggerCollider;
    public int documentId;
    public InteractiveObject[] setObjects; // 세트 오브젝트 배열
    public Canvas canvas;
    public Slider sliderBar;
    public GameObject[] existObjs;
    public SecretPassageGate secretPassageGate;
    public Vector3 moveAmount;
    public int canMoveObstacleCharacterCnt;
    public int itemId;

    float hackingTime = 4f;
    float settingTime = 5f;

    void Start()
    {
        existObjs = new GameObject[6];
    }

    // public void Exist(int characterId, bool canUIUpdate)
    // {
    //     isExists[characterId] = true;
    //     if (canUIUpdate)
    //     {
    //         string text = "";
    //         switch (documentCollectObjectType)
    //         {
    //             case DocumentCollectObjectType.Computer:
    //                 text = "Sophia가 분신술 능력을 사용하여 여러 대의 컴퓨터로 빠르게 해킹할 수 있습니다. 나머지 컴퓨터 앞에 분신이 있다면 F키를 누르세요. 소요 시간 : 4초";
    //                 break;
    //             case DocumentCollectObjectType.LockedCabinet:
    //                 text = "F키를 누르고 R키로 물질통과 능력을 사용하여 증거를 꺼낼 수 있습니다.";
    //                 break;
    //             case DocumentCollectObjectType.Obstacle:
    //                 text = "장애물을 이동시키기 위해, 최소 2명이 필요합니다. F키를 사용하여 이동시키세요.";
    //                 break;
    //         }
    //         UIManager.instance.ShowGuide(text, false);
    //     }
    // }

    public void Activate(PlayerController playerController)
    {
        switch (interactiveObjectType)
        {
            case InteractiveObjectType.Computer:
                StartCoroutine(Hacking());
                break;
            case InteractiveObjectType.LockedCabinet:
                StartCoroutine(OpenLockedCabinet());
                break;
            case InteractiveObjectType.Obstacle:
                StartCoroutine(MoveObstacle());
                break;
            case InteractiveObjectType.Shelf:
                FindOnShelf(playerController);
                break;
            case InteractiveObjectType.TimeBomb:
                StartCoroutine(SetTimeBomb());
                break;
        }
    }

    IEnumerator Hacking()
    {
        canvas.gameObject.SetActive(true);

        float timer = hackingTime;
        while (timer > 0)
        {
            for (int i = 0; i < setObjects.Length; i++)
            {
                if (setObjects[i].existObjs[interactiveCharacterId] == null && setObjects[i].existObjs[3] == null && setObjects[i].existObjs[4] == null && setObjects[i].existObjs[5] == null)
                {
                    canvas.gameObject.SetActive(false);
                    UIManager.instance.ShowGuide("해킹에 실패했습니다.", true);
                    yield break;
                }
            }

            // 해킹바 플레이어 화면 바라보게 하기
            canvas.transform.LookAt(canvas.transform.position + GameManager.instance.mainCamera.transform.rotation * Vector3.forward, GameManager.instance.mainCamera.transform.rotation * Vector3.up);

            sliderBar.value = (hackingTime - timer) / hackingTime;

            Debug.Log("해킹 중...");
            timer -= Time.deltaTime;
            yield return null;
        }

        Debug.Log("해킹 완료!" + secretPassageGate);

        if (secretPassageGate != null)
        {
            UIManager.instance.ShowGuide("비밀 통로가 열렸습니다.", true);
            secretPassageGate.Activate(); // 비밀 통로 오픈
            QuestManager.instance.QuestClear(1, 4); // 스테이지 1의 다섯번째 퀘스트 완료
        }
        else
        {
            QuestManager.instance.havingDocuments[documentId] = true; // 문서 획득

            if (documentId == 0)
            {
                UIManager.instance.ShowGuide("제2실험실에서 비인간적인 실험 내용에 관한 증거를 획득했습니다.", true);
                QuestManager.instance.QuestClear(1, 1); // 스테이지 1의 첫번째 퀘스트 완료
            }
            else if (documentId == 3)
            {
                UIManager.instance.ShowGuide("데이터센터에서 중요한 다량의 증거를 획득했습니다.", true);
                QuestManager.instance.QuestClear(2, 2); // 스테이지 2의 세번째 퀘스트 완료
            }
        }

        for (int i = 0; i < setObjects.Length; i++)
        {
            setObjects[i].gameObject.GetComponent<SphereCollider>().enabled = false;
        }

        canvas.gameObject.SetActive(false);
    }

    IEnumerator OpenLockedCabinet()
    {
        while (existObjs[interactiveCharacterId] != null)
        {
            Debug.Log(GameManager.instance.Characters[interactiveCharacterId].GetComponent<PhasingAbility>().isPhasing + " | " + (GameManager.instance.selectCharacterId == interactiveCharacterId));
            if (GameManager.instance.Characters[interactiveCharacterId].GetComponent<PhasingAbility>().isPhasing && GameManager.instance.selectCharacterId == interactiveCharacterId)
            {
                Debug.Log("획득 완료!");
                QuestManager.instance.havingDocuments[documentId] = true;

                if (documentId == 1)
                {
                    UIManager.instance.ShowGuide("샘플저장소에서 불법적인 약물 실험에 관한 증거를 획득했습니다.", true);
                    QuestManager.instance.QuestClear(1, 2); // 스테이지 1의 세번째 퀘스트 완료
                }
                else if (documentId == 2)
                {
                    UIManager.instance.ShowGuide("제2훈련실에서 불법적인 무기들에 대한 증거를 획득했습니다.", true);
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
            if (existObjs[i] != null)
            {
                existCnt++;
            }
        }

        if (existCnt < canMoveObstacleCharacterCnt)
        {
            UIManager.instance.ShowGuide("장애물을 이동시킬 만큼 사람이 충분하지 않습니다.", true);
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

            UIManager.instance.ShowGuide("장애물 이동을 완료했습니다.", true);
            gameObject.GetComponent<SphereCollider>().enabled = false;
        }
    }

    void FindOnShelf(PlayerController playerController)
    {
        // bool isFind = false;
        // for (int i = 0; i < 3; i++)
        // {
        //     if (isExists[i])
        //     {
        //         isFind = true;
        //         break;
        //     }
        // }

        // if (isFind)
        // {
        //     if (itemId == -1)
        //     {
        //         UIManager.instance.ShowGuide("원하는 아이템을 찾지 못했습니다.", true);
        //     }
        //     else
        //     {
        //         UIManager.instance.ShowGuide("시한폭탄 설치 장치를 획득했습니다!", true);

        //     }
        // }

        if (itemId == -1)
        {
            UIManager.instance.ShowGuide("원하는 아이템을 찾지 못했습니다.", true);
        }
        else
        {
            UIManager.instance.ShowGuide("시한폭탄 설치 장치를 획득했습니다!", true);
            playerController.PickUpItem(GameManager.instance.items[1]);
            QuestManager.instance.QuestClear(2, 1); // 스테이지 2의 두번째 퀘스트 완료
        }
    }

    IEnumerator SetTimeBomb()
    {
        canvas.gameObject.SetActive(true);

        float timer = settingTime;
        while (timer > 0)
        {
            for (int i = 0; i < setObjects.Length; i++)
            {
                bool flag = false;
                for (int j = 0; j < existObjs.Length; j++)
                {
                    if (existObjs[j] != null)
                    {
                        if (existObjs[j].tag == "Player")
                        {
                            if ((existObjs[j].GetComponent<PlayerController>().havingItemIds[0] == 0 && existObjs[j].GetComponent<PlayerController>().havingItemIds[1] == 1)
                            || (existObjs[j].GetComponent<PlayerController>().havingItemIds[0] == 1 && existObjs[j].GetComponent<PlayerController>().havingItemIds[1] == 0))
                            {
                                flag = true;
                                break;
                            }
                        }
                        else if (existObjs[j].tag == "Clone")
                        {
                            if ((existObjs[j].GetComponent<CloneController>().havingItemIds[0] == 0 && existObjs[j].GetComponent<CloneController>().havingItemIds[1] == 1)
                            || (existObjs[j].GetComponent<CloneController>().havingItemIds[0] == 1 && existObjs[j].GetComponent<CloneController>().havingItemIds[1] == 0))
                            {
                                flag = true;
                                break;
                            }
                        }
                    }
                }

                if (!flag)
                {
                    canvas.gameObject.SetActive(false);
                    UIManager.instance.ShowGuide("설치에 실패했습니다.", true);
                    yield break;
                }
            }

            // 해킹바 플레이어 화면 바라보게 하기
            canvas.transform.LookAt(canvas.transform.position + GameManager.instance.mainCamera.transform.rotation * Vector3.forward, GameManager.instance.mainCamera.transform.rotation * Vector3.up);

            sliderBar.value = (settingTime - timer) / settingTime;

            Debug.Log("설치 중...");
            timer -= Time.deltaTime;
            yield return null;
        }

        Debug.Log("설치 완료!" + secretPassageGate);

        QuestManager.instance.QuestClear(2, 3); // 스테이지 2의 네번째 퀘스트 완료

        for (int i = 0; i < setObjects.Length; i++)
        {
            setObjects[i].gameObject.GetComponent<SphereCollider>().enabled = false;
        }

        canvas.gameObject.SetActive(false);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Player")
        {
            existObjs[other.gameObject.GetComponent<PlayerController>().characterId] = other.gameObject;
            if (other.gameObject.GetComponent<PlayerController>().canUIUpdate)
            {
                string text = "";
                switch (interactiveObjectType)
                {
                    case InteractiveObjectType.Computer:
                        text = "Sophia가 분신술 능력을 사용하여 여러 대의 컴퓨터로 빠르게 해킹할 수 있습니다. 나머지 컴퓨터 앞에 분신이 있다면 F키를 누르세요. 소요 시간 : 4초";
                        break;
                    case InteractiveObjectType.LockedCabinet:
                        text = "F키를 누르고 R키로 물질통과 능력을 사용하여 증거를 꺼낼 수 있습니다.";
                        break;
                    case InteractiveObjectType.Obstacle:
                        text = "장애물을 이동시키기 위해, 최소 2명이 필요합니다. F키를 사용하여 이동시키세요.";
                        break;
                    case InteractiveObjectType.Shelf:
                        text = "F키를 눌러 아이템을 탐색하세요.";
                        break;
                    case InteractiveObjectType.TimeBomb:
                        text = "시한폭탄 설치 장치와 시한 폭탄을 모두 들고 있어야 합니다. 지정된 위치에 맞는 아이템을 든 캐릭터가 모두 존재하면 F키를 눌러 시한폭탄을 설치하세요. 소요 시간 : 4초";
                        break;
                }
                UIManager.instance.ShowGuide(text, false);
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.gameObject.tag == "Player")
        {
            existObjs[other.gameObject.GetComponent<PlayerController>().characterId] = null;
        }
    }
}
