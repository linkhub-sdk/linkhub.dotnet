﻿/*
 * =================================================================================
 * Unit for develop interoperation with Linkhub APIs.
 * Functionalities are authentication for Linkhub api products, and to support
 * several base infomation(ex. Remain point).
 *
 * This library coded with .Net framework 3.5, To Process JSON and HMACSHA1.
 * If you need any other version of framework, plz contact with below.
 *
 * https://www.linkhub.co.kr
 * Author : Kim Seongjun
 * Written : 2014-09-22
 * Contributor : Jeong Yohan (code@linkhubcorp.com)
 * Updated : 2025-07-22
 * Thanks for your interest.
 *
 * =================================================================================
*/
using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Security.Cryptography;

namespace Linkhub
{
    public class Authority
    {
        private const String APIVersion = "2.0";
        private const String ServiceURL_REAL = "https://auth.linkhub.co.kr";
        private const String ServiceURL_REAL_Static = "https://static-auth.linkhub.co.kr";
        private const String ServiceURL_REAL_GA = "https://ga-auth.linkhub.co.kr";
        private const String ServiceURL_TEST = "https://demo.innopost.com";

        private String _LinkID;
        private bool _IsTest = false;
        private String _SecretKey;
        private String _AuthURL;

        public bool IsTest
        {
            get { return _IsTest; }
            set { _IsTest = value; }
        }

        public string AuthURL
        {
            get { return _AuthURL; }
            set { _AuthURL = value; }
        }

        public Authority(String LinkID, String SecretKey)
        {
            if (String.IsNullOrEmpty(LinkID)) throw new LinkhubException(-99999999, "NO LinkID");
            if (String.IsNullOrEmpty(SecretKey)) throw new LinkhubException(-99999999, "NO SecretKey");

            this._LinkID = LinkID;
            this._SecretKey = SecretKey;
        }

        public Token getToken(String ServiceID, String access_id, List<String> scope)
        {
            return getToken(ServiceID, access_id, scope, null, false, false, false);
        }

        public Token getToken(String ServiceID, String access_id, List<String> scope, String ForwardIP, bool UseStaticIP, bool UseLocalTimeYN)
        {
            return getToken(ServiceID, access_id, scope, ForwardIP, UseStaticIP, UseLocalTimeYN, false);
        }

        public Token getToken(String ServiceID, String access_id, List<String> scope, String ForwardIP, bool UseStaticIP, bool UseLocalTimeYN, bool UseGAIP)
        {
            if (String.IsNullOrEmpty(ServiceID)) throw new LinkhubException(-99999999, "NO ServiceID");

            Token result = new Token();

            String URL = getTargetURL(UseStaticIP, UseGAIP);

            String xDate = getTime(UseStaticIP, UseLocalTimeYN, UseGAIP);

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(URL + "/" + ServiceID + "/Token");

            request.Timeout = 180 * 1000;

            request.Headers.Add("x-lh-date", xDate);

            request.Headers.Add("x-lh-version", APIVersion);

            request.UserAgent = "DOTNET LINKHUB SDK";

            if (ForwardIP != null) request.Headers.Add("x-lh-forwarded", ForwardIP);

            TokenRequest _TR = new TokenRequest();

            _TR.access_id = access_id;
            _TR.scope = scope;

            String postData = "";

            using (MemoryStream ms = new MemoryStream())
            {
                DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(TokenRequest));
                ser.WriteObject(ms, _TR);
                ms.Seek(0, SeekOrigin.Begin);
                postData = new StreamReader(ms).ReadToEnd();
            }

            String HMAC_target = "POST\n";
            HMAC_target += Convert.ToBase64String(SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(postData))) + "\n";
            HMAC_target += xDate + "\n";
            if (ForwardIP != null) HMAC_target += ForwardIP + "\n";
            HMAC_target += APIVersion + "\n";
            HMAC_target += "/" + ServiceID + "/Token";
            HMACSHA256 hmac = new HMACSHA256(Convert.FromBase64String(_SecretKey));

