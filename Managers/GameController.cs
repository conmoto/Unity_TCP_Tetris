using UnityEngine;
using System.Collections;
using System.Net.Sockets;
using UnityEngine.UI;
using System;

public class GameController : MonoBehaviour {

	public static GameController Instance;
	public bool gameStarted;
	bool isMultiPlay;
	public GameObject m_gameStartButton;
    public GameObject m_exitButton;
	Board m_gameBoard;
	Spawner m_spawner;
	Shape m_activeShape;

    float m_defaultDropInterval = 0.4f;
	public float m_dropInterval = 0.4f;

    float m_timerToSpeedUp = 0f;
    float m_timeSpeedLevel1 = 50f;
    float m_timeSpeedLevel2 = 110f;

    float m_timeToDrop;

	float m_timeToNextKeyLeftRight;

	[Range(0.02f,1f)]
	public float m_keyRepeatRateLeftRight = 0.25f;

	float m_timeToNextKeyDown;

	[Range(0.01f,0.5f)]
	public float m_keyRepeatRateDown = 0.01f;

	float m_timeToNextKeyRotate;

	[Range(0.02f,1f)]
	public float m_keyRepeatRateRotate = 0.02f;

	public GameObject m_gameOverPanel;
	public Text m_gameOverLabel;

	bool m_gameOver = false;


	void Awake()
	{
		if (Instance)
		{
			Destroy(this.gameObject);
		}
		Instance = this;
	}
	void Start () 
	{
		isMultiPlay = LoadManager.Instance.IsMultiPlay;
		// find spawner and board with generic version of GameObject.FindObjectOfType, slower but less typing
		m_gameBoard = GameObject.Find("BoardPlayer").GetComponent<Board>();
		m_spawner = GameObject.FindObjectOfType<Spawner>();

		m_timeToNextKeyDown = Time.time + m_keyRepeatRateDown;
		m_timeToNextKeyLeftRight = Time.time + m_keyRepeatRateLeftRight;
		m_timeToNextKeyRotate = Time.time + m_keyRepeatRateRotate;

		if (!m_gameBoard)
		{
			Debug.LogWarning("WARNING!  There is no game board defined!");
		}
		if (!m_spawner)
		{
			Debug.LogWarning("WARNING!  There is no spawner defined!");
		}
		else
		{
			m_spawner.transform.position = Vectorf.Round(m_spawner.transform.position);
		}

		if (m_gameOverPanel)
		{
			m_gameOverPanel.SetActive(false);
		}

		//호스트 플레이어에게만 Start 버튼 활성화
		if (isMultiPlay && !NetworkManager.Instance.isHostClient)
		{
			m_gameStartButton.SetActive(false);
		}
	}

	// Update is called once per frame
	void Update () 
	{
		// if we are missing a spawner or game board or active shape, then we don't do anything
		if (!gameStarted || !m_spawner || !m_gameBoard || !m_activeShape || m_gameOver)
		{
			return;
		}

        if (m_timerToSpeedUp < m_timeSpeedLevel2)
        {
            CountSpeedUpTimer();
        }
		PlayerInput ();
	}

    private void CountSpeedUpTimer()
    {
        m_timerToSpeedUp += Time.deltaTime;
        if (m_timerToSpeedUp >= m_timeSpeedLevel2)
        {
            m_dropInterval = 0.15f;
        }
        else if (m_timerToSpeedUp > m_timeSpeedLevel1)
        {
            m_dropInterval = 0.25f;
        }
    }

    void PlayerInput ()
	{
		//채팅중에도 블럭은 계속 내려오게
		if(isMultiPlay)
		{
			if (ChatManager.Instance.ingTyping)
			{
				if (Time.time > m_timeToDrop)
				{
					m_timeToDrop = Time.time + m_dropInterval;

					m_activeShape.MoveDown();

					if (!m_gameBoard.IsValidPosition(m_activeShape))
					{
						if (m_gameBoard.IsOverLimit(m_activeShape))
						{
							GameOver();
						}
						else
						{
							LandShape();
						}
					}
				}
				return;
			}
		}
		
		if (Input.GetButton ("MoveRight") && (Time.time > m_timeToNextKeyLeftRight) || Input.GetButtonDown ("MoveRight")) 
		{
			m_activeShape.MoveRight ();
			m_timeToNextKeyLeftRight = Time.time + m_keyRepeatRateLeftRight;

			if (!m_gameBoard.IsValidPosition (m_activeShape)) 
			{
				m_activeShape.MoveLeft ();
			}

		}
		else if  (Input.GetButton ("MoveLeft") && (Time.time > m_timeToNextKeyLeftRight) || Input.GetButtonDown ("MoveLeft")) 
		{
			m_activeShape.MoveLeft ();
			m_timeToNextKeyLeftRight = Time.time + m_keyRepeatRateLeftRight;

			if (!m_gameBoard.IsValidPosition (m_activeShape)) 
			{
				m_activeShape.MoveRight ();
			}

		}
        //else if  (Input.GetButtonDown ("Rotate") && (Time.time > m_timeToNextKeyRotate)) 
        else if (Input.GetButtonDown("Rotate"))
        {
			m_activeShape.RotateRight();

			if (!m_gameBoard.IsValidPosition (m_activeShape)) 
			{
				m_activeShape.RotateLeft();
			}

		}
		else if  (Input.GetButton ("MoveDown") && (Time.time > m_timeToNextKeyDown) ||  (Time.time > m_timeToDrop)) 
		{
			m_timeToDrop = Time.time + m_dropInterval;
			m_timeToNextKeyDown = Time.time + m_keyRepeatRateDown;

			m_activeShape.MoveDown ();

			if (!m_gameBoard.IsValidPosition (m_activeShape)) 
			{
				if (m_gameBoard.IsOverLimit(m_activeShape))
				{
					GameOver ();
				}
				else
				{
					LandShape ();
				}
			}
		}
        else if (Input.GetKeyDown(KeyCode.Space))
        {
            m_timeToDrop = Time.time + m_dropInterval;
            m_timeToNextKeyDown = Time.time + m_keyRepeatRateDown;

            while (m_gameBoard.IsValidPosition(m_activeShape))
            {
                m_activeShape.MoveDown();
            }
            if (m_gameBoard.IsOverLimit(m_activeShape))
            {
                GameOver();
            }
            else
            {
                LandShape();
            }
        }
	}

