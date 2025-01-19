using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    public GameObject[] characterCameras;
    public int selectCharacterId;

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
        characterCameras[0].SetActive(true);
        characterCameras[1].SetActive(false);
        characterCameras[2].SetActive(false);
    }

    void Update()
    {

    }

    public void SelectCharacter(int id)
    {
        characterCameras[selectCharacterId].SetActive(false); // 선택되어있던 캐릭터 카메라 비활성화
        characterCameras[id].SetActive(true); // 선택한 캐릭터 카메라 활성화
        selectCharacterId = id; // 선택한 캐릭터로 변경
    }
}
