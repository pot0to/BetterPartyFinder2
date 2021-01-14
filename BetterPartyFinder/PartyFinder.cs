using Dalamud.Data;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BetterPartyFinder {
    public class PartyFinderListing {
        public uint Id { get; }
        public string Name { get; }
        public string Description { get; }
        public Lazy<World> World { get; }
        public Lazy<World> HomeWorld { get; }
        public Lazy<World> CurrentWorld { get; }
        public Category Category { get; }
        public ushort RawDuty { get; }
        public Lazy<ContentFinderCondition> Duty { get; }
        public DutyType DutyType { get; }
        public bool BeginnersWelcome { get; }
        public ushort SecondsRemaining { get; }
        public ushort MinimumItemLevel { get; }
        public IReadOnlyCollection<PartyFinderSlot> Slots => this._slots;

        private readonly byte _objective;
        public ObjectiveFlags Objective => (ObjectiveFlags) this._objective;

        private readonly byte _conditions;
        public ConditionFlags Conditions => (ConditionFlags) this._conditions;

        private readonly byte _dutyFinderSettings;
        public DutyFinderSettingsFlags DutyFinderSettings => (DutyFinderSettingsFlags) this._dutyFinderSettings;

        private readonly byte _lootRules;
        public LootRuleFlags LootRules => (LootRuleFlags) this._lootRules;

        private readonly byte _searchArea;
        public SearchAreaFlags SearchArea => (SearchAreaFlags) this._searchArea;

        private readonly PartyFinderSlot[] _slots;

        private readonly byte[] _jobsPresent;
        internal IReadOnlyCollection<byte> RawJobsPresent => this._jobsPresent;
        internal IReadOnlyCollection<Lazy<ClassJob?>> JobsPresent { get; }

        public bool this[ObjectiveFlags flag] => this._objective == 0 || (this._objective & (uint) flag) > 0;


        public bool this[ConditionFlags flag] => this._conditions == 0 || (this._conditions & (uint) flag) > 0;

        public bool this[DutyFinderSettingsFlags flag] => this._dutyFinderSettings == 0 || (this._dutyFinderSettings & (uint) flag) > 0;

        public bool this[LootRuleFlags flag] => this._lootRules == 0 || (this._lootRules & (uint) flag) > 0;

        public bool this[SearchAreaFlags flag] => this._searchArea == 0 || (this._searchArea & (uint) flag) > 0;

        internal PartyFinderListing(PfListing listing, DataManager dataManager) {
            this.Id = listing.id;
            this.Name = listing.Name();
            this.Description = listing.Description();
            this.World = new Lazy<World>(() => dataManager.GetExcelSheet<World>().GetRow(listing.world));
            this.HomeWorld = new Lazy<World>(() => dataManager.GetExcelSheet<World>().GetRow(listing.homeWorld));
            this.CurrentWorld = new Lazy<World>(() => dataManager.GetExcelSheet<World>().GetRow(listing.currentWorld));
            this.Category = (Category) listing.category;
            this.RawDuty = listing.duty;
            this.Duty = new Lazy<ContentFinderCondition>(() => dataManager.GetExcelSheet<ContentFinderCondition>().GetRow(listing.duty));
            this.DutyType = (DutyType) listing.dutyType;
            this.BeginnersWelcome = listing.beginnersWelcome == 1;
            this.SecondsRemaining = listing.secondsRemaining;
            this.MinimumItemLevel = listing.minimumItemLevel;

            this._objective = listing.objective;
            this._conditions = listing.conditions;
            this._dutyFinderSettings = listing.dutyFinderSettings;
            this._lootRules = listing.lootRules;
            this._searchArea = listing.searchArea;

            this._slots = listing.slots.Select(accepting => new PartyFinderSlot(accepting)).ToArray();
            this._jobsPresent = listing.jobsPresent;
            this.JobsPresent = this._jobsPresent
                .Select(id => new Lazy<ClassJob?>(() => id == 0
                    ? null
                    : dataManager.GetExcelSheet<ClassJob>().GetRow(id)))
                .ToArray();
        }
    }

    public class PartyFinderSlot {
        private readonly uint _accepting;
        private JobFlags[]? _listAccepting;

        public IReadOnlyCollection<JobFlags> Accepting {
            get {
                if (this._listAccepting != null) {
                    return this._listAccepting;
                }

                this._listAccepting = Enum.GetValues(typeof(JobFlags))
                    .Cast<JobFlags>()
                    .Where(flag => this[flag])
                    .ToArray();

                return this._listAccepting;
            }
        }

        public bool this[JobFlags flag] => (this._accepting & (uint) flag) > 0;

        internal PartyFinderSlot(uint accepting) {
            this._accepting = accepting;
        }
    }

    [Flags]
    public enum SearchAreaFlags : uint {
        DataCentre = 1 << 0,
        Private = 1 << 1,
        Unknown2 = 1 << 2, // set for copied factory pf
        World = 1 << 3,
        OnePlayerPerJob = 1 << 5,
    }

    [Flags]
    public enum JobFlags {
        Gladiator = 1 << 1,
        Pugilist = 1 << 2,
        Marauder = 1 << 3,
        Lancer = 1 << 4,
        Archer = 1 << 5,
        Conjurer = 1 << 6,
        Thamaturge = 1 << 7,
        Paladin = 1 << 8,
        Monk = 1 << 9,
        Warrior = 1 << 10,
        Dragoon = 1 << 11,
        Bard = 1 << 12,
        WhiteMage = 1 << 13,
        BlackMage = 1 << 14,
        Arcanist = 1 << 15,
        Summoner = 1 << 16,
        Scholar = 1 << 17,
        Rogue = 1 << 18,
        Ninja = 1 << 19,
        Machinist = 1 << 20,
        DarkKnight = 1 << 21,
        Astrologian = 1 << 22,
        Samurai = 1 << 23,
        RedMage = 1 << 24,
        BlueMage = 1 << 25,
        Gunbreaker = 1 << 26,
        Dancer = 1 << 27,
    }

    [Flags]
    public enum ObjectiveFlags : uint {
        None = 0,
        DutyCompletion = 1,
        Practice = 2,
        Loot = 4,
    }

    [Flags]
    public enum ConditionFlags : uint {
        None = 1,
        DutyComplete = 2,
        DutyIncomplete = 4,
    }

    [Flags]
    public enum DutyFinderSettingsFlags : uint {
        None = 0,
        UndersizedParty = 1 << 0,
        MinimumItemLevel = 1 << 1,
        SilenceEcho = 1 << 2,
    }

    [Flags]
    public enum LootRuleFlags : uint {
        None = 0,
        GreedOnly = 1,
        Lootmaster = 2,
    }

    public enum Category {
        Duty = 0,
        QuestBattles = 1 << 0,
        Fates = 1 << 1,
        TreasureHunt = 1 << 2,
        TheHunt = 1 << 3,
        GatheringForays = 1 << 4,
        DeepDungeons = 1 << 5,
        AdventuringForays = 1 << 6,
    }

    public enum DutyType {
        Other = 0,
        Roulette = 1 << 0,
        Normal = 1 << 1,
    }
}
