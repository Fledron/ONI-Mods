using System;
using System.Reflection;
using Harmony;
using Klei.AI;
using static TestMod.GlobalVars;

namespace TestMod
{
    public class MinionVitalsPanelPatches
    {
        [HarmonyPatch(typeof(MinionVitalsPanel))]
        [HarmonyPatch("Init")]
        //[HarmonyPatch(new Type[] {typeof()})]
        public static class AddImmuneSystemLine
        {
            public static void
                Prefix(MinionVitalsPanel __instance) //, ref Func<Amount, Func<AmountInstance, string>> ___AddAmountLine)
            {
                MethodInfo addAmountLine = AccessTools.Method(__instance.GetType(), "AddAmountLine");
                if (addAmountLine != null)
                {
                    //Debug.Log("Is Generic: " + addAmountLine.IsGenericMethod);
                    //Debug.Log("Is Private: " + addAmountLine.IsPrivate);
                    //Debug.Log("Is Defined: " + addAmountLine.IsDefined(__instance.GetType(),false));

                    addAmountLine.Invoke(__instance, new object[] {ImmuneSystemAmount, null});
                }
                else
                {
                    Debug.Log("addAmountMethod is null");
                }
            }
        }
    }
    [HarmonyPatch(typeof(MinionVitalsPanel))]
    [HarmonyPatch("AddAmountLine")]
    [HarmonyPatch(new[] {typeof(Amount), typeof(Func<AmountInstance, string>)})]
    public static class CheckIfImmuneSystemAmountLineWasAdded
    {
        // ReSharper disable once InconsistentNaming
        public static void Postfix(MinionVitalsPanel __instance, Amount amount,
            Func<AmountInstance, string> tooltip_func = null)
        {
            // ReSharper disable once InvertIf
            if (amount.Id.Equals("ImmuneSystem"))
            {
                Debug.Log("ImmuneSystem was added");
            }
        }
    }
}
