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

namespace TimePlanning.Pn.Controllers
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Infrastructure.Models.Planning;
    using Microsoft.AspNetCore.Mvc;
    using Microting.eFormApi.BasePn.Infrastructure.Models.API;
    using Services.TimePlanningPlanningService;

    [Route("api/time-planning-pn/plannings")]
    public class TimePlanningPlanningController : Controller
    {
        private readonly ITimePlanningPlanningService _planningService;

        public TimePlanningPlanningController(ITimePlanningPlanningService planningService)
        {
            _planningService = planningService;
        }
        
        [HttpPost]
        [Route("index")]
        public async Task<OperationDataResult<List<TimePlanningPlanningViewModel>>> Index(
            [FromBody] TimePlanningPlanningRequestModel model)
        {
            return await _planningService.Index(model);
        }

        [HttpPost]
        public async Task<OperationResult> CreatePlanning(/*[FromBody]  todo add model*/)
        {
            return await _planningService.CreatePlanning();
        }

        [HttpPut]
        public async Task<OperationResult> UpdatePlannings(/*[FromBody]  todo add model*/)
        {
            return await _planningService.UpdatePlannings();
        }

        [HttpDelete]
        public async Task<OperationResult> DeletePlanning(int id)
        {
            return await _planningService.DeletePlanning(id);
        }

        [HttpGet]
        public async Task<OperationDataResult<object>> GetPlanning(int id) // todo change object to model
        {
            return await _planningService.GetPlanning(id);
        }
    }
}