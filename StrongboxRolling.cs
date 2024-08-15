using ExileCore;
using ExileCore.PoEMemory;
using ExileCore.PoEMemory.Elements;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared;
using ExileCore.Shared.Enums;
using ImGuiNET;
using Newtonsoft.Json;
using Random_Features.Libs;
using StrongboxRolling.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

using static ExileCore.PoEMemory.MemoryObjects.ServerInventory;
using Color = SharpDX.Color;
using Input = ExileCore.Input;
using RectangleF = SharpDX.RectangleF;
using Vector2 = SharpDX.Vector2;

namespace StrongboxRolling
{
    public class StrongboxRolling : BaseSettingsPlugin<StrongboxRollingSettings>
    {
        internal const string StrongboxRollingRuleDirectory = "StrongboxRolling Rules";
        internal readonly List<Entity> _entities = new List<Entity>();
        internal readonly Stopwatch _pickUpTimer = Stopwatch.StartNew();
        internal readonly Stopwatch DebugTimer = Stopwatch.StartNew();
        internal readonly WaitTime toPick = new WaitTime(1);
        internal readonly WaitTime wait1ms = new WaitTime(1);
        internal readonly WaitTime wait2ms = new WaitTime(2);
        internal readonly WaitTime wait3ms = new WaitTime(3);
        internal readonly WaitTime wait100ms = new WaitTime(100);
        internal readonly WaitTime waitForNextTry = new WaitTime(1);
        internal Vector2 _clickWindowOffset;
        internal HashSet<string> _magicRules;
        internal HashSet<string> _normalRules;
        internal HashSet<string> _rareRules;
        internal HashSet<string> _uniqueRules;
        internal HashSet<string> _ignoreRules;
        internal Dictionary<string, int> _weightsRules = new Dictionary<string, int>();
        internal WaitTime _workCoroutine;
        public DateTime buildDate;
        internal uint coroutineCounter;
        internal Vector2 cursorBeforePickIt;
        internal bool FullWork = true;
        internal Element LastLabelClick;
        public string MagicRuleFile;
        internal WaitTime mainWorkCoroutine = new WaitTime(5);
        public string NormalRuleFile;
        internal Coroutine pickItCoroutine;
        public string RareRuleFile;
        internal WaitTime tryToPick = new WaitTime(7);
        public string UniqueRuleFile;
        internal WaitTime waitPlayerMove = new WaitTime(10);
        internal List<string> _customItems = new List<string>();
        internal CraftingManager CraftingManager;
        public int[,] inventorySlots { get; set; } = new int[0, 0];
        public ServerInventory InventoryItems { get; set; }
        public static StrongboxRolling Controller { get; set; }


        public FRSetManagerPublishInformation FullRareSetManagerData = new FRSetManagerPublishInformation();

        public StrongboxRolling()
        {
            Name = "StrongboxRolling";
        }

        public string PluginVersion { get; set; }
        internal List<string> PickitFiles { get; set; }

        public override bool Initialise()
        {
            Controller = this;
            CraftingManager = new(this);
            pickItCoroutine = new Coroutine(MainWorkCoroutine(), this, "StrongboxRolling");
            Core.ParallelRunner.Run(pickItCoroutine);
            pickItCoroutine.Pause();
            DebugTimer.Reset();

            _workCoroutine = new WaitTime(Settings.ExtraDelay);

            //LoadCustomItems();
            return true;
        }



        internal IEnumerator MainWorkCoroutine()
        {
            while (true)
            {
                yield return FindSBToFix();

                coroutineCounter++;
                pickItCoroutine.UpdateTicks(coroutineCounter);
                yield return _workCoroutine;
            }
        }

