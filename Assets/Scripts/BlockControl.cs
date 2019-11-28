using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

// 블록에 관한 정보를 다룬다.
public class Block
{
    public static float COLLISION_SIZE = 1.0f; // 블록의 충돌 크기.
    public static float POP_TIME = 0.5f; // 불 붙고 터질 때까지의 시간.

    public struct iPosition
    { // 그리드에서의 좌표를 나타내는 구조체.
        public int x; // X 좌표.
        public int y; // Y 좌표.
    }

    public enum COLOR
    { // 블록 색상.
        NONE = -1, // 색 지정 없음.
        PINK = 0, // 분홍색.
        BLUE, // 파란색.
        YELLOW, // 노란색.
        GREEN, // 녹색.
        MAGENTA, // 마젠타.
        ORANGE, // 주황색.
        GRAY, // 그레이.
        NUM, // 컬러가 몇 종류인지 나타낸다(=7).
        FIRST = PINK, // 초기 컬러(분홍색).
        LAST = ORANGE, // 최종 컬러(주황색).
        NORMAL_COLOR_NUM = GRAY, // 보통 컬러(회색 이외의 색)의 수.
    };

    public enum DIR4
    { // 상하좌우 네 방향.
        NONE = -1, // 방향지정 없음.
        RIGHT, // 우.
        LEFT, // 좌.
        UP, // 상.
        DOWN, // 하.
        NUM, // 방향이 몇 종류 있는지 나타낸다(=4).
    };

    // 블록이 어떤 상태인지 알려주는 클래스
    public enum STEP
    {
        NONE = -1, // 상태 정보 없음.
        IDLE = 0, // 대기 중.
        GRABBED, // 잡혀 있음.
        RELEASED, // 떨어진 순간.
        SLIDE, // 슬라이드 중.
        POP, // 터지는 중.
        RESPAWN, // 재생성 중.
        DUMMY, // 더미 상태.
        LONG_SLIDE, // 크게 슬라이드 중.
        NUM, // 상태가 몇 종류인지 표시.
    };

    public static int BLOCK_NUM_X = 5;
    // 블록을 배치할 수 있는 X방향 최대수.
    public static int BLOCK_NUM_Y = 5;
    // 블록을 배치할 수 있는 Y방향 최대수.
}


public class BlockControl : MonoBehaviour
{
    public List<Material> Mats;

    // 블록을 조작하는 클래스이다.
    public int color = 0; // 블록 색.
    public BlockRoot block_root = null; // 블록의 신.
    public Block.iPosition i_pos; // 블록 좌표.

    //  블록에 할당되는 숫자
    public int BlockNum = 0;
    public TextMeshPro NumberUI;

    //  클릭할 때
    public Block.STEP step = Block.STEP.NONE; // 지금 상태.
    public Block.STEP next_step = Block.STEP.NONE; // 다음 상태.
    private Vector3 position_offset_initial = Vector3.zero; // 교체 전 위치.
    public Vector3 position_offset = Vector3.zero; // 교체 후 위치.

    //  이동할 때
    public float pop_timer = -1.0f; // 블록이 터질 때까지의 시간.
    public Block.DIR4 slide_dir = Block.DIR4.NONE; // 슬라이드된 방향.
    public float step_timer = 0.0f; // 블록이 교체된 때의 이동시간 등.

    //  태울 때
    public Material transparent_material; // 반투명 머티리얼

    //  채울 때

    void Awake()
    {
        NumberUI = GetComponentInChildren<TextMeshPro>();

        next_step = Block.STEP.IDLE; // 다음 블록을 대기중으로.
    }

