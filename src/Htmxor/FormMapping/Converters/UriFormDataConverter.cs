// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Htmxor.FormMapping.Converters;

internal class UriFormDataConverter : FormDataConverter<Uri>, ISingleValueConverter<Uri>
{
    public bool CanConvertSingleValue() => true;

    public bool TryConvertValue(ref FormDataReader reader, string value, out Uri result)
    {
        if (Uri.TryCreate(value, UriKind.RelativeOrAbsolute, out result!))
        {
            return true;
        }
        else
        {
            // Using ParsableMappingError as ParsableConverter<T> will subsume this
            // converter in a future version.
            var segment = reader.GetLastPrefixSegment();
            reader.AddMappingError(FormattableStringFactory.Create("The value '{0}' is not valid for '{1}'.", value, segment), value);
            result = default!;
            return false;
        }
    }

    [RequiresDynamicCode(FormMappingHelpers.RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(FormMappingHelpers.RequiresUnreferencedCodeMessage)]
    internal override bool TryRead(ref FormDataReader context, Type type, FormDataMapperOptions options, out Uri? result, out bool found)
    {
        found = context.TryGetValue(out var value);
        if (!found)
        {
            result = default;
            return true;
        }
        else
        {
            return TryConvertValue(ref context, value!, out result!);
        }
    }
}
