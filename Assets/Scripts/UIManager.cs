using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager instance;
    public GameObject playerUI;
    public GameObject controlsWindow;
    public GameObject interactionUI;

    // Main UI
    public Image[] equipWeaponImgs; // 장착 무기 이미지 배열
    public Sprite[] weaponImgs; // 무기 스프라이트 배열
    public TextMeshProUGUI characterName; // 캐릭터 이름
    public GameObject[] characterBtns; // 따라오게 할 캐릭터 선택 배열
    public Slider hpBar; // 캐릭터 체력 바

    public GameObject hpBarPrefab; // 체력 바 프리팹

    public Image crosshair; // 조준점 이미지

    // 초능력 쿨타임
    public Image cooldownImg; // 초능력 쿨타임 이미지
    public Image cooldownDisableImg; // 초능력 쿨타임 남은 시간 이미지
    public TextMeshProUGUI cooldownRemainTimeText; // 초능력 쿨타임 남은 시간 텍스트

    // 초능력 지속 시간
    public Image durationImg; // 초능력 지속시간 이미지
    public Image duraitonDisableImg; // 초능력 남은 지속시간 이미지
    public TextMeshProUGUI durationRemainTimeText; // 초능력 남은 지속시간 텍스트

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

    void Update()
    {
        // 플레이어 UI 켜기/끄기
        if (Input.GetKeyDown(KeyCode.M))
        {
            playerUI.SetActive(!playerUI.activeSelf);

            if (Cursor.lockState == CursorLockMode.Locked)
            {
                GameManager.instance.Pause();
                Cursor.lockState = CursorLockMode.None; // 커서 해제
                Cursor.visible = true; // 커서 보이기
            }
            else
            {
                GameManager.instance.Continue();
                Cursor.lockState = CursorLockMode.Locked; // 커서 고정
                Cursor.visible = false; // 커서 숨기기
            }
        }

        // 컨트롤 UI 켜기/끄기
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            controlsWindow.SetActive(!controlsWindow.activeSelf);

            if (Cursor.lockState == CursorLockMode.Locked)
            {
                GameManager.instance.Pause();
                Cursor.lockState = CursorLockMode.None; // 커서 해제
                Cursor.visible = true; // 커서 보이기
            }
            else
            {
                GameManager.instance.Continue();
                Cursor.lockState = CursorLockMode.Locked; // 커서 고정
                Cursor.visible = false; // 커서 숨기기
            }
        }
    }

    public void ShowInteractionUI()
    {
        interactionUI.SetActive(true);
    }

    public void HideInteractionUI()
    {
        interactionUI.SetActive(false);
    }

    // 무기 이미지 변경 함수
    public void ChangeWeaponImg(int equipWeaponId, int weaponId)
    {
        if (weaponId == -1)
            equipWeaponImgs[equipWeaponId].sprite = null;
        else
            equipWeaponImgs[equipWeaponId].sprite = weaponImgs[weaponId];
    }

    // 따라오기 선택 캐릭터 버튼 변경 함수
    public void ResetFollowCharacterBtn(int selectCharacterId)
    {
        for (int i = 0; i < 3; i++)
        {
            if (i == selectCharacterId)
                characterBtns[i].SetActive(false);
            else
                characterBtns[i].SetActive(true);
        }
    }

    // 캐릭터 변경했을 때 기본 UI 설정 함수
    public void PlayingCharacterSetting(int selectCharacterId)
    {
        PlayerController playerController = GameManager.instance.characters[selectCharacterId].GetComponent<PlayerController>();

        // 따라오기 선택 캐릭터 버튼 변경
        ResetFollowCharacterBtn(selectCharacterId);

        // 현재 선택한 플레이어의 무기로 이미지 변경
        for (int i = 0; i < 3; i++)
        {
            if (playerController.equipWeapons[i] == null)
                ChangeWeaponImg(i, -1);
            else
                ChangeWeaponImg(i, playerController.equipWeapons[i].GetComponent<Weapon>().weaponId);
        }

        characterName.text = playerController.gameObject.name; // 캐릭터 이름 설정
        SetHpBar(playerController.CurHp / playerController.MaxHp); // 체력바 설정

        // 초능력 쿨타임 변경
        // 초능력 쿨타임 이미지 바꾸기 코드 추가
        cooldownDisableImg.fillAmount = 0;
        cooldownRemainTimeText.text = "";

        // 초능력 지속시간 변경
        // 초능력 지속시간 이미지 바꾸기 코드 추가
        duraitonDisableImg.fillAmount = 0;
        durationRemainTimeText.text = "";
        Debug.Log("---" + cooldownRemainTimeText.text);
    }

    public void SetHpBar(float value)
    {
        hpBar.value = value;
    }
}