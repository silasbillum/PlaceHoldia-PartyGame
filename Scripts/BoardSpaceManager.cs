using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class BoardSpaceManager : MonoBehaviour
{
    public static BoardSpaceManager Instance;

    [System.Serializable]
    public class Minigame
    {
        public string minigameName;
        public string sceneName;
    }


    [Header("Minigames")]
    [SerializeField]
    private List<Minigame> minigames =
        new List<Minigame>();


    [Header("Minigame Popup")]
    [SerializeField] private GameObject minigamePopup;

    [SerializeField] private TMP_Text minigameNameText;

    [SerializeField] private TMP_Text Description;

    [SerializeField] private Button continueButton;


    private string currentMinigame;
    private string currentSceneName;


    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;


        if (minigamePopup != null)
        {
            minigamePopup.SetActive(false);
        }


        if (continueButton != null)
        {
            continueButton.onClick.AddListener(
                ContinueToMinigame
            );
        }
    }
    public bool GetRandomMinigame(
        out string minigameName,
        out string sceneName)
    {
        minigameName = "";
        sceneName = "";


        if (minigames == null ||
            minigames.Count == 0)
        {

            return false;
        }


        int randomIndex =
            Random.Range(
                0,
                minigames.Count
            );


        Minigame selectedMinigame =
            minigames[randomIndex];


        if (selectedMinigame == null)
        {
            return false;
        }


        if (string.IsNullOrEmpty(
            selectedMinigame.minigameName))
        {
            return false;
        }


        if (string.IsNullOrEmpty(
            selectedMinigame.sceneName))
        {
            return false;
        }


        minigameName =
            selectedMinigame.minigameName;

        sceneName =
            selectedMinigame.sceneName;


        return true;
    }

    public void ShowMinigamePopup(
        string minigameName,
        string sceneName)
    {
        currentMinigame =
            minigameName;

        currentSceneName =
            sceneName;

        if (minigameNameText != null)
        {
            minigameNameText.text =
                currentMinigame;
        }


        if (minigamePopup != null)
        {
            minigamePopup.SetActive(true);
        }
        else
        {
        }
    }

    private void ContinueToMinigame()
    {
        if (minigamePopup != null)
        {
            minigamePopup.SetActive(false);
        }


        if (string.IsNullOrEmpty(
            currentSceneName))
        {

            return;
        }


        if (NetworkManager.Singleton == null)
        {

            return;
        }


      
        if (!NetworkManager.Singleton.IsServer)
        {
           
            return;
        }

        NetworkManager.Singleton.SceneManager.LoadScene(
            currentSceneName,
            LoadSceneMode.Single
        );
    }
}