using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardReader : MonoBehaviour
{
    public GameObject door;
    public int keyCardLevel;

    Animator doorAnimator;

    void Awake()
    {
        doorAnimator = door.GetComponent<Animator>();
    }

    public void OpenDoor()
    {
        doorAnimator.SetBool("open", true);
        StartCoroutine(CloseDoorCoroutine()); // 문을 연 후 2초 뒤에 문을 닫는 코루틴 실행
    }

    // 문을 연 후 2초 뒤에 문을 닫는 코루틴
    IEnumerator CloseDoorCoroutine()
    {
        yield return new WaitForSeconds(2f); // 2초 대기
        doorAnimator.SetBool("open", false);  // 문 닫기
    }
}
