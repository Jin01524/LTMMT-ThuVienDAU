#nullable disable
using System;
using System.Collections.Generic;

namespace ThuVienDAU.Circulation.BC03
{
    public class DailyCirculationRow
    {
        public DateTime ReportDate { get; set; }
        public int BorrowedCount { get; set; }
        public int ReturnedCount { get; set; }
        public int ReadingRoomVisits { get; set; }
        public decimal FineAssessed { get; set; }
        public decimal FineCollected { get; set; }
    }

    public class CirculationDashboardReport
    {
        public string ViewerName { get; set; }
        public string ViewerRole { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }

        // 1. Lưu thông mượn / trả / gia hạn
        public int TotalLoanTickets { get; set; }
        public int TotalBorrowedCopies { get; set; }
        public int TotalReturnedCopies { get; set; }
        public int TotalRenewals { get; set; }

        // 2. Quá hạn
        public int CurrentOverdueCopies { get; set; }
        public int LateReturnedInPeriod { get; set; }

        // 3. Phòng đọc
        public int ReadingRoomVisitsInPeriod { get; set; }
        public int CurrentlyInReadingRoom { get; set; }

        // 4. Đối chiếu tiền phạt với thực thu (không đếm trùng)
        public decimal TotalAssessedFinesInPeriod { get; set; }   // Tổng phạt phát sinh trong kỳ (trừ DA_HUY)
        public decimal TotalCollectedReceiptsInPeriod { get; set; } // Tổng thực thu trên PhieuThu trong kỳ
        public int ReceiptCountInPeriod { get; set; }
        public decimal TotalOutstandingUnpaidFines { get; set; }    // Tổng nợ phạt chưa thu toàn hệ thống
        public decimal CashCollectedInPeriod { get; set; }
        public decimal TransferCollectedInPeriod { get; set; }

        public List<DailyCirculationRow> DailyRows { get; set; } = new List<DailyCirculationRow>();
    }

}

namespace ThuVienDAU.Circulation.LT01
{
    public class BorrowReaderLookup
    {
        public int ReaderId { get; set; }
        public string ReaderCode { get; set; }
        public string FullName { get; set; }
        public string ReaderType { get; set; }
        public string ProfileStatus { get; set; }
        public bool CanBorrow { get; set; }
        public string LockReason { get; set; }
        public int ActiveBorrowCount { get; set; }
        public int MaxBorrowLimit { get; set; }
        public int LoanDays { get; set; }
        public decimal UnpaidFineTotal { get; set; }
        public int OverdueCount { get; set; }
        public List<int> ActiveTitleIds { get; set; } = new List<int>();
    }

    public class BorrowCopyLookup
    {
        public int CopyId { get; set; }
        public int TitleId { get; set; }
        public string Barcode { get; set; }
        public string RegistrationNumber { get; set; }
        public string TitleName { get; set; }
        public string Status { get; set; }
        public string Condition { get; set; }
        public string LocationText { get; set; }
    }

    public class BorrowReceiptItem
    {
        public int DetailId { get; set; }
        public string Barcode { get; set; }
        public string RegistrationNumber { get; set; }
        public string TitleName { get; set; }
        public DateTime DueDate { get; set; }
    }

    public class BorrowReceiptResult
    {
        public int LoanId { get; set; }
        public int ReaderId { get; set; }
        public string ReaderCode { get; set; }
        public string ReaderName { get; set; }
        public DateTime BorrowedAt { get; set; }
        public List<BorrowReceiptItem> Items { get; set; } = new List<BorrowReceiptItem>();
    }

}

namespace ThuVienDAU.Circulation.LT02
{
    public class ActiveLoanCopyInfo
    {
        public int DetailId { get; set; }
        public int LoanId { get; set; }
        public int CopyId { get; set; }
        public int ReaderId { get; set; }
        public string ReaderCode { get; set; }
        public string ReaderName { get; set; }
        public string Barcode { get; set; }
        public string RegistrationNumber { get; set; }
        public string TitleName { get; set; }
        public DateTime BorrowedAt { get; set; }
        public DateTime DueDate { get; set; }
        public string CurrentCondition { get; set; }
        public int OverdueDays { get; set; }
    }

    public class ReturnItemInput
    {
        public int DetailId { get; set; }
        public string ReturnOutcome { get; set; } // "BINH_THUONG", "HU_HONG", "MAT"
        public string BookCondition { get; set; } // "TOT", "MOI", "CU", "HU_HONG", "MAT"
        public string Note { get; set; }
    }

