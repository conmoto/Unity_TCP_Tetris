using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using UnityEngine.UI;

public class Server : MonoBehaviour
{
	List<ServerClients> clientsList;
	List<ServerClients> disconnectList;

	int port = 7652;
	TcpListener server;
	bool isServerStarted;
	public static int allocID = 0;//클라이언트에 할당하는 ID

	public void StartServer()
	{
		clientsList = new List<ServerClients>();
		disconnectList = new List<ServerClients>();

		try
		{
			server = new TcpListener(IPAddress.Any, port);
			server.Start();

			StartListning();
			isServerStarted = true;
			Debug.Log("Server has been started .. port " + port);
			
		}
		catch(Exception e)
		{
			Debug.Log("Socket Error: " + e.Message);
		}
	}

	// HACK: Update 대신 코루틴을 써서 통신 주기(서버의 브로드캐스팅 주기)를 조절하는 것은 어떤가? 
	void Update()
	{
		if (!isServerStarted)
		{
			return;
		}

		foreach (ServerClients c in clientsList)
		{
			if (!IsConnected(c.tcp))
			{
				disconnectList.Add(c);
				c.tcp.Close();
				continue;
			}
			else
			{
				NetworkStream s = c.tcp.GetStream();
				// HACK: 데이터 종류에 따라 다른 브로드캐스트 사용
				if (s.DataAvailable)
				{
					StreamReader reader = new StreamReader(c.tcp.GetStream());
					byte[] dataType = new byte[1];
					s.Read(dataType, 0, dataType.Length);

					if (dataType[0] == 1) // 헤더가 1: 메세지
					{
						//BroadCast String
						string messageData = reader.ReadLine();

						if (messageData != null)
						{
							Broadcast(messageData);
						}
					}
					else if (dataType[0] == 2) // 헤더가 2: 보드 정보
					{
						//BroadCast Byte[]
						byte[] buf = new byte[300];

						s.Read(buf, 0, buf.Length);

						Broadcast(buf, c.clientID);
					}
                    else if (dataType[0] == 3) // 헤더가 3: 적 공격
                    {
                        BroadcastAttack(c.clientID);
                    }
					else if(dataType[0] == 4) // 헤더가 4: 게임 스타트
					{
						BroadcastGameStart();
					}
					else if(dataType[0] == 5) // 헤더가 5: 게임 종료
					{
						BroadcastGameEnd(c.clientID);
					}
				}
			}
		}

		for (int i = 0; i < disconnectList.Count - 1; i++)
		{
			clientsList.Remove(disconnectList[i]);
			allocID--;
			//Broadcast(disconnectList[i].clientName + "님이 방을 나갔습니다.", clientsList);
			//disconnectList.RemoveAt(i);
		}
	}

	IEnumerator ReceiveData()
	{
		
		yield return new WaitForSeconds(0.1f);
	}

	void Broadcast(string data)
	{
		foreach (ServerClients c in clientsList)
		{
			try
			{
				//클라이언트에서도 전달받은 메세지 구분을 하기 위해 헤더를 다시 붙인다.
				char[] dataType = new char[1];
				dataType[0] = (char)1;

				StreamWriter writer = new StreamWriter(c.tcp.GetStream());
				writer.Write(dataType, 0, dataType.Length);
				writer.WriteLine(data);
				writer.Flush();
			}
			catch (Exception e)
			{
				Debug.Log("Write error: " + e.Message + "to Client" + c.clientName);
			}
		}
	}
	void Broadcast(byte[] data, int fromID)
	{
		foreach (ServerClients c in clientsList)
		{
			if(fromID != c.clientID)
			{
				try
				{
					//클라이언트에서도 전달받은 메세지 구분을 하기 위해 헤더를 다시 붙인다.
					byte[] dataType = new byte[1];
					dataType[0] = 2;
					NetworkStream stream = c.tcp.GetStream();
					stream.Write(dataType, 0, dataType.Length);
					stream.Write(data, 0, data.Length);
					stream.Flush();
				}
				catch (Exception e)
				{
					Debug.Log("Write error: " + e.Message + "to Client" + c.clientName);
				}
			}			
		}
	}
	void BroadcastGameStart()
	{
		foreach (ServerClients c in clientsList)
		{
			try
			{
				//클라이언트에서도 전달받은 메세지 구분을 하기 위해 헤더를 다시 붙인다.
				byte[] dataType = new byte[1];
				dataType[0] = (byte)4;

				NetworkStream stream = c.tcp.GetStream();
				stream.Write(dataType, 0, dataType.Length);
				stream.Flush();
			}
			catch (Exception e)
			{
				Debug.Log("Write error: " + e.Message + "to Client" + c.clientName);
			}
		}
	}
	void BroadcastGameEnd(int fromID)
	{
		foreach (ServerClients c in clientsList)
		{
			if(fromID != c.clientID)
			{
				try
				{
					//클라이언트에서도 전달받은 메세지 구분을 하기 위해 헤더를 다시 붙인다.
					byte[] dataType = new byte[1];
					dataType[0] = (byte)5;

					NetworkStream stream = c.tcp.GetStream();
					stream.Write(dataType, 0, dataType.Length);
					stream.Flush();
				}
				catch (Exception e)
				{
					Debug.Log("Write error: " + e.Message + "to Client" + c.clientName);
				}
			}
		}
	}
    void BroadcastAttack(int fromID)
    {
        foreach (ServerClients c in clientsList)
        {
            if (fromID != c.clientID)
            {
                try
                {
                    //클라이언트에서도 전달받은 메세지 구분을 하기 위해 헤더를 다시 붙인다.
                    byte[] dataType = new byte[1];
                    dataType[0] = (byte)3;

                    NetworkStream stream = c.tcp.GetStream();
                    stream.Write(dataType, 0, dataType.Length);
                    stream.Flush();
                }
                catch (Exception e)
                {
                    Debug.Log("Write error: " + e.Message + "to Client" + c.clientName);
                }
            }
        }
    }

    private void StartListning()
	{
		// HACK: 비동기 대기를 시작한다.
		server.BeginAcceptTcpClient(AcceptTcpClient, server);
		// HACK: 두번째 인자가 AcceptTcpClient 함수의 매개변수 IAsyncResult로 전달된다 
	}
	private void AcceptTcpClient(IAsyncResult ar)
	{
		TcpListener listener = (TcpListener)ar.AsyncState;
		clientsList.Add(new ServerClients(listener.EndAcceptTcpClient(ar)));

		//Broadcast(clientsList[clientsList.Count - 1].clientName + "님이 방에 입장하였습니다.", clientsList);

		StartListning();
	}

	bool IsConnected(TcpClient c)
	{
		try
		{
			if (c != null && c.Client != null && c.Connected)
			{
				if (c.Client.Poll(0, SelectMode.SelectRead))
				{
					return !(c.Client.Receive(new byte[1], SocketFlags.Peek) == 0);
				}
				return true;
			}
			else
				return false;
		}
		catch
		{
			return false;
		}
	}

	public class ServerClients
	{
		public string clientName;
		public int clientID;
		public TcpClient tcp;

		public ServerClients(TcpClient clientSocket)
		{
			clientID = allocID;
			allocID++;
			clientName = "Guest";
			tcp = clientSocket;
			if (tcp == null)
			{
				Debug.Log("Client Socket null");
			}
		}
	}
}



