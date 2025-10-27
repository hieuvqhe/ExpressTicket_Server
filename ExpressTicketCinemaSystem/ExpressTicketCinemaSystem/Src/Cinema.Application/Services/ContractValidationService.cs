using Microsoft.EntityFrameworkCore;
using ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Models;
using ExpressTicketCinemaSystem.Src.Cinema.Application.Exceptions;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Common.Responses;
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
            var hasActiveContract = await _context.Contracts
                .AnyAsync(c => c.PartnerId == partnerId
                            && c.Status == "active"
                            && c.StartDate <= DateTime.UtcNow
                            && c.EndDate >= DateTime.UtcNow);

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
            return await _context.Contracts
                .AnyAsync(c => c.PartnerId == partnerId
                            && c.Status == "active"
                            && c.StartDate <= DateTime.UtcNow
                            && c.EndDate >= DateTime.UtcNow);
        }
    }
}