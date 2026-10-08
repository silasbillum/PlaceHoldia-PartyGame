using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class BoardPlayer : NetworkBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float rotationSpeed = 8f;
    [SerializeField] private float playerHeight = 2.1f;

    [Header("Animation")]
    [SerializeField] private Animator animator;

    private int currentSpace = 0;
    private bool isMoving = false;

    public int CurrentSpace =>
        currentSpace;

    public bool IsMoving =>
        isMoving;

    public void SpawnAtStart(
        Transform startSpace,
        int spaceIndex)
    {
        if (startSpace == null)
        {
            Debug.LogError(
                "Start space is NULL!"
            );

            return;
        }

        transform.position = new Vector3(
            startSpace.position.x,
            playerHeight,
            startSpace.position.z
        );

        transform.rotation =
            startSpace.rotation *
            Quaternion.Euler(0f, 180f, 0f);

        currentSpace =
            spaceIndex;
    }

    public void MoveSpaces(
        int spaces,
        Transform[] boardSpaces)
    {
        if (!IsServer)
            return;

        if (isMoving)
            return;

        if (spaces <= 0)
            return;

        if (boardSpaces == null ||
            boardSpaces.Length == 0)
        {
            Debug.LogError(
                "Board spaces are NULL!"
            );

            return;
        }

        int startSpace =
            currentSpace;

        Debug.Log(
            "SERVER START MOVEMENT | " +
            "ClientId: " +
            OwnerClientId +
            " | Start Space: " +
            startSpace +
            " | Spaces: " +
            spaces
        );

       
        MoveVisualClientRpc(
            startSpace,
            spaces
        );

        
        StartCoroutine(
            ServerMoveCoroutine(
                spaces,
                boardSpaces
            )
        );
    }
    private IEnumerator ServerMoveCoroutine(
        int spaces,
        Transform[] boardSpaces)
    {
        isMoving = true;
        UpdateAnimation();

        for (int i = 0; i < spaces; i++)
        {
            int nextSpace =
                currentSpace + 1;

            if (nextSpace >=
                boardSpaces.Length)
            {
                break;
            }

            currentSpace =
                nextSpace;

            Transform target =
                boardSpaces[currentSpace];

            Vector3 targetPosition =
                new Vector3(
                    target.position.x,
                    playerHeight,
                    target.position.z
                );

            while (
                Vector3.Distance(
                    transform.position,
                    targetPosition
                ) > 0.05f
            )
            {
                transform.position =
                    Vector3.MoveTowards(
                        transform.position,
                        targetPosition,
                        moveSpeed *
                        Time.deltaTime
                    );

                Vector3 direction =
                    targetPosition -
                    transform.position;

                if (direction.sqrMagnitude >
                    0.01f)
                {
                    Quaternion targetRotation =
                        Quaternion.LookRotation(
                            direction
                        );

                    transform.rotation =
                        Quaternion.Slerp(
                            transform.rotation,
                            targetRotation,
                            rotationSpeed *
                            Time.deltaTime
                        );
                }

                yield return null;
            }

            transform.position =
                targetPosition;

            yield return new WaitForSeconds(
                0.15f
            );
        }

        isMoving = false;
        UpdateAnimation();

       

        if (BoardGameManager.Instance != null)
        {
            BoardGameManager.Instance
                .PlayerFinishedMove();
        }
    }

    [ClientRpc]
    private void MoveVisualClientRpc(
        int startSpace,
        int spaces)
    {
      
        if (IsServer)
            return;

        if (isMoving)
            return;

        BoardGameManager manager =
            BoardGameManager.Instance;

        if (manager == null)
            return;

        int spaceCount =
            manager.GetSpaceCount();

        Transform[] boardSpaces =
            new Transform[spaceCount];

        for (int i = 0; i < spaceCount; i++)
        {
            boardSpaces[i] =
                manager.GetSpace(i);
        }

       
        currentSpace =
            startSpace;

        transform.position = new Vector3(
            boardSpaces[currentSpace].position.x,
            playerHeight,
            boardSpaces[currentSpace].position.z
        );

        transform.rotation =
            boardSpaces[currentSpace].rotation *
            Quaternion.Euler(0f, 180f, 0f);

        StartCoroutine(
            VisualMoveCoroutine(
                spaces,
                boardSpaces
            )
        );
    }
    private IEnumerator VisualMoveCoroutine(
        int spaces,
        Transform[] boardSpaces)
    {
        isMoving = true;
        UpdateAnimation();

        for (int i = 0; i < spaces; i++)
        {
            int nextSpace =
                currentSpace + 1;

            if (nextSpace >=
                boardSpaces.Length)
            {
                break;
            }

            currentSpace =
                nextSpace;

            Transform target =
                boardSpaces[currentSpace];

            Vector3 targetPosition =
                new Vector3(
                    target.position.x,
                    playerHeight,
                    target.position.z
                );

            while (
                Vector3.Distance(
                    transform.position,
                    targetPosition
                ) > 0.05f
            )
            {
                transform.position =
                    Vector3.MoveTowards(
                        transform.position,
                        targetPosition,
                        moveSpeed *
                        Time.deltaTime
                    );

                Vector3 direction =
                    targetPosition -
                    transform.position;

                if (direction.sqrMagnitude >
                    0.01f)
                {
                    Quaternion targetRotation =
                        Quaternion.LookRotation(
                            direction
                        );

                    transform.rotation =
                        Quaternion.Slerp(
                            transform.rotation,
                            targetRotation,
                            rotationSpeed *
                            Time.deltaTime
                        );
                }

                yield return null;
            }

            transform.position =
                targetPosition;

            yield return new WaitForSeconds(
                0.15f
            );
        }

        isMoving = false;
        UpdateAnimation();
    }
    private void UpdateAnimation()
    {
        if (animator == null)
            return;

        animator.SetBool(
            "IsMoving",
            isMoving
        );
    }
}
