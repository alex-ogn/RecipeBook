using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using RecipeBook.Models;
using HtmlAgilityPack;

namespace RecipeBook.Services
{
    public class RecipePdfService : IRecipePdfService
    {
        public byte[] GenerateRecipePdf(Recipe recipe)
        {
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(50);
                    page.Size(PageSizes.A4);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(14));

                    page.Header().Column(header =>
                    {
                        header.Item().Text(recipe.Title)
                            .FontSize(24)
                            .SemiBold()
                            .AlignCenter();
                    });


                    page.Content().PaddingVertical(20).Column(col =>
                    {

                        if (recipe.Image != null)
                        {
                            col.Item().Image(recipe.Image).FitArea(); // Height(200)
                            col.Item().PaddingTop(10);
                        }

                        col.Item().Text($"Категория: {recipe.RecipeCategory?.Name}");
                        col.Item().Text($"Порции: {recipe.Servings}");
                        col.Item().Text($"Време за готвене: {recipe.CookingTime} мин.");

                        col.Item().PaddingVertical(10).Text("Съставки:")
                            .FontSize(18).SemiBold();

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(200);
                                columns.RelativeColumn();
                            });

                            table.Header(header =>
                            {
                                header.Cell().Text("Съставка").SemiBold();
                                header.Cell().Text("Количество").SemiBold();
                            });

                            foreach (var ri in recipe.RecipeIngredients)
                            {
                                table.Cell().Text(ri.Ingredient?.Name ?? "");
                                table.Cell().Text(ri.QuantityNeeded);
                            }
                        });

                        col.Item().PaddingVertical(10).Text("Описание:")
                            .FontSize(18).SemiBold();

                        AddFormattedText(col, recipe.Description);

                        col.Item().PaddingVertical(10).Text("Инструкции:")
                            .FontSize(18).SemiBold();

                        AddFormattedText(col, recipe.Instructions);

                    });

                    page.Footer()
                        .AlignCenter()
                        .Text(txt =>
                        {
                            txt.Span("RecipeBook © ").FontSize(10);
                            txt.Span(DateTime.Now.ToString("dd.MM.yyyy")).FontSize(10);
                        });
                });
            }).GeneratePdf();
        }

        private void AddFormattedText(ColumnDescriptor col, string htmlText)
        {
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(htmlText ?? "");

            foreach (var node in htmlDoc.DocumentNode.ChildNodes)
            {
                ProcessHtmlNode(col, node);
            }
        }

        private void ProcessHtmlNode(ColumnDescriptor col, HtmlNode node)
        {
            switch (node.Name)
            {
                case "p":
                case "#text":
                    var text = HtmlEntity.DeEntitize(node.InnerText.Trim());
                    if (!string.IsNullOrWhiteSpace(text))
                        col.Item().Text(text).FontSize(14);
                    break;

                case "strong":
                case "b":
                    col.Item().Text(HtmlEntity.DeEntitize(node.InnerText.Trim()))
                        .FontSize(14).SemiBold();
                    break;

                case "em":
                case "i":
                    col.Item().Text(HtmlEntity.DeEntitize(node.InnerText.Trim()))
                        .FontSize(14).Italic();
                    break;

                case "h3":
                    col.Item().Text(HtmlEntity.DeEntitize(node.InnerText.Trim()))
                        .FontSize(18).Bold();
                    break;

                case "h4":
                    col.Item().Text(HtmlEntity.DeEntitize(node.InnerText.Trim()))
                        .FontSize(16).Bold();
                    break;

                case "ul":
                case "ol":
                    foreach (var li in node.Elements("li"))
                    {
                        col.Item().Row(row =>
                        {
                            row.AutoItem().Text("•").FontSize(14);
                            row.RelativeItem().Text(HtmlEntity.DeEntitize(li.InnerText.Trim())).FontSize(14);
                        });
                    }
                    break;

                case "br":
                    col.Item().Text(""); // празен ред
                    break;

                default:
                    // Рекурсивно обработване на вложени елементи
                    foreach (var child in node.ChildNodes)
                        ProcessHtmlNode(col, child);
                    break;
            }
        }


    }
}