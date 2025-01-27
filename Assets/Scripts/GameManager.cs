using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    public GameObject[] characterCameras;
    public Camera mainCamera;
    public int selectCharacterId;

    public GameObject[] weaponItems; // 무기 아이템 배열(버릴 때 생성함)

    public GameObject[] characters; // 초능력자 캐릭터 배열

    // 레이어 마스크
    public LayerMask mapLayerMask; // 맵 레이어 마스크
    public LayerMask playerLayerMask; // 플레이어 레이어 마스크
    public LayerMask enemyLayerMask; // 적 레이어 마스크

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

        // 레이어 마스크 할당
        mapLayerMask = LayerMask.GetMask("Map");
        playerLayerMask = LayerMask.GetMask("Player");
        enemyLayerMask = LayerMask.GetMask("Enemy");
    }

    void Start()
    {
        characterCameras[0].SetActive(true);
        mainCamera = characterCameras[0].GetComponent<Camera>();
        characterCameras[1].SetActive(false);
        characterCameras[2].SetActive(false);
        UIManager.instance.PlayingCharacterSetting(selectCharacterId);
    }

    void Update()
    {

    }

    public void SelectCharacter(int id)
    {
        characterCameras[selectCharacterId].SetActive(false); // 선택되어있던 캐릭터 카메라 비활성화
        characterCameras[id].SetActive(true); // 선택한 캐릭터 카메라 활성화
        mainCamera = characterCameras[id].GetComponent<Camera>();
        selectCharacterId = id; // 선택한 캐릭터로 변경
        UIManager.instance.PlayingCharacterSetting(selectCharacterId); // 캐릭터 변경에 따른 기본 UI 초기화
    }

    public void Pause()
    {
        Time.timeScale = 0f;
    }

    public void Continue()
    {
        Time.timeScale = 1f;
    }

    // 캐릭터가 현재 플레이하고 있는 캐릭터 따라오게 할지 여부 설정하는 함수
    public void SetFollowPlayingCharacter(int characterId)
    {
        if (characters[characterId].GetComponent<PlayerController>().targetCharacter == null)
        {
            Debug.Log("추적 모드로 변경");
            characters[characterId].GetComponent<PlayerController>().targetCharacter = characters[selectCharacterId].transform; // 해당 캐릭터가 현재 플레이하고 있는 캐릭터를 따라오도록 함
        }
        else
        {
            Debug.Log("추적 모드 해제");
            characters[characterId].GetComponent<PlayerController>().targetCharacter = null;
        }

    }
}
