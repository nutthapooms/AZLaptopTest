﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Squidex.Domain.Apps.Core.Contents;
using Squidex.Domain.Apps.Core.Schemas;
using Squidex.Infrastructure;
using Squidex.Infrastructure.Json.Objects;

#pragma warning disable RECS0033 // Convert 'if' to '||' expression

namespace Squidex.Domain.Apps.Core.ConvertContent;

public sealed class ContentConverter(ResolvedComponents components, Schema schema)
{
    private readonly List<IContentDataConverter> dataConverters = [];
    private readonly List<IContentItemConverter> itemConverters = [];
    private readonly List<IContentFieldConverter> fieldConverters = [];
    private readonly List<IContentValueConverter> valueConverters = [];

    public ContentConverter Add(IConverter converter)
    {
        if (converter is IContentDataConverter contentConverter)
        {
            dataConverters.Add(contentConverter);
        }

        if (converter is IContentItemConverter itemConverter)
        {
            itemConverters.Add(itemConverter);
        }

        if (converter is IContentFieldConverter fieldConverter)
        {
            fieldConverters.Add(fieldConverter);
        }

        if (converter is IContentValueConverter valueConverter)
        {
            valueConverters.Add(valueConverter);
        }

        return this;
    }

    public ContentData Convert(ContentData content)
    {
        // The conversion process assumes that we have ownership of the data and can manipulate it.
        // Clones are only created to save allocations.
        var result = new ContentData(content.Count);

        // Some conversions should be made early, e.g. calculating default values.
        foreach (var converter in dataConverters)
        {
            converter.ConvertDataBefore(schema, content);
        }

        foreach (var (fieldName, fieldData) in content)
        {
            if (fieldData == null || !schema.FieldsByName.TryGetValue(fieldName, out var field))
            {
                continue;
            }

            // Some conversions are faster to do upfront, e.g. to remove hidden fields.
            var newData = ConvertField(field, fieldData);

            if (newData == null)
            {
                continue;
            }

            newData = ConvertValues(field, newData);

            // Some conversions are faster to do later, e.g. fallback handling for languages.
            newData = ConvertFieldAfter(field, newData);

            if (newData != null)
            {
                result.Add(field.Name, newData);
            }
        }

        // Some conversions should be done later.
        foreach (var converter in dataConverters)
        {
            converter.ConvertDataAfter(schema, result);
        }

        return result;
    }

    private ContentFieldData? ConvertField(IRootField field, ContentFieldData data)
    {
        foreach (var converter in fieldConverters)
        {
            var newData = converter.ConvertFieldBefore(field, data);

            if (newData == null)
            {
                return null;
            }

            data = newData;
        }

        return data;
    }

    private ContentFieldData? ConvertFieldAfter(IRootField field, ContentFieldData data)
    {
        foreach (var converter in fieldConverters)
        {
            var newData = converter.ConvertFieldAfter(field, data);

            if (newData == null)
            {
                return null;
            }

            data = newData;
        }

        return data;
    }

    private (bool Remove, JsonValue) ConvertByType<T>(T field, JsonValue value, IField? parent) where T : IField
    {
        switch (field)
        {
            case IArrayField arrayField:
                return ConvertArray(arrayField, value);

            case IField<ComponentFieldProperties>:
                return ConvertComponent(value, field);

            case IField<ComponentsFieldProperties>:
                return ConvertComponents(value, field);

            default:
                return ConvertValue(field, value, parent);
        }
    }

    private (bool Remove, JsonValue) ConvertArray(IArrayField field, JsonValue value)
    {
        if (value.Value is not JsonArray array)
        {
            return (true, default);
        }

        for (int i = 0; i < array.Count; i++)
        {
            var oldValue = array[i];

            var (removed, newValue) = ConvertArrayItem(field, oldValue);

            if (removed)
            {
                array.RemoveAt(i);
                i--;
            }
            else if (!ReferenceEquals(newValue.Value, oldValue.Value))
            {
                array[i] = newValue;
            }
        }

        return (false, array);
    }

    private (bool Remove, JsonValue) ConvertComponents(JsonValue value, IField parent)
    {
        if (value.Value is not JsonArray array)
        {
            return (true, default);
        }

        for (int i = 0; i < array.Count; i++)
        {
            var oldValue = array[i];

            var (removed, newValue) = ConvertComponent(oldValue, parent);

            if (removed)
            {
                array.RemoveAt(i);
                i--;
            }
            else if (!ReferenceEquals(newValue.Value, oldValue.Value))
            {
                // Faster to check for reference equality than for deep equals.
                array[i] = newValue;
            }
        }

        return (false, array);
    }

    private (bool Remove, JsonValue) ConvertComponent(JsonValue value, IField parent)
    {
        if (value.Value is not JsonObject obj || !obj.TryGetValue(Component.Discriminator, out var discriminator))
        {
            return (true, default);
        }

        if (!components.TryGetValue(DomainId.Create(discriminator.ToString()), out var component))
        {
            return (true, default);
        }

        return (false, ConvertNested(component.FieldCollection, obj, parent, component.Fields));
    }

    private (bool Remove, JsonValue) ConvertArrayItem(IArrayField field, JsonValue value)
    {
        if (value.Value is not JsonObject obj)
        {
            return (true, default);
        }

        return (false, ConvertNested(field.FieldCollection, obj, field, field.Fields));
    }

    private ContentFieldData ConvertValues(IField field, ContentFieldData source)
    {
        ContentFieldData? result = null;

        foreach (var (key, oldValue) in source)
        {
            var (removed, newData) = ConvertByType(field, oldValue, null);

            // Create a copy to avoid allocations if nothing has been changed.
            if (removed)
            {
                result ??= new ContentFieldData(source);
                result.Remove(key);
            }
            else if (!ReferenceEquals(newData.Value, oldValue.Value))
            {
                // Faster to check for reference equality than for deep equals.
                result ??= new ContentFieldData(source);
                result[key] = newData;
            }
        }

        return result ?? source;
    }

    private JsonValue ConvertNested<T>(FieldCollection<T> fields, JsonObject source, IField parent, IEnumerable<IField> fieldSchema) where T : IField
    {
        foreach (var converter in itemConverters)
        {
            source = converter.ConvertItemBefore(parent, source, fieldSchema);
        }

        JsonObject? result = null;

        foreach (var (key, oldValue) in source)
        {
            var newValue = oldValue;

            var remove = false;

            if (fields.ByName.TryGetValue(key, out var field))
            {
                (remove, newValue) = ConvertByType(field, oldValue, parent);
            }
            else if (key != Component.Discriminator)
            {
                remove = true;
            }

            // Create a copy to avoid allocations if nothing has been changed.
            if (remove)
            {
                result ??= new JsonObject(source);
                result.Remove(key);
            }
            else if (!ReferenceEquals(newValue.Value, oldValue.Value))
            {
                // Faster to check for reference equality than for deep equals.
                result ??= new JsonObject(source);
                result[key] = newValue;
            }
        }

        result ??= source;

        foreach (var converter in itemConverters)
        {
            result = converter.ConvertItemAfter(parent, result, fieldSchema);
        }

        return result ?? source;
    }

    private (bool Remove, JsonValue) ConvertValue(IField field, JsonValue value, IField? parent)
    {
        foreach (var converter in valueConverters)
        {
            // Use a tuple and not a nullable result to avoid boxing and allocations.
            (var remove, value) = converter.ConvertValue(field, value, parent);

            if (remove)
            {
                return (true, default);
            }
        }

        return (false, value);
    }
}
