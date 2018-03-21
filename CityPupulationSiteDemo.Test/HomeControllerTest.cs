using CityPopulationSite.Demo.Controllers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using System;
using Xunit;

namespace CityPupulationSiteDemo.Test
{
    public class HomeControllerTest
    {
        private readonly IHostingEnvironment _hostingEnvironment;
        private IConfiguration _configuration;
        private readonly IMemoryCache _cache;

        [Fact]
        public void HomeControllerIndexTest()
        {
            HomeController homeController = new HomeController(_configuration, _hostingEnvironment, _cache);
            IActionResult result = homeController.Index(string.Empty, string.Empty, string.Empty, null);
            Assert.IsType<ViewResult>(result);
            
        }
    }
}
