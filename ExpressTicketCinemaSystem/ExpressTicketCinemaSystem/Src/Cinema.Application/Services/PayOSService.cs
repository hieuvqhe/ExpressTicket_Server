using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Payment;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Payment.Responses;

namespace ExpressTicketCinemaSystem.Src.Cinema.Application.Services
{
    public class PayOSService : IPayOSService
    {
        private readonly ILogger<PayOSService> _logger;
        private readonly HttpClient _http;
        private readonly string _clientId;
        private readonly string _apiKey;
        private readonly string _checksum;
        private readonly string _baseUrl;

        private const string CREATE_PAYMENT = "/v2/payment-requests";

        public PayOSService(IConfiguration config, ILogger<PayOSService> logger)
        {
            _logger = logger;

            _clientId = config["PayOS:ClientId"]!;
            _apiKey = config["PayOS:ApiKey"]!;
            _checksum = config["PayOS:Checksum"]!;
            _baseUrl = config["PayOS:BaseUrl"] ?? "https://api-merchant.payos.vn";

            _http = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(25)
            };
        }

        // ==================================================
        // 1. TẠO PAYMENT
        // ==================================================
        public async Task<PayOSCreatePaymentResult> CreatePaymentAsync(
     string orderCode,
     long amount,
     string description,
     string returnUrl,
     string cancelUrl,
     CancellationToken ct = default)
        {
            long numericCode = ConvertOrderIdToLong(orderCode);

            // Chuẩn hóa description (<= 25 ký tự, không dấu tuỳ bạn)
            if (description.Length > 25)
                description = description[..25];

            // ====== 1) TẠO CHUỖI ĐỂ KÝ – ĐÚNG THEO DOC ======
            // amount=$amount&cancelUrl=$cancelUrl&description=$description&orderCode=$orderCode&returnUrl=$returnUrl
            var dataToSign =
                $"amount={amount}" +
                $"&cancelUrl={cancelUrl}" +
                $"&description={description}" +
                $"&orderCode={numericCode}" +
                $"&returnUrl={returnUrl}";

            string signature = HmacSha256Hex(dataToSign, _checksum);

            _logger.LogInformation("CreatePayment dataToSign = {Data}", dataToSign);
            _logger.LogInformation("CreatePayment signature = {Sig}", signature);

            // ====== 2) BODY GỬI LÊN PAYOS ======
            var body = new
            {
                orderCode = numericCode,
                amount = amount,
                description,
                cancelUrl,
                returnUrl,
                signature
            };

            string jsonBody = JsonSerializer.Serialize(body);
            _logger.LogInformation("CreatePayOS Request JSON = {Json}", jsonBody);

            var req = new HttpRequestMessage(HttpMethod.Post, _baseUrl + CREATE_PAYMENT);
            req.Headers.Add("x-client-id", _clientId);
            req.Headers.Add("x-api-key", _apiKey);
            req.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            var res = await _http.SendAsync(req, ct);
            string raw = await res.Content.ReadAsStringAsync(ct);

            _logger.LogInformation("PayOS response = {Raw}", raw);

            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;

            var code = root.GetProperty("code").GetString();
            var desc = root.TryGetProperty("desc", out var descElem) ? descElem.GetString() : null;

            if (code != "00")
            {
                _logger.LogError("PayOS error: code={Code}, desc={Desc}", code, desc);
                throw new Exception($"PayOS error: code={code}, desc={desc}");
            }

            var data = root.GetProperty("data");
            var checkoutUrl = data.GetProperty("checkoutUrl").GetString() ?? "";
            var paymentLinkId = data.GetProperty("paymentLinkId").GetString() ?? "";
            var qrCode = data.TryGetProperty("qrCode", out var qrElem)
                ? qrElem.GetString() ?? ""
                : "";

            return new PayOSCreatePaymentResult
            {
                CheckoutUrl = checkoutUrl,
                ProviderRef = paymentLinkId,
                QrCode = qrCode
            };
        }

