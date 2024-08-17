using ExileCore.Shared.Interfaces;
using ExileCore.Shared.Nodes;
using System;
using System.Windows.Forms;

namespace StrongboxRolling
{
    public class StrongboxRollingSettings : ISettings
    {
        public static readonly string defaultRegex = @"[2-9] addi.{1,20}(cart|ambush|harbin|harvest|divination|horned|essence).*scarab|stream|rare mon|stream";
        public static readonly string defaultSpecialBoxRegex = @"(additional item).*(quantity)|((quantity).*(additional item))|[2-9] addi.{1,20}(cart|ambush|harbin|harvest|divination|horned|essence).*scarab|stream|stream";
        public static readonly string defaultStashCraftRegex = @"";
        public StrongboxRollingSettings()
        {
            Enable = new ToggleNode(false);
            CraftBoxKey = Keys.F1;
            ExtraDelay = new RangeNode<int>(0, 0, 200);

            CancelKey = Keys.Escape;
            BoxCraftingUseAltsAugs = new ToggleNode(true);
            BoxCraftingMidStepDelay = new RangeNode<int>(40, 0, 200);
            BoxCraftingStepDelay = new RangeNode<int>(0, 0, 400);
            ModsRegex = defaultRegex;
            ArcanistRegex = defaultSpecialBoxRegex;
            DivinerRegex = defaultSpecialBoxRegex;
            CartogRegex = defaultSpecialBoxRegex;
            UseAlchScourForArcanist = new ToggleNode(true);
            UseEngForArcanist = new ToggleNode(false);
            UseAlchScourForDiviner = new ToggleNode(true);
            UseEngForDiviner = new ToggleNode(true);
            UseAlchScourForCartog = new ToggleNode(true);
            UseEngForCartog = new ToggleNode(true);
            EnableStashCrafting = new ToggleNode(false);
            StashCraftingStartHotKey = Keys.NumPad9;
            StashCraftingRegex = defaultStashCraftRegex;
        }

        public ToggleNode Enable { get; set; }
        public HotkeyNode CraftBoxKey { get; set; }
        public RangeNode<int> ExtraDelay { get; set; }

        public HotkeyNode CancelKey { get; set; }
        public ToggleNode BoxCraftingUseAltsAugs { get; set; } = new ToggleNode(false);
        public RangeNode<int> BoxCraftingMidStepDelay { get; set; } = new RangeNode<int>(0, 0, 200);
        public RangeNode<int> BoxCraftingStepDelay { get; set; } = new RangeNode<int>(0, 0, 200);
        public String ModsRegex { get; set; }
        public String ArcanistRegex { get; set; }
        public ToggleNode UseAlchScourForArcanist { get; set; }
        public ToggleNode UseEngForArcanist { get; set; }
        public String DivinerRegex { get; set; }
        public ToggleNode UseAlchScourForDiviner { get; set; }
        public ToggleNode UseEngForDiviner { get; set; }
        public String CartogRegex { get; set; }
        public ToggleNode UseAlchScourForCartog { get; set; }
        public ToggleNode UseEngForCartog { get; set; }
        public HotkeyNode LazyLootingPauseKey { get; set; } = new HotkeyNode(Keys.Space);

        public ToggleNode EnableStashCrafting { get; set; } = new ToggleNode(false);
        public HotkeyNode StashCraftingStartHotKey { get; set; } = new HotkeyNode(Keys.Multiply);
        public String StashCraftingRegex { get; set; }

    }
}
