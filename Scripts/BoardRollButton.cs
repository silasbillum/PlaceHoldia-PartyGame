using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class BoardRollButton : MonoBehaviour
{
    [Header("Button")]
    [SerializeField] private Button rollButton;

    [Header("Colors")]
    [SerializeField] private Color canRollColor = Color.green;
    [SerializeField] private Color cannotRollColor = Color.red;

    private Image buttonImage;

    private void Awake()
    {
        if (rollButton == null)
            rollButton = GetComponent<Button>();

        if (rollButton != null)
        {
            buttonImage =
                rollButton.GetComponent<Image>();

            rollButton.onClick.AddListener(
                OnRollButtonClicked
            );
        }
    }

    private void Update()
    {
        UpdateButton();
    }

    private void UpdateButton()
    {
        if (rollButton == null)
            return;

        if (NetworkManager.Singleton == null)
        {
            SetButtonState(false);
            return;
        }

        if (!NetworkManager.Singleton.IsClient)
        {
            SetButtonState(false);
            return;
        }

        if (BoardGameManager.Instance == null)
        {
            SetButtonState(false);
            return;
        }

        ulong localClientId =
            NetworkManager.Singleton.LocalClientId;

     
        bool isMyTurn =
            BoardGameManager.Instance
                .IsClientTurn(localClientId);

     
        bool diceLocked =
            BoardGameManager.Instance
                .IsDiceLocked();

        bool canRoll =
            isMyTurn &&
            !diceLocked;

        SetButtonState(canRoll);
    }

    private void SetButtonState(
        bool canRoll)
    {
        rollButton.interactable =
            canRoll;

        if (buttonImage != null)
        {
            buttonImage.color =
                canRoll
                    ? canRollColor
                    : cannotRollColor;
        }
    }

    private void OnRollButtonClicked()
    {
        if (NetworkManager.Singleton == null)
            return;

        if (!NetworkManager.Singleton.IsClient)
            return;

        if (BoardGameManager.Instance == null)
            return;

        ulong localClientId =
            NetworkManager.Singleton.LocalClientId;

        // Extra safety check
        if (!BoardGameManager.Instance.IsClientTurn(
                localClientId))
        {
            return;
        }

        if (BoardGameManager.Instance.IsDiceLocked())
        {
            return;
        }

        BoardGameManager.Instance
      .RequestDiceRollServerRpc();
    }

    private void OnDestroy()
    {
        if (rollButton != null)
        {
            rollButton.onClick.RemoveListener(
                OnRollButtonClicked
            );
        }
    }
}