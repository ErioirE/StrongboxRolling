using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.Elements;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Enums;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using static ExileCore.PoEMemory.MemoryObjects.ServerInventory;

namespace StrongboxRolling.Utils
{
    public class SBCraftingManager
    {
        public static Regex Weird = new(@"[^A-Za-z0-9\ ]");
        public string[] prevMods = Array.Empty<string>();
        public StrongboxRolling instance;
        public SBCraftingManager(StrongboxRolling ins)
        {
            instance = ins;
        }
        public IEnumerator CraftBox(LabelOnGround sbLabel)
        {
            if (sbLabel is null)
            {
                yield return instance.wait2ms;
            }
            if (instance.GameController.Player.GetComponent<Actor>().isMoving)
            {
                instance.FullWork = true;
                yield break;
            }
            SBType boxType = StaticHelpers.GetStrongboxType(sbLabel);

            string[] labelsBefore = StaticHelpers.FindAllLabels(sbLabel);
            if (GetWisFromInv().Any() && labelsBefore.Where(x => x.ToLower().Contains("unidentified")).Any())
            {
                prevMods = StaticHelpers.FindAllLabels(sbLabel);
                CraftWithItem(GetWisFromInv().First());
                if (!WaitForChange(labelsBefore))
                {
                    yield return true;
                }
            }
            sbLabel.ItemOnGround.TryGetComponent<ObjectMagicProperties>(out ObjectMagicProperties magicPropsC);
            if (magicPropsC is null)
            {
                yield return true;
            }
            if (CheckMods())
            {
                yield return true;
            }

            if (!instance.Settings.BoxCraftingUseAltsAugs || CheckBoxTypeAlchOverride(boxType))
            {
                ScourAlchStep(magicPropsC, sbLabel);
            }
            else if (instance.Settings.BoxCraftingUseAltsAugs)
            {
                AlterStep(magicPropsC, sbLabel);
            }
        }
        public bool ScourAlchStep(ObjectMagicProperties magicPropsC, LabelOnGround sbLabel)
        {
            try
            {
                if (magicPropsC.Mods.Count() > 0)
                {
                    if (GetScoursFromInv().Any())
                    {
                        CraftWithItem(GetScoursFromInv().First());
                        return true;
                    }
                }
                else if (magicPropsC.Mods.Count == 0 &&
                    CheckBoxTypeEngOverride(StaticHelpers.GetStrongboxType(sbLabel)) &&
                    GetEngFromInv().Any() &&
                    !HasMaxQuality(sbLabel))
                {
                    CraftWithItem(GetEngFromInv().FirstOrDefault());
                }
                else if (GetAlchsFromInv().Any())
                {
                    prevMods = StaticHelpers.FindAllLabels(sbLabel);
                    CraftWithItem(GetAlchsFromInv().First());
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
        public bool HasMaxQuality(LabelOnGround sbLabel)
        {
            string[] labels = StaticHelpers.FindAllLabels(sbLabel);
            foreach (string label in labels)
            {
                if (label.Contains("<augmented>{+20%}"))
                {
                    return true;
                }
            }
            return false;
        }
        public bool AlterStep(ObjectMagicProperties magicPropsC, LabelOnGround sbLabel)
        {
            try
            {
                if (magicPropsC.Mods.Count() > 2 && GetScoursFromInv().Any())
                {
                    CraftWithItem(GetScoursFromInv().First());
                    return true;
                }
                else if (magicPropsC.Mods.Count() is 0 && GetTransmutesFromInv().Any())
                {
                    prevMods = StaticHelpers.FindAllLabels(sbLabel);
                    CraftWithItem(GetTransmutesFromInv().First());
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
                        CraftWithItem(GetAugsFromInv().First());
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
                    CraftWithItem(GetAltsFromInv().First());
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
        public bool CheckBoxTypeAlchOverride(SBType sb)
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
        public bool CheckBoxTypeEngOverride(SBType sb)
        {
            if (sb is SBType.Arcanist && instance.Settings.UseEngForArcanist)
            {
                return true;
            }
            if (sb is SBType.Diviner && instance.Settings.UseEngForDiviner)
            {
                return true;
            }
            if (sb is SBType.Cartographer && instance.Settings.UseEngForCartog)
            {
                return true;
            }
            return false;
        }
        public bool WaitForChange(string[] labelsBefore)
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
        public void CraftWithItem(InventSlotItem e)
        {
            if (instance.FullWork)
            {
                return;
            }

            LabelOnGround toCraft = instance.GetClosestChest();
            string[] labels = StaticHelpers.FindAllLabels(toCraft);
            List<string> toLog = new();

            toLog.Add(@$"{DateTime.Now.ToString("yyyy-mm-dd_T")}");
            toLog.Add(@$"{e.Item.RenderName}");
            toLog.AddRange(labels);
            if (!e.Item.Metadata.ToLower().Contains("ident") && labels.Where(x => x.ToLower().Contains("unidentified")).Any())
            {
                return;
            }
            string allMods = string.Join(" ", toLog);

            File.AppendAllLines(@"./craftingLog.txt", toLog);
            if (CheckMods() && e.Item.Metadata != "Metadata/Items/Currency/CurrencyAddModToMagic")
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
            if (!StaticHelpers.WaitForMouseIcon(MouseActionType.Free, instance.GameController.IngameState.IngameUi.Cursor))
            {
                return;
            }
            Mouse.RightClick(instance.Settings.BoxCraftingMidStepDelay);
            if (!StaticHelpers.WaitForMouseIcon(MouseActionType.UseItem, instance.GameController.IngameState.IngameUi.Cursor))
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
                if (!StaticHelpers.WaitForMouseIcon(MouseActionType.UseItem, instance.GameController.IngameState.IngameUi.Cursor))
                {
                    return;
                }
                Mouse.LeftClick(instance.Settings.BoxCraftingMidStepDelay);
                StaticHelpers.WaitForMouseIcon(MouseActionType.Free, instance.GameController.IngameState.IngameUi.Cursor);
            }
            else
            {

            }

            Task.Delay(instance.Settings.BoxCraftingStepDelay).Wait();
            Task.Delay(100).Wait();

        }
        public bool CheckMods()
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
                        //if (magicPropsC.Mods.Count == 1)
                        //{
                        //    if (GetAugsFromInv().Any())
                        //    {
                        //        CraftWithItem(GetAugsFromInv().First());
                        //    }
                        //}
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
                            CraftWithItem(GetAugsFromInv().First());
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


        public ServerInventory.InventSlotItem[] GetInvWithMD(string metadataToFind)
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
        public InventSlotItem[] GetScoursFromInv()
        {
            return GetInvWithMD(ItemCodes.ScourCode);
        }
        public InventSlotItem[] GetAlchsFromInv()
        {
            return GetInvWithMD(ItemCodes.AlchCode);
        }
        public InventSlotItem[] GetTransmutesFromInv()
        {
            return GetInvWithMD(ItemCodes.TransmuteCode);
        }
        public InventSlotItem[] GetAugsFromInv()
        {
            return GetInvWithMD(ItemCodes.AugCode);
        }
        public InventSlotItem[] GetAltsFromInv()
        {
            return GetInvWithMD(ItemCodes.AltCode);
        }
        public InventSlotItem[] GetWisFromInv()
        {
            return GetInvWithMD(ItemCodes.WisdomCode);
        }
        public InventSlotItem[] GetEngFromInv()
        {
            return GetInvWithMD(ItemCodes.EngineerCode);
        }

    }
}
