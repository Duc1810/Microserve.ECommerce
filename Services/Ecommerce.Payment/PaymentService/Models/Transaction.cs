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

        // Thông tin tài khoản đối ứng (Nếu cần đối soát chi tiết)
        public string? AccountNumber { get; set; }
        public string? CounterAccountName { get; set; }
        public string? CounterAccountNumber { get; set; }
        public string? CounterAccountBankName { get; set; }
    }
}
