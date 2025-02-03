using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 초능력 인터페이스
public interface ISupernatural
{
    bool CanUIUpdate { get; set; }
    float SupernaturalCoolDown { get; set; }
    float CooldownRemainTime { get; set; }
    bool IsSupernaturalReady { get; set; }


    void Activate(); // 초능력 사용

    void Deactivate(); // 초능력 사용 해제
}
