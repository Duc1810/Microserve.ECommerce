using BuildingBlocks.Observability.BaseEntity;

namespace PaymentService.Models
{
    public class Transaction : Entity<Guid>
    {
        public Guid OrderId { get; set; }

        // Mã đơn hàng của PayOS (kiểu long/int)
        public long OrderCode { get; set; }

        // Số tiền thanh toán
        public decimal Amount { get; set; }

        // Mã tham chiếu giao dịch từ phía ngân hàng/PayOS
        public string Reference { get; set; } = string.Empty;

        // Nội dung chuyển khoản
        public string Description { get; set; } = string.Empty;

        // Idempotent key để đảm bảo không xử lý trùng lặp
        public string IdempotentKey { get; set; } = string.Empty;

        // Trạng thái xử lý
        public TransactionStatus Status { get; set; } = TransactionStatus.Pending;

        // Thông tin tài khoản đối ứng (Nếu cần đối soát chi tiết)
        public string? AccountNumber { get; set; }
        public string? CounterAccountName { get; set; }
        public string? CounterAccountNumber { get; set; }
        public string? CounterAccountBankName { get; set; }

        // Thời gian xử lý
        public DateTime? ProcessedAt { get; set; }
    }

    public enum TransactionStatus
    {
        Pending = 0,
        Completed = 1,
        Failed = 2,
        Cancelled = 3
    }
}