        public override void DrawSettings()
        {
            Settings.CraftBoxKey = ImGuiExtension.HotkeySelector("Craft box Key: " + Settings.CraftBoxKey.Value.ToString(), Settings.CraftBoxKey);
            Settings.CancelKey = ImGuiExtension.HotkeySelector("Key to cancel rolling: " + Settings.CancelKey.Value.ToString(), Settings.CancelKey);
            Settings.BoxCraftingUseAltsAugs.Value = ImGuiExtension.Checkbox("Roll strongboxes using Transmuts, Alts & Augs instead of Scour/Alch", Settings.BoxCraftingUseAltsAugs);
            Settings.BoxCraftingMidStepDelay.Value = ImGuiExtension.IntSlider("Box Crafting Mid-Step delay (wait for hover registration)", Settings.BoxCraftingMidStepDelay);
            Settings.BoxCraftingStepDelay.Value = ImGuiExtension.IntSlider("Box crafting step delay (between crafts)", Settings.BoxCraftingStepDelay);
            Settings.ModsRegex = ImGuiExtension.InputText("RegEx for mod text, I.E. 'Guarded by \\d rare monsters'. Not case sensitive", Settings.ModsRegex, 1024, ImGuiInputTextFlags.None);
            Settings.ArcanistRegex = ImGuiExtension.InputText("RegEx for Arcanist boxes (currency)", Settings.ArcanistRegex, 1024, ImGuiInputTextFlags.None);
            Settings.UseAlchScourForArcanist.Value = ImGuiExtension.Checkbox("Use Alch/Scour for Arcanist boxes", Settings.UseAlchScourForArcanist);
            Settings.DivinerRegex = ImGuiExtension.InputText("RegEx for Diviner boxes", Settings.DivinerRegex, 1024, ImGuiInputTextFlags.None);
            Settings.UseAlchScourForDiviner.Value = ImGuiExtension.Checkbox("Use Alch/Scour for Arcanist boxes", Settings.UseAlchScourForDiviner);
            Settings.CartogRegex = ImGuiExtension.InputText("RegEx for Cartographer boxes", Settings.CartogRegex, 1024, ImGuiInputTextFlags.None);
            Settings.UseAlchScourForCartog.Value = ImGuiExtension.Checkbox("Use Alch/Scour for Arcanist boxes", Settings.UseAlchScourForCartog);
            //Settings.OverrideItemPickup.Value = ImGuiExtension.Checkbox("Item Pickup Override", Settings.OverrideItemPickup); ImGui.SameLine();
            //ImGuiExtension.ToolTip("Override item.CanPickup\n\rDO NOT enable this unless you know what you're doing!");

        }


        internal DateTime DisableLazyLootingTill { get; set; }

        public override Job Tick()
        {
            List<string> toLog = new();
            if (Settings.BoxCraftingUseAltsAugs && !CraftingManager.GetTransmutesFromInv().Any())
            {
                toLog.Add("Trying to craft but no Orbs of Transmutation found in inventory.");

            }
            if (Settings.BoxCraftingUseAltsAugs && !CraftingManager.GetAltsFromInv().Any())
            {
                toLog.Add("Trying to craft but no Orbs of Alteration found in inventory.");
            }
            if (Settings.BoxCraftingUseAltsAugs && !CraftingManager.GetAugsFromInv().Any())
            {
                toLog.Add("Trying to craft but no Orbs of Augmentation found in inventory.");
            }
            if (!CraftingManager.GetScoursFromInv().Any())
            {
                toLog.Add("Trying to craft but no Orbs of Scouring found in inventory.");
            }
            if (
                (
                    Settings.UseAlchScourForArcanist||
                    Settings.UseAlchScourForDiviner||
                    Settings.UseAlchScourForCartog ||
                    !Settings.BoxCraftingUseAltsAugs)
                && !CraftingManager.GetAlchsFromInv().Any())
            {
                toLog.Add("Trying to craft but no Orbs of Alchemy found in inventory.");
            }
            for (int i = 0; i < toLog.Count; i++)
            {
                DrawText(toLog[i], i * 20);
            }
            InventoryItems = GameController.Game.IngameState.ServerData.PlayerInventories[0].Inventory;
            inventorySlots = Misc.GetContainer2DArray(InventoryItems);

            if (Input.GetKeyState(Settings.LazyLootingPauseKey)) DisableLazyLootingTill = DateTime.Now.AddSeconds(2);
            if (Input.GetKeyState(Keys.Escape))
            {
                FullWork = true;
                pickItCoroutine.Pause();
            }

            if (Input.GetKeyState(Settings.CraftBoxKey.Value))
            {
                DebugTimer.Restart();

                if (pickItCoroutine.IsDone)
                {
                    var firstOrDefault = Core.ParallelRunner.Coroutines.FirstOrDefault(x => x.OwnerName == nameof(StrongboxRolling));

                    if (firstOrDefault != null)
                        pickItCoroutine = firstOrDefault;
                }

                pickItCoroutine.Resume();
                FullWork = false;
            }
            else
            {
                if (FullWork)
                {
                    pickItCoroutine.Pause();
                    DebugTimer.Reset();
                }
            }

            if (DebugTimer.ElapsedMilliseconds > 300)
            {
                //FullWork = true;
                //LogMessage("Error pick it stop after time limit 300 ms", 1);
                DebugTimer.Reset();
            }
            //Graphics.DrawText($@"PICKIT :: Debug Tick Timer ({DebugTimer.ElapsedMilliseconds}ms)", new Vector2(100, 100), FontAlign.Left);
            //DebugTimer.Reset();

            return null;
        }


