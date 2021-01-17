using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Configuration;

namespace BetterPartyFinder {
    public class Configuration : IPluginConfiguration {
        private Plugin? Plugin { get; set; }

        public int Version { get; set; } = 1;

        public Dictionary<Guid, ConfigurationFilter> Presets { get; } = new();
        public Guid? SelectedPreset { get; set; }

        public bool ShowWhenPfOpen { get; set; }
        public WindowSide WindowSide { get; set; } = WindowSide.Left;

        internal static Configuration? Load(Plugin plugin) {
            return (Configuration?) plugin.Interface.GetPluginConfig();
        }

        internal void Initialise(Plugin plugin) {
            this.Plugin = plugin;
        }

        internal void Save() {
            this.Plugin?.Interface.SavePluginConfig(this);
        }
    }

    public class ConfigurationFilter {
        public string Name { get; set; } = "<unnamed preset>";

        public ListMode DutiesMode { get; set; } = ListMode.Blacklist;
        public HashSet<uint> Duties { get; set; } = new();

        public HashSet<UiCategory> Categories { get; set; } = new();

        public List<JobFlags> Jobs { get; set; } = new();
        // default to true because that's the PF's default
        // use nosol if trying to avoid spam

        public SearchAreaFlags SearchArea { get; set; } = (SearchAreaFlags) ~(uint) 0;
        public LootRuleFlags LootRule { get; set; } = ~LootRuleFlags.None;
        public DutyFinderSettingsFlags DutyFinderSettings { get; set; } = ~DutyFinderSettingsFlags.None;
        public ConditionFlags Conditions { get; set; } = (ConditionFlags) ~(uint) 0;
        public ObjectiveFlags Objectives { get; set; } = ~ObjectiveFlags.None;

        public bool AllowHugeItemLevel { get; set; } = true;
        public uint? MinItemLevel { get; set; }
        public uint? MaxItemLevel { get; set; }

        public HashSet<PlayerInfo> Players { get; set; } = new();

        internal bool this[SearchAreaFlags flags] {
            get => (this.SearchArea & flags) > 0;
            set {
                if (value) {
                    this.SearchArea |= flags;
                } else {
                    this.SearchArea &= ~flags;
                }
            }
        }

        internal bool this[LootRuleFlags flags] {
            get => (this.LootRule & flags) > 0;
            set {
                if (value) {
                    this.LootRule |= flags;
                } else {
                    this.LootRule &= ~flags;
                }
            }
        }

        internal bool this[DutyFinderSettingsFlags flags] {
            get => (this.DutyFinderSettings & flags) > 0;
            set {
                if (value) {
                    this.DutyFinderSettings |= flags;
                } else {
                    this.DutyFinderSettings &= ~flags;
                }
            }
        }

        internal bool this[ConditionFlags flags] {
            get => (this.Conditions & flags) > 0;
            set {
                if (value) {
                    this.Conditions |= flags;
                } else {
                    this.Conditions &= ~flags;
                }
            }
        }

        internal bool this[ObjectiveFlags flags] {
            get => (this.Objectives & flags) > 0;
            set {
                if (value) {
                    this.Objectives |= flags;
                } else {
                    this.Objectives &= ~flags;
                }
            }
        }

        internal ConfigurationFilter Clone() {
            var categories = this.Categories.ToHashSet();
            var duties = this.Duties.ToHashSet();
            var jobs = this.Jobs.ToList();
            var players = this.Players.Select(info => info.Clone()).ToHashSet();

            return new ConfigurationFilter {
                Categories = categories,
                Conditions = this.Conditions,
                Duties = duties,
                Jobs = jobs,
                Name = string.Copy(this.Name),
                Objectives = this.Objectives,
                DutiesMode = this.DutiesMode,
                LootRule = this.LootRule,
                SearchArea = this.SearchArea,
                DutyFinderSettings = this.DutyFinderSettings,
                MaxItemLevel = this.MaxItemLevel,
                MinItemLevel = this.MinItemLevel,
                AllowHugeItemLevel = this.AllowHugeItemLevel,
                Players = players,
            };
        }

        internal static ConfigurationFilter Create() {
            return new() {
                Categories = Enum.GetValues(typeof(UiCategory))
                    .Cast<UiCategory>()
                    .ToHashSet(),
            };
        }
    }

    public class PlayerInfo {
        public string Name { get; }
        public uint World { get; }

        public PlayerInfo(string name, uint world) {
            this.Name = name;
            this.World = world;
        }

        internal PlayerInfo Clone() {
            return new(this.Name, this.World);
        }

        private bool Equals(PlayerInfo other) {
            return this.Name == other.Name && this.World == other.World;
        }

        public override bool Equals(object? obj) {
            if (ReferenceEquals(null, obj)) {
                return false;
            }

            if (ReferenceEquals(this, obj)) {
                return true;
            }

            return obj.GetType() == this.GetType() && this.Equals((PlayerInfo) obj);
        }

        public override int GetHashCode() {
            unchecked {
                return (this.Name.GetHashCode() * 397) ^ (int) this.World;
            }
        }
    }

    public enum ListMode {
        Whitelist,
        Blacklist,
    }

    public enum WindowSide {
        Left,
        Right,
    }
}
