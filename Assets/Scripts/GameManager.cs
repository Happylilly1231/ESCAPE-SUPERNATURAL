using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    public Camera mainCamera;
    public int selectCharacterId;

    public GameObject[] weaponItems; // 무기 아이템 배열(버릴 때 생성함)
    public GameObject[] items; // 아이템 배열(버릴 때 생성함)

    // 레이어 마스크
    public LayerMask mapLayerMask; // 맵 레이어 마스크
    public LayerMask playerLayerMask; // 플레이어 레이어 마스크
    public LayerMask enemyLayerMask; // 적 레이어 마스크

    GameObject[] characters; // 초능력자 캐릭터 배열
    GameObject[] characterCameras; // 초능력자 캐릭터 카메라 배열

    bool[] isCharactersClear; // 캐릭터가 현재 스테이지를 클리어했는지 여부
    public int curStageId; // 현재 스테이지 정보 (B3 : 1, B2: 2, B1: 3, 지상층: 4); ###저장 필요###

    public bool isAllowOnlyUIInput;

    public GameObject[] Characters { get => characters; set => characters = value; }
    public GameObject[] CharacterCameras { get => characterCameras; set => characterCameras = value; }

    public bool isFirstPickUpTimeBomb;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);

            // 레이어 마스크 할당
            mapLayerMask = LayerMask.GetMask("Map") | LayerMask.GetMask("InvisibleMap") | LayerMask.GetMask("CanTeleport");
            playerLayerMask = LayerMask.GetMask("Player");
            enemyLayerMask = LayerMask.GetMask("Enemy");

            // 배열 공간 초기화
            Characters = new GameObject[3];
            CharacterCameras = new GameObject[3];
            isCharactersClear = new bool[3];

            isAllowOnlyUIInput = false;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        curStageId = DataManager.instance.data.currentStageId; // 저장된 스테이지로 초기화
        SoundManager.instance.PlayBGM(0);
    }

    void Update()
    {

    }

    public void SelectCharacter(int id)
    {
        CharacterCameras[selectCharacterId].SetActive(false); // 선택되어있던 캐릭터 카메라 비활성화
        CharacterCameras[id].SetActive(true); // 선택한 캐릭터 카메라 활성화
        mainCamera = CharacterCameras[id].GetComponent<Camera>();
        selectCharacterId = id; // 선택한 캐릭터로 변경
        UIManager.instance.PlayingCharacterSetting(selectCharacterId); // 캐릭터 변경에 따른 기본 UI 초기화
    }

    public void Pause()
    {
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None; // 커서 해제
        Cursor.visible = true; // 커서 보이기
        isAllowOnlyUIInput = true;
    }

    public void Continue()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked; // 커서 고정
        Cursor.visible = false; // 커서 숨기기
        isAllowOnlyUIInput = false;
    }

    // 캐릭터가 현재 플레이하고 있는 캐릭터 따라오게 할지 여부 설정하는 함수
    public void SetFollowPlayingCharacter(int characterId)
    {
        if (Characters[characterId].GetComponent<PlayerController>().targetCharacter == null)
        {
            Debug.Log("추적 모드로 변경");
            Characters[characterId].GetComponent<PlayerController>().targetCharacter = Characters[selectCharacterId].transform; // 해당 캐릭터가 현재 플레이하고 있는 캐릭터를 따라오도록 함
            UIManager.instance.isFollowImgs[characterId].SetActive(true);
        }
        else
        {
            Debug.Log("추적 모드 해제");
            Characters[characterId].GetComponent<PlayerController>().targetCharacter = null;
            UIManager.instance.isFollowImgs[characterId].SetActive(false);
        }

    }

    // 게임 시작 함수
    public void GameStart()
    {
        // 저장한 게임 데이터 불러오기
        DataManager.instance.LoadGameData();
        Debug.Log("스테이지 " + curStageId + " 부터 게임 시작!");

        if (curStageId == 2)
        {
            GameEnding();
            return;
        }

        selectCharacterId = 0;
        Continue();
        SoundManager.instance.StopBGM();
        SoundManager.instance.PlayBGM(1);
        SceneManager.LoadScene("Stage" + curStageId.ToString());
    }

    // 새 게임 시작 함수
    public void NewGameStart()
    {
        Debug.Log("새 게임 시작!");
        // curStageId = 1; // 현재 스테이지를 처음 스테이지로 초기화

        // 새 게임 데이터 저장
        DataManager.instance.data = new GameData(); // 새로운 게임 데이터 만들기
        DataManager.instance.SaveGameData(); // 새로운 게임 데이터를 현재 데이터로 저장
        curStageId = DataManager.instance.data.currentStageId; // 초기화된 스테이지로 초기화

        // 게임 시작
        GameStart();
    }

    // 캐릭터 1명 스테이지 클리어 함수
    public void CharacterClear(int characterId)
    {
        for (int i = 0; i < QuestManager.instance.isQuestClear.Length; i++)
        {
            if (!QuestManager.instance.isQuestClear[i])
            {
                Debug.Log("퀘스트를 다 완료하지 않았으므로 클리어를 할 수 없습니다.");
                UIManager.instance.ShowGuide("퀘스트를 다 완료하지 않았으므로 클리어를 할 수 없습니다.", true);
                return;
            }
        }

        isCharactersClear[characterId] = true;
        for (int i = 0; i < 3; i++)
        {
            if (!isCharactersClear[i]) // 아직 못 깬 캐릭터가 있다면
            {
                characters[characterId].SetActive(false);
                // 그 캐릭터로 플레이 전환
                SelectCharacter(i);
                return;
            }
        }

        // 모든 캐릭터가 현재 스테이지를 클리어 했을 경우
        StageClear();
    }

    // 스테이지 클리어 함수
    void StageClear()
    {
        curStageId += 1; // 다음 스테이지가 현재 스테이지가 됨

        // 데이터는 클리어 시 저장(다음 스테이지로 넘어가기 전 현재 스테이지에서의 캐릭터 정보가 필요함, 클리어 화면으로 넘어가면 사라짐)
        GameDataUpdate(); // 데이터 갱신
        DataManager.instance.SaveGameData(); // 데이터 저장

        // if (curStageId == 2)
        // {
        //     GameEnding();
        // }

        Debug.Log("스테이지 클리어!");
        SoundManager.instance.PlayBGM(0);
        SceneManager.LoadScene("Clear"); // 클리어 화면으로 전환
    }

    // 게임 오버 함수
    public void GameOver()
    {
        Debug.Log("게임 오버!");
        SoundManager.instance.PlayBGM(0);
        SceneManager.LoadScene("GameOver"); // 게임 오버 화면으로 전환
    }

    // 데이터 갱신 함수
    void GameDataUpdate()
    {
        // 데이터 정리(배열을 리스트로)
        PlayerController[] playerControllers = new PlayerController[3];
        List<List<GameObject>> equipWeapons = new List<List<GameObject>>();
        List<List<int>> equipWeaponCurBulletCnts = new List<List<int>>();
        List<List<bool>> havingKeyCardLevels = new List<List<bool>>();
        for (int i = 0; i < 3; i++)
        {
            playerControllers[i] = Characters[i].GetComponent<PlayerController>();

            equipWeapons.Add(new List<GameObject>(playerControllers[i].equipWeapons));
            havingKeyCardLevels.Add(new List<bool>(playerControllers[i].havingKeyCardLevel));
            equipWeaponCurBulletCnts.Add(new List<int>());
            for (int j = 0; j < 3; j++)
            {
                if (playerControllers[i].equipWeapons[j] == null)
                {
                    equipWeaponCurBulletCnts[i].Add(0);
                }
                else
                {
                    equipWeaponCurBulletCnts[i].Add(playerControllers[i].equipWeapons[j].GetComponent<Weapon>().curBulletCnt);
                }
            }
        }

        // 데이터 갱신
        DataManager.instance.data = new GameData
        {
            currentStageId = curStageId,
            characterEquipWeapons = equipWeapons,
            characterEquipWeaponCurBulletCnts = equipWeaponCurBulletCnts,
            characterHavingKeyCardLevels = havingKeyCardLevels
        };
    }

    // 다음 스테이지로 이동 함수 (클리어 UI의 Continue 버튼에서 이용)
    public void MoveToNextStage()
    {
        Debug.Log("다음 스테이지 : " + curStageId + " (으)로 이동!");

        SceneManager.LoadScene("Ending");

        // GameStart();
    }

    // 메인 메뉴로 이동 함수 (Main Menu 버튼에서 이용)
    public void MoveToMainMenu()
    {
        SoundManager.instance.PlayBGM(0);
        SceneManager.LoadScene("MainMenu");
    }

    public void GameEnding()
    {
        SoundManager.instance.PlayBGM(0);
        SceneManager.LoadScene("Ending");
    }

    public void GameExit()
    {
        Application.Quit();
    }
}
