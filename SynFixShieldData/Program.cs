using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;

namespace SynFixShieldData
{
    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            return await SynthesisPipeline.Instance
                .AddPatch<ISkyrimMod, ISkyrimModGetter>(RunPatch)
                .SetTypicalOpen(GameRelease.SkyrimSE, "YourPatcher.esp")
                .Run(args);
        }

        public static void RunPatch(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            var wPNBashShieldLightImpactSet = FormKey.Factory("0183FB:Skyrim.esm"); // WPNBashShieldLightImpactSet [IPDS:000183FB]
            var materialShieldLight = FormKey.Factory("016978:Skyrim.esm"); // MaterialShieldLight [MATT:00016978]
            var wPNBashShieldHeavyImpactSet = FormKey.Factory("0183FE:Skyrim.esm"); // WPNBashShieldHeavyImpactSet [IPDS:000183FE]
            var materialShieldHeavy = FormKey.Factory("016979:Skyrim.esm"); // MaterialShieldHeavy [MATT:00016979]

            int patchedCount = 0;
            foreach (var itemGetter in state.LoadOrder.PriorityOrder.Armor().WinningOverrides())
            {
                if (itemGetter.IsDeleted) continue;
                if (itemGetter.MajorFlags.HasFlag(Armor.MajorFlag.NonPlayable)) continue;
                if (!itemGetter.MajorFlags.HasFlag(Armor.MajorFlag.Shield)) continue;

                if (itemGetter.BodyTemplate == null) continue;
                if (itemGetter.BodyTemplate.Flags.HasFlag(BodyTemplate.Flag.NonPlayable)) continue;
                if (itemGetter.BodyTemplate.ArmorType == ArmorType.Clothing) continue; // maybe try to parse this variant later?

                bool isHeavy = false;
                bool isLight = false;
                bool isNeedToFixBashImpactDataSet = false;
                bool isNeedToFixAlternateBlockMaterial = false;
                if (isHeavy = itemGetter.BodyTemplate.ArmorType == ArmorType.HeavyArmor)
                {
                    isNeedToFixBashImpactDataSet = itemGetter.BashImpactDataSet.IsNull || itemGetter.BashImpactDataSet.FormKey != wPNBashShieldHeavyImpactSet;
                    isNeedToFixAlternateBlockMaterial = itemGetter.AlternateBlockMaterial.IsNull || itemGetter.AlternateBlockMaterial.FormKey != materialShieldHeavy;
                }
                else if (isLight = !isHeavy && itemGetter.BodyTemplate.ArmorType == ArmorType.LightArmor)
                {
                    isNeedToFixBashImpactDataSet = itemGetter.BashImpactDataSet.IsNull || itemGetter.BashImpactDataSet.FormKey != wPNBashShieldLightImpactSet;
                    isNeedToFixAlternateBlockMaterial = itemGetter.AlternateBlockMaterial.IsNull || itemGetter.AlternateBlockMaterial.FormKey != materialShieldLight;
                }

                if (!isNeedToFixBashImpactDataSet && !isNeedToFixAlternateBlockMaterial) continue;

                patchedCount++;

                var itemToPatch = state.PatchMod.Armors.GetOrAddAsOverride(itemGetter);

                if (isNeedToFixBashImpactDataSet) itemToPatch.BashImpactDataSet.FormKey = isHeavy ? wPNBashShieldHeavyImpactSet : isLight ? wPNBashShieldLightImpactSet : itemToPatch.BashImpactDataSet.FormKey;
                if (isNeedToFixAlternateBlockMaterial) itemToPatch.AlternateBlockMaterial.FormKey = isHeavy ? materialShieldHeavy : isLight ? materialShieldLight : itemToPatch.AlternateBlockMaterial.FormKey;
            }

            Console.WriteLine($"Fixed {patchedCount} records");
        }
    }
}
