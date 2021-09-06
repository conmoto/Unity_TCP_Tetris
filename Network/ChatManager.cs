using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ChatManager : MonoBehaviour
{
	NetworkManager networkManager;
	public static ChatManager Instance;
	Server server;

	public bool ingTyping = false;
	public GameObject chatBox;
	public GameObject messagePrefab;
	
	
	public InputField inputMessage;
	public Color playerMessageColor, infoMessageColor, etcMessageColor;

	string userName = "Default";
	string chatText;
	int maxMessages = 25;

	[SerializeField]
	List<Message> messageList = new List<Message>();

	void Awake()
	{
		if (Instance)
		{
			Destroy(this);
		}
		Instance = this;
	}

	void Start()
	{
		if (chatText != null)
		{
			Destroy(this);
		}

		networkManager = NetworkManager.Instance;
		server = GameObject.Find("ChatManager").GetComponent<Server>();
		userName = networkManager.GetUserName;

		if (networkManager.IsServer)
		{
			SendMessageToServer(userName + "님이 방을 생성하였습니다.", Message.MessageType.infoMessage);
		}
		else
		{
			SendMessageToServer(userName + "님이 방에 입장하였습니다.", Message.MessageType.infoMessage);
		}

		inputMessage.interactable = false;
	}

	void Update()
	{
		switch (networkManager.NetworkState)
		{
			case NetworkManager.NetState.HOST_TYPE_SELECT:
				messageList.Clear();
				break;
			case NetworkManager.NetState.CONNECTED:
				UpdateChat();
				break;
			case NetworkManager.NetState.LEAVE:
				Disconnect();
				break;
		}
	}
	//HACK: 코루틴?
	void UpdateChat()
	{
		if (networkManager.IsConnected)
		{
			SendChat();
		}
	}

	//private void ReceiveChat()
	//{
	//	if (networkManager.Stream.DataAvailable)
	//	{
	//		StreamReader reader = networkManager.GetReader;

	//		char[] type = new char[1];
	//		reader.Read(type, 0, type.Length);
	//		string data = reader.ReadLine();
	//		Debug.Log("Receive type: " + (int)type[0]);

	//		Message.MessageType messageType = Message.MessageType.playerMessage;
	//		switch (type[0])
	//		{
	//			case (char)Message.MessageType.infoMessage:
	//				messageType = Message.MessageType.infoMessage;
	//				break;
	//		}
			
	//		UpdateChatBox(data, messageType);
	//	}
	//}

	void SendChat()
	{
		if (inputMessage.text != "")
		{
			if (Input.GetKeyDown(KeyCode.Return))
			{
				chatText = userName + ": " + inputMessage.text;
				SendMessageToServer(chatText, Message.MessageType.playerMessage);
				chatText = "";
				inputMessage.text = "";
			}
		}
		if (inputMessage.text == "")
		{
			if (Input.GetKeyDown(KeyCode.Return))
			{
				inputMessage.interactable = !inputMessage.interactable;
				ingTyping = !ingTyping;
				if (inputMessage.IsInteractable())
				{
					inputMessage.Select();
				}
			}
		}
	}
	void SendMessageToServer(string data, Message.MessageType messageType = Message.MessageType.playerMessage)
	{
		StreamWriter writer = networkManager.GetWriter;
		NetworkStream stream = networkManager.GetStream;
		byte[] buf = new byte[2];
		buf[0] = (byte)1; // 헤더 1: 메세지
		buf[1] = (byte)messageType;
		stream.Write(buf, 0, buf.Length);
		writer.WriteLine(data);
		writer.Flush();
		//stream.Flush(); -> Error.. Why?
	}

	public void UpdateChatBox(string text, Message.MessageType messageType = Message.MessageType.playerMessage)
	{
		if(messageList.Count >= maxMessages)
		{
			Destroy(messageList[0].textObejct.gameObject);
			messageList.Remove(messageList[0]);
		}

		Message newMessage = new Message();

		GameObject newText = Instantiate(messagePrefab, chatBox.transform);
		newMessage.textObejct = newText.GetComponent<Text>();
		newMessage.textObejct.text = text;
		newMessage.textObejct.color = SetMessageColor(messageType);

		messageList.Add(newMessage);
	}
	private Color SetMessageColor(Message.MessageType messageType)
	{
		Color color = playerMessageColor;

		switch (messageType)
		{
			case Message.MessageType.playerMessage:
				break;
			case Message.MessageType.infoMessage:
				color = infoMessageColor;
				break;
			case Message.MessageType.etcMessage:
				color = etcMessageColor;
				break;
		}

		return color;
	}


	public void Disconnect()
	{
		if (!networkManager.IsConnected)
			return;

		if(networkManager.IsServer)
		{
			SendMessageToServer("Host가 접속을 종료했습니다." + Message.MessageType.infoMessage);
			//server.Broadcast("Host가 접속을 종료했습니다.");
		}
		else
		{
			SendMessageToServer(userName + "님이 방을 나갔습니다.", Message.MessageType.infoMessage);
		}
		// TODO: 코루틴으로 서버 연결 종료 전 처리를 해결 할 수 없음. 다른 방법 찾아보기
		StartCoroutine(WaitForServerClosed());
		networkManager.CloseSocket();
	}
	IEnumerator WaitForServerClosed()
	{
		yield return new WaitForSeconds(1f);
	}
	private void OnApplicationQuit()
	{
		Disconnect();
	}
	private void OnDisable()
	{
		Disconnect();
	}
	
}

[System.Serializable]
public class Message
{
	public Text textObejct;
	public MessageType messageType;

	public enum MessageType
	{
		playerMessage = 1,
		infoMessage,
		etcMessage,
	}
}
