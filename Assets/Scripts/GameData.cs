using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GameData
{
    public bool haveToShowTutorial;
    public int currentStageId;
    public List<List<GameObject>> characterEquipWeapons;
    public List<List<int>> characterEquipWeaponCurBulletCnts;
    public List<List<bool>> characterHavingKeyCardLevels;

    public GameData()
    {
        haveToShowTutorial = true;
        currentStageId = 1;
        characterEquipWeapons = new List<List<GameObject>>();
        characterEquipWeaponCurBulletCnts = new List<List<int>>();
        characterHavingKeyCardLevels = new List<List<bool>>();
    }
}
