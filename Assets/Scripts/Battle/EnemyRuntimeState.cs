using System.Collections.Generic;
using UnityEngine;

public sealed class EnemyRuntimeState
{
    private readonly Dictionary<string, int> _values = new Dictionary<string, int>();
    private readonly Dictionary<string, EnemyRuntimeVariableDefinition> _definitions = new Dictionary<string, EnemyRuntimeVariableDefinition>();

    public CharacterData Source { get; }
    public BattleCharacter RememberedTarget { get; private set; }
    public bool HasFled { get; private set; }

    public EnemyRuntimeState(CharacterData source)
    {
        Source = source;
        if (source?.enemyRuntimeVariables == null) return;

        foreach (EnemyRuntimeVariableDefinition definition in source.enemyRuntimeVariables)
        {
            if (definition == null || string.IsNullOrWhiteSpace(definition.id)) continue;
            string id = definition.id.Trim();
            if (_definitions.ContainsKey(id)) continue;
            _definitions.Add(id, definition);
            _values.Add(id, Clamp(definition, definition.initialValue));
        }
    }

    public bool Contains(string id) => !string.IsNullOrWhiteSpace(id) && _values.ContainsKey(id.Trim());

    public int Get(string id)
    {
        return Contains(id) ? _values[id.Trim()] : 0;
    }

    public EnemyRuntimeVariableDefinition GetDefinition(string id)
    {
        return Contains(id) ? _definitions[id.Trim()] : null;
    }

    public bool Set(string id, int value)
    {
        if (!Contains(id)) return false;
        string key = id.Trim();
        _values[key] = Clamp(_definitions[key], value);
        return true;
    }

    public bool Add(string id, int amount)
    {
        return Set(id, Get(id) + amount);
    }

    public void Remember(BattleCharacter target)
    {
        RememberedTarget = target != null && target.status != null && target.status.currentHp > 0f ? target : null;
    }

    public void MarkFled()
    {
        HasFled = true;
    }

    private static int Clamp(EnemyRuntimeVariableDefinition definition, int value)
    {
        return definition.hasMaxValue ? Mathf.Min(value, definition.maxValue) : value;
    }
}
