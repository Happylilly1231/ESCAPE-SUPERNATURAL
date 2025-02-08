using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager instance;

    public GameObject mapUI;
    public GameObject settingUI;
    public Button mainMenuBtn;
    public GameObject playerUI;

    // Main UI
    public Image[] equipWeaponImgs; // 장착 무기 이미지 배열
    public Image[] equipWeaponOutlines; // 장착 무기 아웃라인 배열
    public Sprite[] weaponImgs; // 무기 스프라이트 배열
    public Image characterProfileImg;
    public TextMeshProUGUI characterName; // 캐릭터 이름
    public GameObject[] keyCardLevelImgs; // 카드키 레벨 이미지 배열
    public GameObject[] characterBtns; // 따라오게 할 캐릭터 선택 배열
    public GameObject[] isFollowImgs; // 캐릭터가 따라오고 있는 중인지 표시하는 이미지
    public Slider hpBar; // 캐릭터 체력 바
    public TextMeshProUGUI hpTxt;
    public GameObject hpBarPrefab; // 체력 바 프리팹
    public Image crosshair; // 조준점 이미지
    public GameObject cloneUI;
    public Image[] cloneImgs;
    public GameObject bulletCntUI;
    public TextMeshProUGUI bulletCntTxt;
    public GameObject researcherTimeAttackUI; // 연구원 카드키 획득 타임어택 쿨타임 이미지
    public Image timeAttackDisableImg; // 연구원 카드키 획득 타임어택 쿨타임 남은 시간 이미지
    public TextMeshProUGUI timeAttackRemainTimeText; // 연구원 카드키 획득 초능력 쿨타임 남은 시간 텍스트
    public GameObject havingItemUI;
    public Image[] havingItemImgs; // 가지고 있는 아이템 이미지 배열
    public Sprite[] itemImgs; // 아이템 스프라이트 배열

    // 초능력 쿨타임
    public Image cooldownImg; // 초능력 쿨타임 이미지
    public Image cooldownDisableImg; // 초능력 쿨타임 남은 시간 이미지
    public TextMeshProUGUI cooldownRemainTimeText; // 초능력 쿨타임 남은 시간 텍스트

    // 초능력 지속 시간
    public Image durationImg; // 초능력 지속시간 이미지
    public Image duraitonDisableImg; // 초능력 남은 지속시간 이미지
    public TextMeshProUGUI durationRemainTimeText; // 초능력 남은 지속시간 텍스트

    public Button[] characterSelectBtn; // 캐릭터 선택 버튼 배열
    public Button[] cloneCntBtn; // 분신 수 선택 버튼 배열

    public GameObject guideUI; // 가이드 UI
    public TextMeshProUGUI guideText; // 가이드 텍스트
    public IEnumerator curGuideCoroutine;

    public GameObject controlUI;
    public GameObject soundUI;
    public Button controlUIBtn;
    public Button soundUIBtn;
    public Slider bgmSlider; // BGM 볼륨 슬라이더
    public Slider sfxSlider; // SFX 볼륨 슬라이더

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
        GameManager.instance.SelectCharacter(GameManager.instance.selectCharacterId);
        // PlayingCharacterSetting(GameManager.instance.selectCharacterId);

        mainMenuBtn.onClick.AddListener(GameManager.instance.MoveToMainMenu);

        for (int i = 0; i < 3; i++)
        {
            int id = i; // for문으로 람다식을 쓸 때는 마지막 값으로만 초기화되는 Closure 문제 때문에 변수를 복사해서 사용

            characterSelectBtn[id].onClick.AddListener(() => GameManager.instance.SelectCharacter(id));

            GameObject cloningAbilityCharacter = GameManager.instance.Characters[1];
            cloneCntBtn[id].onClick.AddListener(() => cloningAbilityCharacter.GetComponent<CloningAbility>().SelectCloneCount(id + 1));
        }

        SoundManager.instance.bgmSlider = bgmSlider;
        SoundManager.instance.sfxSlider = sfxSlider;
        SoundManager.instance.Init();
    }

    void Update()
    {
        // 맵 UI 켜기/끄기
        if (Input.GetKeyDown(KeyCode.M))
        {
            if (!mapUI.activeSelf)
            {
                mapUI.SetActive(true);
                // GameManager.instance.Pause();
                // Cursor.lockState = CursorLockMode.None; // 커서 해제
                // Cursor.visible = true; // 커서 보이기
            }
            else
            {
                mapUI.SetActive(false);
                // GameManager.instance.Continue();
                // Cursor.lockState = CursorLockMode.Locked; // 커서 고정
                // Cursor.visible = false; // 커서 숨기기
            }
        }

        // 설정 UI 켜기/끄기
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (!settingUI.activeSelf)
            {
                settingUI.SetActive(true);
                GameManager.instance.Pause();
            }
            else
            {
                settingUI.SetActive(false);
                GameManager.instance.Continue();
            }
        }

        // 플레이어 UI 켜기/끄기
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (!playerUI.activeSelf)
            {
                playerUI.SetActive(true);
                GameManager.instance.Pause();
            }
            else
            {
                playerUI.SetActive(false);
                GameManager.instance.Continue();
            }
        }
    }

    // 무기 이미지 변경 함수
    public void ChangeWeaponImg(int equipWeaponId, int weaponId)
    {
        if (weaponId == -1)
        {
            equipWeaponImgs[equipWeaponId].gameObject.SetActive(false);
            equipWeaponImgs[equipWeaponId].sprite = null;
            // equipWeaponImgs[equipWeaponId].color = new Color(0 / 255f, 0 / 255f, 0 / 255f, 150f / 225f);
        }
        else
        {
            equipWeaponImgs[equipWeaponId].gameObject.SetActive(true);
            equipWeaponImgs[equipWeaponId].sprite = weaponImgs[weaponId];
            // equipWeaponImgs[equipWeaponId].color = Color.white;
        }
    }

    // 아이템 이미지 변경 함수
    public void ChangeItemImg(int havingItemId, int itemId)
    {
        if (itemId == -1)
        {
            havingItemImgs[havingItemId].sprite = null;
            havingItemImgs[havingItemId].color = new Color(0 / 255f, 0 / 255f, 0 / 255f, 150f / 225f);
        }
        else
        {
            havingItemImgs[havingItemId].sprite = itemImgs[itemId];
            havingItemImgs[havingItemId].color = Color.white;
        }
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
        PlayerController playerController = GameManager.instance.Characters[selectCharacterId].GetComponent<PlayerController>();

        for (int i = 0; i < 3; i++)
        {
            keyCardLevelImgs[i].SetActive(playerController.havingKeyCardLevel[i + 1]);
        }

        // 따라오기 선택 캐릭터 버튼 변경
        ResetFollowCharacterBtn(selectCharacterId);

        // 현재 선택한 플레이어의 무기로 이미지 변경
        for (int i = 0; i < 3; i++)
        {
            Color color = Color.black;
            color.a = 150f / 255f;
            equipWeaponOutlines[i].color = color;

            if (playerController.equipWeapons[i] == null)
                ChangeWeaponImg(i, -1);
            else
                ChangeWeaponImg(i, playerController.equipWeapons[i].GetComponent<Weapon>().weaponId);
        }
        if (playerController.curWeaponId != -1)
        {
            Color color2 = Color.cyan;
            color2.a = 150f / 255f;
            equipWeaponOutlines[playerController.curWeaponId].color = color2;
        }

        if (havingItemUI != null)
        {
            // 현재 선택한 플레이어의 아이템으로 이미지 변경
            for (int i = 0; i < 2; i++)
            {
                if (playerController.havingItemIds[i] == -1)
                    ChangeItemImg(i, -1);
                else
                    ChangeItemImg(i, playerController.havingItemIds[i]);
            }
        }

        Color characterColor = Color.gray;
        switch (selectCharacterId)
        {
            case 0:
                ColorUtility.TryParseHtmlString("#FFDE90", out characterColor);
                break;
            case 1:
                ColorUtility.TryParseHtmlString("#A87BBE", out characterColor);
                break;
            case 2:
                ColorUtility.TryParseHtmlString("#768BBE", out characterColor);
                break;
        }
        characterProfileImg.color = characterColor;

        characterName.text = playerController.gameObject.name; // 캐릭터 이름 설정
        SetHpBar(playerController.CurHp, playerController.MaxHp); // 체력바 설정

        // 초능력 쿨타임 변경
        // 초능력 쿨타임 이미지 바꾸기 코드 추가
        cooldownDisableImg.fillAmount = 0;
        cooldownRemainTimeText.text = "";
        cooldownImg.color = Color.white;

        // 초능력 지속시간 변경
        // 초능력 지속시간 이미지 바꾸기 코드 추가
        duraitonDisableImg.fillAmount = 0;
        durationRemainTimeText.text = "";
        durationImg.color = Color.white;
    }

    public void SetHpBar(float curHp, float maxHp)
    {
        if (curHp <= 0)
        {
            hpBar.value = 0;
            hpTxt.text = "0 / " + maxHp.ToString();
        }
        else
        {
            hpBar.value = curHp / maxHp;
            hpTxt.text = curHp.ToString() + " / " + maxHp.ToString();
        }
    }

    public void ShowGuide(string content, bool showNow)
    {
        if (!showNow && curGuideCoroutine != null)
        {
            return;
        }

        guideUI.SetActive(true);
        guideText.text = content;

        curGuideCoroutine = GuideCoroutine();
        StartCoroutine(curGuideCoroutine);
    }

    IEnumerator GuideCoroutine()
    {
        yield return new WaitForSeconds(3f);
        guideUI.SetActive(false);
        curGuideCoroutine = null;
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