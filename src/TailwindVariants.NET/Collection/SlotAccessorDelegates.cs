﻿namespace TailwindVariants.NET;

/// <summary>
/// A delegate that returns a slot's classes from a typed slots object.
/// </summary>
public delegate string? SlotAccessor<TSlots>(TSlots slots)
    where TSlots : ISlots, new();

/// <summary>
/// Variant accessor that given an owner returns an object (selected variant).
/// </summary>
public delegate object? VariantAccessor<TOwner>(TOwner owner);