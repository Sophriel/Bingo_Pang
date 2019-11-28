using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelCamera : MonoBehaviour
{
    public void LevelSelect3()
    {
        Block.BLOCK_NUM_X = 3;
        Block.BLOCK_NUM_Y = 3;
        SceneManager.LoadScene("InGame");
    }

    public void LevelSelect5()
    {
        Block.BLOCK_NUM_X = 5;
        Block.BLOCK_NUM_Y = 5;
        SceneManager.LoadScene("InGame");
    }

    public void LevelSelect7()
    {
        Block.BLOCK_NUM_X = 7;
        Block.BLOCK_NUM_Y = 7;
        SceneManager.LoadScene("InGame");
    }

    public void LevelSelect9()
    {
        Block.BLOCK_NUM_X = 9;
        Block.BLOCK_NUM_Y = 9;
        SceneManager.LoadScene("InGame");
    }
}
