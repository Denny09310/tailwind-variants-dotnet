using System.Linq.Expressions;

namespace TailwindVariants.NET;

internal record struct CompiledVariant<TOwner, TSlots>(Expression<VariantAccessor<TOwner>> Expr, IVariant<TSlots> Entry, VariantAccessor<TOwner> Accessor)
	where TSlots : ISlots, new()
	where TOwner : ISlotted<TSlots>;


internal record struct CompiledCompoundVariant<TOwner, TSlots>(Predicate<TOwner> Predicate, SlotCollection<TSlots> Slots)
	where TSlots : ISlots, new()
	where TOwner : ISlotted<TSlots>;
