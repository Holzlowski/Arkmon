using UnityEngine;

public interface IInteractableAsPlayer
{
    // Diese Methode wird aufgerufen, wenn der Spieler mit dem Objekt interagiert
    void InteractWithPlayer();
    Vector3 GetTransform();
}
