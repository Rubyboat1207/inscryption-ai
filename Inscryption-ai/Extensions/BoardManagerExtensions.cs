using DiskCardGame;

namespace Inscryption_ai.Extensions
{
    public static class BoardManagerExtensions
    {
        public static string DescribeStateToAI(this BoardManager manager)
        {
            var queue = new string[manager.OpponentQueueSlots.Count];
            var slots = new string[manager.AllSlots.Count];
            
            for (var i = 0; i < queue.Length; i++)
            {
                queue[i] = $"[ ENEMY QUEUE SLOT {i} - EMPTY ]";
            }

            for (var i = 0; i < slots.Length; i++)
            {
                var slot = manager.AllSlots[i];

                var queued = manager.GetCardQueuedForSlot(slot);

                if (i < queue.Length && queued != null)
                {
                    queue[i] = $"[ QUEUE SLOT {i} - {queued.DescribeToAI(false)} ]";
                }


                slots[i] = $"[ {(i < 4 ? "FRIENDLY" : "ENEMY")} SLOT {i % 4} - ";

                if (slot.Card == null)
                {
                    slots[i] += "Empty ]";
                    continue;
                }
                
                slots[i] += $"{slot.Card.DescribeToAI(false)} ";

                if (slot.opposingSlot != null)
                {
                    slots[i] += $"- Opposes slot {manager.AllSlots.IndexOf(slot.opposingSlot)}";
                }

                slots[i] += " ]";
            }

            for (var i = 0; i < slots.Length; i++)
            {
                if (i % 4 == 0 && i != 0)
                {
                    slots[i] = "\n" + slots[i];
                }
            }

            return string.Join(" ", slots) + "\n" + string.Join(" ", queue);
        }
    }
}