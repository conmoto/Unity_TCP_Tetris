using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;

public class Board : MonoBehaviour {
	bool isMultiPlay;
	public bool isPlayerBoard;
    public Shape shapeB;
    // a SpriteRenderer that will be instantiated in a grid to create our board
	public Transform m_emptySprite;
	public Transform m_roundedBlock;
	public int m_height = 30;
	public int m_width = 10;


	byte numEmptyCell = 9;
	// number of rows where we won't have grid lines at the top
	public int m_header = 8;

	// store inactive shapes here
	Transform[,] m_grid;
	public byte[] m_byteGrid;

	void Awake()
	{
		m_grid = new Transform[m_width, m_height];
	}
	void Start () {
		DrawEmptyCells();
		isMultiPlay = LoadManager.Instance.IsMultiPlay;
		if (isMultiPlay)
		{
			m_byteGrid = new byte[m_width * m_height];
			for (int y = 0; y < m_height; y++)
			{
				for (int x = 0; x < m_width; x++)
				{
					m_byteGrid[x + y * m_width] = numEmptyCell;// Empty cell

					if (!isPlayerBoard)
					{
						//EmptyCell은 Transform배열에 저장하지 않고 좌표만 계산하여 그리기만 함.
						Transform clone;
						clone = Instantiate(m_roundedBlock, transform.position + new Vector3(x, y, 0), Quaternion.identity) as Transform;
						m_grid[x, y] = clone;

						// parents all of the empty squares to the Board object
						clone.transform.parent = transform;
						clone.gameObject.SetActive(false);
					}

				}
			}
		}
	}

	// draw our empty board with our empty sprite object
	void DrawEmptyCells()
	{
		if (m_emptySprite)
		{
			for (int y = 0; y < m_height - m_header; y++)
			{
				for (int x = 0; x < m_width; x++)
				{
					//EmptyCell은 Transform배열에 저장하지 않고 좌표만 계산하여 그리기만 함.
					Transform clone;
					//clone = Instantiate(m_emptySprite, new Vector3(x, y, 0), Quaternion.identity) as Transform;
					clone = Instantiate(m_emptySprite, transform.position + new Vector3(x, y, 0), Quaternion.identity) as Transform;
					// names the empty squares for organizational purposes
					clone.name = "Board Space ( x = " + x.ToString() + " , y =" + y.ToString() + " )";
					// parents all of the empty squares to the Board object
					clone.transform.parent = transform;
				}
			}
		}
	}

	bool IsWithinBoard(int x, int y)
	{
		return (x >= 0 && x < m_width && y >= 0);
	}
	bool IsOccupied(int x, int y, Shape shape)
	{
		return (m_grid[x,y] != null && m_grid[x,y].parent != shape.transform);
	}
	public bool IsValidPosition(Shape shape)
	{
		foreach (Transform child in shape.transform)
		{
			//Child의 Transform좌표를 Board 좌표계를 기준으로 계산.
			Vector3 relativePos = transform.InverseTransformPoint(child.position);
			Vector3 pos = Vectorf.Round(relativePos);
			if (!IsWithinBoard((int) pos.x, (int) pos.y))
			{
				return false;
			}
			if (IsOccupied((int) pos.x, (int) pos.y, shape))
			{
				return false;
			}
		}
		return true;
	}
	
	public void StoreShapeInGrid(Shape shape)
	{
		if (shape == null)
		{
			return;
		}

		foreach (Transform child in shape.transform)
		{
			//Child의 Transform좌표를 Board 좌표계를 기준으로 계산.
			Vector3 relativePos = transform.InverseTransformPoint(child.position);
			Vector3 pos = Vectorf.Round(relativePos);
			m_grid[(int) pos.x, (int) pos.y] = child;

			// Update Byte array for send to server
			if (isMultiPlay)
			{
				if (isPlayerBoard)
				{
					m_byteGrid[(int)pos.x + (int)pos.y * m_width] = (byte)shape.shapeType;
				}
			}
		}

		if (isMultiPlay)
		{
			if (isPlayerBoard)
			{
				//자신의 보드의 byte 배열 정보를 서버로 전송
				SendBoardInfoToServer();
			}
		}
	}
		
	/*Game Logic*/
	bool IsComplete(int y)
	{
		for (int x = 0; x < m_width; ++x)
		{
			if (m_grid[x,y] == null)
			{
				return false;
			}

		}
		return true;
	}
	void ClearRow(int y)
	{
		for (int x = 0; x < m_width; ++x)
		{
			if (m_grid[x,y] !=null)
			{
				Destroy(m_grid[x,y].gameObject);

			}
			m_grid[x,y] = null;
			if (isMultiPlay)
			{
				m_byteGrid[x + y * m_width] = numEmptyCell;
			}			
		}
	}
	void ShiftOneRowDown(int y)
	{
		for (int x = 0; x < m_width; ++x)
		{
			if (m_grid[x,y] !=null)
			{
				m_grid[x, y-1] = m_grid[x,y];
				m_grid[x,y] = null;
				m_grid[x, y-1].position += new Vector3(0,-1,0);
			}
			if (isMultiPlay)
			{
				if (m_byteGrid[x + y * m_width] != numEmptyCell)
				{
					m_byteGrid[x + (y - 1) * m_width] = m_byteGrid[x + y * m_width];
					m_byteGrid[x + y * m_width] = numEmptyCell;
				}
			}
		}
	}
    void ShiftRowsDown(int startY)
    {
        for (int i = startY; i < m_height; ++i)
        {
            ShiftOneRowDown(i);
        }
    }

