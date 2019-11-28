using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class SceneControl : MonoBehaviour
{
    private BlockRoot block_root = null;

    public static List<BlockControl>[] blocks;

    public enum STEP
    {
        NONE = -1, // 상태 정보 없음.
        PLAY = 0, // 플레이 중.
        CLEAR, // 클리어.
        NUM, // 상태의 종류가 몇 개인지 나타낸다(= 2).
    };
    public STEP step = STEP.NONE; // 현재 상태.
    public STEP next_step = STEP.NONE; // 다음 상태.

    public float step_timer = 0.0f; // 경과 시간.
    private float clear_time = 0.0f; // 클리어 시간.

    public TextMeshPro TimeCount;
    public TextMeshPro TurnCount;
    public TextMeshPro BingoCount;

    public GameObject ClearScreen;
    public AudioSource Source;
    public AudioClip Sound_Clear;

    void Start()
    {
        // BlockRoot 스크립트를 가져온다.
        block_root = gameObject.GetComponent<BlockRoot>();

        //              재시작 하는 부분인데 잒우 외인지 않되서 떤짐
        ////  스테이지 처음을 로드
        //if (blocks != null)
        //{
        //    for (int y = 0; y < Block.BLOCK_NUM_Y; y++)  // 처음~마지막행
        //        for (int x = 0; x < Block.BLOCK_NUM_X; x++)  // 왼쪽~오른쪽
        //            block_root.blocks[x, y] = blocks[x][y];
        //}

        //else
        //{

        // BlockRoot 스크립트의 initialSetUp()을 호출한다.
        block_root.initialSetUp();

        //    blocks = new List<BlockControl>[Block.BLOCK_NUM_Y];

        //    //  스테이지 처음 시작을 저장
        //    for (int y = 0; y < Block.BLOCK_NUM_Y; y++)  // 처음~마지막행
        //    {
        //        blocks[y] = new List<BlockControl>();

        //        for (int x = 0; x < Block.BLOCK_NUM_X; x++)  // 왼쪽~오른쪽
        //            blocks[y].Add(block_root.blocks[x, y]);
        //    }
        //}

        // ScoreCounter 가져오기
        next_step = STEP.PLAY; // 다음 상태를 '플레이 중'으로.
    }

    void Update()
    {
        step_timer += Time.deltaTime;

        // 상태 변화 대기 -----.
        if (next_step == STEP.NONE)
        {
            switch (step)
            {
                case STEP.PLAY:
                    // 클리어 조건을 만족하면.
                    if (block_root.BingoCount == 3)
                    {
                        Source.PlayOneShot(Sound_Clear);
                        next_step = STEP.CLEAR; // 클리어 상태로 이행.
                    }
                    break;
            }
        }

        // 상태가 변화했다면 ------.
        if (next_step != STEP.NONE)
        {
            step = next_step;
            next_step = STEP.NONE;

            switch (step)
            {
                case STEP.CLEAR:
                    // block_root를 정지.
                    block_root.enabled = false;

                    // 경과 시간을 클리어 시간으로 설정.
                    clear_time = step_timer;
                    break;
            }

            step_timer = 0.0f;
        }
    }

    // 화면에 클리어한 시간과 메시지를 표시
    void OnGUI()
    {
        switch (step)
        {
            case STEP.PLAY:
                TimeCount.text = Mathf.CeilToInt(step_timer).ToString() + "초";
                TurnCount.text = block_root.Turns.ToString();
                BingoCount.text = block_root.BingoCount.ToString();
                break;

            case STEP.CLEAR:
                TimeCount.text = Mathf.CeilToInt(clear_time).ToString() + "초";
                TurnCount.text = block_root.Turns.ToString();
                BingoCount.text = block_root.BingoCount.ToString();
                ClearScreen.SetActive(true);
                break;
        }
    }

    public void Scene_Main()
    {
        SceneManager.LoadScene("Main");
    }

    public void Scene_Restart()
    {
        SceneManager.LoadScene("InGame");
    }
}
