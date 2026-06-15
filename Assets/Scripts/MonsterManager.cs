using UnityEngine;
using System.Collections.Generic;

public class MonsterManager : MonoBehaviour
{
    public static MonsterManager Instance { get; private set; }
    public List<GameObject> monsterPrefabs;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void SpawnMonster(int index, Vector3 position)
    {
        if (index >= 0 && index < monsterPrefabs.Count)
        {
            Instantiate(monsterPrefabs[index], position, Quaternion.identity);
        }
    }
}