        public void DrawText(string text, int offset)
        {
            Graphics.DrawTextWithBackground(text, new(50, 100 + offset), Color.Crimson, Color.Black);
        }
        //TODO: Make function pretty





        public override void ReceiveEvent(string eventId, object args)
        {
            if (!Settings.Enable.Value) return;

            if (eventId == "frsm_display_data")
            {

                var argSerialised = JsonConvert.SerializeObject(args);
                FullRareSetManagerData = JsonConvert.DeserializeObject<FRSetManagerPublishInformation>(argSerialised);
            }
        }
        internal IEnumerator FindSBToFix()
        {
            if (!GameController.Window.IsForeground()) yield break;
            var window = GameController.Window.GetWindowRectangleTimeCache;
            var rect = new RectangleF(window.X, window.X, window.X + window.Width, window.Y + window.Height);
            var playerPos = GameController.Player.GridPos;
            var items = InventoryItems;

            List<CustomItem> currentLabels;
            var morphPath = "Metadata/MiscellaneousObjects/Metamorphosis/MetamorphosisMonsterMarker";
            List<string> labelsToLog = new();







            if (!FullWork)
            {

                yield return TryToCraft(GetClosestChest());
                //FullWork = true;
            }
        }
        public LabelOnGround GetClosestChest()
        {
            IList<ExileCore.PoEMemory.Elements.LabelOnGround> otherLabels = GameController.Game.IngameState.IngameUi.ItemsOnGroundLabelsVisible;
            return otherLabels.Where(x => ((bool)x.ItemOnGround?.Metadata?.Contains("Metadata/Chests/StrongBoxes") && !x.ItemOnGround.IsOpened && x.ItemOnGround.DistancePlayer < 70)).OrderBy(x => x.ItemOnGround.DistancePlayer).MinBy(x => x.ItemOnGround.DistancePlayer);
        }




        /// <summary>
        /// LazyLoot item independent checks
        /// </summary>
        /// <returns></returns>


        /// <summary>
        /// LazyLoot item dependent checks
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        internal bool ShouldLazyLoot(CustomItem item)
        {
            var itemPos = item.LabelOnGround.ItemOnGround.Pos;
            var playerPos = GameController.Player.Pos;
            if (Math.Abs(itemPos.Z - playerPos.Z) > 50) return false;
            var dx = itemPos.X - playerPos.X;
            var dy = itemPos.Y - playerPos.Y;
            if (dx * dx + dy * dy > 275 * 275) return false;

            if (item.IsElder || item.IsFractured || item.IsShaper ||
                item.IsHunter || item.IsCrusader || item.IsRedeemer || item.IsWarlord || item.IsHeist)
                return true;

            if (item.Rarity == ItemRarity.Rare && item.Width * item.Height > 1) return false;

            return true;
        }




