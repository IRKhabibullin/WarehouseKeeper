using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public CharacterController characterController;
    public float speed = 6f;
    public string receiveAreaTag;
    public string releaseAreaTag;
    public GameObject backpackObject;
    public List<GameObject> backpack;
    public int backpackMaxSize;
    public float resourceTransferTime;

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

        HouseController house = other.gameObject.GetComponentInParent<HouseController>();
        if (other.tag == releaseAreaTag)
        {
            resourceTransferCoroutine = TakeResourcesCoroutine(house);
        }
        else if (other.tag == receiveAreaTag)
        {
            resourceTransferCoroutine = PutResourcesCoroutine(house);
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
            characterController.Move(movement * speed * Time.deltaTime);
        }
    }

    /// <summary>
    /// Coroutine to start putting resources into house.
    /// </summary>
    /// <returns></returns>
    private IEnumerator PutResourcesCoroutine(HouseController house)
    {
        while (backpack.Count > 0)
        {
            int acceptableResourceIndex = -1;
            for (int i = backpack.Count - 1; i >= 0; i--)
            {
                if (house.acceptableResources.Contains(backpack[i].tag))
                {
                    acceptableResourceIndex = i;
                    break;
                }
            }
            if (acceptableResourceIndex == -1)
                yield break;

            bool resourceAccepted = house.ReceiveResource(backpack[acceptableResourceIndex]);
            if (resourceAccepted)
            {
                backpack.RemoveAt(acceptableResourceIndex);
            }
            yield return new WaitForSeconds(resourceTransferTime);
        }
        yield break;
    }

    /// <summary>
    /// Coroutine to start taking resources from house.
    /// </summary>
    /// <returns></returns>
    private IEnumerator TakeResourcesCoroutine(HouseController house)
    {
        while (backpack.Count < backpackMaxSize)
        {
            GameObject resource = house.ReleaseResource();
            // don't stop coroutine when house doesn't have resources. It can produce new resource over time
            if (resource != null)
            {
                PutIntoBackpack(resource);
            }
            yield return new WaitForSeconds(resourceTransferTime);
        }
        yield break;
    }

    private void PutIntoBackpack(GameObject resource)
    {
        Vector3 itemPosition = new Vector3(-0.2f, 0.9f + 0.5f * backpack.Count, 0);
        resource.transform.parent = backpackObject.transform;
        resource.transform.localRotation = Quaternion.identity;
        StartCoroutine(PutResourceCoroutine(resource, itemPosition, Quaternion.identity));
        backpack.Add(resource);
    }

    private IEnumerator PutResourceCoroutine(GameObject resource, Vector3 finishPosition, Quaternion finishRotation)
    {
        Vector3 startPosition = resource.transform.localPosition;
        float startTime = Time.time;
        while (Vector3.SqrMagnitude(startPosition - finishPosition) > 0.001)
        {
            if (resource == null)
                yield break;
            resource.transform.localPosition = Vector3.Lerp(startPosition, finishPosition, Mathf.Pow((Time.time - startTime), 0.2f));
            yield return new WaitForSeconds(0.01f);
        }
    }
}
