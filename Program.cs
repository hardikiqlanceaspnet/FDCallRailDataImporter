
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;


namespace FDCallRailDataImporter
{
    internal class Program
    {
        public static string connString = "Data Source=10.10.10.15;Initial Catalog=FDCRM;Max Pool Size=100;Persist Security Info=True;User ID=crmuser;Password=hhf85282$;";
        public static string accountid = "522658321";
        public static string APITOken = "4c295251d59becdc4520f2b3d42985e5";
        static void Main(string[] args)
        {
            Console.WriteLine("------------------------------------------");
            Console.WriteLine("Started Importing CallRAil Log Data");
            ImportCallRailData();
            Console.WriteLine("Finished Importing CallRAil Log Data");
        }

        private static HttpWebResponse CallWebAPIResponseWithErrorDetailsNew(string url, string contentType, out string responseString, string methodType = "GET", string postData = "", string header = "", string header1 = "", string accept = "", string BearerToken = "")
        {
            System.Net.WebResponse response1 = null;
            //ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls | SecurityProtocolType.Tls12;
            //ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            HttpWebRequest request = WebRequest.Create(new Uri(url)) as HttpWebRequest;
            responseString = string.Empty;
            string responseCode = string.Empty;
            methodType = string.IsNullOrWhiteSpace(methodType) ? WebRequestMethods.Http.Get : methodType;


            HttpWebResponse response = null;
            try
            {
                request.Method = methodType;
                request.ContentType = contentType;
                request.Proxy = null;

                if (!string.IsNullOrEmpty(accept))
                {
                    request.Accept = accept;
                }
                if (!string.IsNullOrEmpty(header))
                {
                    request.Headers.Add(header);
                }
                if (!string.IsNullOrEmpty(header1))
                {
                    request.Headers.Add(header1);
                }
                if (!string.IsNullOrEmpty(BearerToken))
                {
                    request.Headers.Add("Authorization", "Bearer " + BearerToken);
                }
                if (!string.IsNullOrWhiteSpace(postData))
                {
                    var data = Encoding.ASCII.GetBytes(postData);
                    request.ContentLength = data.Length;
                    using (var stream = request.GetRequestStream())
                    {
                        stream.Write(data, 0, data.Length);
                    }
                }

                response = (HttpWebResponse)request.GetResponse();
                if (response != null)
                {
                    responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();
                    responseCode = ((System.Net.HttpWebResponse)(response)).StatusCode.ToString();
                }

            }
            catch (WebException e)
            {


                response1 = e.Response;
                HttpWebResponse httpResponse = (HttpWebResponse)response1;
                using (Stream data = response1.GetResponseStream())
                using (var reader = new StreamReader(data))
                {
                    responseString = new StreamReader(response1.GetResponseStream()).ReadToEnd();
                    responseCode = ((System.Net.HttpWebResponse)(response1)).StatusCode.ToString();

                }
                response = httpResponse;

            }
            finally
            {

            }
            return response;
        }
        public static DataTable ConvertToDataTable<T>(IEnumerable<T> iList)
        {
            DataTable table = new DataTable();
            PropertyDescriptorCollection properties = TypeDescriptor.GetProperties((Type)typeof(T));
            int num = 0;
            while (true)
            {
                if (num >= properties.Count)
                {
                    object[] values = new object[properties.Count];
                    foreach (T local in iList)
                    {
                        int index = 0;
                        while (true)
                        {
                            if (index >= values.Length)
                            {
                                table.Rows.Add(values);
                                break;
                            }
                            values[index] = properties[index].GetValue(local);
                            index++;
                        }
                    }
                    return table;
                }
                PropertyDescriptor descriptor = properties[num];
                Type propertyType = descriptor.PropertyType;
                if (propertyType.IsGenericType && (propertyType.GetGenericTypeDefinition() == typeof(Nullable<>)))
                {
                    propertyType = Nullable.GetUnderlyingType(propertyType);
                }
                table.Columns.Add(descriptor.Name, propertyType);
                num++;
            }
        }

        public static string ConvertToUSPhoneNumber(string value)
        {
            string str;
            if (string.IsNullOrEmpty(value))
            {
                str = string.Empty;
            }
            else
            {
                value = new Regex(@"\D").Replace(value, string.Empty);
                value = value.TrimStart('1');
                str = (value.Length != 7) ? ((value.Length != 10) ? ((value.Length <= 10) ? value : ((long)Convert.ToInt64(value)).ToString("###-###-#### " + ((string)new string('#', value.Length - 10)))) : ((long)Convert.ToInt64(value)).ToString("###-###-####")) : ((long)Convert.ToInt64(value)).ToString("###-####");
            }
            return str;
        }