        internal IEnumerator TryToCraft(LabelOnGround sbLabel)
        {





            if (sbLabel is null || sbLabel.Label is null)
            {
                FullWork = true;
                yield break;
            }


            var centerOfItemLabel = sbLabel.Label.GetClientRectCache.Center;
            var rectangleOfGameWindow = GameController.Window.GetWindowRectangleTimeCache;

            var oldMousePosition = Mouse.GetCursorPositionVector();
            _clickWindowOffset = rectangleOfGameWindow.TopLeft;
            rectangleOfGameWindow.Inflate(-36, -36);
            centerOfItemLabel.X += rectangleOfGameWindow.Left;
            centerOfItemLabel.Y += rectangleOfGameWindow.Top;
            if (!rectangleOfGameWindow.Intersects(new RectangleF(centerOfItemLabel.X, centerOfItemLabel.Y, 3, 3)))
            {
                //FullWork = true;
                //LogMessage($"Label outside game window. Label: {centerOfItemLabel} Window: {rectangleOfGameWindow}", 5, Color.Red);
                yield break;
            }

            var tryCount = 0;

            var completeItemLabel = sbLabel?.Label;

            if (completeItemLabel == null)
            {
                if (tryCount > 0)
                {
                    //LogMessage("Probably item already picked.", 3);
                    yield break;
                }

                //LogError("Label for item not found.", 5);
                yield break;
            }

            //while (GameController.Player.GetComponent<Actor>().isMoving)
            //{
            //    yield return waitPlayerMove;
            //}
            var clientRect = completeItemLabel.GetClientRect();

            var clientRectCenter = clientRect.Center;

            var vector2 = clientRectCenter + _clickWindowOffset;

            if (!rectangleOfGameWindow.Intersects(new RectangleF(vector2.X, vector2.Y, 3, 3)))
            {
                FullWork = true;
                //LogMessage($"x,y outside game window. Label: {centerOfItemLabel} Window: {rectangleOfGameWindow}", 5, Color.Red);
                yield break;
            }



            yield return CraftingManager.CraftBox(sbLabel);


            //yield return waitForNextTry;

            //   Mouse.MoveCursorToPosition(oldMousePosition);
        }

        internal Vector2 GetPos(InventSlotItem l)
        {
            Vector2 centerOfItemLabel = l.GetClientRect().TopLeft;
            RectangleF rectangleOfGameWindow = GameController.Window.GetWindowRectangleTimeCache;

            var oldMousePosition = Mouse.GetCursorPositionVector();
            _clickWindowOffset = rectangleOfGameWindow.TopLeft;
            rectangleOfGameWindow.Inflate(-36, -36);
            centerOfItemLabel.X += rectangleOfGameWindow.Left;
            centerOfItemLabel.Y += rectangleOfGameWindow.Top;
            return centerOfItemLabel;
        }
        internal Vector2 GetPos(LabelOnGround l)
        {
            Vector2 botLeftOfLabel = l.Label.GetClientRect().BottomLeft;
            Vector2 centerOfLabel = l.Label.GetClientRect().Center;
            RectangleF rectangleOfGameWindow = GameController.Window.GetWindowRectangleTimeCache;

            var oldMousePosition = Mouse.GetCursorPositionVector();
            _clickWindowOffset = rectangleOfGameWindow.TopLeft;
            rectangleOfGameWindow.Inflate(-36, -36);
            botLeftOfLabel.X += rectangleOfGameWindow.Left;
            botLeftOfLabel.Y += rectangleOfGameWindow.Top;
            float prevX = botLeftOfLabel.X;
            float prevY = botLeftOfLabel.Y;

            return botLeftOfLabel with { X = centerOfLabel.X + 10, Y = prevY - 60 };
        }





        public HashSet<string> LoadPickit(string fileName)
        {
            var hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (fileName == string.Empty)
            {
                return hashSet;
            }

            var pickitFile = $@"{DirectoryFullName}\{StrongboxRollingRuleDirectory}\{fileName}.txt";

            if (!File.Exists(pickitFile))
            {
                return hashSet;
            }

            var lines = File.ReadAllLines(pickitFile);

            foreach (var x in lines.Where(x => !string.IsNullOrWhiteSpace(x) && !x.StartsWith("#")))
            {
                hashSet.Add(x.Trim());
            }

            LogMessage($"PICKIT :: (Re)Loaded {fileName}", 5, Color.GreenYellow);
            return hashSet;
        }



        public override void OnPluginDestroyForHotReload()
        {
            pickItCoroutine.Done(true);
        }



        #region Adding / Removing Entities

        public override void EntityAdded(Entity Entity)
        {
        }

        public override void EntityRemoved(Entity Entity)
        {
        }

        #endregion
    }
}
