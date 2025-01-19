using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager instance;
    public GameObject playerUI;
    public GameObject controlsWindow;
    public GameObject interactionUI;

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
                Cursor.lockState = CursorLockMode.None; // 커서 해제
                Cursor.visible = true; // 커서 보이기
            }
            else
            {
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
                Cursor.lockState = CursorLockMode.None; // 커서 해제
                Cursor.visible = true; // 커서 보이기
            }
            else
            {
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
}