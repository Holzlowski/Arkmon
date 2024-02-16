using System;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    [SerializeField] private GameObject actionCameraGameObject;

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
                //Unit shooterUnit = shootAction.GetUnit();
                //Unit targetUnit = shootAction.GetTargetUnit();

                //Vector3 cameraCharacterHeight = Vector3.up * 1.7f;

                //Vector3 shootDirection = (targetUnit.GetWorldPosition() - shooterUnit.GetWorldPosition()).normalized;

                //float shoulderOffsetAmount = 0.5f;
                //// der Vektor mal die shootdirection zeigt immer zu rechten Seite der Unit und mit dem Offset wird er nach rechts verschoben
                //Vector3 shoulderOffset = Quaternion.Euler(0f, 90f, 0f) * shootDirection * shoulderOffsetAmount;

                //Vector3 actionCameraPosition =
                //    shooterUnit.GetWorldPosition() +
                //    cameraCharacterHeight +
                //    shoulderOffset +
                //    (shootDirection * -1);

                //actionCameraGameObject.transform.position = actionCameraPosition;
                //actionCameraGameObject.transform.LookAt(targetUnit.GetWorldPosition() + cameraCharacterHeight);
                //ShowActionCamera();
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
}
