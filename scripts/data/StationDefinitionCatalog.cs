using System;
using System.Collections.Generic;
using Godot;

namespace GlassesBar;

public static class StationDefinitionCatalog
{
    public const string PrototypeCatalogPath =
        "res://data/gameplay/prototype_station_catalog.tres";

    private static StationCatalogDefinition? _prototypeCatalog;
    private static IReadOnlyDictionary<string, StationDefinition>? _prototypeById;

    public static StationCatalogDefinition LoadPrototypeCatalog()
    {
        EnsurePrototypeLoaded();
        return _prototypeCatalog!;
    }

    public static StationDefinition GetPrototype(string id, StationKind expectedKind)
    {
        EnsurePrototypeLoaded();
        if (!_prototypeById!.TryGetValue(id, out var definition))
            throw new InvalidOperationException($"Unknown station definition '{id}'.");
        if (definition.Kind != expectedKind)
        {
            throw new InvalidOperationException(
                $"Station '{id}' kind mismatch: layout={expectedKind}, definition={definition.Kind}.");
        }

        return definition;
    }

    private static void EnsurePrototypeLoaded()
    {
        if (_prototypeCatalog is not null)
            return;

        _prototypeCatalog =
            ResourceLoader.Load<StationCatalogDefinition>(PrototypeCatalogPath)
            ?? throw new InvalidOperationException(
                $"Station catalog could not be loaded from '{PrototypeCatalogPath}'.");
        _prototypeById = _prototypeCatalog.BuildValidatedIndex();
    }
}
