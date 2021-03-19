using System;
using System.Runtime.InteropServices;
using Dalamud.Hooking;
using Dalamud.Plugin;

namespace BetterPartyFinder {
    public class GameFunctions : IDisposable {
        #region Request PF Listings

        private delegate byte RequestPartyFinderListingsDelegate(IntPtr agent, byte categoryIdx);

        private readonly RequestPartyFinderListingsDelegate _requestPartyFinderListings;
        private readonly Hook<RequestPartyFinderListingsDelegate> _requestPfListingsHook;

        #endregion

        #region PF Listings events

        internal delegate void PartyFinderListingEventDelegate(PartyFinderListing listing, PartyFinderListingEventArgs args);

        internal event PartyFinderListingEventDelegate? ReceivePartyFinderListing;

        private delegate void HandlePfPacketDelegate(IntPtr param1, IntPtr data);

        private readonly Hook<HandlePfPacketDelegate> _handlePacketHook;

        #endregion

        private Plugin Plugin { get; }
        private IntPtr PartyFinderAgent { get; set; } = IntPtr.Zero;

        public GameFunctions(Plugin plugin) {
            this.Plugin = plugin;

            var requestPfPtr = this.Plugin.Interface.TargetModuleScanner.ScanText("48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC 40 0F 10 81 ?? ?? ?? ??");
            var listingPtr = this.Plugin.Interface.TargetModuleScanner.ScanText("40 53 41 57 48 83 EC 28 48 8B D9");

            this._requestPartyFinderListings = Marshal.GetDelegateForFunctionPointer<RequestPartyFinderListingsDelegate>(requestPfPtr);
            this._requestPfListingsHook = new Hook<RequestPartyFinderListingsDelegate>(requestPfPtr, new RequestPartyFinderListingsDelegate(this.OnRequestPartyFinderListings));
            this._requestPfListingsHook.Enable();

            this._handlePacketHook = new Hook<HandlePfPacketDelegate>(listingPtr, new HandlePfPacketDelegate(this.PacketDetour));
            this._handlePacketHook.Enable();
        }

        public void Dispose() {
            this._requestPfListingsHook.Dispose();
            this._handlePacketHook.Dispose();
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


        private void PacketDetour(IntPtr param1, IntPtr data) {
            if (data == IntPtr.Zero) {
                goto Return;
            }

            try {
                this.OnPacket(data);
            } catch (Exception ex) {
                PluginLog.Error(ex, "Unhandled exception in PF packet detour");
            }

            Return:
            this._handlePacketHook!.Original(param1, data);
        }

        private void OnPacket(IntPtr data) {
            var dataPtr = data + 0x10;

            // parse the packet into a struct
            var packet = Marshal.PtrToStructure<PfPacket>(dataPtr);

            var needToRewrite = false;

            for (var i = 0; i < packet.listings.Length; i++) {
                if (packet.listings[i].IsNull()) {
                    continue;
                }

                // invoke event for each non-null listing
                var listing = new PartyFinderListing(packet.listings[i], this.Plugin.Interface.Data);
                var args = new PartyFinderListingEventArgs();
                this.ReceivePartyFinderListing?.Invoke(listing, args);

                if (args.Visible) {
                    continue;
                }

                // zero the listing if it shouldn't be visible
                packet.listings[i] = new PfListing();
                needToRewrite = true;
            }

            if (!needToRewrite) {
                return;
            }

            // get some memory for writing to
            var newPacket = new byte[PacketInfo.PacketSize];
            var pinnedArray = GCHandle.Alloc(newPacket, GCHandleType.Pinned);
            var pointer = pinnedArray.AddrOfPinnedObject();

            // write our struct into the memory (doing this directly crashes the game)
            Marshal.StructureToPtr(packet, pointer, false);

            // copy our new memory over the game's
            Marshal.Copy(newPacket, 0, dataPtr, PacketInfo.PacketSize);

            // free memory
            pinnedArray.Free();
        }
    }

    internal class PartyFinderListingEventArgs {
        public bool Visible { get; set; } = true;
    }
}
