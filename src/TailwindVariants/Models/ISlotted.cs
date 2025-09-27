namespace TailwindVariants;

public interface ISlotted<TSlots>
    where TSlots : ISlots
{
    TSlots? Classes { get; }
}
