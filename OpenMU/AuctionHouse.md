# Auction House UI Handoff

## 1. Project Goal

This is a client-side Auction House UI/UX rework for a MU Online / OpenMU / MUnique-style client.

The server-side Auction House already exists. Future work should remain a client-side UI integration unless a separate task explicitly says otherwise.

Do not change server-side logic, packet contracts, response parsing, operation values, database models, or OpenMU server files for this UI work.

## 2. Current Server Contract

Auction House request packet:

- 16 bytes
- `[4]` op
- `[5]` arg1
- `[6]` currency
- `[7]` jewelSlot
- `[8..11]` arg2 uint32 LE
- `[12..15]` arg3 uint32 LE

Existing operations:

- op `0` = Browse listings
- op `1` = Create listing from inventory slot
- op `2` = Buy listing
- op `3` = My Listings
- op `4` = Cancel listing
- op `5` = Bought items / deliveries
- op `6` = Receive bought item
- op `7` = Seller payouts
- op `8` = Claim payout

Current client sends:

op `1` Create:

- arg1 = inventory slot
- currency = sell currency
- jewelSlot = sell jewel slot or `0xFF`
- arg2 = price
- arg3 = `0`

op `2` Buy:

- arg1 = `0`
- currency = `listing.Currency`
- jewelSlot = `listing.JewelSlot`
- arg2 = `listing.ListingNumber`
- arg3 = `listing.Price`

op `4` Cancel:

- arg1 = `0`
- currency = `0`
- jewelSlot = `0xFF`
- arg2 = `listing.ListingNumber`
- arg3 = `0`

op `6` Receive:

- arg1 = `0`
- currency = `0`
- jewelSlot = `0xFF`
- arg2 = `listing.ListingNumber`
- arg3 = `0`

op `8` Claim:

- arg1 = `0`
- currency = `0`
- jewelSlot = `0xFF`
- arg2 = `listing.ListingNumber`
- arg3 = `0`

## 3. Server-to-Client Listing Data

Current parsed listing row fields from `WSclient.cpp::ReceiveAuctionHousePacket`:

- status = packet `[6]`
- currency = packet `[7]`
- listingNumber = packet `[8..11]`
- itemType = packet `[12..13]`
- itemLevel = packet `[14]`
- price = packet `[15..18]`
- itemName = packet `[19..66]`
- sellerName = packet `[67..78]`
- jewelSlot = packet `[79]`

Only these fields should be displayed as real data in the current UI.

Do not fake unsupported fields such as current bid, minimum bid, expiration, durability, sockets, item options, posted date, or global total count.

## 4. Files Currently Involved

Known client files:

- `C:\MuDev\MuMain\src\source\UI\NewUI\NewUIMuHelper.h`
- `C:\MuDev\MuMain\src\source\UI\NewUI\NewUIMuHelper.cpp`
  - Contains `CNewUIAuctionHouse` and most current Auction House UI implementation.

- `C:\MuDev\MuMain\src\source\UI\NewUI\NewUISystem.h`
- `C:\MuDev\MuMain\src\source\UI\NewUI\NewUISystem.cpp`
  - Holds / creates / releases `CNewUIAuctionHouse` and `g_pNewUIAuctionHouse`.

- `C:\MuDev\MuMain\src\source\Core\Globals\_enum.h`
  - Defines `INTERFACE_AUCTIONHOUSE`.

- `C:\MuDev\MuMain\src\source\UI\NewUI\Inventory\NewUIMyInventory.cpp`
  - Inventory shop button and `S` hotkey handling are related.
  - Known issue: `S` hotkey only opens Auction House when inventory is visible because `CNewUIMyInventory::UpdateKeyEvent()` returns early if `INTERFACE_INVENTORY` is not visible.

- `C:\MuDev\MuMain\src\source\UI\NewUI\Inventory\NewUIInventoryActionController.cpp`
  - Needed later for right-click inventory item selection.
  - Relevant method: `CNewUIInventoryActionController::HandleInventoryRightClickActions(CNewUIInventoryCtrl* targetControl) const`

Packet/response-related client files. Avoid touching unless absolutely necessary:

- `C:\MuDev\MuMain\ClientLibrary\ConnectionManager.ClientToServer.Custom.cs`
- `C:\MuDev\MuMain\src\source\Dotnet\PacketFunctions_Custom.h`
- `C:\MuDev\MuMain\src\source\Dotnet\PacketFunctions_Custom.cpp`
- `C:\MuDev\MuMain\src\source\Network\Server\WSclient.cpp`

## 5. Server Files That Must Not Be Touched

Protected server files:

