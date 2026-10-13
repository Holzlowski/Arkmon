using System;
using System.Collections.Generic;
using UnityEngine;

public class GridSystemVisual : MonoBehaviour
{
    public static GridSystemVisual Instance { get; private set; }

    [Serializable]
    public struct GridVisualTypeMaterial
    {
        public GridVisualType gridVisualType;
        public Material material;
    }

    public enum GridVisualType
    {
        White,
        Blue,
        Red,
        RedSoft,
        Yellow
    }


    [SerializeField] private Transform gridSystemVisualSinglePrefab;
    [SerializeField, Min(0.01f)] private float visualHeight = 0.06f;
    [SerializeField] public List<GridVisualTypeMaterial> gridVisualTypeMaterialList;

    private UnitActionSystem actionSystem;
    private LevelGrid levelGrid;
    private GridSystemVisualSingle[,] gridSystemVisualSingleArray;
    private GridPosition? hoveredFan;

    private void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError("There`s more than one GridSystem! " + transform + " - " + Instance);
            Destroy(Instance);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        actionSystem = UnitActionSystem.Instance;
        levelGrid = LevelGrid.Instance;
        gridSystemVisualSingleArray = new GridSystemVisualSingle[
            LevelGrid.Instance.GetWidth(),
            LevelGrid.Instance.GetHeight()
            ];

        for (int x = 0; x < LevelGrid.Instance.GetWidth(); x++)
        {
            for (int z = 0; z < LevelGrid.Instance.GetHeight(); z++)
            {
                GridPosition gridPosition = new GridPosition(x, z);

                Transform gridVisualSingleTransform =
                    Instantiate(gridSystemVisualSinglePrefab,
                        LevelGrid.Instance.GetWorldPosition(gridPosition) + Vector3.up * visualHeight,
                        Quaternion.identity);

                gridSystemVisualSingleArray[x, z] = gridVisualSingleTransform.GetComponent<GridSystemVisualSingle>();
            }
        }
        actionSystem.OnSelectedActionChanged += UnityActionSystem_OnSelectedActionChanged;
        levelGrid.OnAnyUnitMoveGridPosition += LevelGrid_OnAnyUnitMoveGridPosition;

