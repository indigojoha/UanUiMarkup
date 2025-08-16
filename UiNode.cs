namespace UanUiMarkup;

public class UiNode
{
    public string Id { get; set; } = "";
    public string Type { get; set; } = "";
    public Dictionary<string, object> Attributes { get; set; } = [];
    public List<UiNode> Children { get; set; } = [];

    public bool TryGetStringAttribute(string key, out string value)
    {
        if (Attributes.TryGetValue(key, out var obj) && obj is string str)
        {
            value = str;
            return true;
        }
        value = "";
        return false;
    }

    public bool TryGetIntAttribute(string key, out int value)
    {
        if (Attributes.TryGetValue(key, out var obj) && obj is int i)
        {
            value = i;
            return true;
        }
        value = 0;
        return false;
    }

    public bool TryGetBoolAttribute(string key, out bool value)
    {
        if (Attributes.TryGetValue(key, out var obj) && obj is bool b)
        {
            value = b;
            return true;
        }
        value = false;
        return false;
    }
}
