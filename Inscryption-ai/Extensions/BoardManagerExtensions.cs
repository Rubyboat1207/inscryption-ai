using DiskCardGame;

namespace Inscryption_ai.Extensions
{
    public static class BoardManagerExtensions
    {
        public static string DescribeStateToAI(this BoardManager manager)
        {
            var slots = new string[manager.AllSlots.Count];

            for (var i = 0; i < slots.Length; i++)
            {
                var slot = manager.AllSlots[i];

                slots[i] = $"[ SLOT {i} - ";

                if (slot.Card != null)
                {
                    slots[i] = "Empty ]";
                    continue;
                }
                
                slots[i] = $"{slot.Card.DescribeToAI()} ";

                if (slot.opposingSlot != null)
                {
                    slots[i] += $"- Opposes slot {manager.AllSlots.IndexOf(slot.opposingSlot)}";
                }

                slots[i] += "]";
            }

            for (var i = 0; i < slots.Length; i++)
            {
                if (i % 4 == 0)
                {
                    slots[i] += "\n";
                }
            }

            return string.Join(" ", slots);
        }
    }
}