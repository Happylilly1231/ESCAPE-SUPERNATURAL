using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class DataManager : MonoBehaviour
{
    public static DataManager instance;

    public GameData data = new GameData();

    string filePath;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);

            filePath = Application.persistentDataPath + "/GameData.json";

            // 저장된 데이터 불러오기
            LoadGameData();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 게임 데이터 저장하는 함수
    public void SaveGameData()
    {
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(filePath, json);
        Debug.Log("게임 저장 완료 : " + filePath);
    }

    // 저장된 게임 데이터 불러오는 함수
    public void LoadGameData()
    {
        if (File.Exists(filePath))
        {
            string json = File.ReadAllText(filePath);
            data = JsonUtility.FromJson<GameData>(json);
            Debug.Log("게임 불러오기 완료");
        }
        else
        {
            Debug.Log("저장된 파일이 없습니다.");
        }
    }
}
