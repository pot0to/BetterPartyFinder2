﻿using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.Gui.PartyFinder.Types;

namespace BetterPartyFinder {
    public class Filter : IDisposable {
        private Plugin Plugin { get; }

        internal Filter(Plugin plugin) {
            this.Plugin = plugin;

            this.Plugin.PartyFinderGui.ReceiveListing += this.ReceiveListing;
        }

        public void Dispose() {
            this.Plugin.PartyFinderGui.ReceiveListing -= this.ReceiveListing;
        }

        private void ReceiveListing(PartyFinderListing listing, PartyFinderListingEventArgs args) {
            args.Visible = args.Visible && this.ListingVisible(listing);
        }

        private bool ListingVisible(PartyFinderListing listing) {
            // get the current preset or mark all pfs as visible
            var selectedId = this.Plugin.Config.SelectedPreset;
            if (selectedId == null || !this.Plugin.Config.Presets.TryGetValue(selectedId.Value, out var filter)) {
                return true;
            }

            // check max item level
            if (!filter.AllowHugeItemLevel && Util.MaxItemLevel > 0 && listing.MinimumItemLevel > Util.MaxItemLevel) {
                return false;
            }

            // filter based on duty whitelist/blacklist
            if (filter.Duties.Count > 0 && listing.DutyType == DutyType.Normal) {
                var inList = filter.Duties.Contains(listing.RawDuty);
                // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
                switch (filter.DutiesMode) {
                    case ListMode.Blacklist when inList:
                    case ListMode.Whitelist when !inList:
                        return false;
                }
            }

            // filter based on item level range
            if (filter.MinItemLevel != null && listing.MinimumItemLevel < filter.MinItemLevel) {
                return false;
            }

            if (filter.MaxItemLevel != null && listing.MinimumItemLevel > filter.MaxItemLevel) {
                return false;
            }

            // filter based on restrictions
            // make sure the listing doesn't contain any of the toggled off search areas
            if (((listing.SearchArea ^ filter.SearchArea) & ~filter.SearchArea) > 0) {
                return false;
            }

            if (!listing[filter.LootRule]) {
                return false;
            }

            if (((listing.DutyFinderSettings ^ filter.DutyFinderSettings) & ~filter.DutyFinderSettings) > 0) {
                return false;
            }

            if (!listing[filter.Conditions]) {
                return false;
            }

            if (!listing[filter.Objectives]) {
                return false;
            }

            // filter based on category (slow)
            if (!filter.Categories.Any(category => category.ListingMatches(this.Plugin.DataManager, listing))) {
                return false;
            }

            // filter based on jobs (slow?)
            if (filter.Jobs.Count > 0 && !listing[SearchAreaFlags.AllianceRaid]) {
                var slots = listing.Slots.ToArray();
                var present = listing.RawJobsPresent.ToArray();

                // create a list of sets containing the slots each job is able to join
                var jobs = new HashSet<int>[filter.Jobs.Count];
                for (var i = 0; i < jobs.Length; i++) {
                    jobs[i] = new HashSet<int>();
                }

                for (var idx = 0; idx < filter.Jobs.Count; idx++) {
                    var wanted = filter.Jobs[idx];

                    for (var i = 0; i < listing.SlotsAvailable; i++) {
                        // if the slot is already full or the job can't fit into it, skip
                        if (present[i] != 0 || !slots[i][wanted]) {
                            continue;
                        }

                        // check for one player per job
                        if (filter.OnePlayerPerJob || listing[SearchAreaFlags.OnePlayerPerJob]) {
                            // make sure at least one job in the wanted set isn't taken
                            foreach (var possibleJob in (JobFlags[]) Enum.GetValues(typeof(JobFlags))) {
                                if (!wanted.HasFlag(possibleJob)) {
                                    continue;
                                }

                                var job = possibleJob.ClassJob(this.Plugin.DataManager);
                                if (present.Contains((byte) job.RowId)) {
                                    continue;
                                }

                                jobs[idx].Add(i);
                                break;
                            }
                        } else {
                            // not one player per job
                            jobs[idx].Add(i);
                        }
                    }

                    // if this job couldn't match any slot, can't join the party
                    if (jobs[idx].Count == 0) {
                        return false;
                    }
                }

                // ensure the number of total slots with possibles joins is at least the number of jobs
                // note that this doesn't make sure it's joinable, see below
                var numSlots = jobs
                    .Aggregate((acc, x) => acc.Union(x).ToHashSet())
                    .Count;

                if (numSlots < jobs.Length) {
                    return false;
                }

                // loop through each unique pair of jobs
                for (var i = 0; i < jobs.Length; i++) {
                    // ReSharper disable once LoopCanBeConvertedToQuery
                    for (var j = 0; j < jobs.Length; j++) {
                        if (i >= j) {
                            continue;
                        }

                        var a = jobs[i];
                        var b = jobs[j];

                        // check if the slots either job can join have overlap
                        var overlap = a.Intersect(b);
                        if (overlap.Count() != 1) {
                            continue;
                        }

                        // if there is overlap, check the difference between the sets
                        // if there is no difference, the party can't be joined
                        // note that if the overlap is more than one slot, we don't need to check
                        var difference = a.Except(b);
                        if (!difference.Any()) {
                            return false;
                        }
                    }
                }
            }

            // filter based on player
            if (filter.Players.Count > 0) {
                if (filter.Players.Any(info => info.Name == listing.Name.TextValue && info.World == listing.HomeWorld.Value.RowId)) {
                    return false;
                }
            }

            return true;
        }
    }
}
