using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookShop.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class fixProductIdKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ShoppingCarts_Products_ProdutId",
                table: "ShoppingCarts");

            migrationBuilder.DropIndex(
                name: "IX_ShoppingCarts_ProdutId",
                table: "ShoppingCarts");

            migrationBuilder.DropColumn(
                name: "ProdutId",
                table: "ShoppingCarts");

            migrationBuilder.CreateIndex(
                name: "IX_ShoppingCarts_ProductId",
                table: "ShoppingCarts",
                column: "ProductId");

            migrationBuilder.AddForeignKey(
                name: "FK_ShoppingCarts_Products_ProductId",
                table: "ShoppingCarts",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ShoppingCarts_Products_ProductId",
                table: "ShoppingCarts");

            migrationBuilder.DropIndex(
                name: "IX_ShoppingCarts_ProductId",
                table: "ShoppingCarts");

            migrationBuilder.AddColumn<int>(
                name: "ProdutId",
                table: "ShoppingCarts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_ShoppingCarts_ProdutId",
                table: "ShoppingCarts",
                column: "ProdutId");

            migrationBuilder.AddForeignKey(
                name: "FK_ShoppingCarts_Products_ProdutId",
                table: "ShoppingCarts",
                column: "ProdutId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
