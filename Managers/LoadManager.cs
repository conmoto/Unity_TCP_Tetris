using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadManager : MonoBehaviour
{
	public static LoadManager Instance;
	public GameObject networkManager;
	public GameObject m_titlePanel;
	public GameObject m_selectPlayTypeMenu;
	public GameObject m_multiPlayMenu;

	private bool isMultiPlay = true;
	public bool IsMultiPlay { get { return isMultiPlay; } }

	private void Awake()
	{
		if (Instance)
		{
			Destroy(this.gameObject);
		}
		Instance = this;
		//Manager은 보존
		DontDestroyOnLoad(this.transform.root);
	}

	public void OnSelectSinglePlay()
	{
		isMultiPlay = false;
		networkManager.SetActive(false);
		SceneManager.LoadScene("SingleGame");
	}
	public void OnSelectMultiPlay()
	{
		isMultiPlay = true;
		m_multiPlayMenu.SetActive(true);
		m_selectPlayTypeMenu.SetActive(false);
	}
	public void OnSelectBackToTitle()
	{
		m_selectPlayTypeMenu.SetActive(true);
		m_multiPlayMenu.SetActive(false);
	}
    public void OnSelectExitButton()
    {
        SceneManager.LoadScene("Title");
    }
}
