using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GameData
{
    public int currentStageId;
    public List<List<GameObject>> characterEquipWeapons;
    public List<List<int>> characterEquipWeaponCurBulletCnts;
    public List<List<bool>> characterHavingKeyCardLevels;

    public GameData()
    {
        currentStageId = 1;
        characterEquipWeapons = new List<List<GameObject>>();
        characterEquipWeaponCurBulletCnts = new List<List<int>>();
        characterHavingKeyCardLevels = new List<List<bool>>();
    }
}