            String bearerToken = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(HMAC_target)));

            request.Headers.Add("Authorization", "LINKHUB" + " " + _LinkID + " " + bearerToken);

            request.Method = "POST";

            byte[] btPostDAta = Encoding.UTF8.GetBytes(postData);

            request.ContentLength = btPostDAta.Length;

            request.GetRequestStream().Write(btPostDAta, 0, btPostDAta.Length);

            try
            {
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                Stream stReadData = response.GetResponseStream();
                DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(Token));
                result = (Token)ser.ReadObject(stReadData);

            }
            catch (Exception we)
            {
                if (we is WebException && ((WebException)we).Response != null)
                {
                    Stream stReadData = ((WebException)we).Response.GetResponseStream();
                    DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(Error));
                    Error t = (Error)ser.ReadObject(stReadData);
                    throw new LinkhubException(t.code, t.message);
                }
                throw new LinkhubException(-99999999, we.Message);
            }

            return result;
        }

        public String getTime()
        {
            return getTime(false, false, false);
        }

        public String getTime(bool UseStaticIP, bool UseLocalTimeYN)
        {
            return getTime(UseStaticIP, UseLocalTimeYN, false);
        }

        public String getTime(bool UseStaticIP, bool UseLocalTimeYN, bool UseGAIP)
        {
            if (UseLocalTimeYN)
            {
                DateTime localTime = DateTime.UtcNow;

                return localTime.ToString("yyyy-MM-ddTHH:mm:ssZ");
            }
            else
            {
                String URL = getTargetURL(UseStaticIP, UseGAIP);

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(URL + "/Time");

                request.Timeout = 180 * 1000;

                request.Method = "GET";

                request.UserAgent = "DOTNET LINKHUB SDK";

                try
                {
                    HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                    using (Stream stream = response.GetResponseStream())
                    {
                        StreamReader reader = new StreamReader(stream, Encoding.UTF8);

                        return reader.ReadToEnd();
                    }

                }
                catch (WebException we)
                {
                    if (we.Response != null)
                    {
                        Stream stReadData = we.Response.GetResponseStream();

                        DataContractJsonSerializer ser2 = new DataContractJsonSerializer(typeof(Error));

                        Error t = (Error)ser2.ReadObject(stReadData);

                        throw new LinkhubException(t.code, t.message);
                    }
                    throw new LinkhubException(-99999999, we.Message);

                }
            }
        }

        public Double getBalance(String BearerToken, String ServiceID)
        {
            return getBalance(BearerToken, ServiceID, false, false);
        }

        public Double getBalance(String BearerToken, String ServiceID, bool UseStaticIP)
        {
            return getBalance(BearerToken, ServiceID, UseStaticIP, false);
        }

        public Double getBalance(String BearerToken, String ServiceID, bool UseStaticIP, bool UseGAIP)
        {
            if (String.IsNullOrEmpty(ServiceID)) throw new LinkhubException(-99999999, "NO ServiceID");
            if (String.IsNullOrEmpty(BearerToken)) throw new LinkhubException(-99999999, "NO BearerToken");

            String URL = getTargetURL(UseStaticIP, UseGAIP);

            String URI = URL + "/" + ServiceID + "/Point";

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(URI);

            request.Timeout = 180 * 1000;

            request.Headers.Add("Authorization", "Bearer" + " " + BearerToken);

            request.Method = "GET";

            request.UserAgent = "DOTNET LINKHUB SDK";

            try
            {
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                Stream stReadData = response.GetResponseStream();

                DataContractJsonSerializer ser2 = new DataContractJsonSerializer(typeof(PointResult));

                PointResult result = (PointResult)ser2.ReadObject(stReadData);

                return result.remainPoint;

            }
            catch (WebException we)
            {
                if (we.Response != null)
                {
                    Stream stReadData = we.Response.GetResponseStream();

                    DataContractJsonSerializer ser2 = new DataContractJsonSerializer(typeof(Error));

                    Error t = (Error)ser2.ReadObject(stReadData);

                    throw new LinkhubException(t.code, t.message);
                }
                throw new LinkhubException(-99999999, we.Message);

            }
        }

        public Double getPartnerBalance(String BearerToken, String ServiceID)
        {
            return getPartnerBalance(BearerToken, ServiceID, false, false);
        }

        public Double getPartnerBalance(String BearerToken, String ServiceID, bool UseStaticIP)
        {
            return getPartnerBalance(BearerToken, ServiceID, UseStaticIP, false);
        }

        public Double getPartnerBalance(String BearerToken, String ServiceID, bool UseStaticIP, bool UseGAIP)
        {
            if (String.IsNullOrEmpty(ServiceID)) throw new LinkhubException(-99999999, "NO ServiceID");
            if (String.IsNullOrEmpty(BearerToken)) throw new LinkhubException(-99999999, "NO BearerToken");

            String URL = getTargetURL(UseStaticIP, UseGAIP);

            String URI = URL + "/" + ServiceID + "/PartnerPoint";

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(URI);

            request.Timeout = 180 * 1000;

            request.Headers.Add("Authorization", "Bearer" + " " + BearerToken);

            request.Method = "GET";

            request.UserAgent = "DOTNET LINKHUB SDK";

            try
            {
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                Stream stReadData = response.GetResponseStream();

                DataContractJsonSerializer ser2 = new DataContractJsonSerializer(typeof(PointResult));

                PointResult result = (PointResult)ser2.ReadObject(stReadData);

                return result.remainPoint;

            }
            catch (WebException we)
            {
                if (we.Response != null)
                {
                    Stream stReadData = we.Response.GetResponseStream();

                    DataContractJsonSerializer ser2 = new DataContractJsonSerializer(typeof(Error));

                    Error t = (Error)ser2.ReadObject(stReadData);

                    throw new LinkhubException(t.code, t.message);
                }

                throw new LinkhubException(-99999999, we.Message);

            }
        }

        public String getPartnerURL(String BearerToken, String ServiceID, String TOGO)
        {
            return getPartnerURL(BearerToken, ServiceID, TOGO, false, false);
        }

        public String getPartnerURL(String BearerToken, String ServiceID, String TOGO, bool UseStaticIP)
        {
            return getPartnerURL(BearerToken, ServiceID, TOGO, UseStaticIP, false);
        }

        public String getPartnerURL(String BearerToken, String ServiceID, String TOGO, bool UseStaticIP, bool UseGAIP)
        {
            String URL = getTargetURL(UseStaticIP, UseGAIP);

            String URI = URL + "/" + ServiceID + "/URL?TG=" + TOGO;

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(URI);

            request.Timeout = 180 * 1000;

            request.Headers.Add("Authorization", "Bearer" + " " + BearerToken);

            request.Method = "GET";

            request.UserAgent = "DOTNET LINKHUB SDK";

            try
            {
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                Stream stReadData = response.GetResponseStream();

                DataContractJsonSerializer ser2 = new DataContractJsonSerializer(typeof(URLResult));

                URLResult result = (URLResult)ser2.ReadObject(stReadData);

                return result.url;

            }
            catch (WebException we)
            {
                if (we.Response != null)
                {
                    Stream stReadData = we.Response.GetResponseStream();

                    DataContractJsonSerializer ser2 = new DataContractJsonSerializer(typeof(Error));

                    Error t = (Error)ser2.ReadObject(stReadData);

                    throw new LinkhubException(t.code, t.message);
                }

                throw new LinkhubException(-99999999, we.Message);

            }
        }

        private String getTargetURL(bool UseStaticIP, bool UseGAIP)
        {
            if (_AuthURL != null)
            {
                return _AuthURL;
            }

            if (UseGAIP)
            {
                return ServiceURL_REAL_GA;
            }
            else if (UseStaticIP)
            {
                return ServiceURL_REAL_Static;
            }
            else
            {
                return ServiceURL_REAL;
            }
        }

        [DataContract]
        private class PointResult
        {
            private Double _remainPoint;

            [DataMember]
            public Double remainPoint
            {
                get { return _remainPoint; }
                set { _remainPoint = value; }
            }
        }

        [DataContract]
        private class Error
        {
            private long _code;

            [DataMember]
            public long code
            {
                get { return _code; }
                set { _code = value; }
            }

            private String _message;

            [DataMember]
            public String message
            {
                get { return _message; }
                set { _message = value; }
            }

        }

        [DataContract]
        public class URLResult
        {
            [DataMember]
            public String url;
        }

        [DataContract]
        private class TokenRequest
        {
            private String _access_id;
            private List<String> _scope;

            [DataMember]
            public String access_id
            {
                get { return _access_id; }
                set { _access_id = value; }
            }
            [DataMember]
            public List<String> scope
            {
                get { return _scope; }
                set { _scope = value; }
            }

        }

    }
}
