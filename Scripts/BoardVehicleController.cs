using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class BoardVehicleController : MonoBehaviour
{
    [Header("Board")]
    [SerializeField] private Transform[] boardSpaces;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float pauseBetweenSpaces = 0.15f;
    [SerializeField] private float rotationSpeed = 8f;

    private int currentSpaceIndex = 0;
    private bool isMoving = false;

    private void Start()
    {
        if (boardSpaces == null || boardSpaces.Length == 0)
        {
            Debug.LogError("No board spaces assigned!");
            return;
        }

        // Start on first space
        transform.position = boardSpaces[0].position;

        if (boardSpaces[0].rotation != Quaternion.identity)
        {
            transform.rotation = boardSpaces[0].rotation;
        }
    }

    private void Update()
    {
        // Temporary testing
        if (Keyboard.current != null &&
            Keyboard.current.spaceKey.wasPressedThisFrame &&
            !isMoving)
        {
            StartCoroutine(MoveSpaces(3));
        }
    }

    public IEnumerator MoveSpaces(int spacesToMove)
    {
        if (isMoving)
            yield break;

        if (boardSpaces == null ||
            boardSpaces.Length == 0)
            yield break;

        isMoving = true;

        for (int i = 0; i < spacesToMove; i++)
        {
            int nextIndex = currentSpaceIndex + 1;

            // Stop if we reach the end
            if (nextIndex >= boardSpaces.Length)
            {
                break;
            }

            currentSpaceIndex = nextIndex;

            Transform target =
                boardSpaces[currentSpaceIndex];

            yield return StartCoroutine(
                MoveToSpace(target)
            );

            // Small pause so each space feels like
            // a separate board movement
            yield return new WaitForSeconds(
                pauseBetweenSpaces
            );
        }

        isMoving = false;

        // Check what the player landed on
        LandedOnSpace(currentSpaceIndex);
    }

    private IEnumerator MoveToSpace(Transform target)
    {
        while (Vector3.Distance(
            transform.position,
            target.position) > 0.02f)
        {
            // Smooth movement
            transform.position =
                Vector3.MoveTowards(
                    transform.position,
                    target.position,
                    moveSpeed * Time.deltaTime
                );

            // Rotate toward next space
            Vector3 direction =
                target.position -
                transform.position;

            if (direction.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation =
                    Quaternion.LookRotation(direction);

                transform.rotation =
                    Quaternion.Slerp(
                        transform.rotation,
                        targetRotation,
                        rotationSpeed * Time.deltaTime
                    );
            }

            yield return null;
        }

        // Make sure we land exactly on the space
        transform.position =
            target.position;
    }

    private void LandedOnSpace(int spaceIndex)
    {
        if (spaceIndex < 0 ||
            spaceIndex >= boardSpaces.Length)
            return;

        BoardSpace space =
            boardSpaces[spaceIndex]
                .GetComponent<BoardSpace>();

        if (space == null)
        {
            Debug.Log(
                "Space " + spaceIndex +
                " has no BoardSpace component."
            );

            return;
        }

        switch (space.GetSpaceType())
        {
            case BoardSpace.SpaceType.Normal:

                Debug.Log("Normal space.");

                break;

            case BoardSpace.SpaceType.AddPoints:

                Debug.Log(
                    "+" + space.GetPoints() +
                    " points!"
                );

                // Add points here later

                break;

            case BoardSpace.SpaceType.RemovePoints:

                Debug.Log(
                    "-" + space.GetPoints() +
                    " points!"
                );

                // Remove points here later

                break;

            case BoardSpace.SpaceType.Minigame:

                Debug.Log(
                    "MINIGAME: " +
                    space.GetMinigameName()
                );

              

                break;
        }
    }

    public int GetCurrentSpaceIndex()
    {
        return currentSpaceIndex;
    }

    public bool IsMoving()
    {
        return isMoving;
    }
}