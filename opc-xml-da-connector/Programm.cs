using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ServiceReference1;
using uPLibrary.Networking.M2Mqtt;          // including M2Mqtt library
using uPLibrary.Networking.M2Mqtt.Messages; // including M2Mqtt library
using Newtonsoft.Json;                      // including Json library
using opcxmldaconnector.opcxml;
using System.IO;
using System.Text.Json;
using static opcxmldaconnector.MqttMessage;

namespace opcxmldaconnector
{

    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class Simotion
    {

        [JsonProperty(PropertyName = "ID")]
        public int SimotionID { get; set; }

        [JsonProperty(PropertyName = "IP")]
        public string SimotionIP { get; set; }

        [JsonProperty(PropertyName = "USER")]
        public string SimotionUser { get; set; }

        [JsonProperty(PropertyName = "PASS")]
        public string SimotionPass { get; set; }

        [JsonProperty(PropertyName = "VARIABLES")]
        public List<string> Variables { get; set; }

        public override string ToString() => System.Text.Json.JsonSerializer.Serialize<Simotion>(this);

    }

    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class MqttParams
    {
        [JsonProperty(PropertyName = "MQTT_USER")]
        public string User { get; set; }

        [JsonProperty(PropertyName = "MQTT_PASSWORD")]
        public string Password { get; set; }

        [JsonProperty(PropertyName = "MQTT_IP")]
        public string BrokerIP { get; set; }

        [JsonProperty(PropertyName = "PUB_TOPIC")]
        public string pubTopic { get; set; }

        [JsonProperty(PropertyName = "SUB_TOPIC")]
        public string subTopic { get; set; }


        public override string ToString() => System.Text.Json.JsonSerializer.Serialize<MqttParams>(this);
    }

    public class MqttMessage
    {
        
        public class MqttVariableMessage
        {
            public DateTime Date { get; set; }
            public object Value { get; set; }
            public string ItemName {get; set;}

            public string ItemPath { get; set; }
        }

        public int SimotionID { get; set; }
        public List<MqttVariableMessage> datapointDefinitions { get; set;}
    }

    public class Programm
    {

        public static MqttClient client;
        public static string clientId;
        public static bool quit = false;

        //SIMOTION Variables

        public static List<Simotion> simotionlist;


        //MQTT Variables
        public static string mqtt_broker;
        public static string mqtt_user;
        public static string mqtt_pw;
        public static string mqtt_pubTopic;
        public static string mqtt_subTopic;
        public static MqttParams mqtt_parameter;

        public static OPCxml opcxml;

        //Config
        public static string config_simotion_file = "./cfg-data/config.json";

        public static string config_mqtt_file = "./cfg-data/mqtt-config.json";

        public static List<List<string>> collectionitem;

