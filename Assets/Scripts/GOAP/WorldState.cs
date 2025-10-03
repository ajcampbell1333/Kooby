using System.Collections.Generic;
using UnityEngine;

public class WorldState
{
    public Dictionary<string, object> state = new Dictionary<string, object>();
    
    public WorldState() { }
    
    public WorldState(WorldState other)
    {
        state = new Dictionary<string, object>(other.state);
    }
    
    public void Set(string key, object value) => state[key] = value;
    
    public T Get<T>(string key) => state.ContainsKey(key) ? (T)state[key] : default(T);
    
    public bool Has(string key) => state.ContainsKey(key);
    
    public bool Matches(WorldState other)
    {
        foreach (var kvp in other.state)
        {
            if (!state.ContainsKey(kvp.Key) || !state[kvp.Key].Equals(kvp.Value))
                return false;
        }
        return true;
    }
}
