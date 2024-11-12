using DiskCardGame;
using UnityEngine;

namespace Inscryption_ai
{
    public static class Actions
    {
        public static void RingBell()
        {
            var bell = GameObject.Find("CombatBell").GetComponent<CombatBell3D>();
            
            if ((bool) typeof(CombatBell3D).GetMethod(nameof(CombatBell3D.PressingAllowed)).Invoke(bell, null))
            {
                typeof(CombatBell3D).GetMethod(nameof(CombatBell3D.OnBellPressed)).Invoke(bell, null);
            }
        }
    }
}