- `C:\MuDev\OpenMU\src\GameServer\MessageHandler\MuHelper\AuctionHouseRequestHandlerPlugIn.cs`
- `C:\MuDev\OpenMU\src\GameServer\RemoteView\AuctionHouse\AuctionHouseViewPlugIn.cs`
- `C:\MuDev\OpenMU\src\GameLogic\Views\AuctionHouse\IAuctionHouseViewPlugIn.cs`
- `C:\MuDev\OpenMU\src\GameLogic\PlayerActions\AuctionHouse\AuctionHouseService.cs`
- `C:\MuDev\OpenMU\src\GameLogic\PlugIns\ChatCommands\AuctionHouseChatCommandPlugIn.cs`
- `C:\MuDev\OpenMU\src\DataModel\Entities\AuctionListing.cs`
- `C:\MuDev\OpenMU\src\Persistence\...\AuctionListing*`
- WCoin foundation files and migrations

Do not modify these for client UI work.

## 6. What Was Changed So Far

Auction House client UI changes made in the previous conversation:

- Reworked `CNewUIAuctionHouse` layout multiple times.
- Final current baseline is a minimal compact fixed-layout UI.
- Window rectangle:
  - x = `10`
  - y = `70`
  - width = `430`
  - height = `286`
- It fits with inventory open and does not overlap the bottom skill/action bar.
- Unsupported placeholders were hidden:
  - My Bids
  - Watchlist
  - History
  - Advanced
  - Place Bid
  - Compare
  - Report
- Visible supported tabs:
  - Browse
  - My Listings
  - Bought
  - Payouts
  - Create
- Bottom strip reduced to footer/status only.
- Contextual right-panel action buttons added:
  - Browse -> BUY -> op `2`
  - My Listings -> CANCEL -> op `4`
  - Bought -> RECEIVE -> op `6`
  - Payouts -> CLAIM -> op `8`
  - Create -> POST -> op `1`
- Button dispatch moved into `CNewUIAuctionHouse::UpdateMouseEvent()` via `ProcessMouseButtons()`.
- Table row selection now runs after button handling.
- Visible two-line debug/status was added:
  - `Selected idx=I list=L st=S cur=C price=P jewel=J`
  - `Sending op X list=L price=P cur=C jewel=J`
  - `POST slot=X price=Y cur=Z jewel=J`
  - `Sending op 1 slot=X price=Y cur=Z jewel=J`

## 7. Current Runtime Status

Working:

- Client compiles.
- Auction House opens when inventory is open / via inventory button flow.
- Compact UI fits with inventory visible.
- UI no longer overlaps bottom skill/action bar.
- Browse, My Listings, and Create tabs render.
- Buttons now fire and reach server.

Not working / unresolved:

- Pressing `S` does not open Auction House when inventory is closed.
- Buy and Cancel reach server but server responds: `has no escrow item`.
- Create Listing item selection is unusable with current Add Item flow.
- Add Item depends on selected/hovered/picked inventory slot and can trigger normal drop-to-floor behavior.
- Bought/Receive and Payout/Claim cannot be fully tested until valid buy/sell flow works.
- UI is minimal functional baseline, not final visual design.

## 8. Interpretation of "has no escrow item"

The client appears to be sending `listingNumber` as expected by the chat-command/server contract.

The server response:

`listing #1 has no escrow item`

likely means:

- listing `#1` exists,
- server found the listing,
- but `AuctionListing.EscrowItem` is null,
- so this may be a stale/broken DB listing or a listing created before escrow persistence was correct.

Do not try to fix this in the client without first inspecting the DB/server state.

Recommended future check:

- Inspect `AuctionListing` table row with listing number `1`.
- Confirm whether `EscrowItem` is null.
- Do not modify DB until cause is understood.

## 9. Create Listing Problem

Current Add Item flow:

- Uses `GetSelectedInventorySlot()`.
- It checks picked/dragged item source slot or current hovered inventory slot.
- Moving mouse from inventory item to Add Item can lose hovered slot.
- Dragging/picking item can trigger normal inventory drop behavior.
- Runtime symptom: client acts like item is being dropped on the floor and blocks it because item is too expensive.

Conclusion:

Right-click inventory integration is required for sane UX.

## 10. Planned Right-Click Integration

Required behavior:

When Auction House is open and Create tab is active:

- Right-clicking a valid backpack inventory item selects that item as the Auction House create-listing item.
- It must not drop the item.
- It must not equip/use the item.
- It must consume the right-click only in this exact context.
- It must update the Create tab status: `Item selected for listing: slot X`
- It must not send op `1`.
- POST still sends op `1` later.

Files:

- `C:\MuDev\MuMain\src\source\UI\NewUI\NewUIMuHelper.h`
- `C:\MuDev\MuMain\src\source\UI\NewUI\NewUIMuHelper.cpp`
- `C:\MuDev\MuMain\src\source\UI\NewUI\Inventory\NewUIInventoryActionController.cpp`

Methods to add to `CNewUIAuctionHouse`:

- `bool IsCreateListingView() const;`
- `bool TrySetCreateListingItemFromInventorySlot(int slot);`

Inventory method:

- `CNewUIInventoryActionController::HandleInventoryRightClickActions(CNewUIInventoryCtrl* targetControl) const`

Proposed logic:

- find item under `MouseX` / `MouseY`
- get slot with `targetControl->GetIndexByItem(pItem)`
- if `g_pNewUIAuctionHouse` exists and is visible and `IsCreateListingView()` is true:
  - call `TrySetCreateListingItemFromInventorySlot(slot)`
  - if true, return true
- otherwise continue original inventory right-click behavior unchanged

Strict guards:

- Auction House pointer exists
- Auction House visible/open
- active view is Create
- targetControl has item under cursor
- slot lookup succeeds
- slot is backpack/listable inventory area
- `TrySetCreateListingItemFromInventorySlot(slot)` returns true

## 11. S Hotkey Issue

Current behavior:

- `S` does not open Auction House when inventory is closed.
- `S` works only when inventory is open / via inventory button flow.

Known cause:

- `S` hotkey is handled in `C:\MuDev\MuMain\src\source\UI\NewUI\Inventory\NewUIMyInventory.cpp`
- Method: `CNewUIMyInventory::UpdateKeyEvent()`
- It returns early if `INTERFACE_INVENTORY` is not visible before checking `IsPress('S')`.

Future fix:

- Either move `S` hotkey handling to a global UI key handler,
- or adjust `NewUIMyInventory::UpdateKeyEvent()` carefully so `S` can open Auction House even when inventory is closed.

Do not fix this together with right-click integration. Keep it separate.

## 12. Recommended Next Steps For Clean Codex Conversation

The new Codex conversation should do this in order:

Step 1:

Read `C:\MuDev\OpenMU\AuctionHouse.md` fully.

Step 2:

Inspect current diff and confirm current Auction House UI baseline.

Step 3:

Do not touch server or packet files.

Step 4:

Implement right-click inventory item selection only.

Step 5:

Test Create tab:

- Open Auction House.
- Switch to Create.
- Open Inventory.
- Right-click backpack item.
- Confirm Auction House item slot updates.
- Set price/currency.
- POST.
- Confirm op `1` payload.

Step 6:

Create a brand-new listing and then test:

- My Listings -> Cancel that new listing.
- Browse -> Buy a valid listing.
- Bought -> Receive.
- Payouts -> Claim.

Step 7:

Only after flow works, inspect old listing `#1` DB state if `has no escrow item` persists.

Step 8:

Only after functionality is stable, do visual polish.

## 13. Clean Conversation Starter Prompt

Paste this into a new Codex conversation:

```text
Read C:\MuDev\OpenMU\AuctionHouse.md completely before making changes.

We are starting from a polluted previous conversation, so ignore prior assumptions and use this document as source of truth.

Your task is NOT to redesign the UI from scratch.
Your task is to continue from the current compact functional baseline.

Immediate goal:
Implement right-click inventory item selection for Create Listing only.

Do not touch server-side files.
Do not touch packet contracts.
Do not change operation values.
Do not change response parsing.
Do not do visual polish.
Do not fix S hotkey yet.

Before editing:
1. Confirm current files and methods.
2. Confirm right-click integration plan.
3. Confirm exact files you need to edit.
4. Wait for approval.
```

## 14. Final Visual Target / Reference Image

A final Auction House UI mockup image exists and should be used as the long-term visual target.

Important:
This image is NOT the immediate implementation task.
Do not try to match the full mockup in one pass.

Use the image only as guidance for:
- dark fantasy MU Online style
- panel hierarchy
- table/detail/action organization
- dark steel frame
- antique gold headings
- blue selected states
- green/amber/red button color logic
- final desired Auction House layout

Current immediate priority:
1. Preserve the compact working baseline.
2. Fix Create Listing item selection with right-click inventory integration.
3. Verify Create Listing -> Post -> My Listings -> Cancel works.
4. Verify Browse -> Buy -> Bought -> Receive -> Payouts -> Claim works.
5. Only after functionality is stable, gradually move the UI toward the reference image.

The reference image should be attached to the new Codex conversation and treated as the final design direction, not as a direct one-pass implementation.