        public static int ExecuteNonQuery(string procedureName, SqlParameter[] para, CommandType commandType = CommandType.StoredProcedure)
        {
            using (SqlConnection connection = new SqlConnection(connString))
            {
                SqlCommand command = new SqlCommand
                {
                    CommandTimeout = 0,
                    CommandType = commandType,
                    Connection = connection
                };
                command.Connection.Open();
                command.CommandText = procedureName;
                if ((para != null) && (para.Length != 0))
                {
                    command.Parameters.AddRange(para);
                }
                int num = command.ExecuteNonQuery();
                command.Connection.Close();
                return num;
            }
        }

        public static DateTime GetLastDateFromCallRailTbl()
        {
            DateTime time3;
            DateTime time = DateTime.Now.AddMonths(-2);
            SqlConnection connection = new SqlConnection(connString);
            connection.Open();
            SqlCommand command = new SqlCommand("Select top 1 * from CallRailLogs order by start_time desc", connection);
            SqlDataReader reader = command.ExecuteReader();
            if (!reader.Read())
            {
                reader.Close();
                command.Dispose();
                connection.Close();
                time3 = time;
            }
            else if (!reader.HasRows)
            {
                reader.Close();
                command.Dispose();
                connection.Close();
                time3 = time;
            }
            else
            {
                time = Convert.ToDateTime(reader["start_time"]);
                reader.Close();
                command.Dispose();
                connection.Close();
                time3 = time;
            }
            return time3;
        }

        public static WebResponse GetWebAPIResponseWithErrorDetails(string url, string contentType, string methodType = "GET", string postData = "", string header = "", string header1 = "", string accept = "", string BrarerToken = "")
        {
            WebResponse objWebResponse = new WebResponse();
            string responseString = string.Empty;
            var response = CallWebAPIResponseWithErrorDetailsNew(url, contentType, out responseString, methodType, postData, header, header1, accept, BrarerToken);
            if (response != null)
            {
                objWebResponse.ResponseCode = ((System.Net.HttpWebResponse)(response)).StatusCode;
                objWebResponse.ResponseString = responseString;
            }
            return objWebResponse;
        }

