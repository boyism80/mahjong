using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

public class GameBoard : MonoBehaviour {

    /// <summary>
    /// 현재 생성된 마작 카드 테이블
    /// </summary>
    private MoneySuitedCard[,] _moneySuitedCardTable;

    /// <summary>
    /// 행렬 수
    /// </summary>
    private int _row, _column;

    /// <summary>
    /// 블록 테이블
    /// </summary>
    private BlockTable _blockTable;

    /// <summary>
    /// 현재 선택된 카드
    /// </summary>
    private MoneySuitedCard _selected;

    /// <summary>
    /// 카드 스프라이트 리스트
    /// </summary>
    public Sprite[] Sprites;

    /// <summary>
    /// 카드 크기
    /// </summary>
    public Vector2 MoneySuitedCardSize;

    /// <summary>
    /// 카드 프리팹
    /// </summary>
    public MoneySuitedCard MoneySuitedCard;

    /// <summary>
    /// 초기 행렬 수
    /// </summary>
    public int InitRows = 6, InitColumn = 4;

    /// <summary>
    /// 라인 프리팹 (경로 효과)
    /// </summary>
    public DrawLine DrawLine;


    /// <summary>
    /// 세로는 반드시 짝수로 한다.
    /// 가로를 짝수로 해도 상관없다. 반드시 행x열은 짝수 형태를 띄어야 한다. (홀수면 짝이 안맞으니까;;)
    /// </summary>
    public int Rows
    {
        get
        {
            return this._row;
        }
        private set
        {
            this._row = (value % 2 == 0 ? value : value + 1);
        }
    }

    public int Columns
    {
        get
        {
            return this._column;
        }

        private set
        {
            this._column = value;
        }
    }

    /// <summary>
    /// 현재 선택된 카드
    /// </summary>
    public MoneySuitedCard Selected
    {
        get
        {
            return this._selected;
        }

        set
        {
            if(this._selected == value)
                return;

            if(this._selected != null)
                this._selected.Selected = false;

            this._selected = value;
            if(this._selected != null)
                this._selected.Selected = true;
        }
    }

    /// <summary>
    /// 현재 존재하는 카드의 리스트
    /// </summary>
    public MoneySuitedCard[] Exists
    {
        get
        {
            var list = new List<MoneySuitedCard>();

            for(var col = 0; col < this.Columns; col++)
            {
                for(var row = 0; row < this.Rows; row++)
                {
                    if(this._moneySuitedCardTable[col, row].gameObject.activeSelf)
                        list.Add(this._moneySuitedCardTable[col, row]);
                }
            }
            return list.ToArray();
        }
    }

    /// <summary>
    /// 현재 상태에서 더 이상 진행이 가능한지의 여부를 확인한다.
    /// </summary>
    private bool IsContinuable
    {
        get
        {
            foreach (var src in this.Exists)
            {
                foreach (var dest in this.GetExistsFromMatchingType(src.MatchingIndex))
                {
                    if(src.Equals(dest))
                        continue;

                    if (this._blockTable.GetRoute(src.Offset + 1, dest.Offset + 1) != null)
                        return true;
                }
            }
            return false;
        }
    }

    private bool IsClear
    {
        get
        {
            return this.Exists.Length == 0;
        }
    }

    public void Awake()
    {
        this.Rows = this.InitRows;
        this.Columns = this.InitColumn;
    }


	// Use this for initialization
	void Start () {
	
        this.Setup();
	}
	
	// Update is called once per frame
	void Update () {
	
        if(Input.GetButtonDown("Fire1") == false)
            return;

        var selectedNow = this.Select();
        if(selectedNow == null)
        {

        }
        else if(this.Selected == null)      // 선택된 카드가 없을 때
        {
            this.Selected = selectedNow;
        }
        else // 이전에 선택된 카드가 있었을 때
        {
            // 두 카드가 매칭될 수 있는지 검사한다.
            var route = this.Remove(this.Selected, selectedNow);
            if(route == null) // 매칭될 수 없을 때, 이전에 선택된 카드를 무효화하고 새로 갱신
            {
                this.Selected = selectedNow;
            }
            else if(this.IsClear) // 현재 스테이지 클리어인 경우 난이도를 높이고 게임 다시 시작
            {
                this.Rows++;
                this.Columns++;
                this.Setup();
            }
            else if(this.IsContinuable == false) // 더 이상 진행할 수 없을 때 셔플
            {
                this.Shuffle();
            }
            else if(this.DrawLine != null) // 경로를 표시한다.
            {
                var line = Instantiate<DrawLine>(this.DrawLine);
                line.OnComplete.AddListener(this.OnDrawLineComplete);
                var points = new List<Vector3>();
                foreach (var r in route.Reverse<Point>())
                    points.Add(this.GetMoneySuitedCardPosition(r.X - 1, r.Y - 1));

                line.Positions = points.ToArray();
            }
        }
	}

    private void OnDrawLineComplete(DrawLine drawLine)
    {
        Destroy(drawLine.gameObject);
    }