    public class ReturnProcessedItem
    {
        public int DetailId { get; set; }
        public int LoanId { get; set; }
        public string Barcode { get; set; }
        public string TitleName { get; set; }
        public string ReaderCode { get; set; }
        public string ReaderName { get; set; }
        public DateTime ReturnedAt { get; set; }
        public int OverdueDays { get; set; }
        public string ReturnOutcome { get; set; }
        public bool HasViolationBasis { get; set; }
        public string ViolationSummary { get; set; }
    }

}

namespace ThuVienDAU.Circulation.LT03
{
    public class RenewableLoanItem
    {
        public int DetailId { get; set; }
        public int LoanId { get; set; }
        public int ReaderId { get; set; }
        public int TitleId { get; set; }
        public string ReaderCode { get; set; }
        public string ReaderName { get; set; }
        public string ReaderType { get; set; }
        public string Barcode { get; set; }
        public string TitleName { get; set; }
        public DateTime DueDate { get; set; }
        public int RenewCount { get; set; }
        public bool CanRenew { get; set; }
        public string BlockReason { get; set; }
    }

    public class RenewResult
    {
        public int DetailId { get; set; }
        public string Barcode { get; set; }
        public string TitleName { get; set; }
        public string Channel { get; set; }
        public int NewRenewCount { get; set; }
        public DateTime OldDueDate { get; set; }
        public DateTime NewDueDate { get; set; }
    }

}

namespace ThuVienDAU.Circulation.LT04
{
    public class LoanHistoryFilter
    {
        public string Keyword { get; set; }
        public string StatusFilter { get; set; } // "TAT_CA", "DANG_MUON", "QUA_HAN", "DA_TRA", "HU_HONG", "MAT"
        public bool UseDateRange { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
    }

    public class LoanHistoryRecord
    {
        public int DetailId { get; set; }
        public int LoanId { get; set; }
        public string LoanStatus { get; set; }
        public string ReaderCode { get; set; }
        public string ReaderName { get; set; }
        public string ReaderType { get; set; }
        public string Barcode { get; set; }
        public string RegistrationNumber { get; set; }
        public string TitleName { get; set; }
        public DateTime BorrowedAt { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime? ReturnedAt { get; set; }
        public int RenewCount { get; set; }
        public string DetailStatus { get; set; }
        public string ReturnCondition { get; set; }
        public string Note { get; set; }
        public bool IsOverdue { get; set; }
        public int OverdueDays { get; set; }
    }

}

namespace ThuVienDAU.Circulation.PD01
{
    public class ReadingRoomVisitItem
    {
        public int VisitId { get; set; }
        public int ReaderId { get; set; }
        public string ReaderCode { get; set; }
        public string ReaderName { get; set; }
        public string ReaderType { get; set; }
        public DateTime CheckInTime { get; set; }
        public DateTime? CheckOutTime { get; set; }
        public string LockerKey { get; set; }
        public string Note { get; set; }
        public bool IsInside => !CheckOutTime.HasValue;
    }

    public class CheckInOutResult
    {
        public int VisitId { get; set; }
        public string ActionType { get; set; } // "CHECK_IN" hoặc "CHECK_OUT"
        public string ReaderCode { get; set; }
        public string ReaderName { get; set; }
        public string ReaderType { get; set; }
        public DateTime Timestamp { get; set; }
        public string LockerKey { get; set; }
        public string ReminderMessage { get; set; }
    }

}

namespace ThuVienDAU.Circulation.PH01
{
    public class SubmitFeedbackRequest
    {
        public string ReaderCode { get; set; }
        public string FeedbackType { get; set; } // GOP_Y, DANH_GIA, DE_XUAT
        public int? TitleId { get; set; }
        public int? Rating { get; set; } // 1..5 cho DANH_GIA
        public string Subject { get; set; }
        public string Content { get; set; }
    }

    public class FeedbackItem
    {
        public int FeedbackId { get; set; }
        public int ReaderId { get; set; }
        public string ReaderCode { get; set; }
        public string ReaderName { get; set; }
        public string FeedbackType { get; set; }
        public int? TitleId { get; set; }
        public string TitleName { get; set; }
        public int? Rating { get; set; }
        public string Subject { get; set; }
        public string Content { get; set; }
        public string Status { get; set; } // MOI, DA_XU_LY
        public string StaffReply { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
    }

