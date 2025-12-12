using Microsoft.EntityFrameworkCore;
using ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Models;
using ExpressTicketCinemaSystem.Src.Cinema.Application.Exceptions;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Common.Responses;
using ExpressTicketCinemaSystem.Src.Cinema.Application.Helpers;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ExpressTicketCinemaSystem.Src.Cinema.Application.Services
{
    public interface IContractValidationService
    {
        Task ValidatePartnerHasActiveContractAsync(int partnerId);
        Task<bool> CheckPartnerHasActiveContractAsync(int partnerId);
    }

    public class ContractValidationService : IContractValidationService
    {
        private readonly CinemaDbCoreContext _context;

        public ContractValidationService(CinemaDbCoreContext context)
        {
            _context = context;
        }

        public async Task ValidatePartnerHasActiveContractAsync(int partnerId)
        {
            // Sử dụng giờ Việt Nam (UTC+7) để so sánh với StartDate/EndDate
            // Vì StartDate/EndDate trong DB được lưu theo giờ VN (00:00:00 của ngày VN)
            var nowVN = DateTimeHelper.NowVN();

            var hasActiveContract = await _context.Contracts
                .AnyAsync(c => c.PartnerId == partnerId
                            && c.Status == "active"
                            && c.StartDate <= nowVN
                            && c.EndDate >= nowVN);

            if (!hasActiveContract)
            {
                throw new UnauthorizedException(new Dictionary<string, ValidationError>
                {
                    ["contract"] = new ValidationError
                    {
                        Msg = "Partner chưa có hợp đồng active hoặc hợp đồng đã hết hạn",
                        Path = "contract",
                        Location = "authorization"
                    }
                });
            }
        }

        public async Task<bool> CheckPartnerHasActiveContractAsync(int partnerId)
        {
            // Sử dụng giờ Việt Nam (UTC+7) để so sánh với StartDate/EndDate
            var nowVN = DateTimeHelper.NowVN();

            return await _context.Contracts
                .AnyAsync(c => c.PartnerId == partnerId
                            && c.Status == "active"
                            && c.StartDate <= nowVN
                            && c.EndDate >= nowVN);
        }
    }
}