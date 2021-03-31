using System;
using System.Runtime.InteropServices;
using Dalamud.Hooking;

namespace BetterPartyFinder {
    public class GameFunctions : IDisposable {
        #region Request PF Listings

        private delegate byte RequestPartyFinderListingsDelegate(IntPtr agent, byte categoryIdx);

        private readonly RequestPartyFinderListingsDelegate _requestPartyFinderListings;
        private readonly Hook<RequestPartyFinderListingsDelegate> _requestPfListingsHook;

        #endregion

        private Plugin Plugin { get; }
        private IntPtr PartyFinderAgent { get; set; } = IntPtr.Zero;

        public GameFunctions(Plugin plugin) {
            this.Plugin = plugin;

            var requestPfPtr = this.Plugin.Interface.TargetModuleScanner.ScanText("48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC 40 0F 10 81 ?? ?? ?? ??");

            this._requestPartyFinderListings = Marshal.GetDelegateForFunctionPointer<RequestPartyFinderListingsDelegate>(requestPfPtr);
            this._requestPfListingsHook = new Hook<RequestPartyFinderListingsDelegate>(requestPfPtr, new RequestPartyFinderListingsDelegate(this.OnRequestPartyFinderListings));
            this._requestPfListingsHook.Enable();
        }

        public void Dispose() {
            this._requestPfListingsHook.Dispose();
        }

        private byte OnRequestPartyFinderListings(IntPtr agent, byte categoryIdx) {
            this.PartyFinderAgent = agent;
            return this._requestPfListingsHook.Original(agent, categoryIdx);
        }

        public void RequestPartyFinderListings() {
            // Updated 5.41
            const int categoryOffset = 10_655;

            if (this.PartyFinderAgent == IntPtr.Zero) {
                return;
            }

            var addon = this.Plugin.Interface.Framework.Gui.GetAddonByName("LookingForGroup", 1);
            if (addon == null) {
                return;
            }

            var categoryIdx = Marshal.ReadByte(this.PartyFinderAgent + categoryOffset);
            this._requestPartyFinderListings(this.PartyFinderAgent, categoryIdx);
        }
    }
}
