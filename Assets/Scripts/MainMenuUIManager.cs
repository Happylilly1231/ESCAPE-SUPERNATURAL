using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuUIManager : MonoBehaviour
{
    public Button continueBtn; // 이어하기 버튼
    public Button newGameBtn; // 새 게임 버튼
    public Button gameExitBtn; // 게임 종료 버튼
    public TextMeshProUGUI curStageTxt; // 스테이지 텍스트

    void Start()
    {
        // 스테이지가 2 이상일 때만 이어 하기 버튼 활성화
        if (GameManager.instance.curStageId == 1)
            continueBtn.interactable = false;
        else
            continueBtn.interactable = true;

        continueBtn.onClick.AddListener(GameManager.instance.GameStart); // 이어 하기 버튼 함수 지정(현재 저장된 스테이지에서 시작)
        newGameBtn.onClick.AddListener(GameManager.instance.NewGameStart); // 새 게임 버튼 함수 지정(현재 저장된 스테이지를 1로 초기화)
        gameExitBtn.onClick.AddListener(GameManager.instance.GameExit);

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
        curStageTxt.text = stageTxt;
    }
}
