using ServiceReference1;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading;
using System.Windows;

namespace opcxmldaconnector.opcxml
{
    public class OPCxml
    {
        protected RequestOptions ReadOptions = new RequestOptions();
        protected ReadRequestItemList ReadItemList = new ReadRequestItemList();
        protected ReadRequestItem[] ReadItemArray = new ReadRequestItem[1];
        protected ReadRequestItem ReadItem = new ReadRequestItem();

        //protected Options SubOptions = new RequestOptions();
        protected List<SubscribeRequestItemList> SubItemList;
        protected SubscribeRequestItem[] SubItemArray;
        protected SubscribeRequestItem SubItem = new SubscribeRequestItem();

        protected List<BasicHttpBinding> bindings;
        protected List<EndpointAddress> endpointAddress;

        public List<ServiceClient> C230_2_Server;
        private Read request;
        public List<Subscribe> subscribelist;

        private SubscribeReplyItemList SubReplyList;
        private OPCError[] SubErrorList;

        public ReadResponse readresponse { get; set; }
        public List<SubscribeResponse> subresponse;

        public BackgroundWorker worker;
        public Thread t;

        public ReplyItemList ReadReplyList { get; private set; }
        public OPCError[] ReadErrorList { get; private set; }

        private List<SubscriptionPolledRefresh> subrefreshlist;
        public List<SubscribePolledRefreshReplyItemList> subrefreshresponselist;
        public string variabile { get; set; }
        private Thread thread;

        public event EventHandler PublishData;

        com commService;

        public int numOfdevices { get; set; }

        // The ThreadProc method is called when the thread starts.
        // It loops ten times, writing to the console and yielding
        // the rest of its time slice each time, and then ends.


        public OPCxml()
        {

            //if ((ReadReplyList.Items[0] != null) && (ReadReplyList.Items[0].Value != null) && (ReadReplyList.Items[0].Value.GetType().Name != "XmlNode[]"))
            //    Output.Text = ReadReplyList.Items[0].Value.ToString();
            //else
            //    Output.Text = "<Error>";
            //MessageBox.Show(ReadReplyList.Items[0].ItemName.ToString() + "\nValue: " + ReadReplyList.Items[0].Value);
        }


        public void Initialize(List<Simotion> simotionlist)
        {
            ReadOptions.ClientRequestHandle = "";
            ReadOptions.LocaleID = "DE-AT";
            ReadOptions.RequestDeadlineSpecified = true;
            ReadOptions.ReturnDiagnosticInfo = true;
            ReadOptions.ReturnErrorText = true;
            ReadOptions.ReturnItemName = true;
            ReadOptions.ReturnItemPath = true;
            ReadOptions.ReturnItemTime = true;
            numOfdevices = simotionlist.Count();


            bindings = new List<BasicHttpBinding>();
            C230_2_Server = new List<ServiceClient>();
            subrefreshresponselist = new List<SubscribePolledRefreshReplyItemList>();
            for (var i = 0; i < numOfdevices; i++)
            {
                bindings.Add(new BasicHttpBinding());
                bindings[i].Name = "binding" + i;
                bindings[i].Security.Mode = BasicHttpSecurityMode.None;
                subrefreshresponselist.Add(new SubscribePolledRefreshReplyItemList());
                Connect(simotionlist[i].SimotionID, simotionlist[i].SimotionIP, simotionlist[i].SimotionUser, simotionlist[i].SimotionPass);
            }

        }

        public string Connect(int ID, string ipaddress, string username, string password)
        {
            try
            {
                C230_2_Server.Add(new ServiceClient(bindings[ID], new EndpointAddress(new Uri("http://" + ipaddress + "/soap/opcxml"))));
                //C230_2_Server.Url = "http://" + IpAddress + "/soap/opcxml";
                //System.Net.ICredentials myCredentials = new System.Net.NetworkCredential("simotion", "simotion");
                C230_2_Server[ID].ChannelFactory.Credentials.UserName.UserName = username;
                C230_2_Server[ID].ChannelFactory.Credentials.UserName.Password = password;
                //C230_2_Server.PreAuthenticate = true;
                System.Net.ServicePointManager.Expect100Continue = false;

                return "Config Complete";
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error" + ex.ToString());
                return "Please review your input";
            }

        }