        public static void ImportCallRailData()
        {
            DateTime lastDateFromCallRailTbl = GetLastDateFromCallRailTbl();
            DateTime time2 = DateTime.Now;
            Console.WriteLine("Getting CallRAil Log Data From Date : " + lastDateFromCallRailTbl.ToString("dd/MM/yyyy") + " To Date : " + time2.ToString("dd/MM/yyyy"));
            string[] textArray1 = new string[] { "https://api.callrail.com/v3/a/", (string)accountid, "/calls.json?fields=company_id,company_name,source,source_name,device_type,call_type,created_at,campaign,utm_source,utm_medium,utm_term,utm_content,utm_campaign&per_page=25&start_date=", (string)lastDateFromCallRailTbl.ToString(), "&end_date=", (string)time2.ToString() };
            string url = string.Concat((string[])textArray1);
            Console.WriteLine("Getting CallRAil Log Data for : " + url);
            WebResponse response = GetWebAPIResponseWithErrorDetails(url, "application/json; charset=utf-8", "GET", "", "Authorization: Token token=" + APITOken, "", "", "");
            Console.WriteLine("Response for Above APIURL : " + response.ResponseString);
            CallResponse response2 = JsonConvert.DeserializeObject<CallResponse>(response.ResponseString);
            if (response2.calls != null)
            {
                string path = @"C:\Web\sharedfiles\recordings\";
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                DataTable table = ConvertToDataTable<Call>((IEnumerable<Call>)response2.calls);
                foreach (DataRow row in table.Rows)
                {
                    string str3 = row["id"].ToString();
                    row["customer_phone_number"] = ConvertToUSPhoneNumber(row["customer_phone_number"].ToString());
                    row["business_phone_number"] = ConvertToUSPhoneNumber(row["business_phone_number"].ToString());
                    row["tracking_phone_number"] = ConvertToUSPhoneNumber(row["tracking_phone_number"].ToString());
                    string str4 = row["recording_player"].ToString().Replace("?access_key=", "/redirect?access_key=");
                    DateTime time3 = Convert.ToDateTime(row["start_time"]);
                    string str5 = path + time3.ToString("MM-dd-yyyy");
                    if (!Directory.Exists(str5))
                    {
                        Directory.CreateDirectory(str5);
                    }
                    if (!string.IsNullOrEmpty(str4))
                    {
                        string str6 = str5 + @"\" + str3 + ".mp3";
                        if (System.IO.File.Exists(str6))
                        {
                            string[] textArray2 = new string[] { "~/recordings/", (string)Convert.ToDateTime(row["start_time"]).ToString("MM-dd-yyyy"), "/", (string)str3, ".mp3" };
                            string[] strArray = textArray2;
                            row["recording"] = string.Concat(strArray);
                            Console.WriteLine("Recording Already Exist at : " + str6);
                        }
                        else
                        {
                            try
                            {
                                MemoryStream stream = new MemoryStream();
                                ((HttpWebResponse)((HttpWebRequest)WebRequest.Create(str4)).GetResponse()).GetResponseStream().CopyTo((Stream)stream);
                                byte[] bytes = stream.ToArray();
                                if (bytes.Length != 0)
                                {
                                    System.IO.File.WriteAllBytes(str6, bytes);
                                    string[] strArray2 = new string[] { "~/recordings/", time3.ToString("MM-dd-yyyy"), "/", str3, ".mp3" };
                                    time3 = Convert.ToDateTime(row["start_time"]);
                                    row["recording"] = string.Concat(strArray2);
                                    Console.WriteLine("Recording Saved at : " + str6);
                                }
                            }
                            catch (Exception exception1)
                            {
                                Console.WriteLine(exception1.Message);
                            }
                        }
                    }
                }
                SqlParameter[] para = new SqlParameter[] { new SqlParameter("@tblLog", table) };
                ExecuteNonQuery("ImportCallRailLogData", para, CommandType.StoredProcedure);
                int num = response2.total_pages;
                if (num <= 1)
                {
                    Console.WriteLine("No Call Records for Above APIURL");
                    Thread.Sleep(5000);
                    Environment.Exit(0);
                }
                else
                {
                    int num2 = 2;
                    while (true)
                    {
                        if (num2 > num)
                        {
                            break;
                        }
                        string[] textArray3 = new string[] { "https://api.callrail.com/v3/a/", (string)accountid, "/calls.json?fields=company_id,company_name,source,source_name,device_type,call_type,created_at,campaign,utm_source,utm_medium,utm_term,utm_content,utm_campaign&page=", (string)((int)num2).ToString(), "&per_page=25&start_date=", (string)lastDateFromCallRailTbl.ToString(), "&end_date=", (string)time2.ToString() };
                        url = string.Concat((string[])textArray3);
                        Console.WriteLine("Getting CallRAil Log Data for : " + url);
                        response = GetWebAPIResponseWithErrorDetails(url, "application/json; charset=utf-8", "GET", "", "Authorization: Token token=" + APITOken, "", "", "");
                        Console.WriteLine("Response for Above APIURL : " + response.ResponseString);
                        response2 = JsonConvert.DeserializeObject<CallResponse>(response.ResponseString);
                        if (response2.calls != null)
                        {
                            table = ConvertToDataTable<Call>((IEnumerable<Call>)response2.calls);
                            foreach (DataRow row2 in table.Rows)
                            {
                                string str7 = row2["id"].ToString();
                                row2["customer_phone_number"] = ConvertToUSPhoneNumber(row2["customer_phone_number"].ToString());
                                row2["business_phone_number"] = ConvertToUSPhoneNumber(row2["business_phone_number"].ToString());
                                row2["tracking_phone_number"] = ConvertToUSPhoneNumber(row2["tracking_phone_number"].ToString());
                                string str8 = row2["recording_player"].ToString().Replace("?access_key=", "/redirect?access_key=");
                                string str9 = path + Convert.ToDateTime(row2["start_time"]).ToString("MM-dd-yyyy");
                                if (!Directory.Exists(str9))
                                {
                                    Directory.CreateDirectory(str9);
                                }
                                if (!string.IsNullOrEmpty(str8))
                                {
                                    string str10 = str9 + @"\" + str7 + ".mp3";
                                    if (!System.IO.File.Exists(str10))
                                    {
                                        MemoryStream stream2 = new MemoryStream();
                                        ((HttpWebResponse)((HttpWebRequest)WebRequest.Create(str8)).GetResponse()).GetResponseStream().CopyTo((Stream)stream2);
                                        byte[] bytes = stream2.ToArray();
                                        if (bytes.Length != 0)
                                        {
                                            System.IO.File.WriteAllBytes(str10, bytes);
                                            string[] textArray4 = new string[5];
                                            textArray4[0] = "~/recordings/";
                                            DateTime time4 = Convert.ToDateTime(row2["start_time"]);
                                            textArray4[1] = (string)time4.ToString("MM-dd-yyyy");
                                            textArray4[2] = "/";
                                            textArray4[3] = (string)str7;
                                            textArray4[0x4] = ".mp3";
                                            string[] strArray3 = textArray4;
                                            row2["recording"] = string.Concat(strArray3);
                                            Console.WriteLine("Recording Saved at : " + str10);
                                        }
                                    }
                                    else
                                    {
                                        string[] textArray2 = new string[] { "~/recordings/", (string)Convert.ToDateTime(row2["start_time"]).ToString("MM-dd-yyyy"), "/", (string)str7, ".mp3" };
                                        string[] strArray = textArray2;
                                        row2["recording"] = string.Concat(strArray);
                                        Console.WriteLine("Recording Already Exist at : " + str10);
                                    }
                                }
                            }
                            para = new SqlParameter[] { new SqlParameter("@tblLog", table) };
                            ExecuteNonQuery("ImportCallRAilLogData", para, CommandType.StoredProcedure);
                        }
                        num2++;
                    }

                    Console.WriteLine("All records processed. Exiting the application...");
                    Thread.Sleep(5000); // Optional delay before exit
                    Environment.Exit(0);
                }
            }
            else
            {
                Console.WriteLine("No Call Records for Above APIURL");
                Thread.Sleep(5000);
                Environment.Exit(0);
            }
        }

