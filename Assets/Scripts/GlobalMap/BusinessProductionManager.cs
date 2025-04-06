using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BusinessProductionManager : SerializedSingleton<BusinessProductionManager>
{
    public List<BusinessProductionProfile> profiles;

    private void Start()
    {
        profiles.Clear();
        profiles.AddRange(TypeArrayDeserializer.LoadResourceFromJson<BusinessProductionProfile>("BusinessProductionProfiles"));
    }


    public BusinessProductionProfile GetProfile(BusinessType type)
    {
        return profiles.Find(p => p.businessType == type);
    }

    public List<ProductionEntry> GetProducibleItems(BusinessType type)
    {
        var profile = GetProfile(type);
        return profile != null ? profile.producibleItems : new List<ProductionEntry>();
    }

    public List<ResourceRequirement> GetRequiredItems(BusinessType type)
    {
        if (!JsonReader.Instance.GetBusiness(type).producesProducts)
        {
            return null;
        }
        else
        {
            var profile = GetProfile(type);
            return profile != null ? profile.producibleItems.SelectMany(item => item.requiredResources).GroupBy(r => r.inputItemName)
                      .Select(g => g.First())
                      .ToList() : new List<ResourceRequirement>();
        }
    }
}