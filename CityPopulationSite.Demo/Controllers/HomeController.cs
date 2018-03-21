using CityPopulationSite.Demo.Models;
using log4net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;

namespace CityPopulationSite.Demo.Controllers
{
    public class HomeController : Controller
    {
        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private readonly IHostingEnvironment _hostingEnvironment;
        private IConfiguration _configuration;
        private readonly IMemoryCache _cache;

        public HomeController(IConfiguration Configuration, IHostingEnvironment hostingEnvironment, IMemoryCache cache)
        {
            _configuration = Configuration;
            _hostingEnvironment = hostingEnvironment;
            _cache = cache;
        }

        public ActionResult Index(string sortOrder, string currentFilter, string searchString, int? page)
        {
            ViewData["CurrentSort"] = sortOrder;
            try
            {
                if (searchString != null)
                {
                    page = 1;
                }
                else
                {
                    searchString = currentFilter;
                }

                Int16 pageSize = 25;//Set defauly value, to handle appsetting missing
                if (null != _configuration.GetSection("AppSettings")["PageSize"])
                {
                    pageSize = Convert.ToInt16(_configuration.GetSection("AppSettings")["PageSize"]);
                }

                ViewData["CitySortParm"] = String.IsNullOrEmpty(sortOrder) ? "city_desc" : "";
                ViewData["StateSortParm"] = sortOrder == "State" ? "state_desc" : "State";
                ViewData["CurrentFilter"] = searchString;

                IList<CityDataModel> cityData = GetCityData();

                if (cityData != null)
                {
                    var data = from s in cityData.Where(x => x.City.Any(c => !char.IsDigit(c)))
                               select s;
                    if (!String.IsNullOrEmpty(searchString))
                    {
                        data = data.Where(s => s.City.ToLower().Contains(searchString.ToLower())
                                               || s.State.Contains(searchString) || s.Id.ToString().Contains(searchString));
                    }
                    switch (sortOrder)
                    {
                        case "city_desc":
                            data = data.OrderByDescending(s => s.City);
                            break;
                        case "State":
                            data = data.OrderBy(s => s.State);
                            break;
                        case "Pop_desc":
                            data = data.OrderByDescending(s => s.Pop);
                            break;
                        default:
                            data = data.OrderBy(s => s.City);
                            break;
                    }

                    return View(PaginatedList<CityDataModel>.CreateAsync(data.ToList(), page ?? 1, pageSize).Result);


                }
                else
                {
                    ViewData["Message"] = "No data found!";
                    return View();
                }

            }
            catch (Exception ex)
            {
                Log.Error(ex);
                return View();
            }


        }



        public IActionResult About()
        {
            ViewData["Message"] = "Your application description page.";

            return View();
        }

        public IActionResult Contact()
        {
            ViewData["Message"] = "Your contact page.";

            return View();
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public ActionResult Detail(int Id)
        {
            try
            {
                IList<CityDataModel> cityData = GetCityData();
                if (cityData != null)
                {
                    var data = cityData.SingleOrDefault(x => x.Id == Id);
                    return View(data);
                }
                else
                {
                    ViewData["Message"] = "No Data found";
                    return View();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                return View();
            }
        }

        [HttpPost]
        public ActionResult Detail(CityDataModel cityData)
        {
            try
            {
                UpdateJson(cityData);
                return View(cityData);
            }

            catch (Exception ex)
            {
                ViewData["Error"] = "Error, please contact customer support!";
                Log.Error(ex);
                return View();
            }
        }

        /// <summary>
        /// Get CityData from Restful service: WebAPI
        /// </summary>
        /// <returns>IList<CityData></returns>
        private IList<CityDataModel> GetCityData()
        {
            if (_cache.TryGetValue("CityDataModelCacheKey", out IList<CityDataModel> cityDataFromCache))
            {
                return cityDataFromCache;
            }

            IList<CityDataModel> cityData = null;
            using (var client = new HttpClient())
            {
                var dataAPIUrl = _configuration.GetSection("AppSettings")["DataAPIUrl"];
                client.BaseAddress = new Uri(dataAPIUrl);
                HttpContent content = null;
                var response = client.PostAsync("api/GetCityData", content).Result;
                
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    var contents = response.Content.ReadAsStringAsync().Result;
                    cityData = JsonConvert.DeserializeObject<List<CityDataModel>>(contents); //Get List Of all Data Json File
                    _cache.Set("CityDataModelCacheKey", cityData, new MemoryCacheEntryOptions
                    {
                        SlidingExpiration = TimeSpan.FromDays(7)
                    });
                }
                else
                {
                    ViewData["Error"] = "No Data found, please contact customer support!";                    
                }
            }

            return cityData;
        }

        public bool UpdateJson(CityDataModel cityData) //Getting data from database and save in Json File
        {
            try
            {
                IList<CityDataModel> cityDataAll = GetCityData();
                cityDataAll.SingleOrDefault(x => x.Id == cityData.Id).Pop = cityData.Pop;

                var cityPopulationFilePath = Path.Combine(_hostingEnvironment.ContentRootPath + _configuration.GetSection("AppSettings")["CityPopulationFilePath"]);

                var jsonString = JsonConvert.SerializeObject(cityDataAll);
                if (jsonString != null)
                {                    
                    System.IO.File.WriteAllTextAsync(cityPopulationFilePath, jsonString);
                    ViewData["Message"] = "Updated successfully!";
                }
                else
                {
                    ViewData["error"] = "Error, please contact customer support!";
                }

                //todo: API should be used like code below. There is some issue in passing object.
                //using (var client = new HttpClient())
                //{
                //    string postData = JsonConvert.SerializeObject(cityData);                    
                //    var content = new StringContent(postData);
                 
                //    var dataAPIUrl = _configuration.GetSection("AppSettings")["DataAPIUrl"];
                //    client.BaseAddress = new Uri(dataAPIUrl);
                   
                //    var response = client.PostAsync("api/UpdateCityData", content).Result;

                //    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                //    {
                //        ViewData["msg"] = "Updated successfully!";
                //    }
                //    else
                //    {
                //        ViewData["error"] = "No Data found, please contact customer support!";
                //    }
                //}               
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                throw;
            }

        }

    }
}
