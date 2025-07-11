using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MenuController : MonoBehaviour
{
    public static MenuController Instance { get; private set; }

    public CubeController currentCube;
    public Button changeColorButton;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // remove duplicates
            return;
        }
        Instance = this;
    }

    void Start()
    {
        changeColorButton.onClick.AddListener(OnChangeColorClicked);
    }

    public void SetCurrentCube(CubeController cube)
    {
        currentCube = cube;
    }

    public void OnChangeColorClicked()
    {
        if (currentCube != null)
            currentCube.ChangeColor();
    }
}
