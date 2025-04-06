using System.Collections.Generic;

[System.Serializable]
public class BusinessProductionProfile
{
    public BusinessType businessType;
    public List<ProductionEntry> producibleItems = new List<ProductionEntry>();
}

[System.Serializable]
public class ProductionEntry
{
    public string outputItemName;
    public int outputAmount = 1;
    // Only used when requiredResources is empty
    public float spawnChance = 1.0f;
    public List<ResourceRequirement> requiredResources = new List<ResourceRequirement>();
}

[System.Serializable]
public class ResourceRequirement
{
    public string inputItemName;
    public int inputAmount = 1;
}