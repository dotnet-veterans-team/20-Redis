using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using System.Text;

namespace RedisPoC.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<WeatherForecastController> _logger;
        private readonly IDistributedCache _distributedCache;

        public WeatherForecastController(ILogger<WeatherForecastController> logger, IDistributedCache distributedCache)
        {
            _logger = logger;
            _distributedCache = distributedCache;
        }

        [HttpGet(Name = "GetWeatherForecast")]
        public async Task<IActionResult> Get()
        {
            var cacheKey = "weatherList";
            string serializedCustomerList;
            List<WeatherForecast> weatherList = new List<WeatherForecast>();

            var redisCustomerList = await _distributedCache.GetAsync(cacheKey);

            if (redisCustomerList != null)
            {
                serializedCustomerList = Encoding.UTF8.GetString(redisCustomerList);
                weatherList = JsonConvert.DeserializeObject<List<WeatherForecast>>(serializedCustomerList);
            }
            else
            {
                weatherList = await GetFromDb();
                serializedCustomerList = JsonConvert.SerializeObject(weatherList);
                redisCustomerList = Encoding.UTF8.GetBytes(serializedCustomerList);

                var options = new DistributedCacheEntryOptions()
                    .SetAbsoluteExpiration(DateTime.Now.AddMinutes(10))
                    .SetSlidingExpiration(TimeSpan.FromMinutes(2));

                await _distributedCache.SetAsync(cacheKey, redisCustomerList, options);
            }

            return Ok(weatherList);
        }


        [HttpDelete(Name = "RemoveCache")]
        public async Task<IActionResult> Delete()
        {
            var cacheKey = "weatherList";
            var redisCustomerList = await _distributedCache.GetAsync(cacheKey);

            if (redisCustomerList != null)
            {
                await _distributedCache.RemoveAsync(cacheKey);
            }
            else
            {
                return NotFound();
            }

            return Ok();
        }

        private async Task<List<WeatherForecast>> GetFromDb()
        {
            await Task.Delay(5 * 1000);
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateTime.Now.AddDays(index),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToList();
        }
    }
}