using UnityEngine;
using UnityEngine.UI;

public class ActiveUI : MonoBehaviour
{
    public GameObject activeUI;
    public Button button;

    private bool isActiveUI = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        isActiveUI = activeUI.activeSelf;
        button.onClick.AddListener(() =>
        {
            isActiveUI = !isActiveUI;
            activeUI.SetActive(isActiveUI);
        });
    }
}
