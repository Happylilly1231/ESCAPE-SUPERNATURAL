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

    public void ChangeWeaponImg(int equipWeaponId, int weaponId)
    {
        if (weaponId == -1)
            equipWeaponImgs[equipWeaponId].sprite = null;
        else
            equipWeaponImgs[equipWeaponId].sprite = weaponImgs[weaponId];
    }

    public void ResetCharacterBtn(int selectCharacterId)
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
    public void PlayingCharacterSetting(PlayerController playerController)
    {
        // 현재 선택한 플레이어의 무기로 이미지 변경
        for (int i = 0; i < 3; i++)
        {
            if (playerController.equipWeapons[i] == null)
                ChangeWeaponImg(i, -1);
            else
                ChangeWeaponImg(i, playerController.equipWeapons[i].GetComponent<Weapon>().weaponId);
        }

        characterName.text = playerController.gameObject.name;
        SetHpBar(playerController.CurHp / playerController.MaxHp);
    }

    public void SetHpBar(float value)
    {
        hpBar.value = value;
    }
}