    void Update()
    {
        NumberUI.text = BlockNum.ToString();

        Vector3 mouse_position; // 마우스 위치.

        block_root.unprojectMousePosition( // 마우스 위치 획득.
        out mouse_position, Input.mousePosition);

        // 획득한 마우스 위치를 X와 Y만으로 한다.
        Vector2 mouse_position_xy = new Vector2(mouse_position.x, mouse_position.y);

        //  이동 타이머
        if (pop_timer >= 0.0f) // 타이머가 0 이상이면.
        {
            pop_timer -= Time.deltaTime; // 타이머의 값을 줄인다.

            if (pop_timer < 0.0f) // 타이머가 0 미만이면.
            {
                if (step != Block.STEP.SLIDE) // 슬라이드 중이 아니라면.
                {
                    pop_timer = -1.0f;
                    next_step = Block.STEP.POP; // 상태를 ‘소멸 중’으로.
                }

                else
                {
                    pop_timer = 0.0f;
                }
            }
        }

        step_timer += Time.deltaTime;
        float slide_time = 0.2f;

        //  상태 step 정리
        if (next_step == Block.STEP.NONE) // '상태 정보 없음'의 경우.
        {
            switch (step)
            {
                case Block.STEP.SLIDE:
                    if (step_timer >= slide_time)
                    {
                        // 슬라이드 중인 블록이 소멸되면 POP(터지는) 상태로 이행.
                        if (pop_timer == 0.0f)
                        {
                            next_step = Block.STEP.POP;
                        }
                            
                        // pop_timer가 0이 아니면 IDLE(대기) 상태로 이행.
                        else
                        {
                            next_step = Block.STEP.IDLE;
                        }
                    }

                    break;

                case Block.STEP.IDLE:
                    GetComponent<Renderer>().enabled = true;
                    break;

                case Block.STEP.POP:
                    next_step = Block.STEP.DUMMY;
                    break;
            }
        }

        // '다음 블록' 상태가 '정보 없음' 이외인 동안.
        // ＝'다음 블록' 상태가 변경된 경우.
        while (next_step != Block.STEP.NONE)
        {
            step = next_step;
            next_step = Block.STEP.NONE;

            switch (step)
            {
                case Block.STEP.IDLE: // '대기' 상태.
                    position_offset = Vector3.zero;
                    // 블록 표시 크기를 보통 크기로 한다.
                    transform.localScale = Vector3.one * 1.0f;
                    break;

                case Block.STEP.GRABBED: // '잡힌' 상태.
                    // 블록 표시 크기를 크게 한다.
                    transform.localScale = Vector3.one * 1.2f;
                    slide_dir = calcSlideDir(mouse_position_xy); // 잡힌 상태일 때는 항상 슬라이드 방향을 체크.
                    break;

                case Block.STEP.RELEASED: // '떨어져 있는' 상태.
                    position_offset = Vector3.zero;
                    // 블록 표시 크기를 보통 사이즈로 한다.
                    transform.localScale = Vector3.one * 1.0f;
                    break;

                case Block.STEP.POP:
                    position_offset = Vector3.zero;
                    setVisible(false);
                    break;

                case Block.STEP.SLIDE: // 슬라이드(교체) 중.                                       
                    float rate = step_timer / slide_time; // 블록을 서서히 이동하는 처리.
                    rate = Mathf.Min(rate, 1.0f);
                    rate = Mathf.Sin(rate * Mathf.PI / 2.0f);
                    position_offset = Vector3.Lerp(position_offset_initial, Vector3.zero, rate);
                    setColor(BlockNum);
                    break;

                case Block.STEP.RESPAWN:
                    next_step = Block.STEP.DUMMY;
                    break;

                case Block.STEP.DUMMY:
                    setVisible(true); // 블록을 표시.
                    NumberUI.gameObject.SetActive(false);
                    break;
            }

            step_timer = 0.0f;
        }

        switch (step)
        {
            case Block.STEP.GRABBED: // 잡힌 상태.                                     
                // 잡힌 상태일 때는 항상 슬라이드 방향을 체크.
                slide_dir = calcSlideDir(mouse_position_xy);
                break;

            case Block.STEP.SLIDE: // 슬라이드(교체) 중.
                // 블록을 서서히 이동하는 처리.
                float rate = step_timer / slide_time;
                rate = Mathf.Min(rate, 1.0f);
                rate = Mathf.Sin(rate * Mathf.PI / 2.0f);
                position_offset = Vector3.Lerp(position_offset_initial, Vector3.zero, rate);
                break;

            case Block.STEP.DUMMY:
                BlockNum = 0;
                setColor(BlockNum);
                break;
        }

        // 그리드 좌표를 실제 좌표(씬의 좌표)로 변환하고.
        // position_offset을 추가한다.
        Vector3 position = BlockRoot.calcBlockPosition(i_pos) + position_offset;

        // 실제 위치를 새로운 위치로 변경한다.
        transform.position = position;

        //  터지는 효과
        if (pop_timer >= 0.0f)
        {
            // 현재 색과 흰색의 중간 색.
            Color color0 = Color.Lerp(GetComponent<Renderer>().material.color,
            Color.white, 0.5f);

            // 현재 색과 검은색의 중간 색.
            Color color1 = Color.Lerp(GetComponent<Renderer>().material.color,
            Color.black, 0.5f);

            // 터지는 연출 시간이 절반을 지났다면.
            if (pop_timer < Block.POP_TIME / 2.0f)
            {
                // 투명도(a)를 설정.
                color0.a = pop_timer / (Block.POP_TIME / 2.0f);
                color1.a = color0.a;

                // 반투명 머티리얼을 적용.
                setColor(BlockNum);
            }

            // pop_timer가 줄어들수록 1에 가까워진다.
            float rate = 1.0f - pop_timer / Block.POP_TIME;

            // 서서히 색을 바꾼다.
            GetComponent<Renderer>().material.color = Color.Lerp(color0, color1, rate);
        }
    }


