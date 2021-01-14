using System;
using System.Linq;
using System.Runtime.InteropServices;
using Dalamud.Hooking;
using Dalamud.Plugin;

namespace BetterPartyFinder {
    public class GameFunctions : IDisposable {
        private Plugin Plugin { get; }

        internal delegate void PartyFinderListingEventDelegate(PartyFinderListing listing, PartyFinderListingEventArgs args);

        internal event PartyFinderListingEventDelegate? ReceivePartyFinderListing;

        private delegate void HandlePfPacketDelegate(IntPtr param1, IntPtr data);

        private readonly Hook<HandlePfPacketDelegate>? _handlePacketHook;

        internal GameFunctions(Plugin plugin) {
            this.Plugin = plugin;

            var listingPtr = this.Plugin.Interface.TargetModuleScanner.ScanText("40 53 41 57 48 83 EC 28 48 8B D9");

            this._handlePacketHook = new Hook<HandlePfPacketDelegate>(listingPtr, new HandlePfPacketDelegate(this.PacketDetour));
            this._handlePacketHook.Enable();
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

        public void Dispose() {
            this._handlePacketHook?.Dispose();
        }
    }

    internal class PartyFinderListingEventArgs {
        public bool Visible { get; set; } = true;
    }
}
