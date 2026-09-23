using OfficeOpenXml;
using OfficeOpenXml.Style;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SaaSPlatform.Application.DTOS.Reports;
using SaaSPlatform.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace SaaSPlatform.Application.Services
{
    public class ReportService : IReportService
    {
        static ReportService()
        {
            // QuestPDF community license (free for small/evaluation projects).
            QuestPDF.Settings.License = LicenseType.Community;
            // EPPlus requires an explicit license selection before a package can be created.
            ExcelPackage.License.SetNonCommercialOrganization("SaaS Organization Platform");
        }

        private readonly IReportRepository _reportRepository;

        public ReportService(IReportRepository reportRepository)
        {
            _reportRepository = reportRepository;
        }

        public async Task<object> GetTenantDashboardAsync(Guid tenantId)
        {
            return await _reportRepository.GetTenantDashboardDataAsync(tenantId);
        }

        public async Task<object> GetSuperAdminDashboardAsync()
        {
            return await _reportRepository.GetSuperAdminDashboardDataAsync();
        }

        public async Task<TenantReportDto> GetTenantReportAsync(Guid tenantId)
        {
            return await _reportRepository.GetTenantReportDataAsync(tenantId);
        }

        public async Task<AdminReportDto> GetAdminReportAsync()
        {
            return await _reportRepository.GetAdminReportDataAsync();
        }

        // ------------------------------------------------------------------
        // Export generation (PDF via QuestPDF, Excel via EPPlus)
        // ------------------------------------------------------------------

        public async Task<ReportExportFileDto> ExportTenantReportPdfAsync(Guid tenantId)
        {
            var (tenantName, report, projects) = await LoadExportDataAsync(tenantId);
            var generatedAt = DateTime.UtcNow;
            var monthlyRows = BuildMonthlyRows(report);
            var metrics = BuildMetrics(report);

            var bytes = Document.Create(document =>
            {
                document.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(28);
                    page.DefaultTextStyle(style => style.FontSize(10));

                    page.Header().Column(header =>
                    {
                        header.Item().Text("Workspace Analytics Report").FontSize(20).Bold();
                        header.Item().Text(tenantName).FontSize(13).FontColor("#9F1239");
                        header.Item().Text($"Generated {generatedAt:yyyy-MM-dd HH:mm} UTC").FontSize(9).FontColor("#6B7280");
                        header.Item().PaddingTop(6).LineHorizontal(1).LineColor("#9F1239");
                    });

                    page.Content().Column(content =>
                    {
                        // ----- Summary -----
                        content.Item().PaddingTop(12).Text("Summary").FontSize(14).Bold();
                        content.Item().PaddingTop(4).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(3);
                                columns.RelativeColumn(2);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Background("#1F2937").Padding(5).Text("Metric").Bold().FontColor("#FFFFFF");
                                header.Cell().Background("#1F2937").Padding(5).Text("Value").Bold().FontColor("#FFFFFF");
                            });

                            foreach (var metric in metrics)
                            {
                                table.Cell().BorderBottom(1).BorderColor("#E5E7EB").Padding(5).Text(metric.Label);
                                table.Cell().BorderBottom(1).BorderColor("#E5E7EB").Padding(5).Text(metric.Value).Bold();
                            }
                        });

                        // ----- Monthly activity -----
                        content.Item().PaddingTop(14).Text("Monthly Activity").FontSize(14).Bold();
                        if (monthlyRows.Count == 0)
                        {
                            content.Item().PaddingTop(4).Text("No project or task activity was recorded for this period.")
                                .FontColor("#6B7280");
                        }
                        else
                        {
                            content.Item().PaddingTop(4).Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(1);
                                    columns.RelativeColumn(1);
                                    columns.RelativeColumn(1);
                                    columns.RelativeColumn(1);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Background("#1F2937").Padding(5).Text("Month").Bold().FontColor("#FFFFFF");
                                    header.Cell().Background("#1F2937").Padding(5).Text("Projects Opened").Bold().FontColor("#FFFFFF");
                                    header.Cell().Background("#1F2937").Padding(5).Text("Tasks Opened").Bold().FontColor("#FFFFFF");
                                    header.Cell().Background("#1F2937").Padding(5).Text("Tasks Closed").Bold().FontColor("#FFFFFF");
                                });

                                foreach (var row in monthlyRows)
                                {
                                    table.Cell().BorderBottom(1).BorderColor("#E5E7EB").Padding(5).Text(row.Label);
                                    table.Cell().BorderBottom(1).BorderColor("#E5E7EB").Padding(5).Text(row.ProjectsOpened.ToString());
                                    table.Cell().BorderBottom(1).BorderColor("#E5E7EB").Padding(5).Text(row.TasksOpened.ToString());
                                    table.Cell().BorderBottom(1).BorderColor("#E5E7EB").Padding(5).Text(row.TasksClosed.ToString());
                                }
                            });
                        }

                        // ----- Projects -----
                        content.Item().PaddingTop(14).Text("Projects").FontSize(14).Bold();
                        if (projects.Count == 0)
                        {
                            content.Item().PaddingTop(4).Text("No projects yet.").FontColor("#6B7280");
                        }
                        else
                        {
                            content.Item().PaddingTop(4).Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(3);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(1);
                                    columns.RelativeColumn(1);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Background("#1F2937").Padding(5).Text("Project").Bold().FontColor("#FFFFFF");
                                    header.Cell().Background("#1F2937").Padding(5).Text("Status").Bold().FontColor("#FFFFFF");
                                    header.Cell().Background("#1F2937").Padding(5).Text("Tasks").Bold().FontColor("#FFFFFF");
                                    header.Cell().Background("#1F2937").Padding(5).Text("Completed").Bold().FontColor("#FFFFFF");
                                });

                                foreach (var project in projects)
                                {
                                    table.Cell().BorderBottom(1).BorderColor("#E5E7EB").Padding(5).Text(project.Name);
                                    table.Cell().BorderBottom(1).BorderColor("#E5E7EB").Padding(5).Text(project.Status);
                                    table.Cell().BorderBottom(1).BorderColor("#E5E7EB").Padding(5).Text(project.TaskCount.ToString());
                                    table.Cell().BorderBottom(1).BorderColor("#E5E7EB").Padding(5).Text(project.CompletedTaskCount.ToString());
                                }
                            });
                        }
                    });

                    page.Footer().AlignCenter().PaddingTop(10)
                        .Text($"{tenantName} — Workspace Analytics Report — {generatedAt:yyyy-MM-dd}")
                        .FontSize(8).FontColor("#9CA3AF");
                });
            }).GeneratePdf();

            return new ReportExportFileDto
            {
                Content = bytes,
                ContentType = "application/pdf",
                FileName = BuildFileName(tenantName, "pdf", generatedAt)
            };
        }

        public async Task<ReportExportFileDto> ExportTenantReportExcelAsync(Guid tenantId)
        {
            var (tenantName, report, projects) = await LoadExportDataAsync(tenantId);
            var generatedAt = DateTime.UtcNow;
            var monthlyRows = BuildMonthlyRows(report);

            using var package = new ExcelPackage();

            // ----- Summary sheet -----
            var summary = package.Workbook.Worksheets.Add("Summary");
            summary.Cells[1, 1].Value = "Workspace Analytics Report";
            summary.Cells[1, 1].Style.Font.Size = 16;
            summary.Cells[1, 1].Style.Font.Bold = true;
            summary.Cells[2, 1].Value = "Workspace";
            summary.Cells[2, 2].Value = tenantName;
            summary.Cells[3, 1].Value = "Generated (UTC)";
            summary.Cells[3, 2].Value = generatedAt.ToString("yyyy-MM-dd HH:mm");

            summary.Cells[5, 1].Value = "Metric";
            summary.Cells[5, 2].Value = "Value";
            summary.Cells[5, 1, 5, 2].Style.Font.Bold = true;
            summary.Cells[5, 1, 5, 2].Style.Fill.PatternType = ExcelFillStyle.Solid;
            summary.Cells[5, 1, 5, 2].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(31, 41, 55));
            summary.Cells[5, 1, 5, 2].Style.Font.Color.SetColor(System.Drawing.Color.White);

            summary.Cells[6, 1].Value = "Total Projects";
            summary.Cells[6, 2].Value = report.TotalProjects;
            summary.Cells[7, 1].Value = "Total Tasks";
            summary.Cells[7, 2].Value = report.TotalTasks;
            summary.Cells[8, 1].Value = "Completed Tasks";
            summary.Cells[8, 2].Value = report.CompletedTasks;
            summary.Cells[9, 1].Value = "Pending Tasks";
            summary.Cells[9, 2].Value = report.PendingTasks;
            summary.Cells[10, 1].Value = "In Progress Tasks";
            summary.Cells[10, 2].Value = report.InProgressTasks;
            summary.Cells[11, 1].Value = "Team Members";
            summary.Cells[11, 2].Value = report.TotalMembers;
            summary.Cells[12, 1].Value = "Avg Tasks / Member";
            summary.Cells[12, 2].Value = report.AvgTasksPerMember;
            summary.Cells[13, 1].Value = "Completion Rate (%)";
            summary.Cells[13, 2].Value = report.CompletionRate;
            summary.Column(1).Width = 26;
            summary.Column(2).Width = 22;

            // ----- Monthly Activity sheet -----
            var monthly = package.Workbook.Worksheets.Add("Monthly Activity");
            monthly.Cells[1, 1].Value = "Month";
            monthly.Cells[1, 2].Value = "Projects Opened";
            monthly.Cells[1, 3].Value = "Tasks Opened";
            monthly.Cells[1, 4].Value = "Tasks Closed";
            monthly.Cells[1, 1, 1, 4].Style.Font.Bold = true;
            monthly.Cells[1, 1, 1, 4].Style.Fill.PatternType = ExcelFillStyle.Solid;
            monthly.Cells[1, 1, 1, 4].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(31, 41, 55));
            monthly.Cells[1, 1, 1, 4].Style.Font.Color.SetColor(System.Drawing.Color.White);

            if (monthlyRows.Count == 0)
            {
                monthly.Cells[2, 1].Value = "No project or task activity was recorded for this period.";
            }
            else
            {
                for (var i = 0; i < monthlyRows.Count; i++)
                {
                    var row = monthlyRows[i];
                    monthly.Cells[i + 2, 1].Value = row.Label;
                    monthly.Cells[i + 2, 2].Value = row.ProjectsOpened;
                    monthly.Cells[i + 2, 3].Value = row.TasksOpened;
                    monthly.Cells[i + 2, 4].Value = row.TasksClosed;
                }
            }
            monthly.Column(1).Width = 18;
            monthly.Column(2).Width = 18;
            monthly.Column(3).Width = 16;
            monthly.Column(4).Width = 16;

            // ----- Projects sheet -----
            var projectsSheet = package.Workbook.Worksheets.Add("Projects");
            projectsSheet.Cells[1, 1].Value = "Project";
            projectsSheet.Cells[1, 2].Value = "Status";
            projectsSheet.Cells[1, 3].Value = "Tasks";
            projectsSheet.Cells[1, 4].Value = "Completed";
            projectsSheet.Cells[1, 1, 1, 4].Style.Font.Bold = true;
            projectsSheet.Cells[1, 1, 1, 4].Style.Fill.PatternType = ExcelFillStyle.Solid;
            projectsSheet.Cells[1, 1, 1, 4].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(31, 41, 55));
            projectsSheet.Cells[1, 1, 1, 4].Style.Font.Color.SetColor(System.Drawing.Color.White);

            if (projects.Count == 0)
            {
                projectsSheet.Cells[2, 1].Value = "No projects yet.";
            }
            else
            {
                for (var i = 0; i < projects.Count; i++)
                {
                    var project = projects[i];
                    projectsSheet.Cells[i + 2, 1].Value = project.Name;
                    projectsSheet.Cells[i + 2, 2].Value = project.Status;
                    projectsSheet.Cells[i + 2, 3].Value = project.TaskCount;
                    projectsSheet.Cells[i + 2, 4].Value = project.CompletedTaskCount;
                }
            }
            projectsSheet.Column(1).Width = 32;
            projectsSheet.Column(2).Width = 16;
            projectsSheet.Column(3).Width = 10;
            projectsSheet.Column(4).Width = 12;

            return new ReportExportFileDto
            {
                Content = package.GetAsByteArray(),
                ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                FileName = BuildFileName(tenantName, "xlsx", generatedAt)
            };
        }

        public async Task<ReportExportFileDto> ExportAdminReportPdfAsync()
        {
            var report = await _reportRepository.GetAdminReportDataAsync();
            var tenants = await _reportRepository.GetAdminTenantBreakdownAsync();
            var generatedAt = DateTime.UtcNow;

            var bytes = Document.Create(document =>
            {
                document.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(28);
                    page.DefaultTextStyle(style => style.FontSize(10));

                    page.Header().Column(header =>
                    {
                        header.Item().Text("Platform Executive Analytics").FontSize(20).Bold();
                        header.Item().Text("Global SaaS Operations & Multi-Tenant Performance").FontSize(13).FontColor("#4F46E5");
                        header.Item().Text($"Generated {generatedAt:yyyy-MM-dd HH:mm} UTC").FontSize(9).FontColor("#6B7280");
                        header.Item().PaddingTop(6).LineHorizontal(1).LineColor("#4F46E5");
                    });

                    page.Content().Column(content =>
                    {
                        // ----- Platform Executive Summary -----
                        content.Item().PaddingTop(12).Text("Platform Executive Summary").FontSize(14).Bold();
                        content.Item().PaddingTop(4).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(3);
                                columns.RelativeColumn(2);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Background("#1F2937").Padding(5).Text("Metric").Bold().FontColor("#FFFFFF");
                                header.Cell().Background("#1F2937").Padding(5).Text("Value").Bold().FontColor("#FFFFFF");
                            });

                            var metrics = new List<(string Label, string Value)>
                            {
                                ("Total Organizations (Tenants)", report.TotalTenants.ToString(CultureInfo.InvariantCulture)),
                                ("Total Registered Users", report.TotalUsers.ToString(CultureInfo.InvariantCulture)),
                                ("Avg Customer Lifetime", report.AvgLifetimeMonths.ToString("0.0", CultureInfo.InvariantCulture) + " Months"),
                                ("Customer Acquisition Cost (CAC)", "$" + report.CustomerAcquisitionCost.ToString("N2", CultureInfo.InvariantCulture)),
                                ("Organization Churn Rate", report.ChurnRate.ToString("0.0", CultureInfo.InvariantCulture) + "%")
                            };

                            foreach (var metric in metrics)
                            {
                                table.Cell().BorderBottom(1).BorderColor("#E5E7EB").Padding(5).Text(metric.Label);
                                table.Cell().BorderBottom(1).BorderColor("#E5E7EB").Padding(5).Text(metric.Value).Bold();
                            }
                        });

                        // ----- Quarterly Tenant Growth -----
                        content.Item().PaddingTop(14).Text("Quarterly Organization Registrations").FontSize(14).Bold();
                        if (report.QuarterlyTenants == null || report.QuarterlyTenants.Count == 0)
                        {
                            content.Item().PaddingTop(4).Text("No organization registrations recorded in the last 4 quarters.").FontColor("#6B7280");
                        }
                        else
                        {
                            content.Item().PaddingTop(4).Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(1);
                                    columns.RelativeColumn(2);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Background("#1F2937").Padding(5).Text("Period").Bold().FontColor("#FFFFFF");
                                    header.Cell().Background("#1F2937").Padding(5).Text("Year").Bold().FontColor("#FFFFFF");
                                    header.Cell().Background("#1F2937").Padding(5).Text("New Organizations").Bold().FontColor("#FFFFFF");
                                });

                                foreach (var q in report.QuarterlyTenants)
                                {
                                    table.Cell().BorderBottom(1).BorderColor("#E5E7EB").Padding(5).Text($"Q{q.Quarter} {q.Year}");
                                    table.Cell().BorderBottom(1).BorderColor("#E5E7EB").Padding(5).Text(q.Year.ToString());
                                    table.Cell().BorderBottom(1).BorderColor("#E5E7EB").Padding(5).Text(q.Count.ToString());
                                }
                            });
                        }

                        // ----- Monthly User Registrations -----
                        content.Item().PaddingTop(14).Text("Monthly User Registrations").FontSize(14).Bold();
                        if (report.MonthlyUsers == null || report.MonthlyUsers.Count == 0)
                        {
                            content.Item().PaddingTop(4).Text("No user registrations recorded in the last 6 months.").FontColor("#6B7280");
                        }
                        else
                        {
                            content.Item().PaddingTop(4).Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(1);
                                    columns.RelativeColumn(2);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Background("#1F2937").Padding(5).Text("Month").Bold().FontColor("#FFFFFF");
                                    header.Cell().Background("#1F2937").Padding(5).Text("Year").Bold().FontColor("#FFFFFF");
                                    header.Cell().Background("#1F2937").Padding(5).Text("New Users").Bold().FontColor("#FFFFFF");
                                });

                                foreach (var m in report.MonthlyUsers)
                                {
                                    var monthName = CultureInfo.InvariantCulture.DateTimeFormat.GetAbbreviatedMonthName(Math.Clamp(m.Month, 1, 12));
                                    table.Cell().BorderBottom(1).BorderColor("#E5E7EB").Padding(5).Text(monthName);
                                    table.Cell().BorderBottom(1).BorderColor("#E5E7EB").Padding(5).Text(m.Year.ToString());
                                    table.Cell().BorderBottom(1).BorderColor("#E5E7EB").Padding(5).Text(m.Count.ToString());
                                }
                            });
                        }

                        // ----- Organizations Breakdown -----
                        content.Item().PaddingTop(14).Text("Registered Organizations").FontSize(14).Bold();
                        if (tenants == null || tenants.Count == 0)
                        {
                            content.Item().PaddingTop(4).Text("No organizations found.").FontColor("#6B7280");
                        }
                        else
                        {
                            content.Item().PaddingTop(4).Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(3);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(1);
                                    columns.RelativeColumn(1);
                                    columns.RelativeColumn(2);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Background("#1F2937").Padding(5).Text("Organization").Bold().FontColor("#FFFFFF");
                                    header.Cell().Background("#1F2937").Padding(5).Text("Domain").Bold().FontColor("#FFFFFF");
                                    header.Cell().Background("#1F2937").Padding(5).Text("Plan").Bold().FontColor("#FFFFFF");
                                    header.Cell().Background("#1F2937").Padding(5).Text("Status").Bold().FontColor("#FFFFFF");
                                    header.Cell().Background("#1F2937").Padding(5).Text("Users").Bold().FontColor("#FFFFFF");
                                    header.Cell().BorderBottom(1).BorderColor("#E5E7EB").Background("#1F2937").Padding(5).Text("Created").Bold().FontColor("#FFFFFF");
                                });

                                foreach (var tenant in tenants)
                                {
                                    table.Cell().BorderBottom(1).BorderColor("#E5E7EB").Padding(5).Text(tenant.Name);
                                    table.Cell().BorderBottom(1).BorderColor("#E5E7EB").Padding(5).Text(tenant.Domain);
                                    table.Cell().BorderBottom(1).BorderColor("#E5E7EB").Padding(5).Text(tenant.PlanName);
                                    table.Cell().BorderBottom(1).BorderColor("#E5E7EB").Padding(5).Text(tenant.IsActive ? "Active" : "Inactive");
                                    table.Cell().BorderBottom(1).BorderColor("#E5E7EB").Padding(5).Text(tenant.UserCount.ToString());
                                    table.Cell().BorderBottom(1).BorderColor("#E5E7EB").Padding(5).Text(tenant.CreatedAt.ToString("yyyy-MM-dd"));
                                }
                            });
                        }
                    });

                    page.Footer().AlignCenter().PaddingTop(10)
                        .Text($"SaaS Platform — Executive Analytics Report — {generatedAt:yyyy-MM-dd}")
                        .FontSize(8).FontColor("#9CA3AF");
                });
            }).GeneratePdf();

            return new ReportExportFileDto
            {
                Content = bytes,
                ContentType = "application/pdf",
                FileName = $"platform-analytics_executive_{generatedAt:yyyyMMdd}.pdf"
            };
        }

        public async Task<ReportExportFileDto> ExportAdminReportExcelAsync()
        {
            var report = await _reportRepository.GetAdminReportDataAsync();
            var tenants = await _reportRepository.GetAdminTenantBreakdownAsync();
            var generatedAt = DateTime.UtcNow;

            using var package = new ExcelPackage();

            // ----- Summary sheet -----
            var summary = package.Workbook.Worksheets.Add("Platform Summary");
            summary.Cells[1, 1].Value = "Platform Executive Analytics Report";
            summary.Cells[1, 1].Style.Font.Size = 16;
            summary.Cells[1, 1].Style.Font.Bold = true;
            summary.Cells[2, 1].Value = "Generated (UTC)";
            summary.Cells[2, 2].Value = generatedAt.ToString("yyyy-MM-dd HH:mm");

            summary.Cells[4, 1].Value = "Metric";
            summary.Cells[4, 2].Value = "Value";
            summary.Cells[4, 1, 4, 2].Style.Font.Bold = true;
            summary.Cells[4, 1, 4, 2].Style.Fill.PatternType = ExcelFillStyle.Solid;
            summary.Cells[4, 1, 4, 2].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(31, 41, 55));
            summary.Cells[4, 1, 4, 2].Style.Font.Color.SetColor(System.Drawing.Color.White);

            summary.Cells[5, 1].Value = "Total Organizations";
            summary.Cells[5, 2].Value = report.TotalTenants;
            summary.Cells[6, 1].Value = "Total Users";
            summary.Cells[6, 2].Value = report.TotalUsers;
            summary.Cells[7, 1].Value = "Average Customer Lifetime (Months)";
            summary.Cells[7, 2].Value = report.AvgLifetimeMonths;
            summary.Cells[8, 1].Value = "Customer Acquisition Cost ($)";
            summary.Cells[8, 2].Value = report.CustomerAcquisitionCost;
            summary.Cells[9, 1].Value = "Organization Churn Rate (%)";
            summary.Cells[9, 2].Value = report.ChurnRate;

            summary.Column(1).Width = 34;
            summary.Column(2).Width = 22;

            // ----- Quarterly Growth sheet -----
            var quarterly = package.Workbook.Worksheets.Add("Quarterly Growth");
            quarterly.Cells[1, 1].Value = "Quarter";
            quarterly.Cells[1, 2].Value = "Year";
            quarterly.Cells[1, 3].Value = "New Organizations";
            quarterly.Cells[1, 1, 1, 3].Style.Font.Bold = true;
            quarterly.Cells[1, 1, 1, 3].Style.Fill.PatternType = ExcelFillStyle.Solid;
            quarterly.Cells[1, 1, 1, 3].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(31, 41, 55));
            quarterly.Cells[1, 1, 1, 3].Style.Font.Color.SetColor(System.Drawing.Color.White);

            if (report.QuarterlyTenants == null || report.QuarterlyTenants.Count == 0)
            {
                quarterly.Cells[2, 1].Value = "No organization registrations recorded in the last 4 quarters.";
            }
            else
            {
                for (var i = 0; i < report.QuarterlyTenants.Count; i++)
                {
                    var q = report.QuarterlyTenants[i];
                    quarterly.Cells[i + 2, 1].Value = $"Q{q.Quarter}";
                    quarterly.Cells[i + 2, 2].Value = q.Year;
                    quarterly.Cells[i + 2, 3].Value = q.Count;
                }
            }
            quarterly.Column(1).Width = 16;
            quarterly.Column(2).Width = 12;
            quarterly.Column(3).Width = 20;

            // ----- Monthly Users sheet -----
            var monthly = package.Workbook.Worksheets.Add("Monthly Users");
            monthly.Cells[1, 1].Value = "Month";
            monthly.Cells[1, 2].Value = "Year";
            monthly.Cells[1, 3].Value = "New Users";
            monthly.Cells[1, 1, 1, 3].Style.Font.Bold = true;
            monthly.Cells[1, 1, 1, 3].Style.Fill.PatternType = ExcelFillStyle.Solid;
            monthly.Cells[1, 1, 1, 3].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(31, 41, 55));
            monthly.Cells[1, 1, 1, 3].Style.Font.Color.SetColor(System.Drawing.Color.White);

            if (report.MonthlyUsers == null || report.MonthlyUsers.Count == 0)
            {
                monthly.Cells[2, 1].Value = "No user registrations recorded in the last 6 months.";
            }
            else
            {
                for (var i = 0; i < report.MonthlyUsers.Count; i++)
                {
                    var m = report.MonthlyUsers[i];
                    var monthName = CultureInfo.InvariantCulture.DateTimeFormat.GetAbbreviatedMonthName(Math.Clamp(m.Month, 1, 12));
                    monthly.Cells[i + 2, 1].Value = monthName;
                    monthly.Cells[i + 2, 2].Value = m.Year;
                    monthly.Cells[i + 2, 3].Value = m.Count;
                }
            }
            monthly.Column(1).Width = 16;
            monthly.Column(2).Width = 12;
            monthly.Column(3).Width = 16;

            // ----- Organizations sheet -----
            var orgs = package.Workbook.Worksheets.Add("Organizations");
            orgs.Cells[1, 1].Value = "Organization Name";
            orgs.Cells[1, 2].Value = "Domain";
            orgs.Cells[1, 3].Value = "Subscription Plan";
            orgs.Cells[1, 4].Value = "Status";
            orgs.Cells[1, 5].Value = "Users";
            orgs.Cells[1, 6].Value = "Projects";
            orgs.Cells[1, 7].Value = "Created (UTC)";
            orgs.Cells[1, 1, 1, 7].Style.Font.Bold = true;
            orgs.Cells[1, 1, 1, 7].Style.Fill.PatternType = ExcelFillStyle.Solid;
            orgs.Cells[1, 1, 1, 7].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(31, 41, 55));
            orgs.Cells[1, 1, 1, 7].Style.Font.Color.SetColor(System.Drawing.Color.White);

            if (tenants == null || tenants.Count == 0)
            {
                orgs.Cells[2, 1].Value = "No organizations found.";
            }
            else
            {
                for (var i = 0; i < tenants.Count; i++)
                {
                    var t = tenants[i];
                    orgs.Cells[i + 2, 1].Value = t.Name;
                    orgs.Cells[i + 2, 2].Value = t.Domain;
                    orgs.Cells[i + 2, 3].Value = t.PlanName;
                    orgs.Cells[i + 2, 4].Value = t.IsActive ? "Active" : "Inactive";
                    orgs.Cells[i + 2, 5].Value = t.UserCount;
                    orgs.Cells[i + 2, 6].Value = t.ProjectCount;
                    orgs.Cells[i + 2, 7].Value = t.CreatedAt.ToString("yyyy-MM-dd HH:mm");
                }
            }
            orgs.Column(1).Width = 28;
            orgs.Column(2).Width = 24;
            orgs.Column(3).Width = 18;
            orgs.Column(4).Width = 14;
            orgs.Column(5).Width = 12;
            orgs.Column(6).Width = 12;
            orgs.Column(7).Width = 20;

            return new ReportExportFileDto
            {
                Content = package.GetAsByteArray(),
                ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                FileName = $"platform-analytics_executive_{generatedAt:yyyyMMdd}.xlsx"
            };
        }

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------

        private async Task<(string TenantName, TenantReportDto Report, List<TenantProjectBreakdownDto> Projects)> LoadExportDataAsync(Guid tenantId)
        {
            var report = await _reportRepository.GetTenantReportDataAsync(tenantId);
            var tenantName = await _reportRepository.GetTenantNameAsync(tenantId);
            var projects = await _reportRepository.GetTenantProjectBreakdownAsync(tenantId);

            if (string.IsNullOrWhiteSpace(tenantName))
                tenantName = "Workspace";

            return (tenantName, report, projects);
        }

        private static List<(string Label, string Value)> BuildMetrics(TenantReportDto report)
        {
            var culture = CultureInfo.InvariantCulture;
            return new List<(string Label, string Value)>
            {
                ("Total Projects", report.TotalProjects.ToString(culture)),
                ("Total Tasks", report.TotalTasks.ToString(culture)),
                ("Completed Tasks", report.CompletedTasks.ToString(culture)),
                ("Pending Tasks", report.PendingTasks.ToString(culture)),
                ("In Progress Tasks", report.InProgressTasks.ToString(culture)),
                ("Team Members", report.TotalMembers.ToString(culture)),
                ("Avg Tasks / Member", report.AvgTasksPerMember.ToString(culture)),
                ("Completion Rate", report.CompletionRate.ToString(culture) + "%")
            };
        }

        private sealed class MonthlyExportRow
        {
            public int Year { get; set; }
            public int Month { get; set; }
            public int ProjectsOpened { get; set; }
            public int TasksOpened { get; set; }
            public int TasksClosed { get; set; }
            public string Label =>
                CultureInfo.InvariantCulture.DateTimeFormat.GetAbbreviatedMonthName(Month) + " " + Year;
        }

        private static List<MonthlyExportRow> BuildMonthlyRows(TenantReportDto report)
        {
            var rows = new SortedDictionary<(int Year, int Month), MonthlyExportRow>();

            void Merge(IEnumerable<MonthlyStatDto> stats, Action<MonthlyExportRow> add)
            {
                foreach (var stat in stats)
                {
                    var key = (stat.Year, stat.Month);
                    if (!rows.TryGetValue(key, out var row))
                    {
                        row = new MonthlyExportRow { Year = stat.Year, Month = stat.Month };
                        rows[key] = row;
                    }
                    add(row);
                }
            }

            Merge(report.MonthlyProjects, r => r.ProjectsOpened++);
            Merge(report.MonthlyTasksCreated, r => r.TasksOpened++);
            Merge(report.MonthlyTasksCompleted, r => r.TasksClosed++);

            return rows.Values.ToList();
        }

        private static string BuildFileName(string tenantName, string extension, DateTime generatedAt)
        {
            var safe = new string((string.IsNullOrWhiteSpace(tenantName) ? "workspace" : tenantName)
                .Where(c => char.IsLetterOrDigit(c) || c == ' ' || c == '-' || c == '_')
                .ToArray())
                .Trim()
                .Replace(' ', '-');

            if (safe.Length == 0)
                safe = "workspace";

            return $"workspace-analytics_{safe}_{generatedAt:yyyyMMdd}.{extension}";
        }
    }
}