    // 인수 color의 색으로 블록을 칠한다.
    public void setColor(int blockNum)
    {
        //this.color = color; // 이번에 지정된 색을 멤버 변수에 보관한다.
        //Color color_value; // Color 클래스는 색을 나타낸다.

        //switch (color) // 칠할 색에 따라서 갈라진다.
        //{
        //    default:
        //    case Block.COLOR.PINK:
        //        color_value = new Color(1.0f, 0.5f, 0.5f);
        //        break;
        //    case Block.COLOR.BLUE:
        //        color_value = Color.blue;
        //        break;
        //    case Block.COLOR.YELLOW:
        //        color_value = Color.yellow;
        //        break;
        //    case Block.COLOR.GREEN:
        //        color_value = Color.green;
        //        break;
        //    case Block.COLOR.MAGENTA:
        //        color_value = Color.magenta;
        //        break;
        //    case Block.COLOR.ORANGE:
        //        color_value = new Color(1.0f, 0.46f, 0.0f);
        //        break;
        //    case Block.COLOR.GRAY:
        //        color_value = new Color(0.8f, 0.8f, 0.8f);
        //        break;
        //}
        //// 이 게임 오브젝트의 머티리얼 색상을 변경한다.
        //GetComponent<Renderer>().material.color = color_value;

        if (blockNum == 0)
        {
            GetComponent<Renderer>().material = Mats[5];
            return;
        }

        switch (((blockNum - 1) % 25) / 5 ) // 칠할 색에 따라서 갈라진다.
        {
            default:
            case 0:
                GetComponent<Renderer>().material = Mats[0];
                break;
            case 1:
                GetComponent<Renderer>().material = Mats[1];
                break;
            case 2:
                GetComponent<Renderer>().material = Mats[2];
                break;
            case 3:
                GetComponent<Renderer>().material = Mats[3];
                break;
            case 4:
                GetComponent<Renderer>().material = Mats[4];
                break;
        }
    }


    public void beginGrab() // 잡혔을 때 호출한다.
    {
        next_step = Block.STEP.GRABBED;
    }

    public void endGrab() // 놓았을 때 호출한다.
    {
        next_step = Block.STEP.IDLE;
    }

    public bool isGrabbable() // 잡을 수 있는 상태 인지 판단한다.
    {
        bool is_grabbable = false;

        switch (step)
        {
            case Block.STEP.IDLE: // '대기' 상태일 때와 더미상태인 블록은
            case Block.STEP.DUMMY:
                is_grabbable = true; // true(잡을 수 있다)를 반환한다.
                break;
         }

        return (is_grabbable);
    }

