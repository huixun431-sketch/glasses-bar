using System;
using System.Collections.Generic;
using Godot;
using GlassesBar.Domain;

namespace GlassesBar;

[GlobalClass]
public partial class GameplayCatalogDefinition : Resource
{
    [Export] public Godot.Collections.Array<ToolDefinition> Tools { get; set; } = new();
    [Export] public Godot.Collections.Array<OperationDefinition> Operations { get; set; } = new();

    public Dictionary<string, ToolSpec> BuildToolSpecs()
    {
        var result = new Dictionary<string, ToolSpec>(StringComparer.Ordinal);
        foreach (var definition in Tools)
        {
            var spec = definition.BuildSpec();
            if (string.IsNullOrWhiteSpace(spec.Id))
                throw new InvalidOperationException("Tool definitions require stable IDs.");
            result.Add(spec.Id, spec);
        }
        return result;
    }

    public List<OperationSpec> BuildOperationSpecs()
    {
        var result = new List<OperationSpec>();
        foreach (var definition in Operations)
        {
            var spec = definition.BuildSpec();
            if (string.IsNullOrWhiteSpace(spec.Id))
                throw new InvalidOperationException("Operation definitions require stable IDs.");
            _ = spec.ResolveComplexity();
            result.Add(spec);
        }
        return result;
    }
}
