using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HouseController : MonoBehaviour
{
    private List<GameObject> resourcesStorage = new List<GameObject>();
    [SerializeField] private int resourcesStorageMaxSize;

    public List<GameObject> productionStorage = new List<GameObject>();
    [SerializeField] private int productionStorageMaxSize;

    public List<string> acceptableResources;
    public GameObject resourcesStorageObject;

    void Start()
    {
    }

    void Update()
    {
        
    }

    /// <summary>
    /// Release resource from house to player, who stands in release area
    /// </summary>
    /// <returns></returns>
    public GameObject ReleaseResource()
    {
        if (productionStorage.Count == 0)
            return null;
        var resource = productionStorage[productionStorage.Count - 1];
        productionStorage.RemoveAt(productionStorage.Count - 1);
        return resource;
    }

    /// <summary>
    /// Receive resource from player, who stands in receive area and add it to house's resource storage
    /// </summary>
    /// <returns></returns>
    public bool ReceiveResource(GameObject resource)
    {
        if (resourcesStorage.Count == resourcesStorageMaxSize)
            return false;
        PutIntoStorage(resource);
        return true;
    }

    private void PutIntoStorage(GameObject resource)
    {
        Vector3 itemPosition = new Vector3(0, 0.25f + 0.5f * resourcesStorage.Count, 2.95f);
        resource.transform.parent = resourcesStorageObject.transform;
        resource.transform.localRotation = Quaternion.Euler(0, 90, 0);
        StartCoroutine(PutResourceCoroutine(resource, itemPosition, Quaternion.Euler(0, 90, 0)));
        resourcesStorage.Add(resource);
    }

    private IEnumerator PutResourceCoroutine(GameObject resource, Vector3 finishPosition, Quaternion finishRotation)
    {
        Vector3 startPosition = resource.transform.localPosition;
        float startTime = Time.time;
        while (Vector3.SqrMagnitude(startPosition - finishPosition) > 0.001)
        {
            resource.transform.localPosition = Vector3.Lerp(startPosition, finishPosition, Mathf.Pow((Time.time - startTime), 0.2f));
            yield return new WaitForSeconds(0.01f);
        }
    }
}
