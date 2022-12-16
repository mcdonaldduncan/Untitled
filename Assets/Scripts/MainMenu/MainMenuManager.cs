using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    public void StartLevel(int index)
    {
        SceneManager.LoadScene(index);
    }


    public void QuitGame()
    {
        Application.Quit();
    }


}