namespace TailwindVariants.NET.SourceGenerator;

public static class SourceGenerationHelper
{
    public const string ExtensionMethods =
        """
        using System;
        using System.Collections.Generic;
        using System.Runtime.CompilerServices;

        #nullable enable

        namespace TailwindVariants.NET
        {
            public static class SlotsMapExtensions
            {
                public static string? GetByName<TSlots>(this SlotMap<TSlots> slots, string name) where TSlots : ISlots
                {
                    TryGetByName(slots, name, out var v);
                    return v;
                }

                public static bool TryGetByName<TSlots>(this SlotMap<TSlots> slots, string name, out string? value) where TSlots : ISlots
                {
                    return slots.Map.TryGetValue(name, out value);
                }
            }
        }
        """;
}
