using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockRoot : MonoBehaviour
{
    public MainBlock[] Mainblock;
    public GameObject BlockPrefab = null; // 만들어낼 블록의 프리팹.
    public BlockControl[,] blocks; // 그리드.

    //  행의 갯수 + 열의 갯수 + 대각선 2개
    int ListCount = 0;
    List<int>[] ListsOfNums;

    //  제시되는 숫자
    public int[] MainNum;
    public List<int> ExceptNums;

    // 블록을 잡는데 필요한 멤버 변수 선언
    private GameObject main_camera = null; // 메인 카메라.
    private BlockControl grabbed_block = null; // 잡은 블록.

    //  진행된 턴의 수
    public int Turns = 0;

    //  완성된 빙고 수
    public int BingoCount = 0;

    protected bool is_popping_prev = false; // 앞에서 발화했는가?

    public AudioSource Source;
    public AudioClip Sound_Grab, Sound_Change, Sound_Pop;

    public bool IsCheat = false;


    void Start()
    {
        main_camera = GameObject.FindGameObjectWithTag("MainCamera");
        // 카메라로부터 마우스 커서를 통과하는 광선을 쏘기 위해서 필요
    }

    // 마우스 좌표와 겹치는지 체크한다. 
    // 잡을 수 있는 상태의 블록을 잡는다.
    void Update()
    {
        Vector3 mouse_position; // 마우스 위치.
        unprojectMousePosition(out mouse_position, Input.mousePosition); // 마우스 위치를 가져온다.

        // 가져온 마우스 위치를 하나의 Vector2로 모은다.
        Vector2 mouse_position_xy = new Vector2(mouse_position.x, mouse_position.y);

        // 잡은 블록이 비었으면
        if (grabbed_block == null)
        {
            // 마우스 버튼이 눌렸으면
            if (Input.GetMouseButtonDown(0))
            {
                // blocks 배열의 모든 요소를 차례로 처리한다.
                foreach (BlockControl block in blocks)
                {
                    // 블록을 잡을 수 없다면.
                    if (!block.isGrabbable())
                    {
                        continue; // 루프의 처음으로 점프한다.
                    }

                    // 마우스 위치가 블록 영역 안이 아니면.
                    if (!block.isContainedPosition(mouse_position_xy))
                    {
                        continue; // 루프의 처음으로 점프한다.
                    }

                    //  잡은 블록이 제시된 숫자의 블록이 아니면
                    if (block.BlockNum != MainNum[0])
                        if (!IsCheat)  //  치트 사용중이 아니면
                            continue;

                    // 처리 중인 블록을 grabbed_block에 등록한다.
                    grabbed_block = block;

                    // 잡았을 때의 처리를 실행한다.
                    grabbed_block.beginGrab();

                    Source.PlayOneShot(Sound_Grab);
                    break;
                }
            }

            // 낙하 중 또는 슬라이드 중이면.
            if (is_has_sliding_block())
            {
                // 아무것도 하지 않는다.
                // 낙하 중도 슬라이드 중도 아니면.
            }

            else
            {
                int ignite_count = 0; // 불붙은 개수.

                // 그리드 안의 모든 블록에 대해서 처리.
                foreach (BlockControl block in blocks)
                {
                    // 대기 중이면 루프의 처음으로 점프하고.
                    if (!block.isIdle())
                    {
                        continue; // 다음 블록을 처리한다.
                    }
                }

                // 불붙은 개수가 0보다 크면.＝한 군데라도 맞춰진 곳이 있으면.
                if (ignite_count > 0)
                {
                    if (!is_popping_prev)
                    {
                        // 연속 점화가 아니라면, 점화 횟수를 리셋.
                    }

                    int block_count = 0; // 불 붙는 중인 블록 수.

                    // 그리드 내의 모든 블록에 대해서 처리.
                    foreach (BlockControl block in blocks)
                    {
                        if (block.isPopping()) // 타는 중이면.
                        {
                            block_count++; // 발화 중인 블록 개수를 증가.
                        }
                    }
                }
            }

        }

        // 블록을 잡았을 때.
        else
        {
            do
            {
                // 슬라이드할 곳의 블록을 가져온다.
                BlockControl swap_target = getNextBlock(grabbed_block, grabbed_block.slide_dir);

                // 슬라이드할 곳 블록이 비어 있으면.
                if (swap_target == null)
                {
                    break; // 루프 탈출.
                }

                // 슬라이드할 곳의 블록이 잡을 수 있는 상태가 아니라면.
                if (!swap_target.isGrabbable())
                {
                    break; // 루프 탈출.
                }

                // 현재 위치에서 슬라이드 위치까지의 거리를 얻는다.
                float offset = grabbed_block.calcDirOffset(mouse_position_xy, grabbed_block.slide_dir);

                // 수리 거리가 블록 크기의 절반보다 작다면.
                if (offset < Block.COLLISION_SIZE / 2.0f)
                {
                    break; // 루프 탈출.
                }

                // 블록을 교체한다.
                swapBlock(grabbed_block, grabbed_block.slide_dir, swap_target);

                Source.PlayOneShot(Sound_Change);

                grabbed_block = null; // 지금은 블록을 잡고 있지 않다.
            } while (false);

            // 마우스 버튼이 눌려져 있지 않으면.
            if (!Input.GetMouseButton(0))
            {
                grabbed_block.endGrab(); // 블록을 놨을 때의 처리를 실행.
                grabbed_block = null; // grabbed_block을 비우게 설정.
            }
        }
    }

    // 블록을 만들어 내고 가로 X칸, 세로 Y칸에 배치한다.
    public void initialSetUp()
    {
        // 그리드의 크기를 X×Y로 한다.
        blocks = new BlockControl[Block.BLOCK_NUM_X, Block.BLOCK_NUM_Y];

        //  판별하는 리스트 초기화        
        ListCount = Block.BLOCK_NUM_X + Block.BLOCK_NUM_Y + 2;
        ListsOfNums = new List<int>[ListCount];
        for (int i = 0; i < Block.BLOCK_NUM_X + Block.BLOCK_NUM_Y + 2; i++)
            ListsOfNums[i] = new List<int>();

        //  블록에 할당할 랜덤 숫자들을 저장
        List<int> Nums = new List<int>();
        for (int i = 1; i <= blocks.Length; i++)
            Nums.Add(i);

        for (int y = 0; y < Block.BLOCK_NUM_Y; y++)  // 처음~마지막행
        {
            for (int x = 0; x < Block.BLOCK_NUM_X; x++)  // 왼쪽~오른쪽
            {
                // BlockPrefab의 인스턴스를 씬에 만든다.
                GameObject game_object = Instantiate(BlockPrefab) as GameObject;

                // 위에서 만든 블록의 BlockControl 클래스를 가져온다.
                BlockControl block = game_object.GetComponent<BlockControl>();

                // 블록을 그리드에 저장한다.
                blocks[x, y] = block;

                // 블록의 위치 정보(그리드 좌표)를 설정한다.
                block.i_pos.x = x;
                block.i_pos.y = y;

                // 각 BlockControl이 연계할 GameRoot는 자신이라고 설정한다.
                block.block_root = this;

                // 그리드 좌표를 실제 위치(씬의 좌표)로 변환한다.
                Vector3 position = calcBlockPosition(block.i_pos);

                // 씬의 블록 위치를 이동한다.
                block.transform.position = position;

                // 블록의 이름을 설정(후술)한다. 나중에 블록 정보 확인때 필요.
                block.name = "block(" + block.i_pos.x.ToString() +
                "," + block.i_pos.y.ToString() + ")";

                //  블록에 랜덤 숫자들 할당
                int ran = Random.Range(0, Nums.Count);
                block.BlockNum = Nums[ran];
                Nums.RemoveAt(ran);

                // 블록의 색을 변경한다.
                block.setColor(block.BlockNum);
            }
        }

        MainNum = new int[3];
        //  제시될 세 숫자 생성
        for (int i = 0; i < 3; i++)
            GenerateMainNum(i);
    }

    // 지정된 그리드 좌표로 씬에서의 좌표를 구한다.
    public static Vector3 calcBlockPosition(Block.iPosition i_pos)
    {
        // 배치할 왼쪽 위 구석 위치를 초기값으로 설정한다.
        Vector3 position = new Vector3(-(Block.BLOCK_NUM_X / 2.0f - 0.5f), -(Block.BLOCK_NUM_Y / 2.0f - 0.5f), 0.0f);

        // 초깃값 + 그리드 좌표 × 블록 크기.
        position.x += (float)i_pos.x * Block.COLLISION_SIZE;
        position.y += (float)i_pos.y * Block.COLLISION_SIZE;

        return (position); // 씬에서의 좌표를 반환한다.
    }

    public bool unprojectMousePosition(out Vector3 world_position, Vector3 mouse_position)
    // ref는 초기화된 변수만, out은 초기화되지 않은 변수를 전달 가능
    {
        bool ret;

        // 판을 작성한다. 이 판은 카메라에 대해서 뒤로 향해서(Vector3.back).
        // 블록의 절반 크기만큼 앞에 둔다.
        Plane plane = new Plane(Vector3.back, new Vector3(0.0f, 0.0f, -Block.COLLISION_SIZE / 2.0f));

        // 카메라와 마우스를 통과하는 빛을 만든다.
        Ray ray = main_camera.GetComponent<Camera>().ScreenPointToRay(mouse_position);
        float depth;

        // 광선(ray)이 판(plane)에 닿았다면,
        if (plane.Raycast(ray, out depth))
        {
            // depth에 정보가 기록된다.
            // 인수 world_position을 마우스 위치로 덮어쓴다.
            world_position = ray.origin + ray.direction * depth;
            ret = true;
        }

        // 닿지 않았다면.
        else
        {
            // 인수 world_position을 0인 벡터로 덮어쓴다.
            world_position = Vector3.zero;
            ret = false;
        }

        return (ret); // 카메라를 통과하는 광선이 블록에 닿았는지를 반환
    }

    // 블록이 슬라이드할 곳에 어느 블록이 있는지 반환한다.
    public BlockControl getNextBlock(BlockControl block, Block.DIR4 dir)
    {
        BlockControl next_block = null; // 슬라이드할 곳의 블록을 여기에 저장.

        switch (dir)
        {
            case Block.DIR4.RIGHT:
                if (block.i_pos.x < Block.BLOCK_NUM_X - 1) // 그리드 안이라면.
                {
                    next_block = blocks[block.i_pos.x + 1, block.i_pos.y];
                }
                break;

            case Block.DIR4.LEFT:
                if (block.i_pos.x > 0) // 그리드 안이라면.
                {
                    next_block = blocks[block.i_pos.x - 1, block.i_pos.y];
                }
                break;

            case Block.DIR4.UP:
                if (block.i_pos.y < Block.BLOCK_NUM_Y - 1) // 그리드 안이라면.
                {
                    next_block = blocks[block.i_pos.x, block.i_pos.y + 1];
                }
                break;

            case Block.DIR4.DOWN:
                if (block.i_pos.y > 0) // 그리드 안이라면.
                {
                    next_block = blocks[block.i_pos.x, block.i_pos.y - 1];
                }
                break;
        }

        return (next_block);
    }

    // 인수로 지정된 방향을 바탕으로 이동량의 벡터를 반환한다.
    public static Vector3 getDirVector(Block.DIR4 dir)
    {
        Vector3 v = Vector3.zero;

        switch (dir)
        {
            case Block.DIR4.RIGHT: v = Vector3.right; break; // 오른쪽으로 1단위 이동.
            case Block.DIR4.LEFT: v = Vector3.left; break; // 왼쪽으로 1단위 이동.
            case Block.DIR4.UP: v = Vector3.up; break; // 위로 1단위 이동.
            case Block.DIR4.DOWN: v = Vector3.down; break; // 아래로 1단위 이동.
        }

        v *= Block.COLLISION_SIZE; // 블록의 크기를 곱한다.

        return (v);
    }

    // 인수로 지정된 방향의 반대 방향을 반환한다.
    public static Block.DIR4 getOppositDir(Block.DIR4 dir)
    {
        Block.DIR4 opposit = dir;

        switch (dir)
        {
            case Block.DIR4.RIGHT: opposit = Block.DIR4.LEFT; break;
            case Block.DIR4.LEFT: opposit = Block.DIR4.RIGHT; break;
            case Block.DIR4.UP: opposit = Block.DIR4.DOWN; break;
            case Block.DIR4.DOWN: opposit = Block.DIR4.UP; break;
        }

        return (opposit);
    }

    // 실제로 블록을 교체한다.
    public void swapBlock(BlockControl block0, Block.DIR4 dir, BlockControl block1)
    {
        //  블록 번호를 교환한다.
        int num = block0.BlockNum;
        block0.BlockNum = block1.BlockNum;
        block1.BlockNum = num;

        if (block0.BlockNum == 0)
            block0.NumberUI.gameObject.SetActive(false);
        else
            block0.NumberUI.gameObject.SetActive(true);

        if (block1.BlockNum == 0)
            block1.NumberUI.gameObject.SetActive(false);
        else
            block1.NumberUI.gameObject.SetActive(true);

        // 각각의 블록 색을 기억해 둔다.
        int color0 = block0.color;
        int color1 = block1.color;

        // 각각의 블록의 확대율을 기억해 둔다.
        Vector3 scale0 = block0.transform.localScale;
        Vector3 scale1 = block1.transform.localScale;

        // 각각의 블록의 '사라지는 시간'을 기억해 둔다.
        float pop_timer0 = block0.pop_timer;
        float pop_timer1 = block1.pop_timer;

        // 각각의 블록의 이동할 곳을 구한다.
        Vector3 offset0 = getDirVector(dir);
        Vector3 offset1 = getDirVector(getOppositDir(dir));

        // 색을 교체한다.
        block0.setColor(color1);
        block1.setColor(color0);

        // 확대율을 교체한다.
        block0.transform.localScale = scale1;
        block1.transform.localScale = scale0;

        // '사라지는 시간'을 교체한다.
        block0.pop_timer = pop_timer1;
        block1.pop_timer = pop_timer0;

        block0.beginSlide(offset0); // 원래 블록 이동을 시작한다.
        block1.beginSlide(offset1); // 이동할 위치의 블록 이동을 시작한다.

        //  한 턴을 종료한다
        Turns++;  //  턴 올리고
        ListBlocks();  //  블록들 판별하고
        SetMainNum();  //  다음 숫자 제시
    }


    //  제시되는 숫자 갱신
    public void SetMainNum()
    {
        for (int i = 0; i < MainNum.Length - 1; i++)
        {
            MainNum[i] = MainNum[i+1];

            while (ExceptNums.Contains(MainNum[i]))
                MainNum[i] = Random.Range(1, blocks.Length);

            Mainblock[i].SetMainNum(MainNum[i]);
        }

        GenerateMainNum(MainNum.Length - 1);
    }

    public void GenerateMainNum(int index)
    {
        MainNum[index] = Random.Range(1, blocks.Length);

        while (ExceptNums.Contains(MainNum[index]))
            MainNum[index] = Random.Range(1, blocks.Length);

        Mainblock[index].SetMainNum(MainNum[index]);
    }

    private void ListBlocks()
    {

        ListCount = 0;

        foreach (List<int> i in ListsOfNums)
            i.Clear();

        //  세로줄 리스트
        for (int x = 0; x < Block.BLOCK_NUM_X; x++)
        {
            for (int y = 0; y < Block.BLOCK_NUM_Y; y++)
                ListsOfNums[ListCount].Add(blocks[x, y].BlockNum);

            //  해당 라인이 등차수열인지 확인
            if (CheckArithmetic(ListsOfNums[ListCount]))
                for (int y = 0; y < Block.BLOCK_NUM_Y; y++)  //  라인을 터트려 더미로 만듬
                {
                    blocks[x, y].toPopping();
                    ExceptNums.Add(blocks[x, y].BlockNum);
                }

            ListCount++;
        }

        //  가로줄 리스트
        for (int y = 0; y < Block.BLOCK_NUM_Y; y++)
        {
            for (int x = 0; x < Block.BLOCK_NUM_X; x++)
                ListsOfNums[ListCount].Add(blocks[x, y].BlockNum);

            if (CheckArithmetic(ListsOfNums[ListCount]))
                for (int x = 0; x < Block.BLOCK_NUM_Y; x++)
                {
                    blocks[x, y].toPopping();
                    ExceptNums.Add(blocks[x, y].BlockNum);
                }

            ListCount++;
        }

        //  대각선 리스트
        for (int i = 0; i < Block.BLOCK_NUM_X; i++)
            ListsOfNums[ListCount].Add(blocks[i, i].BlockNum);

        if (CheckArithmetic(ListsOfNums[ListCount]))
            for (int i = 0; i < Block.BLOCK_NUM_X; i++)
            {
                blocks[i, i].toPopping();
                ExceptNums.Add(blocks[i, i].BlockNum);
            }

        ListCount++;

        for (int j = 0; j < Block.BLOCK_NUM_Y; j++)
            ListsOfNums[ListCount].Add(blocks[j, Block.BLOCK_NUM_Y - 1 - j].BlockNum);

        if (CheckArithmetic(ListsOfNums[ListCount]))
            for (int j = 0; j < Block.BLOCK_NUM_Y; j++)
            {
                blocks[j, Block.BLOCK_NUM_Y - 1 - j].toPopping();
                ExceptNums.Add(blocks[j, Block.BLOCK_NUM_Y - 1 - j].BlockNum);
            }

        ListCount++;
    }

    //  등차수열인지 확인
    private bool CheckArithmetic(List<int> nums)
    {
        nums.Sort();

        int gap = nums[0] - nums[1];

        for (int i = 0; i < nums.Count; i++)
            if (nums[i] == 0)
                return false;

        for (int i = 1; i < nums.Count - 1; i++)
            if (nums[i] - nums[i + 1] != gap)
                return false;

        BingoCount++;

        Source.PlayOneShot(Sound_Pop);

        return true;
    }


    private bool is_has_popping_block()
    {
        bool ret = false;

        foreach (BlockControl block in blocks)
        {
            if (block.pop_timer > 0.0f)
            {
                ret = true;
                break;
            }
        }

        return (ret);
    }

    // 슬라이드 중인 블록이 하나라도 있으면 true를 반환한다.
    private bool is_has_sliding_block()
    {
        bool ret = false;

        foreach (BlockControl block in blocks)
        {
            if (block.step == Block.STEP.SLIDE)
            {
                ret = true;
                break;
            }
        }

        return (ret);
    }

    private bool is_has_sliding_block_in_column(int x)
    {
        bool ret = false;
        for (int y = 0; y < Block.BLOCK_NUM_Y; y++)
        {
            // 슬라이드 중인 블록이 있으면,
            if (blocks[x, y].isSliding())
            {
                ret = true; // true를 반환한다. 
                break;
            }
        }
        return (ret);
    }

    public void Cheat_Click()
    {
        IsCheat = true;
    }

    public void Cheat_Bingo()
    {
        BingoCount++;
    }
}