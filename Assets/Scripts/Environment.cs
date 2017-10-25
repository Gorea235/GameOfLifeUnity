using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Environment : MonoBehaviour
{
    // game rules
    public static readonly int[] newLife = { 3 };
    public static readonly int[] keepAlive = { 2, 3 };
    public const float secsPerTick = 0.5f;

    // unity-linked fields
    public Texture2D texAlive;
    public Texture2D texDead;
    public GameObject cellBase;

    // game variabled
    const int _gridWidth = 50;
    const int _gridHeight = 50;
    const float _cellSize = 1;
    CellObj[,] _grid;
    bool _running = false;
    bool _hasRun = false;
    float _timeSinceLastTick = secsPerTick;
    bool _limitTicks = true;
    bool _loopSides = true;

    // control variables
    Rect _startStopButton;
    Rect _resetButton;
    Rect _limitTicksToggleButton;
    Rect _loopSidesButton;
    Vector3 _mouseStartPan;
    Vector3 _cameraStartPos;
    Vector3 _minMouseMovePan;
    bool _mouseIsPanning = false;
    float _mousePanScale = -20.0f;

    struct Point2
    {
        public int x { get; private set; }
        public int y { get; private set; }

        public Point2(int x, int y)
        {
            this.x = x;
            this.y = y;
        }
    }

    enum CellState
    {
        Alive,
        Dead
    }

    class CellObj
    {
        readonly Environment _parent;
        readonly GameObject _obj;
        readonly Cell _logic;

        public int X
        {
            get;
            private set;
        }

        public int Y
        {
            get;
            private set;
        }

        CellState _currentState;
        CellState _newState;
        public CellState State
        {
            get { return _currentState; }
        }

        public CellObj(Environment parent, int x, int y)
        {
            _parent = parent;
            _obj = Instantiate(_parent.cellBase);
            _logic = _obj.GetComponent<Cell>();

            Kill();
            Apply(true);
            X = x;
            Y = y;
            _logic.transform.position = new Vector3(
                (X - (_gridWidth / 2)) * _cellSize,
                (Y - (_gridHeight / 2)) * _cellSize,
                0);
        }

        void SetCell(Texture2D texture)
        {
            _logic.spriteRenderer.sprite = Sprite.Create(texture,
                new Rect(0, 0, texture.width, texture.width), new Vector2(0f, 0f), texture.width / _cellSize);
        }

        void SetCellAlive()
        {
            SetCell(_parent.texAlive);
        }

        void SetCellDead()
        {
            SetCell(_parent.texDead);
        }

        public void DoTick()
        {
            int livingNear = 0;
            for (int x = X - 1; x <= X + 1; x++)
                for (int y = Y - 1; y <= Y + 1; y++)
                    if (!(x == X && y == Y) && _StateOf(x, y) == CellState.Alive)
                        livingNear++;
            if (State == CellState.Alive && !keepAlive.Contains(livingNear))
                Kill();
            else if (State == CellState.Dead && newLife.Contains(livingNear))
                Live();
        }

        CellState _StateOf(int x, int y)
        {
            int? t = _ClampCoord(x, _gridWidth);
            if (!t.HasValue)
                return CellState.Dead;
            x = (int)t;
            t = _ClampCoord(y, _gridHeight);
            if (!t.HasValue)
                return CellState.Dead;
            y = (int)t;
            return _parent._grid[x, y].State;
        }

        int? _ClampCoord(int c, int max)
        {
            if (c < 0)
            {
                if (_parent._loopSides)
                    return max - 1;
                return null;
            }
            if (c >= max)
            {
                if (_parent._loopSides)
                    return 0;
                return null;
            }
            return c;
        }

        public void Live()
        {
            _newState = CellState.Alive;
        }

        public void Kill()
        {
            _newState = CellState.Dead;
        }

        public void Apply(bool force = false)
        {
            if (!force && _currentState == _newState)
                return;
            _currentState = _newState;
            if (_currentState == CellState.Alive)
                SetCellAlive();
            else if (_currentState == CellState.Dead)
                SetCellDead();
            //Debug.LogFormat("set cell {0},{1} to {2}", X, Y, _currentState);
        }

        public void BuildSwitch()
        {
            if (State == CellState.Alive)
            {
                Kill();
                Apply();
            }
            else if (State == CellState.Dead)
            {
                Live();
                Apply();
            }
        }
    }

    Point2 MouseGridHover()
    {
        Vector3 mWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        return new Point2(
            (int)Math.Floor(mWorld.x) + (_gridWidth / 2),
            (int)(Math.Floor(mWorld.y) + (_gridHeight / 2)));
    }

    void Awake()
    {
        _grid = new CellObj[_gridWidth, _gridHeight];

        _startStopButton = new Rect(10,
                                    10,
                                    Screen.width * 0.1f,
                                    Screen.height * 0.08f);
        _resetButton = new Rect(10,
                                (Screen.height * 0.08f) + 20,
                                Screen.width * 0.1f,
                                Screen.height * 0.08f);
        _limitTicksToggleButton = new Rect((Screen.width * 0.1f) + 20,
                                           10,
                                           Screen.width * 0.2f,
                                           Screen.height * 0.08f);
        _loopSidesButton = new Rect((Screen.width * 0.1f) + 20,
                                           (Screen.height * 0.08f) + 20,
                                           Screen.width * 0.2f,
                                           Screen.height * 0.08f); 
        float minMoveDistance = (Screen.width / Screen.height);
        _minMouseMovePan = new Vector3(minMoveDistance, minMoveDistance);
    }

    // Use this for initialization
    void Start()
    {
        for (int x = 0; x < _gridWidth; x++)
            for (int y = 0; y < _gridHeight; y++)
                _grid[x, y] = new CellObj(this, x, y);
    }

    // Update is called once per frame
    void Update()
    {
        if (_running)
        {
            _timeSinceLastTick += Time.deltaTime;
            if (_timeSinceLastTick > secsPerTick || !_limitTicks)
            {
                for (int x = 0; x < _gridWidth; x++)
                    for (int y = 0; y < _gridHeight; y++)
                        _grid[x, y].DoTick();
                for (int x = 0; x < _gridWidth; x++)
                    for (int y = 0; y < _gridHeight; y++)
                        _grid[x, y].Apply();
                _timeSinceLastTick = 0;
            }
        }

        // mouse handling
        if (Input.GetMouseButtonDown(0))
        {
            _mouseStartPan = Input.mousePosition;
            _cameraStartPos = Camera.main.transform.position;
        }

        if (Input.GetMouseButton(0))
        {
            if (!_mouseIsPanning && (Input.mousePosition - _mouseStartPan).sqrMagnitude > _minMouseMovePan.sqrMagnitude)
                _mouseIsPanning = true;
            if (_mouseIsPanning) // we are now padding the camera
            {
                Vector3 move = Camera.main.ScreenToViewportPoint(Input.mousePosition - _mouseStartPan);
                //Vector3 move = Input.mousePosition - _mouseStartPan;
                move = new Vector3(move.x * _mousePanScale, //* (Screen.width / Screen.height),
                                   move.y * _mousePanScale); //* (Screen.height / Screen.width));
                Camera.main.transform.position = _cameraStartPos + move;
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            if (!_mouseIsPanning)
            {
                if (!_hasRun)
                {
                    Point2 gridC = MouseGridHover();
                    Debug.LogFormat("mouse clicked on {0},{1}", gridC.x, gridC.y);
                    if (gridC.x >= 0 && gridC.x < _gridWidth && gridC.y >= 0 && gridC.y < _gridHeight)
                        _grid[gridC.x, gridC.y].BuildSwitch();
                }
            }
            else
                _mouseIsPanning = false;
        }
    }

    void OnGUI()
    {
        if (_running)
        {
            if (GUI.Button(_startStopButton, "Stop"))
                _running = false;
        }
        else
        {
            if (GUI.Button(_startStopButton, "Start"))
                _running = _hasRun = true;
        }

        if (_hasRun)
        {
            if (GUI.Button(_resetButton, "Reset"))
            {
                _running = _hasRun = false;
                for (int x = 0; x < _gridWidth; x++)
                {
                    for (int y = 0; y < _gridHeight; y++)
                    {
                        _grid[x, y].Kill();
                        _grid[x, y].Apply();
                    }
                }
            }
        }

        if (_limitTicks)
        {
            if (GUI.Button(_limitTicksToggleButton, "Disable Limit"))
                _limitTicks = false;
        }
        else
        {
            if (GUI.Button(_limitTicksToggleButton, "Enable Limit"))
                _limitTicks = true;
        }

        if (_loopSides)
        {
            if (GUI.Button(_loopSidesButton, "Disable Looping Sides"))
                _loopSides = false;
        }
        else
        {
            if (GUI.Button(_loopSidesButton, "Enable Looping Sides"))
                _loopSides = true;
        }
        //GUI.DrawTexture(new Rect(10, 10, 11, 11), texDead);
    }
}
