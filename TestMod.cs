using Harmony;
using Klei.AI;
using UnityEngine;
using Amounts = Database.Amounts;
using static TestMod.GlobalVars;

namespace TestMod
{
    //[SerializationConfig(MemberSerialization.OptIn)]
    public class Patches // : ISaveLoadable
    {
        private Patches()
        {
        }

        [HarmonyPatch(typeof(SplashMessageScreen))]
        [HarmonyPatch("OnPrefabInit")]
        public static class OnGameStart
        {
            public static void Postfix()
            {
                Debug.Log("THIS MOD WORKS LMAO!!!");
            }
        }

        [HarmonyPatch(typeof(SimpleInfoScreen))]
        [HarmonyPatch("SetPanels")]
        [HarmonyPatch(new[] {typeof(GameObject)})]
        public static class SelectDupePlease
        {
            public static void Prefix(SimpleInfoScreen __instance, GameObject target)
            {
                var dupe = target.GetComponent<MinionIdentity>();
                // ReSharper disable once InvertIf
                if (dupe)
                {
                    Debug.Log("Dupe selected!");
                    //dupe.SetName("Guess who edited your name");
                    //dupe.
                    //Db.Get().Attributes.GermResistance.Lookup(dupe).Attribute.BaseValue = 100f;
                    //dupe.GetComponent()
                    //((Health) dupe.GetComponent(typeof(Health))).hitPoints = 150;
                    //target.GetAmounts().SetValue("Stress", 5);
                    //if (target.GetAmounts().ModifierList.Find((e) => e.modifier.Id == "ImmuneSystem") == null)
                    if (!HasDupeValue(target, ImmuneSystemAmount))
                    {
                        Debug.Log("Value ImmuneSystem was not present in Duplicant.");
                        target.GetAmounts().ModifierList.Add(new AmountInstance(ImmuneSystemAmount, target));
                        target.GetAmounts().SetValue("ImmuneSystem", 100);
                    }

                    //var value = target.GetAmounts().GetValue("ImmuneSystem");
                    //target.GetAmounts().SetValue("ImmuneSystem", value - 2.5f);
                    //ImmuneSystemAmount.Lookup(target).value -= 2.5f;
                }

                //___vitalsContainer.add LineItemPrefab.AddComponent(typeof(InfoScreenLineItem));
            }
        }

        public static bool HasDupeValue(GameObject dupe, Amount value)
        {
            return dupe.GetComponent<Modifiers>().amounts.Has(value);
        }

        public static bool HasDupeValue(MinionIdentity dupe, Amount value)
        {
            return HasDupeValue(dupe.gameObject, value);
        }


        [HarmonyPatch(typeof(MinionIdentity))]
        [HarmonyPatch("Sim1000ms")]
        [HarmonyPatch(new[] {typeof(float)})]
        public static class UpdateImmuneSystemPatch
        {
            public static void Postfix(MinionIdentity __instance)
            {
                var dupe = __instance;
                if (HasDupeValue(dupe.gameObject, ImmuneSystemAmount))
                {
                    var immuneSystem = ImmuneSystemAmount.Lookup(dupe.gameObject);
                    var max = immuneSystem.GetMax();
                    var value = immuneSystem.value;


                    var amount = ImmuneSystemAmount.Lookup(dupe.gameObject);
                    for (var index = 0; index != amount.deltaAttribute.Modifiers.Count; ++index)
                    {
                        var mod = amount.deltaAttribute.Modifiers[index];
                        if (mod.AttributeId != ImmuneSystemBaseModifier.AttributeId ||
                            mod.Description != ImmuneSystemBaseModifier.Description)
                            continue;
                        if (value >= max)
                        {
                            mod.SetValue(0f);
                        }
                        else
                        {
                            if (mod.Value <= 0f)
                            {
                                mod.SetValue(ImmuneSystemDefaultDelta);
                            }
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(MinionConfig))]
        [HarmonyPatch("AddMinionAmounts")]
        [HarmonyPatch(new[] {typeof(Modifiers)})]
        public static class AddMinionAmounts
        {
            public static void Postfix(MinionConfig __instance, Modifiers modifiers)
            {
                modifiers.initialAmounts.Add("ImmuneSystem");
            }
        }

        [HarmonyPatch(typeof(Amounts))]
        [HarmonyPatch("Load")]
        public static class LoadAmounts
        {
            public static void
                Prefix(Amounts __instance) //, ref Func<Amount, Func<AmountInstance, string>> ___AddAmountLine)
            {
                ImmuneSystemAmount = __instance.CreateAmount("ImmuneSystem", 0f, 100f,
                    false, Units.Flat, 0.04f, true, "STRINGS.DUPLICANTS.STATS", "ui_icon_immunelevel",
                    "attribute_immunelevel");
                ImmuneSystemAmount.Disabled = false;
                ImmuneSystemAmount.showInUI = true;
                ImmuneSystemAmount.description = "This is the strength of your Duplicants immune system.";
                ImmuneSystemAmount.displayer = new AsPercentAmountDisplayer(GameUtil.TimeSlice.PerCycle);
                ImmuneSystemAmount.Name = "Immune System";

                ImmuneSystemAmount.SetDisplayer(new AsPercentAmountDisplayer(GameUtil.TimeSlice.PerCycle));
            }
        }
    }
}