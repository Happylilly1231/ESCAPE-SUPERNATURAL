using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuestManager : MonoBehaviour
{
    public static QuestManager instance;
    public string[] questTexts;
    public GameObject questContent;
    public GameObject questPrefab;
    public Sprite checkImg;
    public bool[] isQuestClear;

    public bool[] havingDocuments; // 문서 획득 여부

    GameObject[] quests;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;

            havingDocuments = new bool[3];
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        isQuestClear = new bool[questTexts.Length];
        quests = new GameObject[questTexts.Length];

        for (int i = 0; i < questTexts.Length; i++)
        {
            quests[i] = Instantiate(questPrefab, questContent.transform);
            quests[i].transform.GetChild(1).GetComponent<TextMeshProUGUI>().text = questTexts[i];
        }
    }

    public void QuestClear(int stageId, int num)
    {
        if (stageId == GameManager.instance.curStageId && !isQuestClear[num])
        {
            Debug.Log(num + 1 + "번째 퀘스트 완료!");
            isQuestClear[num] = true;
            quests[num].transform.GetChild(0).GetComponent<Image>().sprite = checkImg;
            quests[num].transform.GetChild(1).GetComponent<TextMeshProUGUI>().color = Color.green;
        }
    }
}
