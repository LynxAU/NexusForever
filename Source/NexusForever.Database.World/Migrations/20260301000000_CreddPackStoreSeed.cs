using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexusForever.Database.World.Migrations
{
    /// <summary>
    /// Seeds the store with 1-, 3-, and 5-CREDD packs.
    ///
    /// AccountItem.Id = 22 is the single CREDD token entry in AccountItem.tbl
    /// (AccountCurrencyEnum=1 / Credd, AccountCurrencyAmount=1).
    /// The store_offer_item_data.amount column is used as a multiplier so that
    /// one DB row per offer delivers the correct CREDD quantity.
    ///
    /// All packs are priced at 0 NCoin (AccountCurrencyType.NCoin = 7) which
    /// makes them free on a private server without requiring a billing backend.
    /// </summary>
    public partial class CreddPackStoreSeed : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── Categories ────────────────────────────────────────────────
            // A hidden root entry (parentId=0) acts as the top-level placeholder.
            // The visible child category is what players see in the store UI.
            migrationBuilder.InsertData(
                table: "store_category",
                columns: new[] { "id", "parentId", "name", "description", "index", "visible" },
                values: new object[,]
                {
                    { 1u, 0u, "Store",          "",                               0u, (byte)0 },
                    { 2u, 1u, "CREDD Exchange", "Buy CREDD for your account.",    0u, (byte)1 },
                });

            // ── Offer groups (one per pack size) ──────────────────────────
            migrationBuilder.InsertData(
                table: "store_offer_group",
                columns: new[] { "id", "displayFlags", "name", "description", "displayInfoOverride", "visible" },
                values: new object[,]
                {
                    { 1u, 0u, "1 CREDD", "Grants 1 CREDD to your account.",  (ushort)0, (byte)1 },
                    { 2u, 0u, "3 CREDD", "Grants 3 CREDD to your account.",  (ushort)0, (byte)1 },
                    { 3u, 0u, "5 CREDD", "Grants 5 CREDD to your account.",  (ushort)0, (byte)1 },
                });

            // ── Link offer groups → CREDD Exchange category ───────────────
            migrationBuilder.InsertData(
                table: "store_offer_group_category",
                columns: new[] { "id", "categoryId", "index", "visible" },
                values: new object[,]
                {
                    { 1u, 2u, (byte)0, (byte)1 },
                    { 2u, 2u, (byte)1, (byte)1 },
                    { 3u, 2u, (byte)2, (byte)1 },
                });

            // ── Offer items (one purchasable entry per group) ─────────────
            migrationBuilder.InsertData(
                table: "store_offer_item",
                columns: new[] { "id", "groupId", "name", "description", "displayFlags", "field_6", "field_7", "visible" },
                values: new object[,]
                {
                    { 1u, 1u, "1 CREDD", "Grants 1 CREDD.",  0u, 0L, (byte)0, (byte)1 },
                    { 2u, 2u, "3 CREDD", "Grants 3 CREDD.",  0u, 0L, (byte)0, (byte)1 },
                    { 3u, 3u, "5 CREDD", "Grants 5 CREDD.",  0u, 0L, (byte)0, (byte)1 },
                });

            // ── Item data: AccountItem 22 = 1 CREDD/unit, amount = quantity ─
            // amount is used as a multiplier in the delivery code.
            migrationBuilder.InsertData(
                table: "store_offer_item_data",
                columns: new[] { "id", "itemId", "type", "amount" },
                values: new object[,]
                {
                    { 1u, (ushort)22, 0u, 1u },
                    { 2u, (ushort)22, 0u, 3u },
                    { 3u, (ushort)22, 0u, 5u },
                });

            // ── Price: 0 NCoin (AccountCurrencyType.NCoin = 7) ────────────
            // price = 0 means the CanAfford check always passes and nothing is
            // deducted — all packs are free on the private server.
            migrationBuilder.InsertData(
                table: "store_offer_item_price",
                columns: new[] { "id", "currencyId", "price", "discountType", "discountValue", "field_14", "expiry" },
                values: new object[,]
                {
                    { 1u, (byte)7, 0f, (byte)0, 0f, 0L, 0L },
                    { 2u, (byte)7, 0f, (byte)0, 0f, 0L, 0L },
                    { 3u, (byte)7, 0f, (byte)0, 0f, 0L, 0L },
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(table: "store_offer_item_price", keyColumns: new[] { "id", "currencyId" }, keyValues: new object[] { 1u, (byte)7 });
            migrationBuilder.DeleteData(table: "store_offer_item_price", keyColumns: new[] { "id", "currencyId" }, keyValues: new object[] { 2u, (byte)7 });
            migrationBuilder.DeleteData(table: "store_offer_item_price", keyColumns: new[] { "id", "currencyId" }, keyValues: new object[] { 3u, (byte)7 });

            migrationBuilder.DeleteData(table: "store_offer_item_data", keyColumns: new[] { "id", "itemId" }, keyValues: new object[] { 1u, (ushort)22 });
            migrationBuilder.DeleteData(table: "store_offer_item_data", keyColumns: new[] { "id", "itemId" }, keyValues: new object[] { 2u, (ushort)22 });
            migrationBuilder.DeleteData(table: "store_offer_item_data", keyColumns: new[] { "id", "itemId" }, keyValues: new object[] { 3u, (ushort)22 });

            migrationBuilder.DeleteData(table: "store_offer_item", keyColumns: new[] { "id", "groupId" }, keyValues: new object[] { 1u, 1u });
            migrationBuilder.DeleteData(table: "store_offer_item", keyColumns: new[] { "id", "groupId" }, keyValues: new object[] { 2u, 2u });
            migrationBuilder.DeleteData(table: "store_offer_item", keyColumns: new[] { "id", "groupId" }, keyValues: new object[] { 3u, 3u });

            migrationBuilder.DeleteData(table: "store_offer_group_category", keyColumns: new[] { "id", "categoryId" }, keyValues: new object[] { 1u, 2u });
            migrationBuilder.DeleteData(table: "store_offer_group_category", keyColumns: new[] { "id", "categoryId" }, keyValues: new object[] { 2u, 2u });
            migrationBuilder.DeleteData(table: "store_offer_group_category", keyColumns: new[] { "id", "categoryId" }, keyValues: new object[] { 3u, 2u });

            migrationBuilder.DeleteData(table: "store_offer_group", keyColumn: "id", keyValue: 1u);
            migrationBuilder.DeleteData(table: "store_offer_group", keyColumn: "id", keyValue: 2u);
            migrationBuilder.DeleteData(table: "store_offer_group", keyColumn: "id", keyValue: 3u);

            migrationBuilder.DeleteData(table: "store_category", keyColumn: "id", keyValue: 2u);
            migrationBuilder.DeleteData(table: "store_category", keyColumn: "id", keyValue: 1u);
        }
    }
}
