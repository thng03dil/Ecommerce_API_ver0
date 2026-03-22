using Ecommerce.Application.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ecommerce.Infrastructure.Services
{
    public class DeviceService : IDeviceService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private const string HEADER_NAME = "X-Device-Id";

        public DeviceService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public string GetDeviceId()
        {
            var deviceId = _httpContextAccessor
                .HttpContext?
                .Request
                .Headers[HEADER_NAME]
                .FirstOrDefault();

            
            return deviceId ?? string.Empty;
        }
    }
}
