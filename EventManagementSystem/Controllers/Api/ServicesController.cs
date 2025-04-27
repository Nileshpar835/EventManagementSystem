using EventManagementSystem.Models;
using EventManagementSystem.Services;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

namespace EventManagementSystem.Controllers.Api
{
    [Route("api")]
    [ApiController]
    public class ServicesController : ControllerBase
    {
        private readonly FlowerService _flowerService;
        private readonly FoodService _foodService;
        private readonly LightService _lightService;
        private readonly EquipmentService _equipmentService;
        
        public ServicesController(
            FlowerService flowerService,
            FoodService foodService,
            LightService lightService,
            EquipmentService equipmentService)
        {
            _flowerService = flowerService;
            _foodService = foodService;
            _lightService = lightService;
            _equipmentService = equipmentService;
        }
        
        [HttpGet("flowers")]
        public ActionResult<IEnumerable<Flower>> GetFlowers()
        {
            return Ok(_flowerService.GetAvailableFlowers());
        }
        
        [HttpGet("food")]
        public ActionResult<IEnumerable<Food>> GetFood()
        {
            return Ok(_foodService.GetAvailableFoods());
        }
        
        [HttpGet("lights")]
        public ActionResult<IEnumerable<Light>> GetLights()
        {
            return Ok(_lightService.GetAvailableLights());
        }
        
        [HttpGet("equipment")]
        public ActionResult<IEnumerable<Equipment>> GetEquipment()
        {
            return Ok(_equipmentService.GetAvailableEquipment());
        }
    }
}