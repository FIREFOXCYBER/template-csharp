using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Diagnostics; //This can be used with dynamic, unlike the contract

namespace Mycroft.App
{
    public abstract class Client
    {
        private string manifest;
        private TcpClient cli;
        private Stream stream;
        private JavaScriptSerializer ser = new JavaScriptSerializer();
        private StreamReader reader;
        protected MessageEventHandler handler;
        public string InstanceId;

        public Client()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var textStreamReader = new StreamReader("app.json");
            manifest = textStreamReader.ReadToEnd();
            var jsobj = ser.Deserialize<dynamic>(manifest);
            InstanceId = jsobj["instanceId"];
            handler = new MessageEventHandler();

        }
        public async void Connect(string hostname, string port)
        {
            cli = new TcpClient(hostname, Convert.ToInt32(port));
            Logger.GetInstance().Info("Connected to Mycroft");
            stream = cli.GetStream();
            reader = new StreamReader(stream);
            try
            {
                await StartListening();
            }
            finally
            {
                CloseConnection();
            }
        }

        public async Task StartListening()
        {
            await SendManifest();
            handler.Handle("CONNECT");
            while (true)
            {
                dynamic obj = await ReadJson();
                string type = obj.type;
                dynamic message = obj.message;
                handler.Handle(type, message);
            }
        }


        public async void CloseConnection()
        {
            handler.Handle("END");
            await SendData("APP_DOWN", "");
            Logger.GetInstance().Info("Disconnected from Mycroft");
            cli.Close();
        }

        #region Message Sending and Recieving
        public async Task SendData(string type, string data)
        {
            string msg = type + " " + data;
            msg = msg.Trim();
            msg = Encoding.UTF8.GetByteCount(msg) + "\n" + msg;
            Logger.GetInstance().Info("Sending Message " + type);
            Logger.GetInstance().Debug(msg);
            stream.Write(Encoding.UTF8.GetBytes(msg), 0, (int) msg.Length);
        }

        public async Task SendJson(string type, Object o)
        {            
            string obj = ser.Serialize(o);
            string msg = type + " " + obj;
            msg = msg.Trim();
            msg = Encoding.UTF8.GetByteCount(msg) + "\n" + msg;
            stream.Write(Encoding.UTF8.GetBytes(msg), 0, (int) msg.Length);
        }

        public async Task<Object> ReadJson()
        {
            //Size of message in bytes
            string len = reader.ReadLine();
            var size = Convert.ToInt32(len);

            //buffer to put message in
            var buf = new Char[size];

            //Get the message
            reader.Read(buf, 0, size);
            var str = new string(buf).Trim();
            var re = new Regex(@"^([A-Z_]*)");

            //Match the message type
            var match = re.Match(str);
            if (match.Length <= 0)
            {
                throw new ArgumentException("Couldn't match a message type string in message: " + str);
            }

            //Convert the json string to an object
            var jsonstr = str.TrimStart(match.Value.ToCharArray());
            Logger.GetInstance().Info("Recieved Message " + match.Value);
            Logger.GetInstance().Debug(jsonstr);
            if (jsonstr.Trim().Length == 0)
            {
                return new
                {
                    type = match.Value,
                    message = new { }
                };
            }
            var obj = ser.Deserialize<dynamic>(jsonstr);

            //Return the type string and the object
            return new
            {
                type = match.Value,
                message = obj
            };
        }
        #endregion
        #region Message Helpers
        public async Task SendManifest()
        {
            SendData("APP_MANIFEST", manifest);
        }

        public async Task Up()
        {
            SendData("APP_UP", "");
        }

        public async Task Down()
        {
            SendData("APP_DOWN", "");
        }

        public async Task InUse(int priority)
        {
            SendJson("APP_IN_USE", new { priority = priority });
        }

        public async Task Broadcast(dynamic content)
        {
            var broadcast = new
            {
                id = Guid.NewGuid(),
                content = content
            };
            SendJson("MSG_BROADCAST", broadcast);
        }

        public async Task Query(string capability, string action, dynamic data, string[] instanceId = null , int priority = 30)
        {
            if (instanceId == null)
                instanceId = new string[0];
            var query = new
            {
                id = Guid.NewGuid(),
                capability = capability,
                action = action,
                data = data,
                instanceId = instanceId,
                priority = priority
            };
            SendJson("MSG_QUERY", query);
        }

        public async Task QuerySuccess(string id, dynamic ret)
        {
            var querySuccess = new
            {
                id = id,
                ret = ret
            };
            SendJson("MSG_QUERY_SUCCESS", querySuccess);
        }

        public async Task QueryFail(string id, string message)
        {
            var queryFail = new
            {
                id = id,
                message = message
            };
            SendJson("MSG_QUERRY_FAIL", queryFail);
        }
        #endregion
    }
}
