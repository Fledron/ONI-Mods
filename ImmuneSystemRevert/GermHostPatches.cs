using System;
using System.Collections.Generic;
using Harmony;
using Klei.AI;
using KSerialization;
using UnityEngine;
using static TestMod.GlobalVars;
using static GermExposureMonitor.Instance;

namespace TestMod
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class GermHostPatches
    {
        [SerializationConfig(MemberSerialization.OptIn)]
        public class GermHost : KMonoBehaviour
        {
            //[Serialize] public MinionIdentity Dupe;
            [Serialize] public List<Germs> germList = new List<Germs>();

            public void AddGerms(Disease type, float amount)
            {
                if (type?.Id == null || amount <= 0f)
                    return;
                if (germList.Find((e) => type.Id == e.Id) == null)
                {
                    germList.Add(new Germs(type.Id, amount));
                }
                else
                {
                    germList.Find((e) => type.Id == e.Id).Amount += amount;
                }
            }

            public float GetGerms(Disease type)
            {
                if (type?.Id == null)
                    return -1f;
                return GetGerms(type.Id);
            }

            public float GetGerms(string id)
            {
                if (id.IsNullOrWhiteSpace())
                    return -1f;
                if (germList.Find((e) => id == e.Id) == null)
                {
                    return -1f;
                }

                return germList.Find((e) => id == e.Id).Amount;
            }

            public void CalculateImmuneSystem(MinionIdentity dupe)
            {
                var totalGerms = 0;
                foreach (var germ in germList)
                {
                    if (germ.Id == Db.Get().Diseases.SlimeGerms.Id)
                    {
                        totalGerms += (int) (germ.Amount * 1.0f);
                    }
                    else if (germ.Id == Db.Get().Diseases.PollenGerms.Id)
                    {
                        totalGerms += (int) (germ.Amount * 0.5f);
                    }
                    else
                    {
                        totalGerms += (int) germ.Amount;
                    }
                }

                var amount = ImmuneSystemAmount.Lookup(dupe);
                for (int index = 0; index != amount.deltaAttribute.Modifiers.Count; ++index)
                {
                    AttributeModifier mod = amount.deltaAttribute.Modifiers[index];
                    if (mod.AttributeId == ImmuneSystemGermModifier.AttributeId &&
                        mod.Description == ImmuneSystemGermModifier.Description)
                    {
                        mod.SetValue((float) -(totalGerms / 900D / 500D));
                    }
                }
            }


            public void RemoveGermsOnTick(float percentage, int removeAllAt)
            {
                foreach (var germ in germList)
                {
                    germ.Amount -= (germ.Amount * percentage);
                    if (germ.Amount <= removeAllAt)
                    {
                        germ.Amount = 0;
                    }
                }
            }
        }

        public class Germs
        {
            [Serialize] public string Id;
            [Serialize] public float Amount;

            public Germs(Disease type, float amount)
            {
                Init(type.Id, amount);
            }

            public Germs(string id, float amount)
            {
                Init(id, amount);
            }

            private void Init(string id, float amount)
            {
                Id = id;
                Amount = amount;
            }
        }


        [HarmonyPatch(typeof(DiseaseInfoScreen))]
        [HarmonyPatch("Refresh")]
        //[HarmonyPatch(new[] {typeof()})]
        public static class RefreshDiseaseInfoScreen
        {
            public static void Prefix(CollapsibleDetailContentPanel ___immuneSystemPanel,
                GameObject ___selectedTarget)
            {
                if (___selectedTarget == null || ___immuneSystemPanel == null)
                    return;

                var host = ___selectedTarget.GetComponent<GermHost>();

                if (host == null)
                    return;

                host.germList.RemoveAll((e) => e == null);
                host.germList.RemoveAll((e) => e.Id == null);

                foreach (var germs in host.germList)
                {
                    if (germs == null)
                    {
                        Debug.Log("germs = null");
                        continue;
                    }

                    if (germs.Amount < 0.5)
                    {
                        continue;
                    }

                    ___immuneSystemPanel.SetTitle("GERM INFO");
                    if (Db.Get().Diseases.GetIndex((HashedString) germs.Id) == byte.MaxValue)
                    {
                        Debug.Log("germs.Id = byte.MaxValue");
                        host.germList.Remove(germs);
                        return;
                    }

                    var germName = Db.Get().Diseases[Db.Get().Diseases.GetIndex((HashedString) germs.Id)].Name;
                    ___immuneSystemPanel.SetLabel("germs_" + germName,
                        "" + (int) germs.Amount + " " + germName + " germs ",
                        "So many germs!");
                }
            }
        }

        [HarmonyPatch(typeof(MinionConfig))]
        [HarmonyPatch("CreatePrefab")]
        //[HarmonyPatch(new[] {typeof()})]
        public static class SelectDupePlease
        {
            public static void Postfix(MinionConfig __instance, GameObject __result)
            {
                __result.AddOrGet<GermHost>();
            }
        }


        [HarmonyPatch(typeof(GermExposureMonitor.Instance))]
        [HarmonyPatch("InjectDisease")]
        [HarmonyPatch(new[] {typeof(Disease), typeof(int), typeof(Tag), typeof(Sickness.InfectionVector)})]
        public static class GermExposureMonitorOverhaul
        {
            public static bool Prefix(GermExposureMonitor.Instance __instance, Disease disease, int count,
                Tag source,
                Sickness.InfectionVector vector)
            {
                var isExposureValidForTraits = AccessTools.Method(__instance.GetType(), "IsExposureValidForTraits");
                if (isExposureValidForTraits == null)
                    return false;

                foreach (var exposureType in GermExposureMonitor.exposureTypes)
                {
                    if (disease.id == (HashedString) exposureType.germ_id &&
                        count * 1.5 > exposureType.exposure_threshold &&
                        (bool) isExposureValidForTraits.Invoke(__instance, new object[] {exposureType}))
                    {
                        var totalValue = Db.Get().Attributes.GermResistance.Lookup(__instance.gameObject)
                            .GetTotalValue();
                        var contractionChance =
                            (float) (0.5 - 0.5 * Math.Tanh(0.5 * (exposureType.base_resistance + totalValue)));

                        __instance.lastDiseaseSources[disease.id] =
                            new DiseaseSourceInfo(source, vector, contractionChance);

                        __instance.SetExposureState(exposureType.germ_id,
                            GermExposureMonitor.ExposureState.Exposed);
                        var amount = Mathf.Clamp01(contractionChance);
                        GermExposureTracker.Instance.AddExposure(exposureType, amount);
                        float addCount = count;
                        if (vector == Sickness.InfectionVector.Inhalation)
                        {
                            addCount *= 1.0f;
                        }

                        var hostedGerms = __instance.gameObject.GetComponent<GermHost>().GetGerms(disease);
                        if (hostedGerms > 0 && addCount * 11 < hostedGerms)
                        {
                            addCount = addCount * (addCount * 11 / hostedGerms);
                        }

                        __instance.gameObject.GetComponent<GermHost>().AddGerms(disease, addCount);
                        //Debug.Log("added " + count + " germs to a dupe. ");
                    }
                }

                var refreshStatusItems = AccessTools.Method(__instance.GetType(), "RefreshStatusItems");
                refreshStatusItems?.Invoke(__instance, new object[] { });

                return false;
            }
        }

        [HarmonyPatch(typeof(GermExposureMonitor.Instance))]
        [HarmonyPatch("OnSleepFinished")]
        //[HarmonyPatch(new[] {typeof(Disease), typeof(int), typeof(Tag), typeof(Sickness.InfectionVector)})]
        public static class OnSleepFinishedPatch
        {
            public static bool Prefix(GermExposureMonitor.Instance __instance)
            {
                return false;
            }
        }


        [HarmonyPatch(typeof(MinionIdentity))]
        [HarmonyPatch("Sim1000ms")]
        [HarmonyPatch(new[] {typeof(float)})]
        public static class GermOnTickPatch
        {
            public static void Postfix(MinionIdentity __instance)
            {
                var dupe = __instance;
                // ReSharper disable once InvertIf
                if (!dupe)
                    return;
                //check where Mr. Dupe is standing
                //var dupeX = dupe.PosMax().x;
                //var dupeY = dupe.PosMax().y;
                var dupeCell = Grid.PosToCell(dupe.gameObject);
                //var diseaseCount = Grid.DiseaseCount[dupeCell];
                var diseaseId = Grid.DiseaseIdx[dupeCell];
                //Db.Get().Diseases.TryGet(diseaseId.ToString());
                var diseaseType = diseaseId == byte.MaxValue ? null : Db.Get().Diseases[diseaseId];
                //var gasAmount = Grid.Mass[dupeCell];
                //var breathGerms = diseaseCount / (gasAmount * 1000) * 20f;
                //Debug.Log(dupe.name+" has "+diseaseCount+" germs around them");
                //dupe.FindOrAdd<>()
                //breathGerms -= breathGerms * GermsDeadPerTickPercent;
                if (dupe.GetComponent<GermHost>() != null)
                {
                    dupe.GetComponent<GermHost>().RemoveGermsOnTick(GermOnTickRemoval, 5);
                    dupe.GetComponent<GermHost>().CalculateImmuneSystem(dupe);
                    if (diseaseType == null)
                    {
                        //Debug.Log("diseaseType is null");
                        //return;
                    }

                    //dupe.GetComponent<GermHost>().AddGerms(diseaseType, (int) breathGerms);
                    //Debug.Log((int) breathGerms + " " + diseaseType.Name + " germs added");
                }
                else
                {
                    Debug.Log("Added GermHost after init");
                    dupe.FindOrAdd<GermHost>();
                }
            }
        }
    }
}