        public static void ImportCallRailDataRecordingforOldRecords()
        {
            CallResponse response2 = JsonConvert.DeserializeObject<CallResponse>(GetWebAPIResponseWithErrorDetails("https://api.callrail.com/v3/a/" + accountid + "/calls.json?fields=company_id,company_name,source,source_name,device_type,call_type,created_at,campaign,utm_source,utm_medium,utm_term,utm_content,utm_campaign&per_page=250", "application/json; charset=utf-8", "GET", "", "Authorization: Token token=" + APITOken, "", "", "").ResponseString);
            if (response2.calls != null)
            {
                string path = @"E:\recordings\";
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                DataTable table = ConvertToDataTable<Call>((IEnumerable<Call>)response2.calls);
                foreach (DataRow row in table.Rows)
                {
                    row["customer_phone_number"] = ConvertToUSPhoneNumber(row["customer_phone_number"].ToString());
                    row["business_phone_number"] = ConvertToUSPhoneNumber(row["business_phone_number"].ToString());
                    row["tracking_phone_number"] = ConvertToUSPhoneNumber(row["tracking_phone_number"].ToString());
                    string str3 = row["id"].ToString();
                    string str4 = row["recording_player"].ToString().Replace("?access_key=", "/redirect?access_key=");
                    DateTime time = Convert.ToDateTime(row["start_time"]);
                    string str5 = path + time.ToString("MM-dd-yyyy");
                    if (!Directory.Exists(str5))
                    {
                        Directory.CreateDirectory(str5);
                    }
                    if (!string.IsNullOrEmpty(str4))
                    {
                        string str6 = str5 + @"\" + str3 + ".mp3";
                        using (new WebClient())
                        {
                            if (!System.IO.File.Exists(str6))
                            {
                                MemoryStream stream2 = new MemoryStream();
                                ((HttpWebResponse)((HttpWebRequest)WebRequest.Create(str4)).GetResponse()).GetResponseStream().CopyTo((Stream)stream2);
                                byte[] bytes = stream2.ToArray();
                                if ((bytes.Length != 0) && (bytes.Length != 8370))
                                {
                                    System.IO.File.WriteAllBytes(str6, bytes);
                                    row["recording"] = str6;
                                }
                            }
                        }
                    }
                }
                SqlParameter[] para = new SqlParameter[] { new SqlParameter("@tblLog", table) };
                ExecuteNonQuery("UpdateCallRailLogRecordingPath", para, CommandType.StoredProcedure);
                int num = response2.total_pages;
                if (num > 1)
                {
                    int num2 = 2;
                    while (true)
                    {
                        if (num2 > num)
                        {
                            break;
                        }
                        string[] textArray1 = new string[] { "https://api.callrail.com/v3/a/", (string)accountid, "/calls.json?fields=company_id,company_name,source,source_name,device_type,call_type,created_at,campaign,utm_source,utm_medium,utm_term,utm_content,utm_campaign&page=", (string)((int)num2).ToString(), "&per_page=250" };
                        string url = string.Concat((string[])textArray1);
                        WebResponse response = GetWebAPIResponseWithErrorDetails(url, "application/json; charset=utf-8", "GET", "", "Authorization: Token token=" + APITOken, "", "", "");
                        response2 = JsonConvert.DeserializeObject<CallResponse>(response.ResponseString);
                        table = ConvertToDataTable<Call>((IEnumerable<Call>)response2.calls);
                        foreach (DataRow row2 in table.Rows)
                        {
                            row2["customer_phone_number"] = ConvertToUSPhoneNumber(row2["customer_phone_number"].ToString());
                            row2["business_phone_number"] = ConvertToUSPhoneNumber(row2["business_phone_number"].ToString());
                            row2["tracking_phone_number"] = ConvertToUSPhoneNumber(row2["tracking_phone_number"].ToString());
                            string str7 = row2["id"].ToString();
                            string str8 = row2["recording_player"].ToString().Replace("?access_key=", "/redirect?access_key=");
                            string str9 = path + Convert.ToDateTime(row2["start_time"]).ToString("MM-dd-yyyy");
                            if (!Directory.Exists(str9))
                            {
                                Directory.CreateDirectory(str9);
                            }
                            if (!string.IsNullOrEmpty(str8))
                            {
                                string str10 = str9 + @"\" + str7 + ".mp3";
                                if (!System.IO.File.Exists(str10))
                                {
                                    MemoryStream stream4 = new MemoryStream();
                                    ((HttpWebResponse)((HttpWebRequest)WebRequest.Create(str8)).GetResponse()).GetResponseStream().CopyTo((Stream)stream4);
                                    byte[] bytes = stream4.ToArray();
                                    if ((bytes.Length != 0) && (bytes.Length != 0x20b2))
                                    {
                                        System.IO.File.WriteAllBytes(str10, bytes);
                                        row2["recording"] = str10;
                                    }
                                }
                            }
                        }
                        para = new SqlParameter[] { new SqlParameter("@tblLog", table) };
                        ExecuteNonQuery("UpdateCallRailLogRecordingPath", para, CommandType.StoredProcedure);
                        num2++;
                    }
                }
            }
        }

