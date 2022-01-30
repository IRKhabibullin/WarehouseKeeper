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
        for (int i = Count - 1; i >= 0; i--)
        {
            if (!resources[i].isTransfering)
            {
                var resource = resources[i];
                resources.RemoveAt(i);
                return resource;
            }
        }
        return null;
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
                if (resources[i].props.tag == requiredResource && !resources[i].isTransfering)
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

    public IEnumerator Put(Resource resource)
    {
        resource.resourceObject.transform.parent = storageObject.transform;
        resource.isTransfering = true;

        Vector3 finalPosition = transferPoint.localPosition + Vector3.up * Count / 2;
        resources.Add(resource);

        Vector3 startPosition = resource.resourceObject.transform.localPosition;
        Quaternion startRotation = resource.resourceObject.transform.localRotation;
        float startTime = Time.time;
        while (resource.resourceObject.transform.localPosition != finalPosition)
        {
            if (resource.resourceObject == null)
            {
                resource.isTransfering = false;
                yield break;
            }
            var delta = Mathf.Pow((Time.time - startTime), 0.2f);
            resource.resourceObject.transform.localPosition = Vector3.Lerp(startPosition, finalPosition, delta);
            resource.resourceObject.transform.localRotation = Quaternion.Lerp(startRotation, transferPoint.localRotation, delta);
            yield return new WaitForSeconds(0.01f);
        }
        resource.isTransfering = false;
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
        isTransfering = false;
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

public class WarehouseController : MonoBehaviour
{
    [SerializeField] private Storage productionStorage;
    [SerializeField] private Storage supplyStorage;

    public List<ResourceProperty.Tag> acceptableResources;
    public float resourceProductionTime;
    public GameObject resourcePrefab;
    public ResourceProperty productProperty;
    private IEnumerator resourceProductionCoroutine;

    public TextMeshProUGUI statusText;

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
    /// Receive resource from player, who stands in receive area and add it to warehouse's supply storage
    /// </summary>
    /// <returns></returns>
    public bool ReceiveResource(Resource resource)
    {
        if (supplyStorage.Count == supplyStorage.maxCapacity)
            return false;
        StartCoroutine(supplyStorage.Put(resource));
        return true;
    }
    #endregion

    private IEnumerator ProduceResources()
    {
        while (true)
        {
            if (productionStorage.Count >= productionStorage.maxCapacity)
            {
                statusText.text = $"{gameObject.name}: no space";
                yield return new WaitForSeconds(resourceProductionTime);
                continue;
            }
            var supplies = supplyStorage.GetSuppliesForProduction(acceptableResources);
            if (supplies == null)
            {
                statusText.text = $"{gameObject.name}: no supplies";
                yield return new WaitForSeconds(0.1f);
                continue;
            }
            foreach (var item in supplies)
                Destroy(item.resourceObject);
            Resource resource = new Resource(Instantiate(resourcePrefab, transform), productProperty);
            StartCoroutine(productionStorage.Put(resource));
            statusText.text = $"{gameObject.name}: ok";
            yield return new WaitForSeconds(resourceProductionTime);
        }
    }
}
