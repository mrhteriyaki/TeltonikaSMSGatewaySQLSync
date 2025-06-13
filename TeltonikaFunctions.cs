using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
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

        static string smtpserver;
        static string fromaddress;
        static string toaddress;

        public static void LoadConfig()
        {
            
            string AppDirectory = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().CodeBase).Replace("file:\\", "");
            Console.WriteLine("Running from:" + AppDirectory);

            int count = 0;
            foreach (string line in File.ReadAllLines(AppDirectory + "\\config.ini"))
            {
                if (line.StartsWith("username="))
                {
                    username = line.Substring(9);
                    count++;
                }
                else if (line.StartsWith("password="))
                {
                    password = line.Substring(9);
                    count++;
                }
                else if (line.StartsWith("gateway="))
                {
                    gateway = line.Substring(8);
                    count++;
                }
                else if (line.StartsWith("sqlserver="))
                {
                    sqlserver = line.Substring(10);
                    count++;
                }
                else if (line.StartsWith("smtpserver="))
                {
                    smtpserver = line.Substring(11);
                    count++;
                }
                else if (line.StartsWith("fromaddress="))
                {
                    fromaddress = line.Substring(12);
                    count++;
                }
                else if (line.StartsWith("toaddress="))
                {
                    toaddress = line.Substring(10);
                    count++;
                }
            }
            if (count != 7)
            {
                Console.WriteLine("Error in config file (missing property).");
            }

            //Test SQL Connection.
            SqlConnection conn = new SqlConnection(sqlserver);
            conn.Open();
            conn.Close();


        }



        public class SMSMessage
        {
            public int index;
            public DateTime datetime;
            public string sender;
            public string text;
            public string status;
        }

        static public void SendSMS(string number, string message)
        {
            string urlmsg = HttpUtility.UrlEncode(message);
            SendRequest("sms_send", "number=" + number + "&text=" + urlmsg);
        }

        static public void ProcessOutbox()
        {
            SqlConnection conn = new SqlConnection(sqlserver);
            SqlCommand sqlcmd = new SqlCommand();
            sqlcmd.Connection = conn;

            conn.Open();
            sqlcmd.CommandText = "SELECT id,number,message FROM tblOutbox";
            List<SMSMessage> outbox_list = new List<SMSMessage>();
            SqlDataReader SQLOutput = sqlcmd.ExecuteReader();
            while (SQLOutput.Read())
            {
                SMSMessage tmpMsg = new SMSMessage();
                tmpMsg.index = (int)SQLOutput[0];
                tmpMsg.sender = SQLOutput[1].ToString();
                tmpMsg.text = SQLOutput[2].ToString();
                outbox_list.Add(tmpMsg);
            }
            SQLOutput.Close();

            foreach (SMSMessage msg in outbox_list)
            {
                SendSMS(msg.sender, msg.text);

                //Remove from DB.
                sqlcmd.CommandText = "DELETE FROM tblOutbox WHERE id = @mid";
                sqlcmd.Parameters.AddWithValue("mid", msg.index);
                sqlcmd.ExecuteNonQuery();
                sqlcmd.Parameters.Clear();
            }


            conn.Close();


        }



        static public void GetMessageList()
        {
            string MessageData = SendRequest("sms_list").Replace("\r", "");
            string[] MessageDataLines = MessageData.Split('\n');
            SMSMessage message = new SMSMessage();
            List<SMSMessage> message_list = new List<SMSMessage>();
            foreach (string Line in MessageDataLines)
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
                    message.sender = Line.Substring(8);
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

            foreach (SMSMessage msg in message_list)
            {
                Console.WriteLine("Logged message from " + msg.sender);
                DBAddMessage(msg);
                DeleteMessage(msg.index);

                try
                {
                    SendEmail(msg.sender, msg.datetime.ToString(), msg.text);
                    Console.WriteLine("Message sent to email.");
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


        public static void DeleteMessage(int index)
        {
            SendRequest("sms_delete", "number=" + index.ToString());
        }


        public static string SendRequest(string request_type)
        {
            return SendRequest(request_type, "");
        }

        public static string SendRequest(string request_type, string param)
        {
            // Create a request for the URL. 		
            if (param != "")
            {
                param = "&" + param;
            }
            string URI = "http://" + gateway + "/cgi-bin/" + request_type + "?username=" + username + "&password=" + password + param;
            WebRequest request = WebRequest.Create(URI);

            // Get the response.
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            // Display the status.

            if (response.StatusCode != HttpStatusCode.OK)
            {
                Console.Write("Web Request Failure ");
                Console.WriteLine(response.StatusDescription);
                return "";
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

        public static void DBAddMessage(SMSMessage message)
        {
            SqlConnection conn = new SqlConnection(sqlserver);
            SqlCommand sqlcmd = new SqlCommand();
            sqlcmd.Connection = conn;
            sqlcmd.CommandText = "INSERT into tblMessages ([sender],[message],[timestamp],[timestamp_router]) VALUES (@sender,@message,GETDATE(),@timestamp)";
            sqlcmd.Parameters.AddWithValue("sender", message.sender);
            sqlcmd.Parameters.AddWithValue("message", message.text);
            sqlcmd.Parameters.AddWithValue("timestamp", message.datetime);
            conn.Open();
            sqlcmd.ExecuteNonQuery();
            conn.Close();
        }

    }
}
