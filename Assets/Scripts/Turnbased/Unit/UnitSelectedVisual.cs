using System;
using UnityEngine;

public class UnitSelectedVisual : MonoBehaviour
{
    [SerializeField] private Unit unit;

    private MeshRenderer meshRenderer;

    void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
    }

    void Start()
    {
        actionSystem = UnitActionSystem.Instance;
        actionSystem.OnSelectedUnitChange += UnitActionSystem_OnSelectedUnitChanged;
        UpdateVisual();
    }

    private void UnitActionSystem_OnSelectedUnitChanged(object sender, EventArgs empty)
    {
        UpdateVisual();
    }

    private void UpdateVisual()
    {
        if (actionSystem.GetSelectedUnit() == unit)
        {
            meshRenderer.enabled = true;
        }
        else
        {
            meshRenderer.enabled = false;
        }
    }

    private UnitActionSystem actionSystem;

    private void OnDestroy()
    {
        if (actionSystem != null) actionSystem.OnSelectedUnitChange -= UnitActionSystem_OnSelectedUnitChanged;
    }
}