        HideAllGridPositions();
        UpdateGridVisual();
    }

    public void HideAllGridPositions()
    {
        foreach (GridSystemVisualSingle gridSystemVisual in gridSystemVisualSingleArray)
        {
            gridSystemVisual.Hide();
        }
    }

    private void ShowGridPositionRange(GridPosition gridPosition, int range, GridVisualType gridVisualType)
    {
        List<GridPosition> gridPositionList = new List<GridPosition>();

        for (int x = -range; x <= range; x++)
        {
            for (int z = -range; z <= range; z++)
            {
                GridPosition testGridPosition = gridPosition + new GridPosition(x, z);

                if (!LevelGrid.Instance.IsValidGridPosition(testGridPosition))
                {
                    continue;
                }

                int testDistance = Mathf.Abs(x) + Mathf.Abs(z);
                if (testDistance > range)
                {
                    continue;
                }
                gridPositionList.Add(testGridPosition);
            }
        }
        ShowGridPositionList(gridPositionList, gridVisualType);
    }

    private void ShowGridPositionRangeSquare(GridPosition gridPosition, int range, GridVisualType gridVisualType)
    {
        List<GridPosition> gridPositionList = new List<GridPosition>();

        for (int x = -range; x <= range; x++)
        {
            for (int z = -range; z <= range; z++)
            {
                GridPosition testGridPosition = gridPosition + new GridPosition(x, z);

                if (!LevelGrid.Instance.IsValidGridPosition(testGridPosition))
                {
                    continue;
                }
                gridPositionList.Add(testGridPosition);
            }
        }
        ShowGridPositionList(gridPositionList, gridVisualType);
    }

    public void ShowGridPositionList(List<GridPosition> gridPositionList, GridVisualType gridVisualType)
    {
        foreach (GridPosition gridPosition in gridPositionList)
        {
            gridSystemVisualSingleArray[gridPosition.x, gridPosition.z].Show(GetGridVisualTypeMaterial(gridVisualType));
        }
    }

    public void Refresh() => UpdateGridVisual();

    private void UpdateGridVisual()
    {
        hoveredFan = null;
        if (UnitManager.Instance != null && !UnitManager.Instance.IsBattleRunning) return;
        HideAllGridPositions();

        Unit selectedUnit = actionSystem.GetSelectedUnit();
        BaseAction selectedAction = actionSystem.GetSelectedAction();
        if (selectedUnit == null || selectedAction == null) return;

        GridVisualType gridVisualType;

        switch (selectedAction)
        {
            default:
            case MoveAction moveAction:
                gridVisualType = GridVisualType.White;
                break;
            case SpinAction spinAction:
                gridVisualType = GridVisualType.Blue;
                break;
            case ShootAction shootAction:
                gridVisualType = GridVisualType.Red;
                ShowGridPositionRange(selectedUnit.GetGridPosition(), shootAction.GetMaxShootDistance(), GridVisualType.RedSoft);
                break;
            case MeleeAttackAction meleeAction:
                gridVisualType = GridVisualType.Red;
                ShowGridPositionRangeSquare(selectedUnit.GetGridPosition(), meleeAction.Range, GridVisualType.RedSoft);
                break;
            case EmberAction ember:
                gridVisualType = GridVisualType.Red;
                ShowGridPositionRangeSquare(selectedUnit.GetGridPosition(), ember.Range, GridVisualType.RedSoft);
                break;
            case GuardAction guard:
                gridVisualType = GridVisualType.Blue;
                break;
            case RamAction ramAction:
                gridVisualType = GridVisualType.Red;
                break;
            case GrenadeAction grenadeAction:
                gridVisualType = GridVisualType.Yellow;
                break;
            case SwordAction swordAction:
                gridVisualType = GridVisualType.Red;
                ShowGridPositionRangeSquare(selectedUnit.GetGridPosition(), swordAction.GetMaxSwordDistance(), GridVisualType.RedSoft);
                break;
            case InteractAction interactAction:
                gridVisualType = GridVisualType.Blue;
                break;
        }
        ShowGridPositionList(
            selectedAction.GetValidActionGridPositionList(), gridVisualType);
    }

    private void UnityActionSystem_OnSelectedActionChanged(object sender, EventArgs e)
    {
        UpdateGridVisual();
    }

    private void LevelGrid_OnAnyUnitMoveGridPosition(object sender, EventArgs e)
    {
        UpdateGridVisual();
    }

    private Material GetGridVisualTypeMaterial(GridVisualType gridVisualType)
    {
        foreach (GridVisualTypeMaterial gridVisualTypeMaterial in gridVisualTypeMaterialList)
        {
            if (gridVisualTypeMaterial.gridVisualType == gridVisualType)
            {
                return gridVisualTypeMaterial.material;
            }
        }
        Debug.LogError("Could not find GridvisualTypeMaterial for GridVisualType " + gridVisualType);
        return null;
    }

    private void OnDestroy()
    {
        if (actionSystem != null) actionSystem.OnSelectedActionChanged -= UnityActionSystem_OnSelectedActionChanged;
        if (levelGrid != null) levelGrid.OnAnyUnitMoveGridPosition -= LevelGrid_OnAnyUnitMoveGridPosition;
        if (Instance == this) Instance = null;
    }
    private void LateUpdate()
    {
        if (gridSystemVisualSingleArray == null || actionSystem == null) return;
        EmberAction fan = actionSystem.GetSelectedAction() as EmberAction;
        bool allowed = fan != null && UnityEngine.InputSystem.Mouse.current != null && !actionSystem.IsBusy
            && !BaseAction.IsAnyActionActive && TurnSystem.Instance.IsPlayerTurn()
            && UnitManager.Instance.IsBattleRunning && !SceneLoadManager.IsTransitioning
            && !(UnityEngine.EventSystems.EventSystem.current != null
                && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject());
        if (!allowed) { if (hoveredFan.HasValue) UpdateGridVisual(); return; }
        GridPosition centre = levelGrid.GetGridPosition(MouseWorld.GetPosition());
        if (hoveredFan.HasValue && hoveredFan.Value == centre) return;
        UpdateGridVisual(); hoveredFan = centre;
        if (!fan.IsValidActionGridPosition(centre)) return;
        ShowGridPositionList(fan.GetAffectedGridPositionList(centre), GridVisualType.RedSoft);
        ShowGridPositionList(fan.GetImpactGridPositionList(centre), GridVisualType.Yellow);
    }
}
