using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameOverUIManager : MonoBehaviour
{
    public Button continueBtn; // 이어 하기 버튼
    public Button mainMenuBtn; // 메인메뉴 버튼

    void Start()
    {
        Cursor.lockState = CursorLockMode.None; // 커서 해제
        Cursor.visible = true; // 커서 보이기

        continueBtn.onClick.AddListener(GameManager.instance.GameStart); // 이어 하기 버튼 함수 지정(현재 저장된 스테이지에서 시작)
        mainMenuBtn.onClick.AddListener(GameManager.instance.MoveToMainMenu); // 메인 메뉴 버튼 함수 지정(메인 메뉴로 이동)
    }
}
