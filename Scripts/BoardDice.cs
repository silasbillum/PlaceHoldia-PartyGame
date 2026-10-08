using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class BoardDice : NetworkBehaviour
{
    public static BoardDice Instance;

    [Header("References")]
    [SerializeField] private TMP_Text diceResultText;

    [Header("Dice Faces")]
    [SerializeField] private Transform face1;
    [SerializeField] private Transform face2;
    [SerializeField] private Transform face3;
    [SerializeField] private Transform face4;
    [SerializeField] private Transform face5;
    [SerializeField] private Transform face6;

    [Header("Roll")]
    [SerializeField] private float rollForce = 6f;
    [SerializeField] private float torqueForce = 12f;
    [SerializeField] private float rollTime = 1.5f;

    [Header("Dice Position")]
    [SerializeField] private Transform diceStartPosition;

    [Header("Network Sync")]
    [SerializeField] private float syncRate = 30f;

    private Rigidbody rb;

    private bool isRolling = false;

    private int diceResult;

    private ulong rollingClientId =
        ulong.MaxValue;

    private float syncTimer = 0f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        Instance = this;
    }


    private void Start()
    {
        
    }


    private void Update()
    {
        if (Keyboard.current != null &&
            Keyboard.current.spaceKey.wasPressedThisFrame &&
            !isRolling)
        {
            RollDice();
        }
        if (!isRolling)
            return;

        if (NetworkManager.Singleton == null)
            return;

        if (NetworkManager.Singleton.LocalClientId !=
            rollingClientId)
        {
            return;
        }

        syncTimer += Time.deltaTime;

        float interval =
            1f / syncRate;

        if (syncTimer >= interval)
        {
            syncTimer = 0f;

            SendDiceTransformServerRpc(
                transform.position,
                transform.rotation
            );
        }
    }

    public void RollDice()
    {
        if (NetworkManager.Singleton == null)
            return;

        if (BoardGameManager.Instance == null)
        {
            
            return;
        }

        if (isRolling)
            return;

        ulong myClientId =
            NetworkManager.Singleton.LocalClientId;


        BoardGameManager.Instance
            .RequestDiceRollServerRpc();
    }
    public void StartApprovedRoll(
        ulong approvedRollingClientId)
    {
        
        isRolling = false;

        rollingClientId =
            approvedRollingClientId;

        syncTimer = 0f;

        
        if (NetworkManager.Singleton.LocalClientId ==
            rollingClientId)
        {
            StartCoroutine(
                RollDiceCoroutine()
            );
        }
        else
        {
            isRolling = true;

            if (rb != null)
            {
                rb.isKinematic = true;
            }
        }
    }
    private IEnumerator RollDiceCoroutine()
    {
        isRolling = true;

        syncTimer = 0f;

        HideResult();

        if (diceStartPosition != null)
        {
            transform.position =
                diceStartPosition.position;

            transform.rotation =
                diceStartPosition.rotation;
        }

        rb.linearVelocity =
            Vector3.zero;

        rb.angularVelocity =
            Vector3.zero;

        rb.isKinematic = false;


      
        SendDiceTransformServerRpc(
            transform.position,
            transform.rotation
        );


        yield return new WaitForSeconds(
            0.1f
        );
        rb.AddForce(
            transform.forward *
            rollForce,
            ForceMode.Impulse
        );

        rb.AddForce(
            Vector3.up * 1.5f,
            ForceMode.Impulse
        );

        rb.AddTorque(
            Random.insideUnitSphere *
            torqueForce,
            ForceMode.Impulse
        );


        yield return new WaitForSeconds(
            rollTime
        );

        rb.linearVelocity =
            Vector3.zero;

        rb.angularVelocity =
            Vector3.zero;

        diceResult =
            GetTopFace();

        Debug.Log(
            "DICE LANDED ON: " +
            diceResult
        );


        SendDiceTransformServerRpc(
            transform.position,
            transform.rotation
        );


        yield return new WaitForSeconds(
            0.1f
        );

        isRolling = false;

        ContinueAfterRoll();
    }
    [ServerRpc(
        RequireOwnership = false
    )]
    private void SendDiceTransformServerRpc(
        Vector3 position,
        Quaternion rotation,
        ServerRpcParams rpcParams = default)
    {
        if (!IsServer)
            return;

        ulong senderClientId =
            rpcParams.Receive.SenderClientId;

        if (senderClientId !=
            rollingClientId)
        {
            return;
        }


        UpdateDiceTransformClientRpc(
            position,
            rotation
        );
    }

    [ClientRpc]
    private void UpdateDiceTransformClientRpc(
        Vector3 position,
        Quaternion rotation)
    {
        if (NetworkManager.Singleton == null)
            return;

        if (NetworkManager.Singleton.LocalClientId ==
            rollingClientId)
        {
            return;
        }


        transform.position =
            position;

        transform.rotation =
            rotation;
    }
    private int GetTopFace()
    {
        Transform[] faces =
        {
            face1,
            face2,
            face3,
            face4,
            face5,
            face6
        };

        int bestFace = 1;

        float highestDot = -1f;

        for (int i = 0;
             i < faces.Length;
             i++)
        {
            if (faces[i] == null)
                continue;

            float dot =
                Vector3.Dot(
                    faces[i].up,
                    Vector3.up
                );


            if (dot > highestDot)
            {
                highestDot = dot;

                bestFace =
                    i + 1;
            }
        }

        return bestFace;
    }
    private void ContinueAfterRoll()
    {
        if (NetworkManager.Singleton == null)
            return;

        if (BoardGameManager.Instance == null)
            return;

        if (NetworkManager.Singleton.LocalClientId !=
            rollingClientId)
        {
            return;
        }


        BoardGameManager.Instance
            .DiceResultServerRpc(
                diceResult
            );
    }
    public void ShowResultForEveryone(
        int result)
    {
        if (diceResultText != null)
        {
            diceResultText.text =
                result.ToString();

            diceResultText.gameObject.SetActive(
                true
            );
        }
        isRolling = false;

    }
    public void HideResult()
    {
        if (diceResultText == null)
            return;

        diceResultText.gameObject.SetActive(
            false
        );
    }
}