using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[Serializable]
public class Storage
{
    private List<Resource> resources;
    [SerializeField] private GameObject storageObject;
    public int maxCapacity;
    public Transform transferPoint;

    public Storage()
    {
        resources = new List<Resource>();
    }

    public int Count { get { return resources.Count; } }

    public Resource Pop()
    {
        if (Count == 0)
            return null;
        var resource = resources[Count - 1];
        resources.RemoveAt(Count - 1);
        return resource;
    }

    public List<Resource> GetSuppliesForProduction(List<ResourceProperty.Tag> recipe)
    {
        List<int> resourcesIndexes = new List<int>();
        List<Resource> supplies = new List<Resource>();
        foreach (var requiredResource in recipe)
        {
            bool hasResource = false;
            for (int i = Count - 1; i >= 0; i--)
            {
                if (resources[i].props.tag == requiredResource)
                {
                    resourcesIndexes.Add(i);
                    supplies.Add(resources[i]);
                    hasResource = true;
                    break;
                }
            }
            if (!hasResource)
                return null;
        }
        foreach (int i in resourcesIndexes)
            resources.RemoveAt(i);
        return supplies;
    }

    public Vector3 CalculateNewResourcePosition()
    {
        return transferPoint.position + Vector3.up * Count / 2;
    }

    public void Put(Resource resource)
    {
        resource.resourceObject.transform.parent = storageObject.transform;
        resource.resourceObject.transform.position = CalculateNewResourcePosition();
        resource.resourceObject.transform.rotation = transferPoint.rotation;
        resources.Add(resource);
    }
}

public class Resource
{
    public GameObject resourceObject;
    public bool isTransfering;
    public ResourceProperty props;

    public Resource(GameObject resource, ResourceProperty property)
    {
        resourceObject = resource;
        this.props = property;
        resourceObject.GetComponent<MeshRenderer>().material = props.material;
        resourceObject.tag = props.tag.ToString();
    }
}

[Serializable]
public class ResourceProperty
{
    public enum Tag
    {
        Resource1,
        Resource2,
        Resource3
    }

    public Tag tag;
    public Material material;
}

public class HouseController : MonoBehaviour
{
    [SerializeField] private Storage productionStorage;
    [SerializeField] private Storage supplyStorage;

    public List<ResourceProperty.Tag> acceptableResources;
    public float resourceProductionTime;
    public GameObject resourcePrefab;
    public ResourceProperty productProperty;
    private IEnumerator resourceProductionCoroutine;

    public TextMeshProUGUI debugText;

    private void Start()
    {
        resourceProductionCoroutine = ProduceResources();
        StartCoroutine(resourceProductionCoroutine);
    }

    private void OnDestroy()
    {
        if (resourceProductionCoroutine != null)
        {
            StopCoroutine(resourceProductionCoroutine);
        }
    }

    #region api
    /// <summary>
    /// Release resource from production storage to player, who stands in release area
    /// </summary>
    /// <returns></returns>
    public Resource ReleaseResource()
    {
        return productionStorage.Pop();
    }

    /// <summary>
    /// Receive resource from player, who stands in receive area and add it to house's supply storage
    /// </summary>
    /// <returns></returns>
    public bool ReceiveResource(Resource resource)
    {
        if (supplyStorage.Count == supplyStorage.maxCapacity)
            return false;
        StartCoroutine(PutResourceCoroutine(supplyStorage, resource.resourceObject));
        supplyStorage.Put(resource);
        return true;
    }
    #endregion

    private IEnumerator ProduceResources()
    {
        while (true)
        {
            if (productionStorage.Count >= productionStorage.maxCapacity)
            {
                yield return new WaitForSeconds(resourceProductionTime);
                continue;
            }
            var supplies = supplyStorage.GetSuppliesForProduction(acceptableResources);
            if (supplies == null)
            {
                yield return new WaitForSeconds(0.1f);
                continue;
            }
            foreach (var item in supplies)
                Destroy(item.resourceObject);
            Resource resource = new Resource(Instantiate(resourcePrefab), productProperty);
            productionStorage.Put(resource);
            yield return new WaitForSeconds(resourceProductionTime);
        }
    }

    private IEnumerator PutResourceCoroutine(Storage storage, GameObject resource)
    {
        Vector3 startPosition = resource.transform.position;
        Quaternion startRotation = resource.transform.rotation;
        Vector3 finishPosition = storage.CalculateNewResourcePosition();
        float startTime = Time.time;
        while (Vector3.SqrMagnitude(startPosition - finishPosition) > 0.001)
        {
            if (resource == null)
                yield break;
            var delta = Mathf.Pow((Time.time - startTime), 0.2f);
            resource.transform.position = Vector3.Lerp(startPosition, finishPosition, delta);
            resource.transform.rotation = Quaternion.Lerp(startRotation, storage.transferPoint.rotation, delta);
            yield return new WaitForSeconds(0.01f);
        }
    }
}
