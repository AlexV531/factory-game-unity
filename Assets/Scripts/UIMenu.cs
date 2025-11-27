using UnityEngine;
using UnityEngine.UI;

public class UIMenu : MonoBehaviour
{
    public GameObject menuScreen;
    public GameObject nonMenuUIElements;
    private bool menuOpen = true;
    
    protected virtual void Start()
    {
        CloseMenu();
        CursorManager.SetCursorActive(true);
    }

    public virtual void OpenMenu()
    {
        menuScreen.SetActive(true);
        if (nonMenuUIElements != null)
            nonMenuUIElements.SetActive(false);
        CursorManager.SetCursorActive(true);
        menuOpen = true;
    }

    public virtual void CloseMenu()
    {
        menuScreen.SetActive(false);
        if (nonMenuUIElements != null)
            nonMenuUIElements.SetActive(true);
        CursorManager.SetCursorActive(false);
        menuOpen = false;
    }

    public bool IsMenuOpen()
    {
        return menuOpen;
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus && IsMenuOpen())
        {
            CursorManager.SetCursorActive(true);
        }
    }
}