using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UnitManager : MonoBehaviour
{
    public static UnitManager Instance { get; private set; }

    private List<Unit> unitList;
    private List<Unit> friendlyUnitList;
    private List<Unit> enemyUnitList;

    private bool hasLoadedScene = false;

    private void Awake()
    {
        unitList = new List<Unit>();
        friendlyUnitList = new List<Unit>();
        enemyUnitList = new List<Unit>();
    }

    private void Start()
    {
        if (Instance != null)
        {
            Debug.LogError("There's more than one UnitActionSystem! " + transform + " - " + Instance);
            Destroy(gameObject);
            return;
        }
        Instance = this;

        Unit.OnAnyUnitSpawned += Unit_OnAnyUnitSpawned;
        Unit.OnAnyUnitDead += Unit_OnAnyUnitDead;
    }


    private void Update()
    {
        if (enemyUnitList.Count <= 0 && !hasLoadedScene)
        {
            StartCoroutine(LoadSceneAfterDelay());
        }
    }

    private IEnumerator LoadSceneAfterDelay()
    {
        // Warte 1 Sekunde
        yield return new WaitForSeconds(1f);

        if (SceneLoadManager.instance != null)
        {
            // Rufe die LoadSceneBeforeBattle-Funktion auf
            SceneLoadManager.instance.LoadSceneBeforeBattle();
        }
        else
        {
            SceneManager.LoadScene(0);
        }


        // Setze hasLoadedScene auf true, um sicherzustellen, dass die Funktion nur einmal aufgerufen wird
        hasLoadedScene = true;
    }

    private void Unit_OnAnyUnitSpawned(object sender, EventArgs e)
    {
        Unit unit = sender as Unit;

        unitList.Add(unit);
        if (unit.IsEnemy())
        {
            enemyUnitList.Add(unit);
        }
        else
        {
            friendlyUnitList.Add(unit);
        }
    }

    private void Unit_OnAnyUnitDead(object sender, EventArgs e)
    {
        Unit unit = sender as Unit;

        unitList.Remove(unit);
        if (unit.IsEnemy())
        {
            enemyUnitList.Remove(unit);
        }
        else
        {
            friendlyUnitList.Remove(unit);
        }
    }

    public List<Unit> GetUnitList()
    {
        return unitList;
    }
    public List<Unit> GetFriendlyUnitList()
    {
        return friendlyUnitList;
    }
    public List<Unit> GetEnemyUnitList()
    {
        return enemyUnitList;
    }
}
