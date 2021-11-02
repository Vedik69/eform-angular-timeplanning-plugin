﻿/*
The MIT License (MIT)

Copyright (c) 2007 - 2021 Microting A/S

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

namespace TimePlanning.Pn.Services.TimePlanningPlanningService
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Threading.Tasks;
    using Extensions;
    using Infrastructure.Models.Planning;
    using Infrastructure.Models.Planning.HelperModel;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging;
    using Microting.eForm.Infrastructure.Constants;
    using Microting.eFormApi.BasePn.Abstractions;
    using Microting.eFormApi.BasePn.Infrastructure.Models.API;
    using Microting.TimePlanningBase.Infrastructure.Data;
    using Microting.TimePlanningBase.Infrastructure.Data.Entities;
    using TimePlanningLocalizationService;

    public class TimePlanningPlanningService : ITimePlanningPlanningService
    {
        private readonly ILogger<TimePlanningPlanningService> _logger;
        private readonly TimePlanningPnDbContext _dbContext;
        private readonly IUserService _userService;
        private readonly ITimePlanningLocalizationService _localizationService;
        private readonly IEFormCoreService _core;

        public TimePlanningPlanningService(
            ILogger<TimePlanningPlanningService> logger,
            TimePlanningPnDbContext dbContext,
            IUserService userService,
            ITimePlanningLocalizationService localizationService,
            IEFormCoreService core)
        {
            _logger = logger;
            _dbContext = dbContext;
            _userService = userService;
            _localizationService = localizationService;
            _core = core;
        }

        public async Task<OperationDataResult<List<TimePlanningPlanningModel>>> Index(TimePlanningPlanningRequestModel model)
        {
            try
            {
                var dateFrom = DateTime.ParseExact(model.DateFrom, "dd-MM-yyyy", CultureInfo.InvariantCulture);
                var dateTo = DateTime.ParseExact(model.DateTo, "dd-MM-yyyy", CultureInfo.InvariantCulture);

                Debugger.Break();

                var timePlanningRequest = _dbContext.PlanRegistrations
                    .Where(x => x.WorkflowState != Constants.WorkflowStates.Removed)
                    .Where(x => x.Date >= dateFrom || x.Date <= dateTo)
                    .Where(x => x.AssignedSiteId == model.WorkerId);
                
                var timePlannings = await timePlanningRequest
                    .Select(x => new TimePlanningPlanningHelperModel
                    {
                        WeekDay = (int)x.Date.DayOfWeek,
                        Date = x.Date,
                        PlanText = x.PlanText,
                        PlanHours = x.PlanHours,
                        MessageId = x.MessageId,
                    })
                    .ToListAsync();

                var date = (int)(dateTo - dateFrom).TotalDays + 1;

                if (timePlannings.Count < date)
                {
                    var daysForAdd = new List<TimePlanningPlanningHelperModel>();
                    for (var i = 0; i < date; i++)
                    {
                        if (timePlannings.All(x => x.Date != dateFrom.AddDays(i)))
                        {
                            daysForAdd.Add(new TimePlanningPlanningHelperModel
                            {
                                Date = dateFrom.AddDays(i),
                                WeekDay = (int)dateFrom.AddDays(i).DayOfWeek,
                            });
                        }
                    }
                    timePlannings.AddRange(daysForAdd);
                }

                if (model.Sort.ToLower() == "weekday")
                {
                    timePlannings = model.IsSortDesc
                        ? timePlannings.OrderByDescending(x => x.WeekDay).ToList()
                        : timePlannings.OrderBy(x => x.WeekDay).ToList();
                }
                else
                {
                    timePlannings = model.IsSortDesc
                        ? timePlannings.OrderByDescending(x => x.Date).ToList()
                        : timePlannings.OrderBy(x => x.Date).ToList();
                }

                var result = timePlannings
                    .Select(x => new TimePlanningPlanningModel
                    {
                        WeekDay = x.WeekDay,
                        Date = x.Date.ToString("dd-MM-yyyy"),
                        PlanText = x.PlanText,
                        PlanHours = x.PlanHours,
                        MessageId = x.MessageId,
                    })
                    .ToList();

                return new OperationDataResult<List<TimePlanningPlanningModel>>(
                    true,
                    result);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                _logger.LogError(e.Message);
                return new OperationDataResult<List<TimePlanningPlanningModel>>(
                    false,
                    _localizationService.GetString("ErrorWhileObtainingPlannings"));
            }
        }

        public async Task<OperationResult> UpdateCreatePlanning(TimePlanningPlanningUpdateModel model)
        {
            try
            {
                var date = DateTime.Parse(model.Date);

                var planning = await _dbContext.PlanRegistrations
                    .Where(x => x.WorkflowState != Constants.WorkflowStates.Removed)
                    .Where(x => x.AssignedSiteId == model.SiteId)
                    .Where(x => x.Date == date)
                    .FirstOrDefaultAsync();
                if (planning != null)
                {
                    return await UpdatePlanning(planning, model);
                }

                return await CreatePlanning(model, date);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                _logger.LogError(e.Message);
                return new OperationResult(
                    false,
                    _localizationService.GetString("ErrorWhileUpdatePlanning"));
            }
        }

        private async Task<OperationResult> CreatePlanning(TimePlanningPlanningUpdateModel model, DateTime date)
        {
            try
            {
                var planning = new PlanRegistration
                {
                    MessageId = (int)model.MessageId,
                    PlanText = model.PlanText,
                    AssignedSiteId = model.SiteId,
                    Date = date,
                    PlanHours = model.PlanHours,
                    CreatedByUserId = _userService.UserId,
                    UpdatedByUserId = _userService.UserId,
                };

                await planning.Create(_dbContext);
                return new OperationResult(
                    true,
                    _localizationService.GetString("SuccessfullyCreatePlanning"));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                _logger.LogError(e.Message);
                return new OperationResult(
                    false,
                    _localizationService.GetString("ErrorWhileCreatePlanning"));
            }
        }

        private async Task<OperationResult> UpdatePlanning(PlanRegistration planning, TimePlanningPlanningUpdateModel model)
        {
            try
            {
                planning.PlanText = model.PlanText;
                planning.MessageId = (int)model.MessageId;
                planning.PlanHours = model.PlanHours;

                await planning.Update(_dbContext);

                return new OperationResult(
                    true,
                    _localizationService.GetString("SuccessfullyUpdatePlanning"));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                _logger.LogError(e.Message);
                return new OperationResult(
                    false,
                    _localizationService.GetString("ErrorWhileUpdatePlanning"));
            }
        }
    }
}