    void ShiftOneRowUp(int y)
    {
        if(y < m_height - 1)
        {
            for (int x = 0; x < m_width; ++x)
            {
                if (m_grid[x, y] != null)
                {
                    m_grid[x, y + 1] = m_grid[x, y];
                    m_grid[x, y] = null;
                    m_grid[x, y + 1].position += new Vector3(0, 1, 0);
                }
                if (isMultiPlay)
                {
                    if (m_byteGrid[x + y * m_width] != numEmptyCell)
                    {
                        m_byteGrid[x + (y + 1) * m_width] = m_byteGrid[x + y * m_width];
                        m_byteGrid[x + y * m_width] = numEmptyCell;
                    }
                }
            }
        }
    }
    void ShiftRowsUp()
    {
        for (int i = m_height - 2; i >= 0; i--)
        {
            ShiftOneRowUp(i);
        }
    }
    public void BeAttacked()
    {
        if (!isPlayerBoard)
        {
            return;
        }
        ShiftRowsUp();
        int empty1 = Random.Range(0, m_width);
        int empty2 = Random.Range(0, m_width);
        for(int i = 0; i < m_width; i++)
        {
            if(i != empty1 && i != empty2)
            {
                //보드의 i,0 위치에 회색 블럭 생성
                Shape shape = null;
                shape = Instantiate(shapeB, transform.position + new Vector3(i,0,0), Quaternion.identity) as Shape;
                if (shape)
                {
                    m_grid[i, 0] = shape.transform;
                    m_byteGrid[i] = (byte)shape.shapeType;
                }
                else
                {
                    Debug.LogWarning("WARNING! Invalid shape in spawner!");
                }
                
            }
        }
        SendBoardInfoToServer();
    }
    public void ClearAllRows()
	{
        int clearRowCount = 0;
		for (int y = 0; y < m_height; ++y)
		{
			if (IsComplete(y)) 
			{
				ClearRow(y);
				ShiftRowsDown(y+1);
				y--;
                clearRowCount++;
			}
		}
		if (isMultiPlay)
		{
			SendBoardInfoToServer();
            if(clearRowCount > 3)
            {
                SendAttackToServer();
                SendAttackToServer();
            }
            else if(clearRowCount > 1)
            {
                SendAttackToServer();
            }
		}
	}
	public bool IsOverLimit(Shape shape)
	{
		foreach (Transform child in shape.transform) 
		{
			Vector3 relativePos = transform.InverseTransformPoint(child.position);
			if(relativePos.y >= m_height - m_header)
			{
				return true;
			}
		}
		return false;
	}
	public void ResetBoard()
	{
		if (isMultiPlay)
		{
			for (int y = 0; y < m_height; y++)
			{
				for (int x = 0; x < m_width; x++)
				{
					m_byteGrid[x + y * m_width] = numEmptyCell;// Empty cell

					if (!isPlayerBoard)
					{
						m_grid[x, y].gameObject.SetActive(false);
					}
				}
			}
		}
	}
	/*Network*/
	void SendBoardInfoToServer()
	{
		Debug.Log("SendInfo To Server");
		NetworkStream stream = NetworkManager.Instance.GetStream;
		//첫번째 바이트는 메세지(1), 보드정보(2) 구분
		byte[] dataType = new byte[1];
		dataType[0] = 2;
		stream.Write(dataType, 0, dataType.Length);
		stream.Write(m_byteGrid, 0, m_byteGrid.Length);
		stream.Flush();
	}
    void SendAttackToServer()
    {
        NetworkStream stream = NetworkManager.Instance.GetStream;
        //첫번째 바이트는 메세지(1), 보드정보(2), 상대공격(3), 게임시작(4), 게임종료(5)
        byte[] dataType = new byte[1];
        dataType[0] = 3;
        stream.Write(dataType, 0, dataType.Length);
        stream.Flush();
    }
	public void UpdateRemoteBoard()
	{
		if (!isPlayerBoard)
		{
			for(int i = 0; i < m_width*m_height - 1; i++)
			{
				if (m_byteGrid[i] != numEmptyCell)
				{
					byte type = m_byteGrid[i];
					Color shapeColor;
					string hexString = "#00E1FF";
					switch (type)
					{
						case (byte)Shape.ShapeType.I:
							hexString = "#00E1FF";
							break;
						case (byte)Shape.ShapeType.J:
							hexString = "#2445FF";
							break;
						case (byte)Shape.ShapeType.L:
							hexString = "#FF8400";
							break;
						case (byte)Shape.ShapeType.O:
							hexString = "#FBDA23";
							break;
						case (byte)Shape.ShapeType.S:
							hexString = "#00BE00";
							break;
						case (byte)Shape.ShapeType.T:
							hexString = "#764EFF";
							break;
						case (byte)Shape.ShapeType.Z:
							hexString = "#CD2823";
                            break;
                        case (byte)Shape.ShapeType.B:
                            hexString = "#5568B2";
                            break;
					};
					ColorUtility.TryParseHtmlString(hexString, out shapeColor);
					m_grid[i % m_width, i / m_width].gameObject.GetComponent<SpriteRenderer>().color = shapeColor;
					m_grid[i % m_width, i / m_width].gameObject.SetActive(true);
				}
				else
				{
					m_grid[i % m_width, i / m_width].gameObject.SetActive(false);
				}
			}
			
		}
	}
}
