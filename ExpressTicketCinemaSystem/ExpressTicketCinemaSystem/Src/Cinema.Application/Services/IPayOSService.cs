using System.Threading;
using System.Threading.Tasks;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Payment.Requests;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Payment.Responses;

namespace ExpressTicketCinemaSystem.Src.Cinema.Application.Services
{
    public interface IPayOSService
    {
        /// <summary>
        /// Tạo payment link tại PayOS
        /// </summary>
        Task<PayOSCreatePaymentResult> CreatePaymentAsync(
            string orderCode,
            long amount,
            string description,
            string returnUrl,
            string cancelUrl,
            CancellationToken ct = default);

        /// <summary>
        /// Kiểm tra chữ ký webhook do PayOS gửi về
        /// </summary>
        bool VerifyWebhookSignature(string body, string signature);

        /// <summary>
        /// Tính signature cho webhook (dùng để test)
        /// </summary>
        string CalculateWebhookSignature(string body);

        /// <summary>
        /// (Optional) Lấy trạng thái thanh toán từ PayOS
        /// </summary>
        Task<PayOSPaymentStatusResult> GetPaymentStatusAsync(
            string orderCode,
            CancellationToken ct = default);
    }
}





