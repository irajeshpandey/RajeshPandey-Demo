using CityPopulationAPI.DataModel;
using log4net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;

namespace CityPopulationAPI.Controllers
{

    public class CityDataController : Controller
    {
        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private readonly IHostingEnvironment _hostingEnvironment;
        private IConfiguration _configuration;

        public CityDataController(IConfiguration Configuration, IHostingEnvironment hostingEnvironment)
        {
            _configuration = Configuration;
            _hostingEnvironment = hostingEnvironment;
        }     
        
        [HttpPost]
        [Route("api/GetCityData")]
        public string GetCityData()
        {
            try
            {
                return  CityData();
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                return "Error";
            }

        }

        [HttpPost]
        [Route("api/UpdateCityData")]       
        public HttpResponseMessage UpdateCityData(CityDataModel cityData) //Getting data from database and save in Json File
        {
            try
            {
                string data = CityData();
                var cityDataAll = JsonConvert.DeserializeObject<List<CityDataModel>>(data); //Get List Of all Data Json File

                cityDataAll.SingleOrDefault(x => x._id == cityData._id).pop = cityData.pop;

                var cityPopulationFilePath = Path.Combine(_hostingEnvironment.ContentRootPath + _configuration.GetSection("AppSettings")["CityPopulationFilePath"]);

                var jsonString = JsonConvert.SerializeObject(cityDataAll);
                if (jsonString != null)
                {
                    System.IO.File.WriteAllTextAsync(cityPopulationFilePath, jsonString);
                }

                return new HttpResponseMessage(HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                throw;
            }

        }      


        private string CityData()
        {
            var cityPopulationFilePath = Path.Combine(_hostingEnvironment.ContentRootPath + _configuration.GetSection("AppSettings")["CityPopulationFilePath"]);
            string returnData = string.Empty;
            // StreamReader with using, will free resources on its own.
            using (StreamReader jsonCityData = new StreamReader(cityPopulationFilePath))
            {
                var rawJsonData = jsonCityData.ReadToEndAsync();
                returnData = rawJsonData.Result; //Get List Of all Data Json File
               
            }
            return returnData;

        }
    }
}
