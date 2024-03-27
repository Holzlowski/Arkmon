using System;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    [SerializeField] private GameObject actionCameraGameObject;
    [SerializeField] private float desiredCameraDistance = 5f;
    [SerializeField] private float cameraHeight = 5f; // Höhe der Kamera über dem Mittelpunkt

    private void Start()
    {
        BaseAction.OnAnyActionStarted += BaseAction_OnAnyActionStarted;
        BaseAction.OnAnyActionCompleted += BaseAction_OnAnyActionCompleted;

        HideActionCamera();
    }

    private void ShowActionCamera()
    {
        actionCameraGameObject.SetActive(true);
    }

    private void HideActionCamera()
    {
        actionCameraGameObject.SetActive(false);
    }

    private void BaseAction_OnAnyActionStarted(object sender, EventArgs e)
    {
        switch (sender)
        {
            case ShootAction shootAction:
                Unit shooterUnit = shootAction.GetUnit();
                Unit targetUnit = shootAction.GetTargetUnit();
                Vector3 shooterUnitPosition = shooterUnit.transform.position;
                Vector3 targetUnitPosition = targetUnit.transform.position;

                Vector3 centerPoint = (shooterUnitPosition + targetUnitPosition) / 2f;

                // Setze die Kameraposition
                Vector3 actionCameraPosition = centerPoint + Vector3.up * cameraHeight - actionCameraGameObject.transform.forward * desiredCameraDistance;
                actionCameraGameObject.transform.position = actionCameraPosition;
                // Richte die Kamera auf den Mittelpunkt aus
                transform.LookAt(centerPoint);

                ShowActionCamera();
                break;
        }
    }

    private void BaseAction_OnAnyActionCompleted(object sender, EventArgs e)
    {
        switch (sender)
        {
            case ShootAction shootAction:
                HideActionCamera();
                break;
        }
    }

    private void OnDestroy()
    {
        BaseAction.OnAnyActionStarted -= BaseAction_OnAnyActionStarted;
        BaseAction.OnAnyActionCompleted -= BaseAction_OnAnyActionCompleted;

    }
}
