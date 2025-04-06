using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TexturesContainer : SerializedSingleton <TexturesContainer>
{
    public Dictionary<string, Sprite> sprites;
    public Dictionary<string, Sprite> modifiers;
    public Dictionary<BusinessType, Sprite> businesses;

    public Sprite GetSprite (string keyword)
    {
        Sprite tmpSprite;
        if (sprites.TryGetValue(keyword, out tmpSprite))
        {
            return tmpSprite;
        }
        if (sprites.Count > 0)
        {
            var firstItem = sprites.First();
            return firstItem.Value;
        }
        return null;
    }
    public Sprite GetModifier(string keyword)
    {
        Sprite tmpSprite;
        if (modifiers.TryGetValue(keyword, out tmpSprite))
        {
            return tmpSprite;
        }
        if (modifiers.Count > 0)
        {
            var firstItem = modifiers.First();
            return firstItem.Value;
        }
        return null;
    }

    public Sprite GetBusinessPicture (BusinessType type)
    {
        Sprite tmpSprite;
        if (businesses.TryGetValue(type, out tmpSprite))
        {
            return tmpSprite;
        }
        if (businesses.Count > 0)
        {
            var firstItem = businesses.First();
            return firstItem.Value;
        }
        return null;
    }
}
