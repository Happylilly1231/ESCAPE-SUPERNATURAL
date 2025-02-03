using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager instance;
    public TextMeshProUGUI tutorialTextPart;
    public GameObject tutorialUI;
    public GameObject nextTextPart;
    public GameObject skipBtn;
    IEnumerator curCoroutine;

    string[] tutorialTexts = {
        "Derek은 물질통과 초능력 소유자입니다. 2초 동안 물체를 통과할 수 있습니다.\n현재는 지하이기 때문에 외부벽은 통과할 수 없습니다.",
        "R키를 눌러 능력을 사용하고 문을 통과해보세요.|0|R",
        "E키를 눌러 캐릭터를 전환할 수 있습니다. Sophia로 전환해 보세요.|1|E",
        "Sophia는 분신술 초능력 소유자입니다. 최대 3명까지 분신을 만들 수 있습니다. 1명은 40초, 2명은 30초, 3명은 20초 동안 유지 가능합니다.",
        "R키를 눌러 능력을 사용하여 분신을 만들어보세요.|1|R",
        "넘버패드 1, 2, 3키 중 하나를 눌러 따라오게 할 분신을 선택하세요.|1|123",
        "다시 같은 방법으로 진행하면 따라오는 것을 해제할 수 있습니다.",
        "넘버패드 4, 5, 6키 중 하나를 눌러 이동을 원하는 분신을 선택하세요.|1|456",
        "화면을 움직여 목표 위치를 이동시키고, T키를 눌러 위치를 확정하세요.",
        "E키를 눌러 Ethan을 선택하세요.|2|E",
        "Ethan은 순간이동 초능력 소유자입니다. 1명을 데리고 순간이동할 수도 있습니다. 높은 곳으로 이동할 수 있습니다.",
        "R키를 눌러 능력을 사용하세요.|2|R",
        "다른 캐릭터 가까이에 가면 목표 위치 원이 커지고, 같이 이동할 수 있는 상태가 됩니다.",
        "화면을 움직여 목표 위치를 이동시키고, T를 눌러 위치를 확정하고 순간이동하세요.|2|T",
        "M키를 눌러 맵을 확인할 수 있습니다.|0|M",
        "적은 빨간색 원 안으로 들어온 사람과 시야각 내 시야 거리 안에 있는 사람을 공격합니다.",
        "타겟이 장애물에 가려지거나 시야 거리 밖으로 도망가면 추격을 멈추고 다시 순찰합니다.",
        "연구원도 같은 범위로 사람을 감지합니다. 연구원과 적 모두 시야각 바깥에서 Ctrl 키를 눌러 천천히 이동하면 들키지 않고 다가갈 수 있습니다.",
        "연구원에게서 카드키를 획득하려면 빨간 표시 원 내에서 상호작용 F키를 누릅니다. 이후 3초 이내에 시야 거리 바깥으로 탈출합니다.",
        "F키로 무기를 줍고, Q키로 무기를 버릴 수 있습니다.",
        "위에 있는 숫자키 1/2/3을 눌러 장착된 무기를 들거나 내려놓을 수 있습니다.",
        "위에 있는 숫자키 4/5를 눌러 나머지 캐릭터의 따라오기 여부를 설정할 수 있습니다.",
        "조작 방법은 ESC키를 눌러 설정 창에서 다시 확인할 수 있습니다.",
        "E키를 눌러 퀘스트를 확인하고 모두 클리어한 뒤, 위층으로 탈출하세요.",
    };

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (DataManager.instance.data.haveToShowTutorial)
        {
            tutorialUI.SetActive(true);
            // tutorialTextPart.text = tutorialTexts[0];
            Debug.Log("튜토리얼 시작 " + tutorialTexts.Length);
            curCoroutine = ShowTutorial();
            StartCoroutine(curCoroutine);
        }
        else
        {
            tutorialUI.SetActive(false);
        }
    }

    void Update()
    {
        if (!GameManager.instance.isAllowOnlyUIInput)
        {
            if (Input.GetKeyDown(KeyCode.Z))
            {
                Skip();
            }
        }
    }

    void Skip()
    {
        StopCoroutine(curCoroutine);
        tutorialUI.SetActive(false);
        DataManager.instance.data.haveToShowTutorial = false;
        DataManager.instance.SaveGameData();
        Debug.Log("튜토리얼 스킵");
    }

    IEnumerator ShowTutorial()
    {
        for (int i = 0; i < tutorialTexts.Length; i++)
        {
            string[] splitTexts = tutorialTexts[i].Split('|');
            tutorialTextPart.text = splitTexts[0];

            if (i == tutorialTexts.Length - 1) // 마지막 메시지일 때
            {
                nextTextPart.GetComponent<TextMeshProUGUI>().text = "End : F키";
            }

            switch (splitTexts.Length)
            {
                case 1:
                    nextTextPart.SetActive(true);
                    while (!Input.GetKeyDown(KeyCode.F) || GameManager.instance.isAllowOnlyUIInput)
                    {
                        yield return null;
                    }
                    break;
                case 2:
                    nextTextPart.SetActive(false);
                    while (GameManager.instance.selectCharacterId != int.Parse(splitTexts[1]))
                    {
                        yield return null;
                    }
                    break;
                case 3:
                    nextTextPart.SetActive(false);
                    bool isInput = false;

                    while (true)
                    {
                        if ((GameManager.instance.selectCharacterId == int.Parse(splitTexts[1])
                        || splitTexts[1] == "0") && isInput)
                        {
                            break;
                        }

                        if (!GameManager.instance.isAllowOnlyUIInput)
                        {
                            if (splitTexts[2] == "R")
                            {
                                isInput = Input.GetKeyDown(KeyCode.R);
                            }
                            else if (splitTexts[2] == "E")
                            {
                                isInput = Input.GetKeyDown(KeyCode.E);
                            }
                            else if (splitTexts[2] == "123")
                            {
                                isInput = Input.GetKeyDown(KeyCode.Keypad1) || Input.GetKeyDown(KeyCode.Keypad2) || Input.GetKeyDown(KeyCode.Keypad3);
                            }
                            else if (splitTexts[2] == "456")
                            {
                                isInput = Input.GetKeyDown(KeyCode.Keypad4) || Input.GetKeyDown(KeyCode.Keypad5) || Input.GetKeyDown(KeyCode.Keypad6);
                            }
                            else if (splitTexts[2] == "T")
                            {
                                isInput = Input.GetKeyDown(KeyCode.T);
                            }
                            else if (splitTexts[2] == "M")
                            {
                                isInput = Input.GetKeyDown(KeyCode.M);
                            }
                        }

                        yield return null;
                    }
                    break;
            }
            yield return null;
        }
        tutorialUI.SetActive(false);
        DataManager.instance.data.haveToShowTutorial = false;
        DataManager.instance.SaveGameData();
        Debug.Log("튜토리얼 끝");
    }
}
