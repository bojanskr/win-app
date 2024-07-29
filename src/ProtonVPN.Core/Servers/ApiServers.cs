﻿/*
 * Copyright (c) 2023 Proton AG
 *
 * This file is part of ProtonVPN.
 *
 * ProtonVPN is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * ProtonVPN is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with ProtonVPN.  If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ProtonVPN.Api.Contracts;
using ProtonVPN.Api.Contracts.Servers;
using ProtonVPN.Common.Extensions;
using ProtonVPN.Core.Settings;
using ProtonVPN.Core.Users;
using ProtonVPN.Logging.Contracts;
using ProtonVPN.Logging.Contracts.Events.ApiLogs;

namespace ProtonVPN.Core.Servers
{
    public class ApiServers : IApiServers
    {
        public string GATEWAY_DEFAULT_NAME = "Gateway";

        private readonly ILogger _logger;
        private readonly IApiClient _apiClient;
        private readonly TruncatedLocation _location;
        private readonly IUserLocationService _userLocationService;
        private readonly IAppSettings _appSettings;

        public ApiServers(
            ILogger logger,
            IApiClient apiClient,
            TruncatedLocation location,
            IUserLocationService userLocationService,
            IAppSettings appSettings)
        {
            _logger = logger;
            _apiClient = apiClient;
            _location = location;
            _userLocationService = userLocationService;
            _appSettings = appSettings;
        }

        public async Task<IReadOnlyCollection<LogicalServerResponse>> GetServersAsync()
        {
            try
            {
                string ip = await GetLocationIPAsync();
                ApiResponseResult<ServersResponse> response = await _apiClient.GetServersAsync(ip);

                if (response.LastModified.HasValue)
                {
                    _appSettings.LogicalsLastModifiedDate = response.LastModified.Value;
                }
                if (response.Success && !response.IsNotModified)
                {
                    NameUnnamedGateways(response);
                    return response.Value.Servers;
                }
            }
            catch (Exception ex)
            {
                _logger.Error<ApiErrorLog>("API: Get servers failed", ex);
            }

            return Array.Empty<LogicalServerResponse>();
        }

        private void NameUnnamedGateways(ApiResponseResult<ServersResponse> response)
        {
            IEnumerable<LogicalServerResponse> b2bServersWithoutGatewayName = response.Value.Servers
                .Where(s => ServerFeatures.IsB2B(s.Features) && string.IsNullOrWhiteSpace(s.GatewayName));
            foreach (LogicalServerResponse server in b2bServersWithoutGatewayName)
            {
                server.GatewayName = GATEWAY_DEFAULT_NAME;
            }
        }

        private async Task<string> GetLocationIPAsync()
        {
            string ip = _location.Ip();
            if (ip.IsNullOrEmpty())
            {
                await UpdateLocation();
            }
            return _location.Ip();
        }

        private async Task UpdateLocation()
        {
            try
            {
                await _userLocationService.Update();
            }
            catch (Exception e)
            {
                _logger.Error<ApiErrorLog>("Error when fetching API location.", e);
            }
        }

        public async Task<IReadOnlyCollection<LogicalServerResponse>> GetLoadsAsync()
        {
            try
            {
                string ip = await GetLocationIPAsync();
                ApiResponseResult<ServersResponse> response = await _apiClient.GetServerLoadsAsync(ip);
                if (response.Success)
                {
                    return response.Value.Servers;
                }
            }
            catch (Exception ex)
            {
                _logger.Error<ApiErrorLog>("API: Get servers failed", ex);
            }

            return Array.Empty<LogicalServerResponse>();
        }
    }
}