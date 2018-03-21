# RajeshPandey-Demo
Citypopulation

Used ASP.Net Core, because it is platform independent and can be ported to other operating systems and Docker.

1.) Added MemoryCache to save JsonData for City. Used sliding cache for 7 days. So that for each call, application need not to fetch the JSON file.
     Fast solution for this type of small data is in-memory databases like: Elastic Cache or Radis
2.) Added WebOptimizer: For removing spaces from html.
3.) GZIP Compression for faster resource loads.
4.) UseStaticFiles
5.) Log4Net for logging 

Created WebAPI in .Net Core for separation of concern. Restful service may be useful if planning to move to Angular or React.

Used Bootstrap for UI and modified Site.css and Added assetsite.css in wwwroot/css
Added bootstrap top-bar navigation, just for demo(Like login/register, ...)

Filtered bad data from JSON
Fixed invalid JSON.

Added css in bundle, so that it loads faster.
Added pagination.

Added Unit test project, but did not add unit tests, just one.