    public class ProcessFeedbackRequest
    {
        public int FeedbackId { get; set; }
        public string NewStatus { get; set; } // DA_XU_LY
        public string StaffReply { get; set; }
    }

}

namespace ThuVienDAU.Circulation.VP01
{
    public class ViolationCandidate
    {
        public int DetailId { get; set; }
        public int LoanId { get; set; }
        public int ReaderId { get; set; }
        public string ReaderCode { get; set; }
        public string ReaderName { get; set; }
        public string ReaderType { get; set; }
        public string Barcode { get; set; }
        public string TitleName { get; set; }
        public decimal BookPrice { get; set; }
        public bool HasBookPrice { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime? ReturnedAt { get; set; }
        public string DetailStatus { get; set; }
        public int RawOverdueDays { get; set; }
        public int HolidayDaysExcluded { get; set; }
        public int ChargeableDays { get; set; }
        public bool IsCalendarFinalized { get; set; }
        public string ViolationType { get; set; } // QUA_HAN, MAT_SACH, HONG_TRANG; staff overdue becomes a zero-amount notice.
        public decimal SuggestedAmount { get; set; }
        public string CalculationNote { get; set; }
    }

    public class CreateViolationInput
    {
        public int DetailId { get; set; }
        public string ViolationType { get; set; }
        public int ChargeableDays { get; set; }
        public decimal Amount { get; set; }
        public string Description { get; set; }
        public string SettlementMethod { get; set; } = "DEN_TIEN";
        public decimal? AssessedEquivalentPrice { get; set; }
    }

    public class ViolationRecordResult
    {
        public int ViolationId { get; set; }
        public string ReaderCode { get; set; }
        public string ReaderName { get; set; }
        public string ViolationType { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; }
        public string Description { get; set; }
    }

}

namespace ThuVienDAU.Circulation.VP02
{
    public class UnpaidViolationItem
    {
        public int ViolationId { get; set; }
        public int ReaderId { get; set; }
        public string ReaderCode { get; set; }
        public string ReaderName { get; set; }
        public string ViolationType { get; set; }
        public int OverdueDays { get; set; }
        public decimal Amount { get; set; }
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Barcode { get; set; }
        public string TitleName { get; set; }
    }

    public class CollectFineRequest
    {
        public string PaymentMethod { get; set; } // "TIEN_MAT", "CHUYEN_KHOAN"
        public string ReferenceNote { get; set; }
        public List<int> ViolationIds { get; set; } = new List<int>();
    }

    public class FineReceiptResult
    {
        public int ReceiptId { get; set; }
        public string ReceiptCode { get; set; }
        public int ReaderId { get; set; }
        public string ReaderCode { get; set; }
        public string ReaderName { get; set; }
        public string CollectorName { get; set; }
        public DateTime CollectedAt { get; set; }
        public string PaymentMethod { get; set; }
        public decimal TotalAmount { get; set; }
        public int ItemCount { get; set; }
        public string BasisSummary { get; set; }
        public bool ReaderUnlocked { get; set; }
    }

}

namespace ThuVienDAU.Circulation.VP03
{
    public class ViolationLookupItem
    {
        public int ViolationId { get; set; }
        public int ReaderId { get; set; }
        public string ReaderCode { get; set; }
        public string ReaderName { get; set; }
        public string ViolationType { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; }
        public int? ReceiptId { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Description { get; set; }
    }

    public class VoidViolationRequest
    {
        public int ViolationId { get; set; }
        public string Reason { get; set; }
    }

    public class VoidViolationResult
    {
        public int ViolationId { get; set; }
        public string ReaderCode { get; set; }
        public string ReaderName { get; set; }
        public decimal Amount { get; set; }
        public string PreviousStatus { get; set; }
        public bool HasExistingReceipt { get; set; }
        public int? ReceiptId { get; set; }
        public string ManagerName { get; set; }
        public DateTime VoidedAt { get; set; }
        public string Reason { get; set; }
        public bool ReaderUnlocked { get; set; }
    }

}

namespace ThuVienDAU.Circulation
{
 public sealed record CirculationQuery(string Keyword = "", bool Flag = false);
 public sealed record BorrowCommand(int ReaderId, int[] CopyIds);
 public sealed record ReturnCommand(List<LT02.ReturnItemInput> Items);
 public sealed record RenewCommand(int DetailId, DateTime NewDueDate, bool ConfirmedNoOtherRequest, string Reason);
 public sealed record StatisticsQuery(DateTime FromDate, DateTime ToDate);
 public sealed record ReceiptQuery(string Kind, int Id);
 public sealed record ReceiptDocument(string Title, string[] Lines);
 public sealed record VisitCommand(string ReaderCode, string LockerKey = "", string Note = "", bool ConfirmedPhysicalHandover = false);
}
