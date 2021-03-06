﻿using System;

namespace Vostok.Logging.Abstractions.Helpers
{
    internal static class TypesHelper
    {
        public static bool IsAnonymousType(Type type)
            => type.IsConstructedGenericType &&
               Nullable.GetUnderlyingType(type) == null &&
               type.Name.Contains("<>") &&
               type.Name.Contains("AnonymousType");
    }
}