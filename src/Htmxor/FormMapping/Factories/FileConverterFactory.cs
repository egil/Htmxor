// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Htmxor.FormMapping.Converters;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Http;

namespace Htmxor.FormMapping.Factories;

internal sealed class FileConverterFactory : IFormDataConverterFactory
{
    [RequiresDynamicCode(FormMappingHelpers.RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(FormMappingHelpers.RequiresUnreferencedCodeMessage)]
    public bool CanConvert(Type type, FormDataMapperOptions options) => CanConvertCommon(type) || type == typeof(IBrowserFile) || type == typeof(IReadOnlyList<IBrowserFile>);

    private static bool CanConvertCommon(Type type) => type == typeof(IFormFile) || type == typeof(IFormFileCollection) || type == typeof(IReadOnlyList<IFormFile>);

    [RequiresDynamicCode(FormMappingHelpers.RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(FormMappingHelpers.RequiresUnreferencedCodeMessage)]
    public FormDataConverter CreateConverter(Type type, FormDataMapperOptions options)
    {
        return Activator.CreateInstance(typeof(FileConverter<>).MakeGenericType(type)) as FormDataConverter ??
            throw new InvalidOperationException($"Unable to create converter for '{type.FullName}'.");
    }
}
