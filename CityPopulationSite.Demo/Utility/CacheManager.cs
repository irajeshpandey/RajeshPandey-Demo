using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Net.Cache;
using Microsoft.Extensions.Caching.Memory;

namespace CityPopulationSite.Demo.Utility
{
    public class CacheManager
        {

        private IMemoryCache cache;

        public CacheManager(IMemoryCache cache)
        {
            this.cache = cache;
        }
        public static void Save(string cacheKey, object cacheObject, double expiredMinutes = 10)
            {
                if (expiredMinutes < 0.1) expiredMinutes = 0.1;

          
                if (cacheObject != null)
                    cache.CreateEntry(cacheKey);

                else
                    Remove(cacheKey);

            }

            public static void SaveSlid(string cacheKey, object cacheObject, int expiredMinutes = 2)
            {
                if (expiredMinutes < 1) expiredMinutes = 1;

                Cache cache = HttpRuntime.Cache;
                if (cacheObject != null)
                {
                    cache.Insert(cacheKey, cacheObject, null, System.Web.Caching.Cache.NoAbsoluteExpiration, new TimeSpan(0, 0, expiredMinutes, 0, 0), CacheItemPriority.Normal, null);
                }
                else
                {
                    Remove(cacheKey);
                }

            }
            public static object GetObj(string cacheKey)
            {
                ResetLastGet(cacheKey);
                return HttpRuntime.Cache[cacheKey];
            }

            public static T Get<T>(string cacheKey) where T : class
            {
                ResetLastGet(cacheKey);

                return HttpRuntime.Cache[cacheKey] as T;
            }

            public static void Remove(string cacheKey)
            {
                HttpRuntime.Cache.Remove(cacheKey);
            }

            public static void Clear()
            {
                try
                {
                    foreach (System.Collections.DictionaryEntry entry in HttpRuntime.Cache)
                    {
                        HttpContext.Current.Cache.Remove((string)entry.Key);
                    }
                }
                catch { }
            }

            #region Background Refresh Cache

            private static void ResetLastGet(string cacheKey)
            {
                try
                {
                    CacheManagerWorker cmk = null;
                    if (_CachemanagerWorkers.TryGetValue(cacheKey, out cmk))
                    {
                        if (cmk != null) { cmk.LastGetTime = DateTime.Now; }
                    }
                }
                catch { }
            }
            public class CacheManagerWorker
            {
                public CacheManagerWorker()
                {
                    this.RefreshMinute = 10.0d;
                    this.LastGetTime = DateTime.Now;
                    this.LastRefreshTime = DateTime.Now;
                    this.ExpiredMaxMinutes = 90;
                }

                public string cacheKey;
                public Func<dynamic, object> RefreshFunction;
                public Func<object, double> GetDynamicRefreshMinute;
                public dynamic Params;

                private double _RefreshMinute;
                public double RefreshMinute
                {
                    get { return _RefreshMinute; }
                    set
                    {
                        if (value < 0.1)
                        {
                            _RefreshMinute = 0.1;
                        }
                        else
                        {
                            _RefreshMinute = value;
                        }
                    }
                }
                public DateTime LastGetTime; //if the time is too long, that mean this cache do not need refresh again, should be cleared from the list
                public DateTime LastRefreshTime;

                private double _ExpiredMaxMinutes;
                public double ExpiredMaxMinutes
                {
                    get { return _ExpiredMaxMinutes; }
                    set
                    {
                        if (value < 1)
                        {
                            _ExpiredMaxMinutes = 1;
                        }
                        _ExpiredMaxMinutes = value;
                    }
                }

            }

            private static System.Timers.Timer _Timer = new System.Timers.Timer(10000);
            private static System.Timers.Timer _TimerClearExpired = new System.Timers.Timer(1800000);

            private static Dictionary<string, CacheManagerWorker> _CachemanagerWorkers = new Dictionary<string, CacheManagerWorker>();

            static CacheManager()
            {
                _Timer.Elapsed += new System.Timers.ElapsedEventHandler(_Timer_Elapsed);
                _TimerClearExpired.Elapsed += new System.Timers.ElapsedEventHandler(_TimerClearExpired_Elapsed);
                _Timer.Start();
                _TimerClearExpired.Start();
            }

            static void _Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
            {
                RefreshCacheWork();
            }
            static void _TimerClearExpired_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
            {
                ClearExpiredListWork();
            }

            private static bool _IsWorking = false;

            private static void RefreshCacheWork()
            {
                if (_IsWorking) return;
                _IsWorking = true;
                try
                {
                    DateTime now = DateTime.Now;

                    var works = _CachemanagerWorkers.Where(kv => kv.Value.LastRefreshTime.AddMinutes(kv.Value.RefreshMinute) < now).ToList();
                    if (works != null && works.Count > 0)
                    {
                        foreach (var item in works)
                        {
                            try
                            {
                                item.Value.LastRefreshTime = now;
                                var obj = item.Value.RefreshFunction(item.Value.Params);
                                if (obj != null)
                                {
                                    if (item.Value.GetDynamicRefreshMinute != null)
                                    {
                                        try
                                        {
                                            item.Value.RefreshMinute = item.Value.GetDynamicRefreshMinute(obj);
                                            if (item.Value.RefreshMinute < 1) { item.Value.RefreshMinute = 1; }
                                        }
                                        catch { }
                                    }


                                    Save(item.Value.cacheKey, obj, item.Value.ExpiredMaxMinutes);

                                }
                            }
                            catch { }
                        }
                    }

                }
                catch { }
                finally
                {
                    _IsWorking = false;
                }
            }

            private static void ClearExpiredListWork()
            {
                if (_IsWorking) return;
                _IsWorking = true;
                try
                {
                    DateTime now = DateTime.Now;

                    var works = _CachemanagerWorkers.Where(kv => kv.Value.LastGetTime.AddMinutes(kv.Value.ExpiredMaxMinutes) < now).ToList();
                    if (works != null && works.Count > 0)
                    {
                        foreach (var item in works)
                        {
                            try
                            {
                                _CachemanagerWorkers.Remove(item.Key);
                            }
                            catch { }
                        }
                    }

                }
                catch { }
                finally
                {
                    _IsWorking = false;
                }
            }

            public static void AddToBackgroupdRefreshList(CacheManagerWorker cacheRefreshWorker)
            {
                try
                {
                    if (_CachemanagerWorkers.ContainsKey(cacheRefreshWorker.cacheKey) == false)
                    {
                        _CachemanagerWorkers.Add(cacheRefreshWorker.cacheKey, cacheRefreshWorker);
                    }
                    else
                    {
                        _CachemanagerWorkers[cacheRefreshWorker.cacheKey] = cacheRefreshWorker;
                    }
                }
                catch { }
            }

            #endregion


        }
    }


}
