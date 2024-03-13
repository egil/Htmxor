// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Htmxor.FormMapping;

internal class FormDataMappingException : Exception
{
    public FormDataMappingException(FormDataMappingError error) : this(error, null) { }

    public FormDataMappingException(FormDataMappingError error, Exception? innerException)
        : base("An error occurred while trying to map a value from form data. For more details, see the 'Error' property and the 'InnerException' property.", innerException)
    {
        ArgumentNullException.ThrowIfNull(error);
        Error = error;
    }

    public FormDataMappingError Error { get; }
}