	// shape lands
	void LandShape ()
	{
		// move the shape up, store it in the Board's grid array
		m_activeShape.MoveUp ();
		m_gameBoard.StoreShapeInGrid (m_activeShape);

		// spawn a new shape
		m_activeShape = m_spawner.SpawnShape ();

		// set all of the timeToNextKey variables to current time, so no input delay for the next spawned shape
		m_timeToNextKeyLeftRight = Time.time;
		m_timeToNextKeyDown = Time.time;
		m_timeToNextKeyRotate = Time.time;

		// remove completed rows from the board if we have any 
		m_gameBoard.ClearAllRows();
	}

	// triggered when we are over the board's limit
	void GameOver ()
	{
		// move the shape one row up
		m_activeShape.MoveUp ();

		// set the game over condition to true
		m_gameOver = true;
		gameStarted = false;

		// turn on the Game Over Panel
		if (m_gameOverPanel) {
            m_gameOverLabel.text = "You Lose!!";
            m_gameOverPanel.SetActive (true);
		}
		
		if (isMultiPlay)
		{
            if (NetworkManager.Instance.isHostClient)
            {
                m_gameStartButton.SetActive(true);
            }
            m_exitButton.SetActive(true);
			//자신은 브로드캐스팅 상대가 아니여야 하고 다른 플레이어들에겐 GameWin을 호출하고 자신은 Lose한다.
			SendGameEnd();
		}
	}
	public void GameWin()
	{
		m_gameOver = true;
		gameStarted = false;

        if (NetworkManager.Instance.isHostClient)
        {
            m_gameStartButton.SetActive(true);
        }
        m_exitButton.SetActive(true);

        // turn on the Game Over Panel
        if (m_gameOverPanel)
		{
			m_gameOverLabel.text = "You Win!!";
			m_gameOverPanel.SetActive(true);
		}
	}

	public void RestartGame()
	{
		// Reset Game
		if (m_gameOver)
		{
			GameObject[] shapes = GameObject.FindGameObjectsWithTag("Shape");

			foreach(GameObject shape in shapes)
			{
				Destroy(shape);
			}

			GameObject[] boards = GameObject.FindGameObjectsWithTag("Board");
			foreach (GameObject board in boards)
			{
				board.GetComponent<Board>().ResetBoard();
			}
			m_gameOver = false;
			m_gameOverPanel.SetActive(false);
		}

		m_gameStartButton.SetActive(false);
        m_exitButton.SetActive(false);

        m_dropInterval = m_defaultDropInterval;
        m_timerToSpeedUp = 0f;
		// 초기화할 시간 여유
		Invoke("StartGame", 0.5f);
	}
	public void StartGame()
	{
		gameStarted = true;
		if (!m_activeShape)
		{
			m_activeShape = m_spawner.SpawnShape();
		}
	}

	/*Network*/
	public void SendGameStart()
	{
		NetworkStream stream = NetworkManager.Instance.GetStream;
		//첫번째 바이트는 메세지(1), 보드정보(2), 상대공격(3), 게임시작(4)
		byte[] dataType = new byte[1];
		dataType[0] = 4;
		stream.Write(dataType, 0, dataType.Length);
		stream.Flush();
	}
	public void SendGameEnd()
	{
		NetworkStream stream = NetworkManager.Instance.GetStream;
        //첫번째 바이트는 메세지(1), 보드정보(2), 상대공격(3), 게임시작(4), 게임 승리(5)
        byte[] dataType = new byte[1];
		dataType[0] = 5;
		stream.Write(dataType, 0, dataType.Length);
		stream.Flush();
	}

    public void ExitGame()
    {
        ChatManager.Instance.Disconnect();
        LoadManager.Instance.OnSelectExitButton();
    }
}