    // 지정된 마우스 좌표가 자신과 겹치는지 반환한다.
    public bool isContainedPosition(Vector2 position)
    {
        bool ret = false;
        Vector3 center = this.transform.position;
        float h = Block.COLLISION_SIZE / 2.0f;

        do
        {
            // X 좌표가 자신과 겹치지 않으면 break로 루프를 빠져 나간다.
            if (position.x < center.x - h || center.x + h < position.x)
            {
                break;
            }

            // Y 좌표가 자신과 겹치지 않으면 break로 루프를 빠져 나간다.
            if (position.y < center.y - h || center.y + h < position.y)
            {
                break;
            }

            // X 좌표, Y 좌표 모두 겹쳐 있으면 true(겹쳐 있다)를 반환한다.
            ret = true;

        } while (false);

        return (ret);
    }


    // 마우스 위치를 바탕으로 슬라이드된 방향을 구한다.
    public Block.DIR4 calcSlideDir(Vector2 mouse_position)
    {
        Block.DIR4 dir = Block.DIR4.NONE;

        // 지정된 mouse_position과 현재 위치의 차를 나타내는 벡터.
        Vector2 v = mouse_position - new Vector2(this.transform.position.x, this.transform.position.y);

        // 벡터의 크기가 0.1보다 크면.
        // (그보다 작으면 슬라이드하지 않은 걸로 간주한다).
        if (v.magnitude > 0.1f)
        {
            if (v.y > v.x)
            {
                if (v.y > -v.x)
                {
                    dir = Block.DIR4.UP;
                }

                else
                {
                    dir = Block.DIR4.LEFT;
                }
            }

            else
            {
                if (v.y > -v.x)
                {
                    dir = Block.DIR4.RIGHT;
                }

                else
                {
                    dir = Block.DIR4.DOWN;
                }
            }
        }

        return (dir);
    }

    // 현재 위치와 슬라이드할 곳의 거리가 어느 정도인가 반환한다.
    public float calcDirOffset(Vector2 position, Block.DIR4 dir)
    {
        float offset = 0.0f;

        // 지정된 위치와 블록의 현재 위치의 차를 나타내는 벡터.
        Vector2 v = position - new Vector2(transform.position.x, transform.position.y);

        switch (dir) // 지정된 방향에 따라 갈라진다.
        {
            case Block.DIR4.RIGHT:
                offset = v.x;
                break;
            case Block.DIR4.LEFT:
                offset = -v.x;
                break;
            case Block.DIR4.UP:
                offset = v.y;
                break;
            case Block.DIR4.DOWN:
                offset = -v.y;
                break;
        }

        return (offset);
    }

    // 이동 시작을 알리는 메서드
    public void beginSlide(Vector3 offset)
    {
        position_offset_initial = offset;
        position_offset = position_offset_initial;

        // 상태를 SLIDE로 변경.
        next_step = Block.STEP.SLIDE;
    }


    public void toPopping()
    {
        pop_timer = Block.POP_TIME;
    }

    public bool isPopping()
    {
        // pop_timer가 0보다 크면 true.
        bool is_poping = (pop_timer > 0.0f);
        return (is_poping);
    }

    public bool isVisible()
    {
        // 그리기 가능(renderer.enabled가 true) 상태라면 표시.
        bool is_visible = GetComponent<Renderer>().enabled;

        return (is_visible);
    }

    public void setVisible(bool is_visible)
    {
        // 그리기 가능 설정에 인수를 대입.
        GetComponent<Renderer>().enabled = is_visible;
    }

    public bool isIdle()
    {
        bool is_idle = false;

        // 현재 블록 상태가 '대기 중'이고, 다음 블록 상태가 '없음'이면.
        if (step == Block.STEP.IDLE && next_step == Block.STEP.NONE)
        {
            is_idle = true;
        }

        return (is_idle);
    }


    //  더미블록으로 변경
    public void beginRespawn()
    {
        next_step = Block.STEP.DUMMY;

        setColor(BlockNum);
    }

    // 블록이 비표시(그리드상의 위치가 텅 빔)로 되어 있다면 true를 반환한다.
    public bool isVacant()
    {
        bool is_vacant = false;

        if(step == Block.STEP.POP && next_step == Block.STEP.NONE)
        {
            is_vacant = true;
        }

        return (is_vacant);
    }

    // 교체 중(슬라이드 중)이라면 true를 반환한다.
    public bool isSliding()
    {
        bool is_sliding = (position_offset.x != 0.0f);

        return (is_sliding);
    }
}