    public void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        for(var col = 0; col < this.Columns; col++)
        {
            for(var row = 0; row < this.Rows; row++)
                Gizmos.DrawWireCube(this.GetMoneySuitedCardPosition(row, col), this.MoneySuitedCardSize);
        }
    }

    /// <summary>
    /// 두 카드를 서로 제거한다.
    /// </summary>
    /// <param name="card1">첫 번째 카드</param>
    /// <param name="card2">두 번째 카드</param>
    /// <returns>성공시 두 카드 사이의 경로, 실패시 null</returns>
    private Stack<Point> Remove(MoneySuitedCard card1, MoneySuitedCard card2)
    {
        // 두 카드가 같은 형식이 아닌 경우
        if (card1.MatchingIndex != card2.MatchingIndex)
            return null;

        // 경로가 존재하지 않는 경우
        var route = this._blockTable.GetRoute(card1.Offset + 1, card2.Offset + 1);
        if(route == null)
            return null;

        // 블록 테이블에서 해당 위치 업데이트를 해주고 (통과할 수 있도록)
        this._blockTable.SetDontMove(card1.Offset + 1, false);
        this._blockTable.SetDontMove(card2.Offset + 1, false);

        // 두 카드 폭발 애니메이션 ㄱㄱ
        card1.Explode();
        card2.Explode();

        // 현재 선택된 카드 없음
        this.Selected = null;
        return route;
    }

    /// <summary>
    /// 카드 정보를 입력한다.
    /// </summary>
    /// <param name="index">카드 고유 인덱스</param>
    /// <param name="matchingIndex">카드 타입 인덱스</param>
    /// <param name="row">행</param>
    /// <param name="col">열</param>
    /// <returns>초기화된 카드 객체</returns>
    private MoneySuitedCard SetMoneySuitedCard(int index, int matchingIndex, int row, int col)
    {
        try
        {
            if(this._moneySuitedCardTable[col, row] == null)
                this._moneySuitedCardTable[col, row] = Instantiate<MoneySuitedCard>(this.MoneySuitedCard);

            _moneySuitedCardTable[col, row].Initialize(index, matchingIndex, row, col, this.Sprites[matchingIndex], this.MoneySuitedCardSize);
            _moneySuitedCardTable[col, row].transform.position = this.GetMoneySuitedCardPosition(row, col);
            return _moneySuitedCardTable[col, row];
        }
        catch(IndexOutOfRangeException)
        {
            return null;
        }
    }

    /// <summary>
    /// 카드의 위치를 얻는다.
    /// </summary>
    /// <param name="row">행</param>
    /// <param name="col">열</param>
    /// <returns></returns>
    public Vector2 GetMoneySuitedCardPosition(int row, int col)
    {
        return new Vector2(-this.Rows / 2 + row + this.MoneySuitedCardSize.x / 2.0f,
                           this.Columns / 2 - col - this.MoneySuitedCardSize.y / 2.0f);
    }

    public void Clear()
    {
        if(this._moneySuitedCardTable == null)
            return;

        for(int col = 0; col < this._moneySuitedCardTable.GetLength(0); col++)
        {
            for(int row = 0; row < this._moneySuitedCardTable.GetLength(1); row++)
                Destroy(this._moneySuitedCardTable[col, row].gameObject);
        }

        this._moneySuitedCardTable = null;
        this.Selected = null;
    }

    public void Resize(int row, int col)
    {
        this.Clear();
        this.Rows = row;
        this.Columns = col;
        this._moneySuitedCardTable = new MoneySuitedCard[this.Columns, this.Rows];

        this._blockTable = new BlockTable(this.Rows, this.Columns);
    }

    public void Setup()
    {
        this.Resize(this.Rows, this.Columns);

        var requireCardCount = this.Rows * this.Columns;
        for(var col = 0; col < this.Columns; col++)
        {
            for(var row = 0; row < this.Rows; row += 2)
            {
                var index = (col * this.Rows) + row;
                var matchingIndex = index % (Mathf.Min(this.Sprites.Length, requireCardCount / 2));

                this.SetMoneySuitedCard(index + 0, matchingIndex, row, col);
                this.SetMoneySuitedCard(index + 1, matchingIndex, row + 1, col);
            }
        }

        this.Shuffle();
    }

    private MoneySuitedCard[] GetExistsFromMatchingType(int matchingIndex)
    {
        var list = new List<MoneySuitedCard>();
        foreach(var card in this.Exists)
        {
            if(card.MatchingIndex == matchingIndex)
                list.Add(card);
        }

        return list.ToArray();
    }

    private void Swap(ref MoneySuitedCard card1, ref MoneySuitedCard card2)
    {
        var temp = card1;
        card1 = card2;
        card2 = temp;

        card1.Swap(card2);
    }


    /// <summary>
    /// 카드를 섞는다. (진행 가능할때까지 섞는다.)
    /// </summary>
    public void Shuffle()
    {
        this.Selected = null;

        var exists = this.Exists;
        for(var i = 0; i < exists.Length; i++)
        {
            var randomIndex = UnityEngine.Random.Range(0, exists.Length);
            var src = exists[i];
            var dest = exists[randomIndex];
            this.Swap(ref src, ref dest);
        }

        if (this.IsContinuable == false)
            this.Shuffle();
    }

    /// <summary>
    /// 카드를 선택한다.
    /// </summary>
    /// <returns></returns>
    public MoneySuitedCard Select()
    {
        // 마우스 위치에서 레이저를 쏴갈김
        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        var hit = Physics2D.GetRayIntersection(ray, Mathf.Infinity);
        if(hit == false)
            return null;

        // 레이저에 맞은 카드 선택
        var moneySuitedCard = hit.transform.GetComponent<MoneySuitedCard>();
        if(moneySuitedCard == null)
            return null;

        // 선택된 카드 리턴
        return moneySuitedCard;
    }
}