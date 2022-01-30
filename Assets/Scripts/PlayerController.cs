using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[Serializable]
public class Player
{
    public float speed = 6f;
    public List<Resource> backpack;
    public int backpackMaxSize = 10;
    public float resourceTransferTime = 1f;

    public GameObject backpackObject;
    public Transform transferPoint;

    public Player()
    {
        backpack = new List<Resource>();
    }

    public IEnumerator PutIntoBackpack(Resource resource)
    {
        resource.resourceObject.transform.parent = backpackObject.transform;

        Vector3 itemPosition = transferPoint.localPosition + Vector3.up * backpack.Count / 4;
        backpack.Add(resource);

        Vector3 startPosition = resource.resourceObject.transform.localPosition;
        Quaternion startRotation = resource.resourceObject.transform.localRotation;
        float startTime = Time.time;
        while (resource.resourceObject.transform.localPosition != itemPosition)
        {
            if (resource.resourceObject == null)
                yield break;
            var delta = Mathf.Pow((Time.time - startTime), 0.2f);
            resource.resourceObject.transform.localPosition = Vector3.Lerp(startPosition, itemPosition, delta);
            resource.resourceObject.transform.localRotation = Quaternion.Lerp(startRotation, Quaternion.identity, delta);
            yield return new WaitForSeconds(0.01f);
        }
    }
}

public class PlayerController : MonoBehaviour
{
    public Player player;
    public CharacterController characterController;
    public string receiveAreaTag;
    public string releaseAreaTag;

    private IEnumerator resourceTransferCoroutine;

    private void OnDestroy()
    {
        if (resourceTransferCoroutine != null)
            StopCoroutine(resourceTransferCoroutine);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag != receiveAreaTag && other.tag != releaseAreaTag)
            return;

        WarehouseController warehouse = other.gameObject.GetComponentInParent<WarehouseController>();
        if (other.tag == releaseAreaTag)
        {
            resourceTransferCoroutine = TakeResourcesCoroutine(warehouse);
        }
        else if (other.tag == receiveAreaTag)
        {
            resourceTransferCoroutine = PutResourcesCoroutine(warehouse);
        }
        StartCoroutine(resourceTransferCoroutine);
    }

    private void OnTriggerExit(Collider other)
    {
        if (resourceTransferCoroutine != null)
            StopCoroutine(resourceTransferCoroutine);
    }

    void Update()
    {
        var gamepad = Gamepad.current;
        if (gamepad == null) return;

        Vector2 leftStickValue = gamepad.leftStick.ReadValue();
        Vector3 direction = new Vector3(leftStickValue.x, 0f, leftStickValue.y).normalized;

        if (direction.magnitude >= 0.1f)
        {
            float directionAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg - 45f;
            transform.rotation = Quaternion.Euler(0, directionAngle, 0);

            var cameraForward = Camera.main.transform.forward;
            var cameraRight = Camera.main.transform.right;
            cameraForward.y = 0f;
            cameraRight.y = 0f;
            Vector3 movement = direction.x * cameraRight.normalized + direction.z * cameraForward.normalized;
            characterController.Move(movement * player.speed * Time.deltaTime);
        }
    }

    /// <summary>
    /// Coroutine to start putting resources into warehouse.
    /// </summary>
    /// <returns></returns>
    private IEnumerator PutResourcesCoroutine(WarehouseController warehouse)
    {
        while (player.backpack.Count > 0)
        {
            int acceptableResourceIndex = -1;
            for (int i = player.backpack.Count - 1; i >= 0; i--)
            {
                if (warehouse.acceptableResources.Contains(player.backpack[i].props.tag))
                {
                    acceptableResourceIndex = i;
                    break;
                }
            }
            if (acceptableResourceIndex == -1)
                yield break;

            bool resourceAccepted = warehouse.ReceiveResource(player.backpack[acceptableResourceIndex]);
            if (resourceAccepted)
            {
                player.backpack.RemoveAt(acceptableResourceIndex);
                if (player.backpack.Count > acceptableResourceIndex)
                    for (int i = acceptableResourceIndex; i < player.backpack.Count; i++)
                    {
                        Vector3 newResourcePosition = player.backpack[i].resourceObject.transform.localPosition - Vector3.up / 4;
                        StartCoroutine(StackUpResource(player.backpack[i].resourceObject, newResourcePosition));
                    }
            }
            yield return new WaitForSeconds(player.resourceTransferTime);
        }
        yield break;
    }

    /// <summary>
    /// Coroutine to start taking resources from warehouse.
    /// </summary>
    /// <returns></returns>
    private IEnumerator TakeResourcesCoroutine(WarehouseController warehouse)
    {
        while (player.backpack.Count < player.backpackMaxSize)
        {
            Resource resource = warehouse.ReleaseResource();
            // don't stop coroutine when warehouse doesn't have resources. It can produce new resource over time
            if (resource != null)
            {
                StartCoroutine(player.PutIntoBackpack(resource));
            }
            yield return new WaitForSeconds(player.resourceTransferTime);
        }
        yield break;
    }

    private IEnumerator StackUpResource(GameObject resourceObject, Vector3 newPosition)
    {
        Vector3 startPosition = resourceObject.transform.localPosition;
        float startTime = Time.time;
        while (resourceObject.transform.localPosition != newPosition)
        {
            if (resourceObject == null)
                yield break;
            resourceObject.transform.localPosition = Vector3.Lerp(startPosition, newPosition, (Time.time - startTime) * 10);
            yield return new WaitForSeconds(0.01f);
        }
    }
}
