using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Dtos.Account;
using api.Dtos.Borrow;
using api.Interfaces;
using api.Models;
using Hangfire;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QuestPDF;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace api.Services
{
    public class ReportService : IReportService
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IBorrowService _borrowService;
        private readonly IReturnRequestService _returnRequestService;
        private readonly INotificationService _notifications;
        private readonly IEmailService _emailService;
        private readonly ILogger<ReportService> _logger;

        public ReportService(
            UserManager<AppUser> userManager,
            IBorrowService borrowService,
            IReturnRequestService returnRequestService,
            INotificationService notifications,
            IEmailService emailService,
            ILogger<ReportService> logger)
        {
            _borrowService = borrowService;
            _returnRequestService = returnRequestService;
            _userManager = userManager;
            _notifications = notifications;
            _emailService = emailService;
            _logger = logger;
        }

        public async System.Threading.Tasks.Task GeneratePdf(UserDtoWithID user)
        {
            var pastDueBorrows = await _borrowService.GetPastDueBorrowsAsync(user.Id);
            var allActiveBorrows = await _borrowService.GetActiveBorrowsAsync(user.Id);
            // Filter out past due books from active borrows to avoid duplication
            var activeBorrows = allActiveBorrows.Where(b => b.Status == BorrowStatus.Active);
            var recentlyReturned = await _borrowService.GetRecentlyReturnedBorrowsAsync(user.Id, 7);
            var pendingReturnRequests = await _returnRequestService.GetPendingReturnRequestsAsync(user.Id);

            Settings.License = LicenseType.Community;
            Settings.EnableDebugging = true;

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                    page.Header()
                        .Height(80)
                        .Background(Colors.Blue.Darken2)
                        .AlignCenter()
                        .AlignMiddle()
                        .Text(text =>
                        {
                            text.DefaultTextStyle(x => x.FontColor(Colors.White));
                            text.AlignCenter();
                            text.Span("📚 Weekly Library Report").FontSize(24).Bold();
                            text.EmptyLine();
                            text.Span($"For: {user.Username}").FontSize(14);
                        });

                    page.Content()
                        .PaddingVertical(20)
                        .Column(column =>
                        {
                            // User Info Section
                            column.Item().Column(col =>
                            {
                                col.Item().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingBottom(10).Row(row =>
                                {
                                    row.RelativeItem().Column(userCol =>
                                    {
                                        userCol.Item().Text($"👤 {user.Username ?? "Unknown User"}").FontSize(16).Bold().FontColor(Colors.Blue.Darken2);
                                        userCol.Item().Text($"📧 {user.Email ?? "No Email"}").FontSize(12).FontColor(Colors.Grey.Darken1);
                                    });
                                    row.ConstantItem(150).AlignRight().Text($"📅 {DateTime.Now:MMM dd, yyyy}").FontSize(12).FontColor(Colors.Grey.Darken1);
                                });
                                col.Item().PaddingTop(20);
                            });

                            // Summary Cards
                            column.Item().PaddingBottom(20).Row(summaryRow =>
                            {
                                summaryRow.RelativeItem(1).Padding(5).Border(1).BorderColor(Colors.Red.Lighten3).CornerRadius(8).Column(summaryCol =>
                                {
                                    summaryCol.Item().AlignCenter().Text("⚠️").FontSize(24);
                                    summaryCol.Item().AlignCenter().Text($"{pastDueBorrows?.Count() ?? 0}").FontSize(20).Bold().FontColor(Colors.Red.Medium);
                                    summaryCol.Item().AlignCenter().Text("Overdue").FontSize(10).FontColor(Colors.Red.Medium);
                                });

                                summaryRow.RelativeItem(1).Padding(5).Border(1).BorderColor(Colors.Blue.Lighten3).CornerRadius(8).Column(summaryCol =>
                                {
                                    summaryCol.Item().AlignCenter().Text("📖").FontSize(24);
                                    summaryCol.Item().AlignCenter().Text($"{activeBorrows?.Count() ?? 0}").FontSize(20).Bold().FontColor(Colors.Blue.Medium);
                                    summaryCol.Item().AlignCenter().Text("Active").FontSize(10).FontColor(Colors.Blue.Medium);
                                });

                                summaryRow.RelativeItem(1).Padding(5).Border(1).BorderColor(Colors.Orange.Lighten3).CornerRadius(8).Column(summaryCol =>
                                {
                                    summaryCol.Item().AlignCenter().Text("🔄").FontSize(24);
                                    summaryCol.Item().AlignCenter().Text($"{pendingReturnRequests?.Count() ?? 0}").FontSize(20).Bold().FontColor(Colors.Orange.Medium);
                                    summaryCol.Item().AlignCenter().Text("Pending Returns").FontSize(10).FontColor(Colors.Orange.Medium);
                                });

                                summaryRow.RelativeItem(1).Padding(5).Border(1).BorderColor(Colors.Green.Lighten3).CornerRadius(8).Column(summaryCol =>
                                {
                                    summaryCol.Item().AlignCenter().Text("✅").FontSize(24);
                                    summaryCol.Item().AlignCenter().Text($"{recentlyReturned?.Count() ?? 0}").FontSize(20).Bold().FontColor(Colors.Green.Medium);
                                    summaryCol.Item().AlignCenter().Text("Returned").FontSize(10).FontColor(Colors.Green.Medium);
                                });
                            });

                            // Past Due Borrows Section
                            column.Item().Column(pastDueCol =>
                            {
                                pastDueCol.Item().PaddingBottom(15).Row(row =>
                                {
                                    row.RelativeItem().Text("⚠️ Overdue Books").FontSize(18).Bold().FontColor(Colors.Red.Medium);
                                    row.ConstantItem(80).AlignRight().Text($"({pastDueBorrows?.Count() ?? 0})").FontSize(14).FontColor(Colors.Red.Medium).Bold();
                                });

                                if (pastDueBorrows?.Any() == true)
                                {
                                    var limitedPastDue = pastDueBorrows.Take(8);
                                    foreach (var borrow in limitedPastDue)
                                    {
                                        pastDueCol.Item().PaddingBottom(10).Border(1).BorderColor(Colors.Red.Lighten3).CornerRadius(8).Padding(12).Column(borrowCol =>
                                        {
                                            borrowCol.Item().Row(borrowRow =>
                                            {
                                                borrowRow.RelativeItem().Text(TruncateText(borrow.BookTitle, 50)).FontSize(14).Bold().FontColor(Colors.Red.Darken1);
                                                borrowRow.ConstantItem(120).AlignRight().Column(dateCol =>
                                                {
                                                    dateCol.Item().Text($"Due: {borrow.DueDate:MMM dd}").FontSize(10).FontColor(Colors.Red.Medium);
                                                    var daysOverdue = (DateTime.Now - borrow.DueDate).Days;
                                                    dateCol.Item().Text($"{daysOverdue} days overdue").FontSize(9).Bold().FontColor(Colors.Red.Darken1);
                                                });
                                            });

                                            borrowCol.Item().PaddingTop(5).Row(statusRow =>
                                            {
                                                statusRow.RelativeItem().Text($"Borrowed: {borrow.StartDate:MMM dd, yyyy}").FontSize(10).FontColor(Colors.Grey.Darken1);
                                                if (borrow.HasPendingReturnRequest)
                                                {
                                                    statusRow.ConstantItem(120).AlignRight().Text("Return Requested").FontSize(9).FontColor(Colors.Orange.Medium).Bold();
                                                }
                                            });
                                        });
                                    }

                                    if (pastDueBorrows.Count() > 8)
                                    {
                                        pastDueCol.Item().PaddingTop(10).AlignCenter()
                                            .Text($"... and {pastDueBorrows.Count() - 8} more overdue books")
                                            .FontSize(10).Italic().FontColor(Colors.Grey.Medium);
                                    }
                                }
                                else
                                {
                                    pastDueCol.Item().Padding(20).AlignCenter().Column(emptyCol =>
                                    {
                                        emptyCol.Item().Text("🎉").FontSize(32).AlignCenter();
                                        emptyCol.Item().PaddingTop(5).Text("Excellent! No overdue books.")
                                            .FontSize(12).FontColor(Colors.Green.Medium).AlignCenter();
                                    });
                                }

                                pastDueCol.Item().PaddingTop(20);
                            });

                            // Active Borrows Section
                            column.Item().Column(activeCol =>
                            {
                                activeCol.Item().PaddingBottom(15).Row(row =>
                                {
                                    row.RelativeItem().Text("📖 Currently Borrowed").FontSize(18).Bold().FontColor(Colors.Blue.Medium);
                                    row.ConstantItem(80).AlignRight().Text($"({activeBorrows?.Count() ?? 0})").FontSize(14).FontColor(Colors.Blue.Medium).Bold();
                                });

                                if (activeBorrows?.Any() == true)
                                {
                                    var limitedActive = activeBorrows.OrderBy(b => b.DueDate).Take(8);
                                    foreach (var borrow in limitedActive)
                                    {
                                        var daysLeft = (borrow.DueDate - DateTime.Now).Days;
                                        var dueSoonColor = daysLeft <= 2 ? Colors.Orange.Medium : Colors.Blue.Medium;

                                        activeCol.Item().PaddingBottom(10).Border(1).BorderColor(Colors.Blue.Lighten3).CornerRadius(8).Padding(12).Column(borrowCol =>
                                        {
                                            borrowCol.Item().Row(borrowRow =>
                                            {
                                                borrowRow.RelativeItem().Text(TruncateText(borrow.BookTitle, 50)).FontSize(14).Bold().FontColor(Colors.Blue.Darken1);
                                                borrowRow.ConstantItem(120).AlignRight().Column(dateCol =>
                                                {
                                                    dateCol.Item().Text($"Due: {borrow.DueDate:MMM dd}").FontSize(10).FontColor(dueSoonColor);
                                                    if (daysLeft <= 2)
                                                    {
                                                        dateCol.Item().Text($"Due in {daysLeft} days!").FontSize(9).Bold().FontColor(Colors.Orange.Darken1);
                                                    }
                                                    else
                                                    {
                                                        dateCol.Item().Text($"{daysLeft} days left").FontSize(9).FontColor(Colors.Blue.Medium);
                                                    }
                                                });
                                            });

                                            borrowCol.Item().PaddingTop(5).Row(statusRow =>
                                            {
                                                statusRow.RelativeItem().Text($"Borrowed: {borrow.StartDate:MMM dd, yyyy}").FontSize(10).FontColor(Colors.Grey.Darken1);
                                                if (borrow.HasPendingReturnRequest)
                                                {
                                                    statusRow.ConstantItem(120).AlignRight().Text("Return Requested").FontSize(9).FontColor(Colors.Orange.Medium).Bold();
                                                }
                                            });
                                        });
                                    }

                                    if (activeBorrows.Count() > 8)
                                    {
                                        activeCol.Item().PaddingTop(10).AlignCenter()
                                            .Text($"... and {activeBorrows.Count() - 8} more active borrows")
                                            .FontSize(10).Italic().FontColor(Colors.Grey.Medium);
                                    }
                                }
                                else
                                {
                                    activeCol.Item().Padding(20).AlignCenter().Column(emptyCol =>
                                    {
                                        emptyCol.Item().Text("📚").FontSize(32).AlignCenter();
                                        emptyCol.Item().PaddingTop(5).Text("No books currently borrowed. Time to visit the library!")
                                            .FontSize(12).FontColor(Colors.Blue.Medium).AlignCenter();
                                    });
                                }

                                activeCol.Item().PaddingTop(20);
                            });

                            // Recently Returned Section
                            column.Item().Column(returnedCol =>
                            {
                                returnedCol.Item().PaddingBottom(15).Row(row =>
                                {
                                    row.RelativeItem().Text("✅ Recently Returned (Last 7 Days)").FontSize(18).Bold().FontColor(Colors.Green.Medium);
                                    row.ConstantItem(80).AlignRight().Text($"({recentlyReturned?.Count() ?? 0})").FontSize(14).FontColor(Colors.Green.Medium).Bold();
                                });

                                if (recentlyReturned?.Any() == true)
                                {
                                    var limitedReturned = recentlyReturned.Take(6);
                                    foreach (var borrow in limitedReturned)
                                    {
                                        var wasLate = borrow.Status == BorrowStatus.ReturnedPastDue;
                                        var statusColor = wasLate ? Colors.Orange.Medium : Colors.Green.Medium;

                                        returnedCol.Item().PaddingBottom(10).Border(1).BorderColor(Colors.Green.Lighten3).CornerRadius(8).Padding(12).Column(borrowCol =>
                                        {
                                            borrowCol.Item().Row(borrowRow =>
                                            {
                                                borrowRow.RelativeItem().Text(TruncateText(borrow.BookTitle, 50)).FontSize(14).Bold().FontColor(Colors.Green.Darken1);
                                                borrowRow.ConstantItem(120).AlignRight().Text($"Returned: {borrow.ReturnDate:MMM dd}").FontSize(10).FontColor(statusColor);
                                            });

                                            borrowCol.Item().PaddingTop(5).Row(statusRow =>
                                            {
                                                statusRow.RelativeItem().Text($"Borrowed: {borrow.StartDate:MMM dd} - Due: {borrow.DueDate:MMM dd}").FontSize(10).FontColor(Colors.Grey.Darken1);
                                                if (wasLate)
                                                {
                                                    statusRow.ConstantItem(80).AlignRight().Text("Late Return").FontSize(9).FontColor(Colors.Orange.Medium).Bold();
                                                }
                                                else
                                                {
                                                    statusRow.ConstantItem(80).AlignRight().Text("On Time").FontSize(9).FontColor(Colors.Green.Medium).Bold();
                                                }
                                            });
                                        });
                                    }

                                    if (recentlyReturned.Count() > 6)
                                    {
                                        returnedCol.Item().PaddingTop(10).AlignCenter()
                                            .Text($"... and {recentlyReturned.Count() - 6} more returned books")
                                            .FontSize(10).Italic().FontColor(Colors.Grey.Medium);
                                    }
                                }
                                else
                                {
                                    returnedCol.Item().Padding(20).AlignCenter().Column(emptyCol =>
                                    {
                                        emptyCol.Item().Text("📋").FontSize(32).AlignCenter();
                                        emptyCol.Item().PaddingTop(5).Text("No books returned this week.")
                                            .FontSize(12).FontColor(Colors.Grey.Medium).AlignCenter();
                                    });
                                }
                            });
                        });

                    page.Footer()
                        .Height(50)
                        .Background(Colors.Grey.Lighten4)
                        .AlignCenter()
                        .AlignMiddle()
                        .Text(text =>
                        {
                            text.AlignCenter();
                            text.Span("Generated on ").FontSize(9).FontColor(Colors.Grey.Darken1);
                            text.Span($"{DateTime.Now:yyyy-MM-dd HH:mm:ss}").FontSize(9).Bold().FontColor(Colors.Grey.Darken2);
                            text.EmptyLine();
                            text.Span("📚 Keep reading, keep learning! Return books on time to avoid late fees.").FontSize(9).FontColor(Colors.Grey.Darken1);
                        });
                });
            });

            try
            {
                var pdfBytes = document.GeneratePdf();
                var fileName = $"LibraryReport_{user.Username}_{DateTime.Now:yyyyMMdd}.pdf";
                var tempPath = Path.Combine(Path.GetTempPath(), fileName);
                await File.WriteAllBytesAsync(tempPath, pdfBytes);

                // Only send email if user has a valid email address
                if (!string.IsNullOrEmpty(user.Email))
                {
                    await _notifications.SendPrivateMessageAsync(user.Id, "Your library report is ready! Check your email");
                    BackgroundJob.Enqueue<IEmailService>(x => x.SendReportEmailAsync(user.Email, user.Username, tempPath));
                }
                else
                {
                    _logger.LogWarning("Cannot send report email for user {Username} - no email address", user.Username);
                    await _notifications.SendPrivateMessageAsync(user.Id, "Your library report is ready! (Email not sent - no email address on file)");
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to generate PDF for user {user.Username}: {ex.Message}", ex);
            }
        }

        private static string TruncateText(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
                return text ?? string.Empty;

            return text.Substring(0, maxLength - 3) + "...";
        }

        public async System.Threading.Tasks.Task QueueWeeklyReports()
        {
            try
            {
                var allUsers = await _userManager.Users.Select(u => new UserDtoWithID
                {
                    Id = u.Id,
                    Email = u.Email,
                    Username = u.UserName
                }).ToListAsync();

                foreach (var user in allUsers)
                {
                    try
                    {
                        await GeneratePdf(user);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("Failed to generate report for user {Username}: {Message}", user.Username, ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to queue weekly reports: {ex.Message}", ex);
            }
        }
    }
}