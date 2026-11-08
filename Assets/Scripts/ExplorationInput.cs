using UnityEngine;
using UnityEngine.InputSystem;

// Shared keyboard semantics for exploration and menus, with no legacy Input Manager dependency.
public static class ExplorationInput
{
    public static bool Pressed(Key key) => Keyboard.current != null && Keyboard.current[key].wasPressedThisFrame;
    public static bool Released(Key key) => Keyboard.current != null && Keyboard.current[key].wasReleasedThisFrame;
    static bool Held(Key a, Key b) => Keyboard.current != null && (Keyboard.current[a].isPressed || Keyboard.current[b].isPressed);
    public static Vector2 Movement => Vector2.ClampMagnitude(new Vector2(
        (Held(Key.D, Key.RightArrow) ? 1 : 0) - (Held(Key.A, Key.LeftArrow) ? 1 : 0),
        (Held(Key.W, Key.UpArrow) ? 1 : 0) - (Held(Key.S, Key.DownArrow) ? 1 : 0)), 1);
}
