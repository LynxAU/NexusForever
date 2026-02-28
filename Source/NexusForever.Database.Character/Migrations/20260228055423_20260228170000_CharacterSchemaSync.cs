using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexusForever.Database.Character.Migrations
{
    /// <inheritdoc />
    public partial class _20260228170000_CharacterSchemaSync : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"CREATE TABLE IF NOT EXISTS `character_auction` (
                    `auctionId` bigint(20) unsigned NOT NULL AUTO_INCREMENT,
                    `id` bigint(20) unsigned NOT NULL DEFAULT 0,
                    `itemGuid` bigint(20) unsigned NOT NULL DEFAULT 0,
                    `itemId` int(10) unsigned NOT NULL DEFAULT 0,
                    `quantity` int(10) unsigned NOT NULL DEFAULT 0,
                    `minimumBid` bigint(20) unsigned NOT NULL DEFAULT 0,
                    `buyoutPrice` bigint(20) unsigned NOT NULL DEFAULT 0,
                    `currentBid` bigint(20) unsigned NOT NULL DEFAULT 0,
                    `ownerCharacterId` bigint(20) unsigned NOT NULL DEFAULT 0,
                    `topBidderCharacterId` bigint(20) unsigned NOT NULL DEFAULT 0,
                    `expirationTime` datetime NOT NULL,
                    `createTime` datetime NOT NULL DEFAULT current_timestamp(),
                    `worldRequirement_Item2Id` int(10) unsigned NOT NULL DEFAULT 0,
                    `glyphData` int(10) unsigned NOT NULL DEFAULT 0,
                    `thresholdData` bigint(20) unsigned NOT NULL DEFAULT 0,
                    `circuitData` bigint(20) unsigned NOT NULL DEFAULT 0,
                    `unknown2` int(10) unsigned NOT NULL DEFAULT 0,
                    PRIMARY KEY (`auctionId`),
                    KEY `itemId` (`itemId`),
                    KEY `ownerCharacterId` (`ownerCharacterId`),
                    KEY `expirationTime` (`expirationTime`)
                  ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;");

            migrationBuilder.Sql(
                @"CREATE TABLE IF NOT EXISTS `character_commodity_order` (
                    `orderId` bigint(20) unsigned NOT NULL AUTO_INCREMENT,
                    `id` bigint(20) unsigned NOT NULL DEFAULT 0,
                    `characterId` bigint(20) unsigned NOT NULL DEFAULT 0,
                    `itemId` int(10) unsigned NOT NULL DEFAULT 0,
                    `quantity` int(10) unsigned NOT NULL DEFAULT 0,
                    `filledQuantity` int(10) unsigned NOT NULL DEFAULT 0,
                    `unitPrice` bigint(20) unsigned NOT NULL DEFAULT 0,
                    `isBuyOrder` tinyint(1) NOT NULL,
                    `expirationTime` datetime NOT NULL,
                    `createTime` datetime NOT NULL,
                    PRIMARY KEY (`orderId`),
                    KEY `itemId` (`itemId`),
                    KEY `characterId` (`characterId`),
                    KEY `isBuyOrder` (`isBuyOrder`),
                    KEY `expirationTime` (`expirationTime`)
                  ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TABLE IF EXISTS `character_commodity_order`;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS `character_auction`;");
        }
    }
}
