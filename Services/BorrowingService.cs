using Microsoft.EntityFrameworkCore;
using SWD.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SWD.Services
{
    public class BorrowingService
    {
        private readonly LibraryManagementSystemContext _context;

        public BorrowingService(LibraryManagementSystemContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Gets the list of borrowed books for a specific member
        /// Corresponds to: getBorrowedBooks(memberId) in sequence diagram
        /// </summary>
        public async Task<List<BorrowTransaction>> GetBorrowedBooks(int memberId)
        {
            var borrowedList = await _context.BorrowTransactions
                .Include(b => b.User)
                .Include(b => b.BorrowDetails)
                    .ThenInclude(d => d.Book)
                .Where(b => b.UserId == memberId)
                .OrderByDescending(b => b.BorrowDate)
                .ToListAsync();

            return borrowedList;
        }

        /// <summary>
        /// Gets all pending borrow requests (Status = "Processing") for admin approval
        /// </summary>
        public async Task<List<BorrowTransaction>> GetPendingBorrowRequests()
        {
            var pendingRequests = await _context.BorrowTransactions
                .Include(b => b.User)
                .Include(b => b.BorrowDetails)
                    .ThenInclude(d => d.Book)
                .Where(b => b.Status == "Processing")
                .OrderByDescending(b => b.BorrowDate)
                .ToListAsync();

            return pendingRequests;
        }

        /// <summary>
        /// Gets all borrow transactions (for admin to see all requests including their own)
        /// </summary>
        public async Task<List<BorrowTransaction>> GetAllBorrowTransactions()
        {
            var allTransactions = await _context.BorrowTransactions
                .Include(b => b.User)
                .Include(b => b.BorrowDetails)
                    .ThenInclude(d => d.Book)
                .OrderByDescending(b => b.BorrowDate)
                .ToListAsync();

            return allTransactions;
        }

        /// <summary>
        /// Processes a renewal request for a borrowed book
        /// Corresponds to: processRenewal(borrowId) in sequence diagram
        /// </summary>
        public async Task<RenewalStatus> ProcessRenewal(int borrowId)
        {
            var borrow = await _context.BorrowTransactions
                .FirstOrDefaultAsync(b => b.BorrowId == borrowId);

            if (borrow == null)
            {
                return new RenewalStatus
                {
                    Success = false,
                    Message = "Borrow transaction not found."
                };
            }

            // Check if eligible for renewal
            if (IsEligible(borrow))
            {
                // Update due date (extend by 7 days, but not beyond 6 months from borrow date)
                UpdateDueDate(borrow);
                await _context.SaveChangesAsync();

                return new RenewalStatus
                {
                    Success = true,
                    Message = "Book renewal successful. Due date extended by 7 days.",
                    NewDueDate = borrow.DueDate
                };
            }
            else
            {
                // Log rejection
                await LogRejection(borrow, "Book is not eligible for renewal.");
                
                return new RenewalStatus
                {
                    Success = false,
                    Message = "Book is not eligible for renewal. It may have been returned, is overdue, or maximum renewal period has been reached."
                };
            }
        }

        /// <summary>
        /// Checks if a borrow transaction is eligible for renewal
        /// Condition: [isEligible] in sequence diagram
        /// </summary>
        private bool IsEligible(BorrowTransaction borrow)
        {
            // Cannot renew if already returned
            if (borrow.ReturnDate != null)
                return false;

            // Cannot renew if status is not "Borrowing" or "Borrowed"
            if (borrow.Status != "Borrowing" && borrow.Status != "Borrowed")
                return false;

            // Cannot renew if maximum renewal period (6 months from borrow date) would be exceeded
            // Renewal extends by 7 days, so check if new due date would exceed 6 months
            var maxDueDate = borrow.BorrowDate.AddMonths(6);
            if (borrow.DueDate.AddDays(7) > maxDueDate)
                return false;

            return true;
        }

        /// <summary>
        /// Updates the due date for a borrow transaction
        /// Corresponds to: updateDueDate() in sequence diagram
        /// </summary>
        private void UpdateDueDate(BorrowTransaction borrow)
        {
            var maxDueDate = borrow.BorrowDate.AddMonths(6);
            var newDueDate = borrow.DueDate.AddDays(7);

            // Extend by 7 days, but not beyond 6 months from borrow date
            if (newDueDate <= maxDueDate)
            {
                borrow.DueDate = newDueDate;
            }
            else
            {
                // If extending would exceed max, set to max
                borrow.DueDate = maxDueDate;
            }
        }

        /// <summary>
        /// Logs a rejection reason for renewal
        /// Corresponds to: logRejection() in sequence diagram
        /// </summary>
        private async Task LogRejection(BorrowTransaction borrow, string reason)
        {
            var log = new Log
            {
                UserId = borrow.UserId,
                Action = "Renewal Rejected",
                Description = $"Renewal rejected for Borrow ID {borrow.BorrowId}. Reason: {reason}",
                TimeStamp = DateTime.Now
            };

            _context.Logs.Add(log);
            await _context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Represents the status of a renewal request
    /// Corresponds to: renewalStatus in sequence diagram
    /// </summary>
    public class RenewalStatus
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime? NewDueDate { get; set; }
    }
}

