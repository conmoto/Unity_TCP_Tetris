using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class NetworkManager : MonoBehaviour
{
	public static NetworkManager Instance;
	Server server;
	public InputField inputUserName;
	public InputField inputIPAddress;
	public bool isHostClient;

    Board playerBoard;
	Board remoteBoard;
	string userName;
	public string GetUserName { get { return userName; } }

	// TCP Field
	TcpClient socket;
	public TcpClient Socket { get { return socket; } }
	NetworkStream stream;
	public NetworkStream GetStream { get { return stream; } }
	StreamWriter writer;
	public StreamWriter GetWriter { get { return writer; } }
	StreamReader reader;
	public StreamReader GetReader { get { return reader; } }

	string hostAddress = "";
	string defaultHostAddress = "localhost";
	const int port = 7652;
	bool isConnected = false;
	public bool IsConnected { get { return isConnected; } }
	bool isServer = false;
	public bool IsServer { get { return isServer; } }

	private NetState networkState = NetState.HOST_TYPE_SELECT;
	public NetState NetworkState { get { return networkState; } }
	public enum NetState
	{
		HOST_TYPE_SELECT = 0,   // 방 선택.
		CONNECTED,               // 채팅 중.
		LEAVE,                  // 나가기.
		ERROR,                  // 오류.
	};

	private void Awake()
	{
		if (Instance)
		{
			Destroy(this);
		}
		Instance = this;
	}
	private void Start()
	{
		server = this.GetComponent<Server>();
	}
	private void Update()
	{
		if (IsConnected)
		{
			ReceiveData();
		}
	}

	void ReceiveData()
	{
		if (stream.DataAvailable)
		{
			byte[] dataType = new byte[1];
			stream.Read(dataType, 0, dataType.Length);
			
			if (dataType[0] == 1) // 헤더가 1: 메세지
			{
				char[] type = new char[1];
				reader.Read(type, 0, type.Length);
				Message.MessageType messageType = Message.MessageType.playerMessage;
				switch (type[0])
				{
					case (char)Message.MessageType.infoMessage:
						messageType = Message.MessageType.infoMessage;
						break;
				}
				string data = reader.ReadLine();

				ChatManager.Instance.UpdateChatBox(data, messageType);
			}
			// TODO: 서버에 보낸 자신의 정보를 리모트로 구분하지 않으려면? -> 클라이언트에 ID를 부여하여 해결!
			// 자기 자신을 제외한 클라이언트들에게만 브로드캐스팅
			else if (dataType[0] == 2) // 헤더가 2: 보드 정보
			{
				if (remoteBoard == null)
				{
					remoteBoard = GameObject.Find("BoardRemote").GetComponent<Board>();
				}

				stream.Read(remoteBoard.m_byteGrid, 0, remoteBoard.m_byteGrid.Length);
 				remoteBoard.UpdateRemoteBoard();
			}
            else if(dataType[0] == 3) // 헤더가 3: 공격 받음
            {
                if(playerBoard == null)
                {
                    playerBoard = GameObject.Find("BoardPlayer").GetComponent<Board>();
                }
                playerBoard.BeAttacked();
            }
			else if(dataType[0] == 4) // 헤더가 4: GameStart 알람
			{
				GameController.Instance.RestartGame();
			}
			else if(dataType[0] == 5) // 헤더 5: GameLose(다른 플레이어 승)
			{
				GameController.Instance.GameWin();
			}
		}
	}

	//소켓 생성
	bool CreateTCPSocket()
	{
		if (isConnected)
		{
			return true;
		}

		//hostAddress = inputIPAddress.text;
		if (hostAddress == "")
		{
			hostAddress = defaultHostAddress;
		}

		try
		{
			// TODO: 채팅과 게임 정보 전달을 서로 다른 포트를 사용해서 구현?
			socket = new TcpClient(hostAddress, port);
			stream = socket.GetStream();
			writer = new StreamWriter(stream);
			reader = new StreamReader(stream);

            Debug.Log("Address: " + hostAddress);
			//inputMessage.interactable = false;
			return true;
		}
		catch (Exception e)
		{
            Debug.Log(e);
			Debug.Log("Socket error: " + e.Message);
			Debug.Log("IP" + hostAddress + ", port" + port);
			return false;
		}
	}

	public void CloseSocket()
	{
		if (!isConnected)
			return;

		writer.Close();
		reader.Close();
		socket.Close();
		isConnected = false;
	}


	public void OnHostButton()
	{
		isHostClient = true;
		userName = "Host";
		if (inputUserName.text != "")
		{
			userName = inputUserName.text;
		}

		server.StartServer();
		isServer = true;

		isConnected = CreateTCPSocket();
		if (isConnected)
		{
			networkState = NetState.CONNECTED;
			SceneManager.LoadScene("MultiGame");
		}
		else
		{
			networkState = NetState.ERROR;
		}
	}
	public void OnJoinButton()
	{
		isHostClient = false;
		userName = "Guest";
		if (inputUserName.text != "")
		{
			userName = inputUserName.text;
		}
        if (inputIPAddress.text != "")
        {
            hostAddress = inputIPAddress.text;
        }
        else if(inputIPAddress.text == "")
        {
            hostAddress = defaultHostAddress;
        }

        isConnected = CreateTCPSocket();
		server.enabled = false;
		isServer = false;
		if (isConnected)
		{
			networkState = NetState.CONNECTED;
			SceneManager.LoadScene("MultiGame");
		}
		else
		{
			networkState = NetState.ERROR;
		}
	}

	void OnGUI()
	{
		switch (networkState)
		{
			case NetState.HOST_TYPE_SELECT:
				//SelectHostTypeGUI();
				break;

			case NetState.CONNECTED:
				//ChattingGUI();
				break;

			case NetState.ERROR:
				ErrorGUI();
				break;
		}
	}
	void ErrorGUI()
	{
		float sx = 800.0f;
		float sy = 600.0f;
		float px = sx * 0.5f - 150.0f;
		float py = sy * 0.5f;

		if (GUI.Button(new Rect(px, py, 300, 80), "접속에 실패했습니다.\n\n버튼을 누르세요."))
		{
			networkState = NetState.HOST_TYPE_SELECT;
		}
	}
}
