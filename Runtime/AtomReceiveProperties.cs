
using System;
using System.IO;
using System.Xml;
using System.Net;
using System.Collections;
using Microsoft.BizTalk.Message.Interop;
using Microsoft.BizTalk.Adapter.Common;

namespace BizTalk.Adapter.Atom
{

   // public enum SizeUnit { Bytes, KBytes, MBytes }

    // //////////////////////////////////////////////////////////////////////////////////////////////

    internal class AtomReceiveProperties : ConfigProperties
    {
        private string uri;
        private string address;
        private int pollingInterval;
        private string pollingIntervalUnit;
        private string stateFile;
        public string StateFile { get { return stateFile; } }
        public string Uri { get { return uri; } }

        public string Address { get { return address; } }
        public int PollingInterval { get { return pollingInterval; } }
        public string PollingIntervalUnit { get { return pollingIntervalUnit; } }

        /*
        public static TimeSpan Time(string time)
        {
            TimeSpan span;
            if (TimeSpan.TryParse(time, out span) == false)
                span = new TimeSpan(0, 0, 1);

            return span;
        }
        */
        public AtomReceiveProperties (string uri)
        {
            try
            {
                this.uri = uri;
                this.pollingInterval = 3000;
                this.pollingIntervalUnit = "seconds";
                this.stateFile = String.Empty;
                this.address = String.Empty;
            }
            finally
            {
            }
        }

        public virtual void HandlerConfiguration (XmlDocument configDOM)
        {
        }

        public virtual void LocationConfiguration (XmlDocument configDOM,bool update)
        {
            try
            {
                int pollingIntervalMultiplier = 1;

                XmlNode nodePollingIntervalUnit = configDOM.SelectSingleNode("Config/pollingIntervalUnit");
                switch (nodePollingIntervalUnit.InnerText.ToLower())
                {
                    case "milliseconds":
                        pollingIntervalMultiplier = 1;
                        break;
                    case "seconds":
                        pollingIntervalMultiplier = 1000;
                        break;
                    case "minutes":
                        pollingIntervalMultiplier = 1000 * 60;
                        break;
                }

                XmlNode nodePollingInterval = configDOM.SelectSingleNode("Config/pollingInterval");


                XmlNode nodeAddress = configDOM.SelectSingleNode("Config/address");

                if (nodeAddress == null)
                    throw new ArgumentNullException("NodeAddress", "Atom feed address must be specified!");

                this.address = nodeAddress.InnerText;

                int pollingIntervalValue = int.Parse(nodePollingInterval.InnerText);

                this.pollingInterval = pollingIntervalMultiplier * pollingIntervalValue;

                XmlNode nodeStateFile = configDOM.SelectSingleNode("Config/stateFile");

                if (nodeStateFile == null)
                    throw new ArgumentNullException("NodeStateFile", "Path to state file must be specified!");

                this.stateFile = nodeStateFile.InnerText;


            }
            catch (Exception ex)
            {
                throw new ArgumentNullException("LocationConfiguration", "One or more configuration parameters are missing!");
            }
           
        }
    }
}
