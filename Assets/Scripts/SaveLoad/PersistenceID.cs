using System;
using UnityEngine;

public class PersistenceID : MonoBehaviour
{
    [SerializeField] private string uniqueID;
    public string ID => uniqueID;

    public void RegenerateID()
    {
        uniqueID = Guid.NewGuid().ToString();
    }

    private void OnValidate()
    {
        if (string.IsNullOrEmpty(uniqueID))
            uniqueID = Guid.NewGuid().ToString(); // Automatically generates a new unique ID if missing
    }
}