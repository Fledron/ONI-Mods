using Harmony;
using Klei.AI;
using STRINGS;
using UnityEngine;
using static TestMod.GlobalVars;

namespace TestMod
{
    public class MinionConfigPatches
    {
        
        
        [HarmonyPatch(typeof(MinionConfig))]
        [HarmonyPatch("CreatePrefab")]
        public static class CreatePrefab
        {
            public static void Postfix(MinionConfig __instance)
            {
                string baseModifierName = DUPLICANTS.MODIFIERS.BASEDUPLICANT.NAME;
                ImmuneSystemBaseModifier = new AttributeModifier(ImmuneSystemAmount.deltaAttribute.Id,
                    ImmuneSystemDefaultDelta, baseModifierName,
                    false, false,
                    false);

                Db.Get().traits.Get(MinionConfig.MINION_BASE_TRAIT_ID).Add(ImmuneSystemBaseModifier);
                
                
                string germModifierName = "Germs Present";
                ImmuneSystemGermModifier = new AttributeModifier(ImmuneSystemAmount.deltaAttribute.Id,
                    0f, germModifierName,
                    false, false,
                    false);

                Db.Get().traits.Get(MinionConfig.MINION_BASE_TRAIT_ID).Add(ImmuneSystemGermModifier);
            }
        }

        [HarmonyPatch(typeof(MinionConfig))]
        [HarmonyPatch("OnPrefabInit")]
        [HarmonyPatch(new[] {typeof(GameObject)})]
        public static class InitAmounts
        {
            public static void Postfix(MinionConfig __instance, GameObject go)
            {
                AmountInstance immuneSys = ImmuneSystemAmount.Lookup(go);
                immuneSys.value = immuneSys.GetMax();
            }
        }
    }
}