        public class Call
        {
            public bool answered { get; set; }

            public string business_phone_number { get; set; }

            public string customer_city { get; set; }

            public string customer_country { get; set; }

            public string customer_name { get; set; }

            public string customer_phone_number { get; set; }

            public string customer_state { get; set; }

            public string direction { get; set; }

            public int duration { get; set; }

            public string id { get; set; }

            public string recording { get; set; }

            public string recording_duration { get; set; }

            public string recording_player { get; set; }

            public DateTime start_time { get; set; }

            public string tracking_phone_number { get; set; }

            public bool voicemail { get; set; }

            public string agent_email { get; set; }

            public string company_id { get; set; }

            public string company_name { get; set; }

            public string source { get; set; }

            public string source_name { get; set; }

            public string device_type { get; set; }

            public string call_type { get; set; }

            public DateTime created_at { get; set; }

            public string campaign { get; set; }

            public string utm_source { get; set; }

            public string utm_medium { get; set; }

            public string utm_term { get; set; }

            public string utm_content { get; set; }

            public string utm_campaign { get; set; }
        }

        public class CallResponse
        {
            public int page { get; set; }

            public int per_page { get; set; }

            public int total_pages { get; set; }

            public int total_records { get; set; }

            public List<Program.Call> calls { get; set; }
        }

        public class RecordingResponse
        {
            public string url { get; set; }
        }

        public class WebResponse
        {
            public string ResponseString { get; set; }

            public HttpStatusCode ResponseCode { get; set; }
        }
    }
}