        public void ReadVariable(int ID, string[] varPath)
        {
            var i = 0;
            foreach (var varpath in varPath)
            {
                ReadItem.ItemPath = "SIMOTION";
                ReadItem.ItemName = varpath; //"unit/Programm.variabile"; // varPath; //"var/userData.user5";
                ReadItemArray[i] = ReadItem;
                i++;
            }

            ReadItemList.Items = ReadItemArray;

            try
            {
                request = new Read();
                request.ItemList = ReadItemList;
                request.Options = ReadOptions;
                readresponse = C230_2_Server[ID].Read(request);

                ReadReplyList = readresponse.RItemList;
                ReadErrorList = readresponse.Errors;
                Console.WriteLine(ReadReplyList);
                Console.WriteLine(ReadErrorList);
                //C230_2_Server.Read(ReadOptions, ReadItemList, out ReadReplyList, out ReadErrorList);

            }
            catch (Exception ex)
            {
                Console.WriteLine("Error" + ex.ToString());
            }

        }


        public void Subscription(List<List<string>> variables)
        {
            var k = 0;
            var j = 0;
            SubItemList = new List<SubscribeRequestItemList>();
            foreach (var lista in variables) {
                SubItemList.Add(new SubscribeRequestItemList());
                SubItemArray = new SubscribeRequestItem[lista.Count];
                foreach (var item in lista)
                {
                    SubItem.ItemPath = "SIMOTION";
                    SubItem.ItemName = item; //"var/userData.user5";
                    SubItemArray[k]=SubItem;
                    k++;
                }
                SubItemList[j].Items = SubItemArray;
                k = 0;
                j++;
            }

            bool ReturnValuesOnReply = true;
            int SubscriptionPingRate = 160;

            try
            {

                subscribelist = new List<Subscribe>();
                subresponse = new List<SubscribeResponse>();
                for (var i = 0; i < numOfdevices; i++)
                {
                    subscribelist.Add(new Subscribe());
                    subresponse.Add(new SubscribeResponse());
                    subscribelist[i].ItemList = SubItemList[i];
                    subscribelist[i].Options = ReadOptions;
                    subscribelist[i].ReturnValuesOnReply = ReturnValuesOnReply;
                    subscribelist[i].SubscriptionPingRate = SubscriptionPingRate;
                    subresponse[i] = C230_2_Server[i].Subscribe(subscribelist[i]);
                }

                commService = new com(ReadOptions, subresponse, C230_2_Server, numOfdevices);
                commService.VariableChanged += VariableChanged;

            }
            catch (Exception ex)
            {
                Console.WriteLine("Error" + ex.ToString());
            }
        }


        private void VariableChanged(object sender, EventArgs e)
        {
            SubsciptionEventArgs local = (SubsciptionEventArgs)e;
            Console.WriteLine("var value : " + local.ret[0].Items[0].Value.ToString());
            subrefreshresponselist[local.ID] = local.ret[0];
            PublishData?.Invoke(this, local);
            //foreach (ReplyItemList ret in commService.listItem)
            //{

            //    Console.WriteLine("var value: " + ret.Items[0].Value.ToString());
            //});
        }


    }

    public class com
    {
        public event EventHandler VariableChanged;
        //public List<ReplyItemList> listItem;
        public string item { get; set; }
        public List<string> variabile = new List<string>();
        public int devices { get; set; }

        public RequestOptions subReadOptions;
        List<SubscriptionPolledRefresh> subrefreshlist;
        List<SubscriptionPolledRefreshResponse> subrefreshresponselist;
        List<SubscribeResponse> subresponselist;
        List<ServiceClient> C230_2_Serverlist;
        List<Thread> threadComlist;

