using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace Atomex.WatchTower.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Parties",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Requisites_SecretHash = table.Column<string>(nullable: true),
                    Requisites_ReceivingAddress = table.Column<string>(nullable: true),
                    Requisites_RefundAddress = table.Column<string>(nullable: true),
                    Requisites_RewardForRedeem = table.Column<decimal>(type: "decimal(18,9)", nullable: true),
                    Requisites_LockTime = table.Column<decimal>(nullable: true),
                    Requisites_WatchTower = table.Column<string>(nullable: true),
                    Side = table.Column<int>(nullable: false),
                    Status = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Parties", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Transactions",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Currency = table.Column<string>(nullable: true),
                    TxId = table.Column<string>(nullable: true),
                    BlockHeight = table.Column<long>(nullable: false),
                    Confirmations = table.Column<long>(nullable: false),
                    Status = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transactions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Swaps",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Symbol = table.Column<string>(nullable: true),
                    TimeStamp = table.Column<DateTime>(nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,9)", nullable: false),
                    Qty = table.Column<decimal>(type: "decimal(18,9)", nullable: false),
                    Secret = table.Column<string>(nullable: true),
                    SecretHash = table.Column<string>(nullable: true),
                    InitiatorId = table.Column<long>(nullable: false),
                    AcceptorId = table.Column<long>(nullable: false),
                    BaseCurrencyContract = table.Column<string>(nullable: true),
                    QuoteCurrencyContract = table.Column<string>(nullable: true),
                    OldId = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Swaps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Swaps_Parties_AcceptorId",
                        column: x => x.AcceptorId,
                        principalTable: "Parties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Swaps_Parties_InitiatorId",
                        column: x => x.InitiatorId,
                        principalTable: "Parties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PartyTransactions",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PartyId = table.Column<long>(nullable: false),
                    TransactionId = table.Column<long>(nullable: false),
                    Type = table.Column<int>(nullable: false),
                    Amount = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartyTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PartyTransactions_Parties_PartyId",
                        column: x => x.PartyId,
                        principalTable: "Parties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PartyTransactions_Transactions_TransactionId",
                        column: x => x.TransactionId,
                        principalTable: "Transactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PartyTransactions_PartyId",
                table: "PartyTransactions",
                column: "PartyId");

            migrationBuilder.CreateIndex(
                name: "IX_PartyTransactions_TransactionId",
                table: "PartyTransactions",
                column: "TransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_Swaps_AcceptorId",
                table: "Swaps",
                column: "AcceptorId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Swaps_InitiatorId",
                table: "Swaps",
                column: "InitiatorId",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PartyTransactions");

            migrationBuilder.DropTable(
                name: "Swaps");

            migrationBuilder.DropTable(
                name: "Transactions");

            migrationBuilder.DropTable(
                name: "Parties");
        }
    }
}
