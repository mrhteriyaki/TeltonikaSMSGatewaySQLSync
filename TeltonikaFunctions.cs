using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mail;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace TeltonikaService
{
    public class TeltonikaFunctions
    {
        static string username;// = "smsuser";
        static string password;// = "sms3131";
        static string gateway;// = "192.168.2.1";
        static string sqlserver; // = "Data Source=MRH-DC,1433;Initial Catalog=SMS;Integrated Security=true";
        static string modem_id;

        static string smtpserver;
        static string fromaddress;
        static string toaddress;

        public static void LoadConfig()
        {

            string AppDirectory = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().CodeBase).Replace("file:\\", "");
            Console.WriteLine("Running from:" + AppDirectory);

            foreach (string line in File.ReadAllLines(AppDirectory + "\\config.ini"))
            {
                if (line.StartsWith("username="))
                {
                    username = line.Substring(9);
                }
                else if (line.StartsWith("password="))
                {
                    password = line.Substring(9);
                }
                else if (line.StartsWith("gateway="))
                {
                    gateway = line.Substring(8);
                }
                else if (line.StartsWith("sqlserver="))
                {
                    sqlserver = line.Substring(10);
                }
                else if (line.StartsWith("smtpserver="))
                {
                    smtpserver = line.Substring(11);
                }
                else if (line.StartsWith("fromaddress="))
                {
                    fromaddress = line.Substring(12);
                }
                else if (line.StartsWith("toaddress="))
                {
                    toaddress = line.Substring(10);
                }
                else if (line.StartsWith("modem="))
                {
                    modem_id = line.Substring(6);
                }
            }
            Console.WriteLine("Config Loaded");

            //Test SQL Connection.
            SqlConnection conn = new SqlConnection(sqlserver);
            conn.Open();
            conn.Close();
            Console.WriteLine("SQL Connection OK");

        }



        public class SMSMessageLegacy
        {
            public int index;
            public DateTime datetime;
            public string number;
            public string text;
            public string status;
        }

        public class SMSInboundMessage
        {
            public string id;
            public string modem_id;
            public string date;
            public string sender;
            public string message;
            public string status;
        }

        public class SMSOutboundMessage
        {
            public int id;
            public string number;
            public string message;
        }

        static public void SendSMS(string number, string message)
        {
            if (string.IsNullOrEmpty(modem_id))
            {
                throw new Exception("modem id not set");
            }

            string urlmsg = HttpUtility.UrlEncode(message);
            //GetRequest("sms_send", "number=" + number + "&text=" + urlmsg);
            string sendjson = "{\"data\": { \"number\": \"" + number + "\",\"message\":\"" + message + "\",\"modem\":\"" + modem_id + "\"}}";
            PostRequest("messages/action/send", sendjson);

        }

        static public void ProcessOutbox()
        {
            SqlConnection conn = new SqlConnection(sqlserver);
            SqlCommand sqlcmd = new SqlCommand();
            sqlcmd.Connection = conn;

            conn.Open();
            sqlcmd.CommandText = "SELECT id,number,message FROM tblOutbox";
            List<SMSOutboundMessage> outbox_list = new List<SMSOutboundMessage>();
            SqlDataReader SQLOutput = sqlcmd.ExecuteReader();
            while (SQLOutput.Read())
            {
                SMSOutboundMessage tmpMsg = new SMSOutboundMessage();
                tmpMsg.id = SQLOutput.GetInt32(0);
                tmpMsg.number = SQLOutput[1].ToString();
                tmpMsg.message = SQLOutput[2].ToString();
                outbox_list.Add(tmpMsg);
            }
            SQLOutput.Close();

            foreach (SMSOutboundMessage msg in outbox_list)
            {
                SendSMS(msg.number, msg.message);

                //Remove from DB.
                sqlcmd.CommandText = "INSERT INTO tblSent (timestamp,destination,message) VALUES (GETDATE(),@msgnumber,@msgtext); DELETE FROM tblOutbox WHERE id = @mid";
                sqlcmd.Parameters.AddWithValue("@mid", msg.id);
                sqlcmd.Parameters.AddWithValue("@msgnumber", msg.number);
                sqlcmd.Parameters.AddWithValue("@msgtext", msg.message);

                sqlcmd.ExecuteNonQuery();
                sqlcmd.Parameters.Clear();
            }
            conn.Close();
        }

        class TelMessageList
        {
            public bool success;
            public List<SMSInboundMessage> data;
        }

        static public void GetMessageList()
        {
            //string MessageData = SendRequest("sms_list").Replace("\r", ""); //Legacy API
            //string[] MessageDataLines = MessageData.Split('\n');

            string MessageData = GetRequest("messages/status");
            var msgList = Newtonsoft.Json.JsonConvert.DeserializeObject<TelMessageList>(MessageData);

            /*
            SMSInboundMessage message = new SMSInboundMessage();
            List<SMSInboundMessage> message_list = new List<SMSInboundMessage>();
            foreach (string Line in msgList)
            {
                if (Line.StartsWith("Index: "))
                {
                    message = new SMSMessage();
                    message.index = int.Parse(Line.Substring(7));
                    continue;
                }
                else if (Line.StartsWith("Date: "))
                {
                    message.datetime = DateTime.Parse(Line.Substring(6));
                }
                else if (Line.StartsWith("Sender: "))
                {
                    message.number = Line.Substring(8);
                }
                else if (Line.StartsWith("Text: "))
                {
                    message.text = Line.Substring(6);
                }
                else if (Line.StartsWith("Status: "))
                {
                    message.status = Line.Substring(8);
                }
                else if (Line.StartsWith("------------------------------"))
                {
                    //Terminator - add message to list.
                    message_list.Add(message);
                }
                else
                {
                    message.text = message.text + Line + "\n";
                }
            }
            */

            foreach (var msg in msgList.data)
            {
                Console.WriteLine("Logged message from " + msg.sender);
                DBAddMessage(msg);
                DeleteMessage(msg.id);

                try
                {
                    SendEmail(msg.sender, msg.date.ToString(), msg.message);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Failed to send email: " + ex.ToString());
                }
            }

        }


        public static void SendEmail(string sender, string datetime, string message)
        {
            if (string.IsNullOrEmpty(smtpserver))
            {
                return; //Skip email if smtp not configured.
            }

            string htmlbody = "<body>From: " + sender + "<br>" + "Timestamp: " + datetime + "<br>Message:<br>" + message.Replace("\n", "<br>") + "</body>";

            SmtpClient Smtp_Server = new SmtpClient();
            MailMessage e_mail = new MailMessage();
            Smtp_Server.Port = 25;
            Smtp_Server.EnableSsl = true;
            Smtp_Server.Host = smtpserver;

            MailAddress FA = new MailAddress(fromaddress);
            e_mail.From = FA;
            e_mail.To.Add(toaddress);
            e_mail.Subject = "SMS from" + sender;
            e_mail.IsBodyHtml = true;
            e_mail.Body = htmlbody;

            Smtp_Server.Send(e_mail);

        }



        public static void DeleteMessage(string msgid)
        {
            // SendRequest("sms_delete", "number=" + index.ToString());
            string deletereq = "{\"data\":{\"modem_id\":\"" + modem_id + "\",\"sms_id\":[\"" + msgid + "\"]}}";
            Console.WriteLine(deletereq);
            PostRequest("messages/actions/remove_messages", deletereq);
        }

        private static HttpClient client = new HttpClient();
        public static async Task<string> HttpPostAsync(string url, string jsonBody, bool useAuth = false)
        {

            using (var content = new StringContent(jsonBody, Encoding.UTF8, "application/json"))
            {
                var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Content = content;
                if (useAuth)
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authtoken);
                }
                HttpResponseMessage response = await client.SendAsync(request);
                Console.WriteLine(url);
                Console.WriteLine(jsonBody);
                Console.WriteLine("use auth " + useAuth);
                response.EnsureSuccessStatusCode(); // throws if not 2xx
                string responseBody = await response.Content.ReadAsStringAsync();

                return responseBody;
            }

        }

        public static void UpdateAuth()
        {
            if (authtoken_expiry < DateTime.Now)
            {
                Console.WriteLine("Updating auth token.");
                string url = $"http://{gateway}/api/login";
                string json = "{ \"username\": \"admin\", \"password\": \"Stooge@31\" }";
                string result = HttpPostAsync(url, json).GetAwaiter().GetResult();
                TelAuth TA = Newtonsoft.Json.JsonConvert.DeserializeObject<TelAuth>(result);
                authtoken = TA.data.token;
                authtoken_expiry = DateTime.Now.AddSeconds(TA.data.expires - 1);
            }
        }

        class TelAuth
        {
            public bool success;
            public TelAuthData data;
        }
        class TelAuthData
        {
            public string username;
            public int expires;
            public string token;
            public string group;
        }

        public static string authtoken;
        public static DateTime authtoken_expiry = DateTime.Now;


        public static void PostRequest(string request_type, string json)
        {
            string url = $"http://{gateway}/api/{request_type}";
            string result = HttpPostAsync(url, json, true).GetAwaiter().GetResult();
            Console.WriteLine("Post response: " + result);
        }

        public static string GetRequest(string request_type, string param = "", bool authModeLegacy = false)
        {
            // Create a request for the URL. 		
            if (param != "")
            {
                param = "&" + param;
            }

            string URI = $"http://{gateway}/api/{request_type}";


            //For Legacy / older firmware.
            if (authModeLegacy)
            {
                URI = $"http://{gateway}/cgi-bin/{request_type}?username={username}&password={password}{param}";
            }
            else
            {
                UpdateAuth();
            }

            WebRequest request = WebRequest.Create(URI);
            if (!authModeLegacy)
            {
                request.Headers.Add("Authorization", $"Bearer {authtoken}");
            }

            // Get the response.
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            // Display the status.

            if (response.StatusCode != HttpStatusCode.OK)
            {
                Console.Write("Web Request Failed");
                throw new Exception(response.StatusDescription);
            }
            // Get the stream containing content returned by the server.
            Stream dataStream = response.GetResponseStream();
            // Open the stream using a StreamReader for easy access.
            StreamReader reader = new StreamReader(dataStream);
            // Read the content.
            string responseFromServer = reader.ReadToEnd();
            // Cleanup the streams and the response.
            reader.Close();
            dataStream.Close();
            response.Close();

            return responseFromServer;
        }

        public static void DBAddMessage(SMSInboundMessage message)
        {
            SqlConnection conn = new SqlConnection(sqlserver);
            SqlCommand sqlcmd = new SqlCommand();
            sqlcmd.Connection = conn;
            sqlcmd.CommandText = "INSERT into tblMessages ([sender],[message],[timestamp],[timestamp_router]) VALUES (@sender,@message,GETDATE(),@timestamp)";
            sqlcmd.Parameters.AddWithValue("sender", message.sender);
            sqlcmd.Parameters.AddWithValue("message", message.message);
            sqlcmd.Parameters.AddWithValue("timestamp", message.date);
            conn.Open();
            sqlcmd.ExecuteNonQuery();
            conn.Close();
        }

    }
}
