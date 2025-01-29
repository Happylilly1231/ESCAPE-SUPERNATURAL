using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ClearUIManager : MonoBehaviour
{
    public Button nextStageBtn; // 다음 스테이지 버튼
    public Button mainMenuBtn; // 메인메뉴 버튼
    public TextMeshProUGUI clearTxt; // 클리어 메세지

    void Start()
    {
        Cursor.lockState = CursorLockMode.None; // 커서 해제
        Cursor.visible = true; // 커서 보이기

        nextStageBtn.onClick.AddListener(GameManager.instance.MoveToNextStage); // 다음 스테이지 버튼 함수 지정(다음 스테이지로 이동)
        mainMenuBtn.onClick.AddListener(GameManager.instance.MoveToMainMenu); // 메인 메뉴 버튼 함수 지정(메인 메뉴로 이동)

        // 클리어 메세지 설정
        string stageTxt = "";
        switch (GameManager.instance.curStageId)
        {
            case 1:
                stageTxt = "B3";
                break;
            case 2:
                stageTxt = "B2";
                break;
            case 3:
                stageTxt = "B1";
                break;
            case 4:
                stageTxt = "Ground Floor";
                break;
        }
        clearTxt.text = stageTxt + " Stage Clear";
    }
}
