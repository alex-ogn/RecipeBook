using HtmlAgilityPack;
using Microsoft.Extensions.Localization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using RecipeBook.Models;
using RecipeBook.Resources;
using System.Reflection;

namespace RecipeBook.Services
{
    /// <summary>
    /// Class for creating PDF files
    /// </summary>
    public class RecipePdfService : IRecipePdfService
    {
        private readonly String _recipeInfoFormat = "{0}: {1}";
        private readonly IStringLocalizer _localizer;

        public RecipePdfService(IStringLocalizerFactory factory)
        {
            _localizer = factory.Create("RecipePdfTexts", Assembly.GetExecutingAssembly().GetName().Name);
        }

        /// <summary>
        /// Generates pdf recipe
        /// </summary>
        /// <param name="recipe"></param>
        /// <returns></returns>
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
                                            
                        col.Item().Text(string.Format(_recipeInfoFormat, _localizer["Category"], recipe.RecipeCategory?.Name));
                        col.Item().Text(string.Format(_recipeInfoFormat, _localizer["Servings"], recipe.Servings));
                        col.Item().Text(string.Format(_localizer["CookingTime"], recipe.CookingTime));
                        
                        col.Item().PaddingVertical(10).Text(_localizer["Ingredients"])
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
                                header.Cell().Text(_localizer["Ingredient"]).SemiBold();
                                header.Cell().Text(_localizer["Quantity"]).SemiBold();
                            });

                            foreach (var ri in recipe.RecipeIngredients)
                            {
                                table.Cell().Text(ri.Ingredient?.Name ?? "");
                                table.Cell().Text(ri.QuantityNeeded);
                            }
                        });

                        col.Item().PaddingVertical(10).Text(_localizer["Description"])
                            .FontSize(18).SemiBold();

                        AddFormattedText(col, recipe.Description);

                        col.Item().PaddingVertical(10).Text(_localizer["Instructions"])
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

        /// <summary>
        /// Transforms html tags to formatted text
        /// </summary>
        /// <param name="col"></param>
        /// <param name="htmlText"></param>
        private void AddFormattedText(ColumnDescriptor col, string htmlText)
        {
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(htmlText ?? "");

            foreach (var node in htmlDoc.DocumentNode.ChildNodes)
            {
                ProcessHtmlNode(col, node);
            }
        }

        /// <summary>
        /// Transforms html tag to formatted text
        /// </summary>
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
                    col.Item().Text(""); // empty row
                    break;

                default:
                    // Notes are recursive processed
                    foreach (var child in node.ChildNodes)
                        ProcessHtmlNode(col, child);
                    break;
            }
        }


    }
}