        public static IEnumerable<Simotion> GetProducts(string path)
        {
            using (var jsonFileReader = File.OpenText(path))
            {
                var text = jsonFileReader.ReadToEnd();
                return System.Text.Json.JsonSerializer.Deserialize<Simotion[]>(text,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
            }
        }

        public static List<Simotion> ReadSimotionConfig(string path)
        {
            List<Simotion> simotionlist = new List<Simotion>();
            try
            {
                Console.WriteLine("Start reading config params from " + path + ":");
                string text = File.ReadAllText(path);
                //Console.WriteLine(text);
                simotionlist = JsonConvert.DeserializeObject<List<Simotion>>(text);

                foreach (var item in simotionlist)
                {
                    Console.WriteLine(item.SimotionIP);
                    Console.WriteLine("ip= " + item.SimotionIP);
                    Console.WriteLine("user= " + item.SimotionUser);
                    Console.WriteLine("password= " + item.SimotionPass);
                    Console.WriteLine("variables= " + item.Variables);
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine("Error while reading config file: " + ex);
            }
            return simotionlist;
        }

        public static MqttParams ReadMqttParam(string path)
        {
            MqttParams parameters = new MqttParams();
            try
            {
                Console.WriteLine("Start reading config params from " + path + ":");
                string text = File.ReadAllText(path);
                //Console.WriteLine(text);
                parameters = JsonConvert.DeserializeObject<MqttParams>(text);

                Console.WriteLine("user= " + parameters.User);
                Console.WriteLine("password= " + parameters.Password);
                Console.WriteLine("broker ip= " + parameters.BrokerIP);
                Console.WriteLine("pubTopic= " + parameters.pubTopic);
                Console.WriteLine("subTopic= " + parameters.subTopic);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error while reading config file: " + ex);
            }

            return parameters;
        }



        public static void CreateMqttClient()
        {

            try

            {

                Console.WriteLine("Create client instance");

                // create mqtt client instance (host name OR IP address work)

                client = new MqttClient(mqtt_parameter.BrokerIP, 1883, false, null, null, MqttSslProtocols.None);

                Console.WriteLine("port 1883");


                Console.WriteLine("Created client");

                clientId = Guid.NewGuid().ToString();


            }

            catch (Exception e)

            {

                Console.WriteLine(e); ;

            }
        }
        public static void Connect()
        {
            try

            {

                Console.WriteLine("Connecting..");

                client.Connect(clientId, mqtt_parameter.User, mqtt_parameter.Password);
                
                Console.WriteLine("Mqtt cleint Connected");

            }

            catch (Exception e)

            {

                Console.WriteLine(e); ;

            }

        }

        public void Subscribe()

        {

            client.Subscribe(new string[] { mqtt_pubTopic }, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });

            Console.WriteLine("Subscribed " + mqtt_pubTopic + "...\n");

        }



        public static void Publish(SubsciptionEventArgs results)

        {
            MqttMessage message = new MqttMessage();
            message.datapointDefinitions = new List<MqttVariableMessage>();
            message.SimotionID = results.ID;
            for (int i = 0; i < results.ret[0].Items.Count(); i++)
            {
                message.datapointDefinitions.Add(new MqttVariableMessage());
                message.datapointDefinitions[i].ItemName = results.ret[0].Items[i].ItemName;
                message.datapointDefinitions[i].ItemPath = results.ret[0].Items[i].ItemPath;
                message.datapointDefinitions[i].Date = results.ret[0].Items[i].Timestamp;
                message.datapointDefinitions[i].Value = results.ret[0].Items[i].Value;
            }

            string jsonString = System.Text.Json.JsonSerializer.Serialize(message);
            client.Publish(mqtt_parameter.pubTopic, Encoding.UTF8.GetBytes(jsonString), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, true);

            Console.WriteLine("Send " + message + "\n");

        }



        public bool Disconnect()

        {

            Console.WriteLine("Disconnecting client");

            client.Disconnect();

            Console.WriteLine("Ending now...");

            Environment.Exit(-1);

            return true;

        }

        public string GetStatus()

        {
            return client.IsConnected.ToString();
        }

        public static void PublishHandler(object sender, EventArgs e)
        {
            if (client.IsConnected)
            {
                SubsciptionEventArgs mqttargs = (SubsciptionEventArgs)e;
                Publish(mqttargs);
            }
            else
            {
                Connect();
            }

        }

        static void Main(string[] args)
        {
            try
            {
                Console.WriteLine("Starting pingpong app...");
                Console.WriteLine(Directory.GetCurrentDirectory());
                //simotionlist = new IEnumerable<Simotion>;

                // If a config file exists, get params from here (mqtt-config.json)
                if (File.Exists(config_simotion_file))
                {
                    Console.WriteLine("Reading parameters from configuration file");
                    simotionlist = ReadSimotionConfig(config_simotion_file);
                }
                if (File.Exists(config_mqtt_file))
                {
                    Console.WriteLine("Reading parameters from configuration file");
                    mqtt_parameter = ReadMqttParam(config_mqtt_file);
                }

                CreateMqttClient();
                Connect();

                while (!client.IsConnected)
                {
                    Connect();

                }

                opcxml = new OPCxml();

                if (simotionlist!=null)
                {
                    opcxml.Initialize(simotionlist);
                    opcxml.PublishData += PublishHandler;

                    collectionitem = new List<List<string>>();

                    // Test read of one simotion
                    //opcxml.ReadVariable(simotionlist[0].SimotionID,simotionlist[0].Variables);

                    foreach (var simotion in simotionlist)
                    {
                        collectionitem.Add(simotion.Variables);
                    }
                    opcxml.Subscription(collectionitem);
                }
                else
                {
                    Console.WriteLine("Please Correct Simotion configuration");
                }


            }
            catch (Exception e)
            {

                Console.WriteLine("error in main: " + e);
            }

        }
    }
}
