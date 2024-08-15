using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.Elements;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Enums;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using static ExileCore.PoEMemory.MemoryObjects.ServerInventory;

namespace StrongboxRolling.Utils
{
    internal class CraftingManager
    {
        public static Regex Weird = new(@"[^A-Za-z0-9\ ]");
        internal string[] prevMods = Array.Empty<string>();
        internal StrongboxRolling instance;
        internal CraftingManager(StrongboxRolling ins)
        {
            instance = ins;
        }
        internal IEnumerator CraftBox(LabelOnGround sbLabel)
        {
            if (sbLabel is null)
            {
                yield return instance.wait2ms;
            }
            if (instance.GameController.Player.GetComponent<Actor>().isMoving)
            {
                yield return instance.wait2ms;
            }
            SBType boxType = StaticHelpers.GetStrongboxType(sbLabel);

            string[] labelsBefore = StaticHelpers.FindAllLabels(sbLabel);
            sbLabel.ItemOnGround.TryGetComponent<ObjectMagicProperties>(out ObjectMagicProperties magicPropsC);
            if (magicPropsC is null)
            {
                yield return true;
            }
            if (CheckMods())
            {                
                yield return true;
            }
            if (GetWisFromInv().Any() && labelsBefore.Where(x => x.ToLower().Contains("unidentified")).Any())
            {
                prevMods = StaticHelpers.FindAllLabels(sbLabel);
                CraftWithItem(GetWisFromInv().First(), sbLabel);
                if (!WaitForChange(labelsBefore))
                {
                    yield break;
                }
            }
            if (!instance.Settings.BoxCraftingUseAltsAugs || CheckBoxTypeOverride(boxType))
            {
                ScourAlchStep(magicPropsC, sbLabel);
            }
            else if (instance.Settings.BoxCraftingUseAltsAugs)
            {
                AlterStep(magicPropsC,sbLabel);
            }            
        }
        internal bool ScourAlchStep(ObjectMagicProperties magicPropsC, LabelOnGround sbLabel)
        {
            try
            {
                if (magicPropsC.Mods.Count() > 0)
                {
                    if (GetScoursFromInv().Any())
                    {
                        CraftWithItem(GetScoursFromInv().First(), sbLabel);
                        return true;
                    }
                }
                else if (GetAlchsFromInv().Any())
                {
                    prevMods = StaticHelpers.FindAllLabels(sbLabel);
                    CraftWithItem(GetAlchsFromInv().First(), sbLabel);
                    if (!WaitForChange(prevMods))
                    {
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            return false;
        }
        internal bool AlterStep(ObjectMagicProperties magicPropsC, LabelOnGround sbLabel)
        {
            try
            {
                if (magicPropsC.Mods.Count() > 2 && GetScoursFromInv().Any())
                {
                    CraftWithItem(GetScoursFromInv().First(), sbLabel);
                    return true;
                }
                else if (magicPropsC.Mods.Count() is 0 && GetTransmutesFromInv().Any())
                {
                    prevMods = StaticHelpers.FindAllLabels(sbLabel);
                    CraftWithItem(GetTransmutesFromInv().First(), sbLabel);
                    if (!WaitForChange(prevMods))
                    {
                        return false;
                    }
                    return true;
                }
                else if (magicPropsC.Mods.Count() == 1 && instance.Settings.BoxCraftingUseAltsAugs && StaticHelpers.FindAllLabels(sbLabel).Where(x => x.ToLower().Contains("suffix")).Any())
                {
                    if (GetAugsFromInv().Any())
                    {
                        prevMods = StaticHelpers.FindAllLabels(sbLabel);
                        CraftWithItem(GetAugsFromInv().First(), sbLabel);
                        if (!WaitForChange(prevMods))
                        {
                            return false;
                        }
                        return true;
                    }
                }
                else if (GetAltsFromInv().Any())
                {
                    prevMods = StaticHelpers.FindAllLabels(sbLabel);
                    CraftWithItem(GetAltsFromInv().First(), sbLabel);
                    if (!WaitForChange(prevMods))
                    {
                        return false;
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            return false;
        }
        internal bool CheckBoxTypeOverride(SBType sb)
        {
            if (sb is SBType.Arcanist && instance.Settings.UseAlchScourForArcanist)
            {
                return true;
            }
            if (sb is SBType.Diviner && instance.Settings.UseAlchScourForDiviner)
            {
                return true;
            }
            if (sb is SBType.Cartographer && instance.Settings.UseAlchScourForCartog)
            {
                return true;
            }
            return false;
        }
        internal bool WaitForChange(string[] labelsBefore)
        {
            int maxWait = 200;
            int totalWait = 0;
            string[] labels = StaticHelpers.FindAllLabels(instance.GetClosestChest());
            while (!StaticHelpers.LabelsChanged(labelsBefore, labels) && totalWait < maxWait)
            {
                int delay = 10;
                Task.Delay(delay).Wait();
                totalWait += delay;
                labels = StaticHelpers.FindAllLabels(instance.GetClosestChest());
            }
            if (totalWait >= maxWait)
            {
                return false;
            }
            return true;
        }
        internal void CraftWithItem(InventSlotItem e, LabelOnGround toCraft)
        {
            if (instance.FullWork)
            {
                return;
            }
            toCraft = instance.GetClosestChest();
            string[] labels = StaticHelpers.FindAllLabels(toCraft);
            List<string> toLog = new();

            toLog.Add(@$"{DateTime.Now.ToString("yyyy-mm-dd_T")}");
            toLog.Add(@$"{e.Item.RenderName}");
            toLog.AddRange(labels);
            string allMods = string.Join(" ", toLog);
            if (allMods.ToLower().Contains("stream") || allMods.ToLower().Contains("3 rare"))
            {

            }
            File.AppendAllLines(@"./craftingLog.txt", toLog);
            if (CheckMods())
            {               
                return;
            }
            if (!instance.GameController.Window.IsForeground()) return;
            if (!IngameState.pTheGame.IngameState.IngameUi.InventoryPanel.IsVisibleLocal)
            {
                SendKeys.SendWait("i");

                Task.Delay(instance.Settings.BoxCraftingMidStepDelay).Wait();
            }
            Mouse.MoveCursorToPosition(instance.GetPos(e));
            Task.Delay(instance.Settings.BoxCraftingMidStepDelay).Wait();
            if (!WaitForMouseIcon(MouseActionType.Free))
            {
                return;
            }
            Mouse.RightClick(instance.Settings.BoxCraftingMidStepDelay);
            if (!WaitForMouseIcon(MouseActionType.UseItem))
            {
                return;
            }

            Task.Delay(instance.Settings.BoxCraftingMidStepDelay).Wait();
            //Mouse.SetCursorPos(GetPos(toCraft));
            Mouse.LinearSmoothMove(instance.GetPos(toCraft));
            bool? isTargeted = toCraft.ItemOnGround.GetComponent<Targetable>()?.isTargeted;
            int limit = 200;
            int i = 0;
            while (isTargeted is null || !isTargeted.Value)
            {
                Task.Delay(1).Wait();
                if (i >= limit)
                {
                    return;
                }
                i++;
                isTargeted = toCraft.ItemOnGround.GetComponent<Targetable>()?.isTargeted;
            }
            if (isTargeted is not null && isTargeted.Value)
            {
                Task.Delay(instance.Settings.BoxCraftingMidStepDelay).Wait();
                Mouse.LeftClick(instance.Settings.BoxCraftingMidStepDelay);
                WaitForMouseIcon(MouseActionType.Free);
            }
            else
            {

            }

            Task.Delay(instance.Settings.BoxCraftingStepDelay).Wait();
            Task.Delay(100).Wait();

        }
        internal bool CheckMods()
        {
            try
            {
                Regex goodMods;
                LabelOnGround chest = instance.GetClosestChest();
                SBType sbType = StaticHelpers.GetStrongboxType(chest);

                if (sbType is SBType.Diviner)
                {
                    goodMods = new Regex(@$"{instance.Settings.DivinerRegex}", RegexOptions.IgnoreCase | RegexOptions.Compiled);
                }
                else if (sbType is SBType.Arcanist)
                {
                    goodMods = new Regex(@$"{instance.Settings.ArcanistRegex}", RegexOptions.IgnoreCase | RegexOptions.Compiled);
                }
                else if (sbType is SBType.Cartographer)
                {
                    goodMods = new Regex(@$"{instance.Settings.CartogRegex}", RegexOptions.IgnoreCase | RegexOptions.Compiled);
                }
                else
                {
                    goodMods = new Regex(@$"{instance.Settings.ModsRegex}", RegexOptions.IgnoreCase | RegexOptions.Compiled);
                }

                string[] allMods = StaticHelpers.FindAllLabels(chest);

                allMods = allMods.Select(x => Weird.Replace(x, "").ToLower()).ToArray();
                string added = String.Join(" ", allMods);
                chest.ItemOnGround.TryGetComponent<ObjectMagicProperties>(out ObjectMagicProperties magicPropsC);
                
                foreach (string s in allMods)
                {
                    if (goodMods.IsMatch(s))
                    {
                        File.AppendAllText("./LabelLog.txt", s);
                        if (magicPropsC.Mods.Count == 1)
                        {
                            if (GetAugsFromInv().Any())
                            {
                                CraftWithItem(GetAugsFromInv().First(), chest);
                            }
                        }
                        instance.pickItCoroutine.Pause();
                        instance.FullWork = true;

                        return true;
                    }
                }

                if (goodMods.IsMatch(added))
                {
                    File.AppendAllText("./LabelLog.txt", "Warning: Grouped mods matched where separate did not. Investigate?" + added);
                    if (magicPropsC.Mods.Count == 1)
                    {
                        if (GetAugsFromInv().Any())
                        {
                            CraftWithItem(GetAugsFromInv().First(), chest);
                        }
                    }
                    instance.pickItCoroutine.Pause();
                    instance.FullWork = true;
                    return true;
                }
            }
            catch (Exception ex)
            {
                instance.LogError(ex.ToString());
            }
            return false;
        }
        internal bool WaitForMouseIcon(MouseActionType mat)
        {
            ExileCore.PoEMemory.MemoryObjects.Cursor cursor = instance.GameController.IngameState.IngameUi.Cursor;

            bool usingItem = false;
            int maxWait = 200;
            int totalWait = 0;

            while (!usingItem && totalWait < maxWait)
            {
                int delay = 1;

                if (cursor.Action == mat)
                {
                    usingItem = true;
                }

                Task.Delay(delay).Wait();
                totalWait += delay;
            }

            return usingItem;
        }
        internal static readonly string ScourCode = @"Metadata/Items/Currency/CurrencyConvertToNormal";
        internal static readonly string AlchCode = @"Metadata/Items/Currency/CurrencyUpgradeToRare";
        internal static readonly string EngineerCode = @"Metadata/Items/Currency/CurrencyStrongboxQuality";
        internal static readonly string ChaosCode = @"Metadata/Items/Currency/CurrencyRerollRare";
        internal static readonly string AltCode = @"Metadata/Items/Currency/CurrencyRerollMagic";
        internal static readonly string TransmuteCode = @"Metadata/Items/Currency/CurrencyUpgradeToMagic";
        internal static readonly string AugCode = @"Metadata/Items/Currency/CurrencyAddModToMagic";
        internal static readonly string WisdomCode = @"Metadata/Items/Currency/CurrencyIdentification";
        internal ServerInventory.InventSlotItem[] GetInvWithMD(string metadataToFind)
        {
            try
            {
                if (instance.InventoryItems is not null && instance.InventoryItems.InventorySlotItems.Any())
                {
                    return instance.InventoryItems.InventorySlotItems.Where(x => (bool)(x.Item?.Metadata?.Contains(metadataToFind))).OrderBy(x => x.PosX).ThenBy(x => x.PosY).ToArray();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            return Array.Empty<InventSlotItem>();
        }
        internal InventSlotItem[] GetScoursFromInv()
        {
            return GetInvWithMD(ScourCode);
        }
        internal InventSlotItem[] GetAlchsFromInv()
        {
            return GetInvWithMD(AlchCode);
        }
        internal InventSlotItem[] GetTransmutesFromInv()
        {
            return GetInvWithMD(TransmuteCode);
        }
        internal InventSlotItem[] GetAugsFromInv()
        {
            return GetInvWithMD(AugCode);
        }
        internal InventSlotItem[] GetAltsFromInv()
        {
            return GetInvWithMD(AltCode);
        }
        internal InventSlotItem[] GetWisFromInv()
        {
            return GetInvWithMD(WisdomCode);
        }

    }
}
