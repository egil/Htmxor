// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Htmxor.FormMapping.Factories.ComplexType;

internal abstract class ComplexTypeExpressionConverterFactory
{
    [RequiresDynamicCode(FormMappingHelpers.RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(FormMappingHelpers.RequiresUnreferencedCodeMessage)]
    internal abstract FormDataConverter CreateConverter(Type type, FormDataMapperOptions options);
}