        // ==================================================
        // 2. VERIFY WEBHOOK SIGNATURE (payment-requests)
        // ==================================================
        /// <summary>
        /// body = JSON của field "data" trong webhook (theo docs PayOS).
        /// </summary>
        public bool VerifyWebhookSignature(string body, string signature)
        {
            try
            {
                // Parse JSON data (object)
                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;

                if (root.ValueKind != JsonValueKind.Object)
                {
                    _logger.LogWarning("Webhook data không phải object: {Body}", body);
                    return false;
                }

                // Build key=value&... sort alphabetically
                var pairs = new List<KeyValuePair<string, string>>();

                foreach (var prop in root.EnumerateObject())
                {
                    string value = prop.Value.ValueKind switch
                    {
                        JsonValueKind.Null => "",
                        JsonValueKind.String => prop.Value.GetString() ?? "",
                        _ => prop.Value.ToString()
                    };

                    pairs.Add(new KeyValuePair<string, string>(prop.Name, value));
                }

                var sorted = pairs.OrderBy(p => p.Key, StringComparer.Ordinal);

                var sb = new StringBuilder();
                foreach (var kv in sorted)
                {
                    if (sb.Length > 0) sb.Append('&');
                    sb.Append(kv.Key).Append('=').Append(kv.Value);
                }

                string dataToSign = sb.ToString();
                string expected = HmacSha256Hex(dataToSign, _checksum);

                _logger.LogInformation("Webhook dataToSign = {Data}, expectedSig = {Sig}", dataToSign, expected);

                return string.Equals(expected, signature, StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying PayOS webhook signature. body={Body}", body);
                return false;
            }
        }

        // ==================================================
        // 2.5. CALCULATE WEBHOOK SIGNATURE (dùng để test)
        // ==================================================
        public string CalculateWebhookSignature(string body)
        {
            try
            {
                // Parse JSON data (object)
                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;

                if (root.ValueKind != JsonValueKind.Object)
                {
                    _logger.LogWarning("Webhook data không phải object: {Body}", body);
                    throw new ArgumentException("Body must be a valid JSON object");
                }

                // Build key=value&... sort alphabetically
                var pairs = new List<KeyValuePair<string, string>>();

                foreach (var prop in root.EnumerateObject())
                {
                    string value = prop.Value.ValueKind switch
                    {
                        JsonValueKind.Null => "",
                        JsonValueKind.String => prop.Value.GetString() ?? "",
                        _ => prop.Value.ToString()
                    };

                    pairs.Add(new KeyValuePair<string, string>(prop.Name, value));
                }

                var sorted = pairs.OrderBy(p => p.Key, StringComparer.Ordinal);

                var sb = new StringBuilder();
                foreach (var kv in sorted)
                {
                    if (sb.Length > 0) sb.Append('&');
                    sb.Append(kv.Key).Append('=').Append(kv.Value);
                }

                string dataToSign = sb.ToString();
                string signature = HmacSha256Hex(dataToSign, _checksum);

                _logger.LogInformation("Calculate signature - dataToSign = {Data}, signature = {Sig}", dataToSign, signature);

                return signature;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating webhook signature. body={Body}", body);
                throw;
            }
        }

        // ==================================================
        // 3. GET PAYMENT STATUS QUA REST API (nếu cần)
        // ==================================================
        public async Task<PayOSPaymentStatusResult> GetPaymentStatusAsync(
            string orderCode,
            CancellationToken ct = default)
        {
            long numericCode = ConvertOrderIdToLong(orderCode);
            var url = $"{_baseUrl}/v2/payment-requests/{numericCode}";

            var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Add("x-client-id", _clientId);
            req.Headers.Add("x-api-key", _apiKey);

            var res = await _http.SendAsync(req, ct);
            string raw = await res.Content.ReadAsStringAsync(ct);

            _logger.LogInformation("PayOS Status Response = {Raw}", raw);

            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;

            var code = root.GetProperty("code").GetString();
            var desc = root.GetProperty("desc").GetString();

            if (code != "00")
                throw new Exception($"PayOS status error: code={code}, desc={desc}");

            var data = root.GetProperty("data");

            return new PayOSPaymentStatusResult
            {
                Status = data.GetProperty("status").GetString() ?? "",
                Code = code ?? "",
                Description = desc ?? ""
            };
        }

        // ==================================================
        // HELPERS
        // ==================================================
        // Build signature cho payment-requests (request tạo link)
        private string BuildSignature(SortedDictionary<string, object?> map)
        {
            // map đã SortedDictionary => key tự sắp xếp alphabet
            var sb = new StringBuilder();

            foreach (var kv in map)
            {
                if (kv.Key == "signature") continue;

                string value = kv.Value switch
                {
                    null => "",
                    string s => s,
                    _ => kv.Value!.ToString() ?? ""
                };

                if (sb.Length > 0) sb.Append('&');
                sb.Append(kv.Key).Append('=').Append(value);
            }

            string dataToSign = sb.ToString();
            _logger.LogInformation("CreatePayment dataToSign = {Data}", dataToSign);

            return HmacSha256Hex(dataToSign, _checksum);
        }

        private static string HmacSha256Hex(string data, string key)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
            return BitConverter.ToString(hmac.ComputeHash(Encoding.UTF8.GetBytes(data)))
                .Replace("-", "")
                .ToLowerInvariant();
        }

        private long ConvertOrderIdToLong(string orderId)
        {
            if (string.IsNullOrWhiteSpace(orderId))
                throw new ArgumentException("orderId is required", nameof(orderId));

            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(orderId));

            // Lấy 8 byte đầu thành UInt64
            ulong raw = BitConverter.ToUInt64(hash, 0);

            // Giảm về phạm vi an toàn cho JSON / JavaScript (<= 2^53-1)
            // và đồng thời giới hạn khoảng 9–10 chữ số cho dễ nhìn
            const ulong maxSafe = 9_007_199_254_740_991UL; // 2^53 - 1
            ulong safe = raw % maxSafe;

            // Thu nhỏ hơn nữa để chắc chắn chỉ 9–10 digits (tùy bạn)
            const ulong mod = 1_000_000_000UL; // 9 chữ số
            safe = safe % mod;

            if (safe == 0)
                safe = 1;

            return (long)safe;
        }
    }
}