        public com(RequestOptions ReadOptions, List<SubscribeResponse> subresponse, List<ServiceClient> C230_2_Server, int numOfdevice)
        {

            devices = numOfdevice;
            subReadOptions = ReadOptions;
            subresponselist = subresponse;
            C230_2_Serverlist = C230_2_Server;

            subrefreshlist = new List<SubscriptionPolledRefresh>();
            subrefreshresponselist = new List<SubscriptionPolledRefreshResponse>();
            threadComlist = new List<Thread>();

            for(int i = 0; i<devices;i++)
            {
                variabile.Add(new string(""));
                //listItem.Add(new ReplyItemList());
                subrefreshlist.Add(new SubscriptionPolledRefresh());
                subrefreshresponselist.Add(new SubscriptionPolledRefreshResponse());
                var index = i;
                threadComlist.Add(new Thread(() => ThreadDoWork(index, subReadOptions, subresponselist[index].ServerSubHandle, subrefreshlist[index], subrefreshresponselist[index], C230_2_Serverlist[index])));
                threadComlist[i].Start();
            }
        }

        public void ThreadDoWork(int ID, RequestOptions subReadOptions, string ServerSubHandle, SubscriptionPolledRefresh subrefresh, SubscriptionPolledRefreshResponse subrefreshresponse, ServiceClient C230_2_Server)
        {
            EventHandler handler = VariableChanged;
            SubsciptionEventArgs e = new SubsciptionEventArgs();
            e.ret = new List<SubscribePolledRefreshReplyItemList>();
            e.ret.Add(new SubscribePolledRefreshReplyItemList());
            e.ID = ID;
            var temp = e.ret[0].Items;

            while (true)
            {
                try
                {
                    e.ret[0] = ThreadProc(ID, subReadOptions, ServerSubHandle, subrefresh, subrefreshresponse, C230_2_Server)[0];
                    if (e.ret[0] != null)
                    {
                        for (int i = 0; i < e.ret[0].Items.Count(); i++)
                        {
                            if (temp == null)
                            {
                                e.isChanged = true;
                                temp = e.ret[0].Items;
                            }
                            else
                            {
                                if (e.ret[0].Items[i].Value.ToString() != temp[i].Value.ToString())
                                {
                                    e.isChanged = true;
                                    temp = e.ret[0].Items;
                                }
                                else
                                {
                                    e.isChanged = false;
                                }

                            }
                        }

                    }

                    if (e.isChanged)
                    {
                        handler?.Invoke(this, e);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error" + ex.ToString());
                }
                

            }
        }

        public SubscribePolledRefreshReplyItemList[] ThreadProc(int ID, RequestOptions subReadOptions, string ServerSubHandle, SubscriptionPolledRefresh subrefresh, SubscriptionPolledRefreshResponse subrefreshresponse, ServiceClient C230_2_Server)
        {
            string stringa = ServerSubHandle;
            //subrefresh.ServerSubHandles[0] = subresponse.ServerSubHandle;
            //subrefresh.ReturnAllItems = true;
            subrefresh.ReturnAllItems = true;
            subrefresh.Options = subReadOptions;
            string[] stringAraay = new string[1];
            stringAraay[0] = stringa;
            subrefresh.ServerSubHandles = stringAraay;
            System.DateTime HoldTime = new DateTime();
            bool HoldTimeSpecified = true;
            int WaitTime = 100;
            subrefresh.HoldTime = HoldTime;
            subrefresh.WaitTime = WaitTime;
            subrefresh.HoldTimeSpecified = HoldTimeSpecified;
            C230_2_Server.SubscriptionPolledRefresh(subrefresh);
            subrefreshresponse = C230_2_Server.SubscriptionPolledRefresh(subrefresh);
            //SubReplyList = subrefreshresponse.RItemList;
            return subrefreshresponse.RItemList;
        }
    }

    public class SubsciptionEventArgs : EventArgs
    {
        public int ID { get; set; }
        public List<SubscribePolledRefreshReplyItemList> ret {get; set;}
        public bool isChanged { get;set; }
    }


}
