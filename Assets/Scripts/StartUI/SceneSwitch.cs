using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneSwitch : MonoBehaviour
{
    public Button Level1_Button;
    public Button Level2_Button;
    public Button Level3_Button;
    public Button Level4_Button;
    public Button Level5_Button;

    void Start()
    {
        // Unlock and show cursor for UI interaction
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Level1_Button.onClick.AddListener(LoadLevel1);
        Level2_Button.onClick.AddListener(LoadLevel2);
        Level3_Button.onClick.AddListener(LoadLevel3);
        Level4_Button.onClick.AddListener(LoadLevel4);
        Level5_Button.onClick.AddListener(LoadLevel5);
    }

    public void LoadLevel1()
    {
        SceneManager.LoadScene("Level1_demo");
    }

    public void LoadLevel2()
    {
        SceneManager.LoadScene("Level2_demo");
    }

    public void LoadLevel3()
    {
        SceneManager.LoadScene("Level3");
    }
    
    public void LoadLevel4()
    {
        SceneManager.LoadScene("Level4_demo");
    }
    
    public void LoadLevel5()
    {
        SceneManager.LoadScene("Level5_demo");
    }
    
    public void swithScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
}
