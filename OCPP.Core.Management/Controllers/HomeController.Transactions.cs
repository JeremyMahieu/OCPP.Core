﻿/*
 * OCPP.Core - https://github.com/dallmann-consulting/OCPP.Core
 * Copyright (C) 2020-2021 dallmann consulting GmbH.
 * All Rights Reserved.
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using OCPP.Core.Database;
using OCPP.Core.Management.Models;

namespace OCPP.Core.Management.Controllers
{
    public partial class HomeController : BaseController
    {
        [Authorize]
        public IActionResult Transactions(string Id, string ConnectorId)
        {
            Logger.LogTrace("Transactions: Loading charge point transactions...");

            int currentConnectorId = -1;
            int.TryParse(ConnectorId, out currentConnectorId);

            TransactionListViewModel tlvm = new TransactionListViewModel();
            tlvm.CurrentChargePointId = Id;
            tlvm.CurrentConnectorId = currentConnectorId;
            tlvm.ConnectorStatuses = new List<ConnectorStatus>();
            tlvm.Transactions = new List<Transaction>();

            try
            {
                string ts = Request.Query["t"];
                int days = 30;
                if (ts == "2")
                {
                    // 90 days
                    days = 90;
                    tlvm.Timespan = 2;
                }
                else if (ts == "3")
                {
                    // 365 days
                    days = 365;
                    tlvm.Timespan = 3;
                }
                else
                {
                    // 30 days
                    days = 30;
                    tlvm.Timespan = 1;
                }

                Logger.LogTrace("Transactions: Loading charge points...");
                tlvm.ChargePoints = DbContext.ChargePoints.ToList<ChargePoint>();

                Logger.LogTrace("Transactions: Loading charge points connectors...");
                tlvm.ConnectorStatuses = DbContext.ConnectorStatuses.ToList<ConnectorStatus>();

                // Count connectors for every charge point (=> naming scheme)
                Dictionary<string, int> dictConnectorCount = new Dictionary<string, int>();
                foreach (ConnectorStatus cs in tlvm.ConnectorStatuses)
                {
                    if (dictConnectorCount.ContainsKey(cs.ChargePointId))
                    {
                        // > 1 connector
                        dictConnectorCount[cs.ChargePointId] = dictConnectorCount[cs.ChargePointId] + 1;
                    }
                    else
                    {
                        // first connector
                        dictConnectorCount.Add(cs.ChargePointId, 1);
                    }
                }


                // load charge tags for name resolution
                Logger.LogTrace("Transactions: Loading charge tags...");
                tlvm.ChargeTags = DbContext.ChargeTags.ToList<ChargeTag>();

                if (!string.IsNullOrEmpty(tlvm.CurrentChargePointId))
                {
                    Logger.LogTrace("Transactions: Loading charge point transactions...");
                    tlvm.Transactions = DbContext.Transactions
                                        .Where(t => t.ChargePointId == tlvm.CurrentChargePointId &&
                                                    t.ConnectorId == tlvm.CurrentConnectorId &&
                                                    t.StartTime >= DateTime.UtcNow.AddDays(-1 * days))
                                        .OrderByDescending(t => t.TransactionId)
                                        .ToList<Transaction>();
                }
            }
            catch (Exception exp)
            {
                Logger.LogError(exp, "Transactions: Error loading charge points from database");
            }

            return View(tlvm);
        }
    }
}
