// AMS.Application/Services/PdfService.cs
using AMS.Application.DTOs.Attendance;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AMS.Application.Services;

public class PdfService : IPdfService
{
    public byte[] GenerateAttendanceReportPdf(List<AttendanceReportDto> reports, string title)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header()
                    .Text(title)
                    .SemiBold().FontSize(20).FontColor(Colors.Blue.Medium);

                page.Content()
                    .PaddingVertical(1, Unit.Centimetre)
                    .Column(column =>
                    {
                        column.Spacing(5);

                        column.Item().Element(ComposeTable);
                    });

                page.Footer()
                    .AlignCenter()
                    .Text(x =>
                    {
                        x.Span("Generated on: ");
                        x.Span(DateTime.Now.ToString("MMMM dd, yyyy HH:mm")).SemiBold();
                    });

                void ComposeTable(IContainer container)
                {
                    container.Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3); // Student Name
                            columns.RelativeColumn(2); // Reg No
                            columns.RelativeColumn(3); // Course
                            columns.RelativeColumn(1.5f); // Total
                            columns.RelativeColumn(1.5f); // Present
                            columns.RelativeColumn(1.5f); // Absent
                            columns.RelativeColumn(1.5f); // Late
                            columns.RelativeColumn(2); // Percentage
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(CellStyle).Text("Student Name").SemiBold();
                            header.Cell().Element(CellStyle).Text("Reg No").SemiBold();
                            header.Cell().Element(CellStyle).Text("Course").SemiBold();
                            header.Cell().Element(CellStyle).Text("Total").SemiBold();
                            header.Cell().Element(CellStyle).Text("Present").SemiBold();
                            header.Cell().Element(CellStyle).Text("Absent").SemiBold();
                            header.Cell().Element(CellStyle).Text("Late").SemiBold();
                            header.Cell().Element(CellStyle).Text("Percentage").SemiBold();

                            static IContainer CellStyle(IContainer container)
                            {
                                return container
                                    .DefaultTextStyle(x => x.SemiBold())
                                    .PaddingVertical(5)
                                    .BorderBottom(1)
                                    .BorderColor(Colors.Black);
                            }
                        });

                        foreach (var report in reports)
                        {
                            table.Cell().Element(CellStyle).Text(report.StudentName);
                            table.Cell().Element(CellStyle).Text(report.RegistrationNumber);
                            table.Cell().Element(CellStyle).Text(report.CourseName);
                            table.Cell().Element(CellStyle).Text(report.TotalClasses.ToString());
                            table.Cell().Element(CellStyle).Text(report.PresentCount.ToString()).FontColor(Colors.Green.Medium);
                            table.Cell().Element(CellStyle).Text(report.AbsentCount.ToString()).FontColor(Colors.Red.Medium);
                            table.Cell().Element(CellStyle).Text(report.LateCount.ToString()).FontColor(Colors.Orange.Medium);
                            table.Cell().Element(CellStyle).Text($"{report.AttendancePercentage}%")
                                .FontColor(report.AttendancePercentage >= 75 ? Colors.Green.Medium : Colors.Red.Medium);

                            static IContainer CellStyle(IContainer container)
                            {
                                return container
                                    .BorderBottom(1)
                                    .BorderColor(Colors.Grey.Lighten2)
                                    .PaddingVertical(5);
                            }
                        }
                    });
                }
            });
        });

        return document.GeneratePdf();
    }
}