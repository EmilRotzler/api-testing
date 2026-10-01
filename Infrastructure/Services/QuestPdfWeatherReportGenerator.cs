using System.Globalization;
using ApiTesting.Core.Interfaces;
using ApiTesting.Core.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ApiTesting.Infrastructure.Services;

public class QuestPdfWeatherReportGenerator : IWeatherReportPdfGenerator
{
    private static readonly string[] ColumnTitles =
    [
        "Date",
        "Location",
        "Temp",
        "Feels like",
        "Summary",
        "Wind",
        "Humidity",
        "Precip",
    ];

    public byte[] Generate(IReadOnlyList<WeatherForecast> forecasts, DateTime generatedAtUtc)
    {
        return Document
            .Create(document =>
                document.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1.5f, Unit.Centimetre);
                    page.DefaultTextStyle(style => style.FontSize(10));

                    page.Header()
                        .Column(column =>
                        {
                            column.Item().Text("Weather Forecast Report").FontSize(18).Bold();
                            column
                                .Item()
                                .Text(
                                    $"Generated at {generatedAtUtc.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)} UTC"
                                )
                                .FontSize(9)
                                .FontColor(Colors.Grey.Darken1);
                        });

                    page.Content()
                        .PaddingTop(10)
                        .Element(content =>
                        {
                            if (forecasts.Count == 0)
                            {
                                content.Text("No forecast data available").Italic();
                                return;
                            }

                            ComposeTable(content, forecasts);
                        });

                    page.Footer()
                        .AlignCenter()
                        .Text(text =>
                        {
                            text.CurrentPageNumber();
                            text.Span(" / ");
                            text.TotalPages();
                        });
                })
            )
            .GeneratePdf();
    }

    private static void ComposeTable(IContainer container, IReadOnlyList<WeatherForecast> forecasts)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(65); // Date
                columns.RelativeColumn(2); // Location
                columns.RelativeColumn(2); // Temp
                columns.RelativeColumn(1.5f); // Feels like
                columns.RelativeColumn(2); // Summary
                columns.RelativeColumn(2.5f); // Wind
                columns.RelativeColumn(1.5f); // Humidity
                columns.RelativeColumn(1.5f); // Precip
            });

            table.Header(header =>
            {
                foreach (var title in ColumnTitles)
                {
                    header.Cell().Element(HeaderCell).Text(title);
                }
            });

            foreach (var forecast in forecasts)
            {
                table
                    .Cell()
                    .Element(BodyCell)
                    .Text(forecast.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
                table.Cell().Element(BodyCell).Text(forecast.Location);
                table
                    .Cell()
                    .Element(BodyCell)
                    .Text($"{forecast.TemperatureC} °C / {forecast.TemperatureF} °F");
                table
                    .Cell()
                    .Element(BodyCell)
                    .Text(
                        $"{forecast.FeelsLikeC.ToString("0.0", CultureInfo.InvariantCulture)} °C"
                    );
                table.Cell().Element(BodyCell).Text(forecast.Summary ?? "-");
                table
                    .Cell()
                    .Element(BodyCell)
                    .Text($"{forecast.WindSpeedKmh} km/h, {forecast.WindDirectionDegrees}°");
                table.Cell().Element(BodyCell).Text($"{forecast.HumidityPercent} %");
                table.Cell().Element(BodyCell).Text($"{forecast.PrecipitationChancePercent} %");
            }
        });
    }

    private static IContainer HeaderCell(IContainer container) =>
        container
            .Background(Colors.Grey.Lighten3)
            .Padding(4)
            .DefaultTextStyle(style => style.SemiBold());

    private static IContainer BodyCell(IContainer container) =>
        container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4);
}
