using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlazingPizza.Data.Migrations;

/// <inheritdoc />
public partial class PizzaStoreHtmxor : Migration
{
	/// <inheritdoc />
	protected override void Up(MigrationBuilder migrationBuilder)
	{
		migrationBuilder.CreateTable(
			name: "Address",
			columns: table => new
			{
				Id = table.Column<int>(type: "INTEGER", nullable: false)
					.Annotation("Sqlite:Autoincrement", true),
				Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
				Line1 = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
				Line2 = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
				City = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
				Region = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
				PostalCode = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false)
			},
			constraints: table =>
			{
				table.PrimaryKey("PK_Address", x => x.Id);
			});

		migrationBuilder.CreateTable(
			name: "NotificationSubscriptions",
			columns: table => new
			{
				NotificationSubscriptionId = table.Column<int>(type: "INTEGER", nullable: false)
					.Annotation("Sqlite:Autoincrement", true),
				UserId = table.Column<string>(type: "TEXT", nullable: true),
				Url = table.Column<string>(type: "TEXT", nullable: true),
				P256dh = table.Column<string>(type: "TEXT", nullable: true),
				Auth = table.Column<string>(type: "TEXT", nullable: true)
			},
			constraints: table =>
			{
				table.PrimaryKey("PK_NotificationSubscriptions", x => x.NotificationSubscriptionId);
			});

		migrationBuilder.CreateTable(
			name: "Specials",
			columns: table => new
			{
				Id = table.Column<int>(type: "INTEGER", nullable: false)
					.Annotation("Sqlite:Autoincrement", true),
				Name = table.Column<string>(type: "TEXT", nullable: false),
				BasePrice = table.Column<decimal>(type: "TEXT", nullable: false),
				Description = table.Column<string>(type: "TEXT", nullable: false),
				ImageUrl = table.Column<string>(type: "TEXT", nullable: false)
			},
			constraints: table =>
			{
				table.PrimaryKey("PK_Specials", x => x.Id);
			});

		migrationBuilder.CreateTable(
			name: "Toppings",
			columns: table => new
			{
				Id = table.Column<int>(type: "INTEGER", nullable: false)
					.Annotation("Sqlite:Autoincrement", true),
				Name = table.Column<string>(type: "TEXT", nullable: false),
				Price = table.Column<decimal>(type: "TEXT", nullable: false)
			},
			constraints: table =>
			{
				table.PrimaryKey("PK_Toppings", x => x.Id);
			});

		migrationBuilder.CreateTable(
			name: "Orders",
			columns: table => new
			{
				OrderId = table.Column<int>(type: "INTEGER", nullable: false),
				UserId = table.Column<string>(type: "TEXT", nullable: true),
				CreatedTime = table.Column<DateTime>(type: "TEXT", nullable: false),
				DeliveryAddressId = table.Column<int>(type: "INTEGER", nullable: false),
				DeliveryLocation_Latitude = table.Column<double>(type: "REAL", nullable: true),
				DeliveryLocation_Longitude = table.Column<double>(type: "REAL", nullable: true)
			},
			constraints: table =>
			{
				table.PrimaryKey("PK_Orders", x => x.OrderId);
				table.ForeignKey(
					name: "FK_Orders_Address_DeliveryAddressId",
					column: x => x.DeliveryAddressId,
					principalTable: "Address",
					principalColumn: "Id",
					onDelete: ReferentialAction.Cascade);
			});

		migrationBuilder.CreateTable(
			name: "Pizzas",
			columns: table => new
			{
				Id = table.Column<int>(type: "INTEGER", nullable: false)
					.Annotation("Sqlite:Autoincrement", true),
				OrderId = table.Column<int>(type: "INTEGER", nullable: false),
				SpecialId = table.Column<int>(type: "INTEGER", nullable: false),
				Size = table.Column<int>(type: "INTEGER", nullable: false)
			},
			constraints: table =>
			{
				table.PrimaryKey("PK_Pizzas", x => x.Id);
				table.ForeignKey(
					name: "FK_Pizzas_Orders_OrderId",
					column: x => x.OrderId,
					principalTable: "Orders",
					principalColumn: "OrderId",
					onDelete: ReferentialAction.Cascade);
				table.ForeignKey(
					name: "FK_Pizzas_Specials_SpecialId",
					column: x => x.SpecialId,
					principalTable: "Specials",
					principalColumn: "Id",
					onDelete: ReferentialAction.Cascade);
			});

		migrationBuilder.CreateTable(
			name: "PizzaTopping",
			columns: table => new
			{
				ToppingId = table.Column<int>(type: "INTEGER", nullable: false),
				PizzaId = table.Column<int>(type: "INTEGER", nullable: false)
			},
			constraints: table =>
			{
				table.PrimaryKey("PK_PizzaTopping", x => new { x.PizzaId, x.ToppingId });
				table.ForeignKey(
					name: "FK_PizzaTopping_Pizzas_PizzaId",
					column: x => x.PizzaId,
					principalTable: "Pizzas",
					principalColumn: "Id",
					onDelete: ReferentialAction.Cascade);
				table.ForeignKey(
					name: "FK_PizzaTopping_Toppings_ToppingId",
					column: x => x.ToppingId,
					principalTable: "Toppings",
					principalColumn: "Id",
					onDelete: ReferentialAction.Cascade);
			});

		migrationBuilder.CreateIndex(
			name: "IX_Orders_DeliveryAddressId",
			table: "Orders",
			column: "DeliveryAddressId");

		migrationBuilder.CreateIndex(
			name: "IX_Pizzas_OrderId",
			table: "Pizzas",
			column: "OrderId");

		migrationBuilder.CreateIndex(
			name: "IX_Pizzas_SpecialId",
			table: "Pizzas",
			column: "SpecialId");

		migrationBuilder.CreateIndex(
			name: "IX_PizzaTopping_ToppingId",
			table: "PizzaTopping",
			column: "ToppingId");
	}

	/// <inheritdoc />
	protected override void Down(MigrationBuilder migrationBuilder)
	{
		migrationBuilder.DropTable(
			name: "NotificationSubscriptions");

		migrationBuilder.DropTable(
			name: "PizzaTopping");

		migrationBuilder.DropTable(
			name: "Pizzas");

		migrationBuilder.DropTable(
			name: "Toppings");

		migrationBuilder.DropTable(
			name: "Orders");

		migrationBuilder.DropTable(
			name: "Specials");

		migrationBuilder.DropTable(
			name: "Address");
	}
}
