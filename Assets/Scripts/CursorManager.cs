using UnityEngine;

public static class CursorManager
{
    public static void SetCursorActive(bool cursorActive)
    {
        if (cursorActive)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}
