using System;
using System.Linq;

namespace BetterPartyFinder {
    public class Filter : IDisposable {
        private Plugin Plugin { get; }

        internal Filter(Plugin plugin) {
            this.Plugin = plugin;

            this.Plugin.Functions.ReceivePartyFinderListing += this.ReceiveListing;
        }

        private void ReceiveListing(PartyFinderListing listing, PartyFinderListingEventArgs args) {
            args.Visible = this.ListingVisible(listing);
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
            if (!filter.Categories.Any(category => category.ListingMatches(this.Plugin.Interface.Data, listing))) {
                return false;
            }

            return true;
        }

        public void Dispose() {
            this.Plugin.Functions.ReceivePartyFinderListing -= this.ReceiveListing;
        }
    }
}
