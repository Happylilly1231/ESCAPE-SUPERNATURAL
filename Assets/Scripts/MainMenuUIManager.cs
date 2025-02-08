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
    public GameObject settingUI;
    public GameObject controlUI;
    public GameObject soundUI;
    public Button controlUIBtn;
    public Button soundUIBtn;
    public Slider bgmSlider; // BGM 볼륨 슬라이더
    public Slider sfxSlider; // SFX 볼륨 슬라이더

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
                stageTxt = "현재 층 : B3";
                break;
            case 2:
                stageTxt = "현재 층 : B2";
                break;
            case 3:
                stageTxt = "현재 층 : B1";
                break;
            case 4:
                stageTxt = "현재 층 : 지상층";
                break;
        }
        curStageTxt.text = stageTxt;

        SoundManager.instance.bgmSlider = bgmSlider;
        SoundManager.instance.sfxSlider = sfxSlider;
        SoundManager.instance.Init();
    }

    public void ShowSettingUI()
    {
        settingUI.SetActive(true);
    }

    public void HideSettingUI()
    {
        settingUI.SetActive(false);
    }

    public void ShowControlUI()
    {
        soundUI.SetActive(false);
        controlUI.SetActive(true);
    }

    public void ShowSoundUI()
    {
        soundUI.SetActive(true);
        controlUI.SetActive(false);
    }
}
