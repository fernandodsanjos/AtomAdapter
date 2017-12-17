
using System;
using System.IO;
using System.Xml;
using System.Threading;
using System.Text;
using System.Transactions;
using System.Data.SqlClient;
using Microsoft.BizTalk.Component.Interop;
using Microsoft.BizTalk.Message.Interop;
using Microsoft.BizTalk.TransportProxy.Interop;
using Microsoft.BizTalk.Adapter.Common;
using System.Collections;
using System.Collections.Generic;
using Shared.Components;
using System.Xml.Serialization;
using System.Net;

namespace BizTalk.Adapter.Atom
{
    //  An instance of this class should exist for every enabled receive location.
    //
    //  This is where all the receive location specific work goes. For example, any listening or polling generally goes in here.
    //
    //  If you want to have multiple independent threads listening or working per receive location that might effectively
    //  be done as member objects from this endpoint class. In this particular example there is only a single timer doing some
    //  polling and it is contained in here.
    //
    //  After the Receiver creates a member of this class it calls Open and when the receive location is deleted Dispose is called.
    //  Otherwise all Updates are plumbed through to the Update function on this class.

    internal class AtomReceiverEndpoint : ReceiverEndpoint
    {
        //  timer and buffer
        Timer timer;
        private ManualResetEvent batchFinished = new ManualResetEvent(false);
        //  properties
        private AtomReceiveProperties properties;

        private bool busy = false; 
        //  handle to the EPM
        private IBTTransportProxy transportProxy;
        private IBaseMessageFactory messageFactory;
        private ControlledTermination control;

        private string uri;
        private string transportType;
        private string propertyNamespace;
        private AtomState atomState = null;
        private XmlSerializer stateSerializer = null;


        public AtomReceiverEndpoint()
        {
        }

        public override void Open(string uri, Microsoft.BizTalk.Component.Interop.IPropertyBag config, IPropertyBag bizTalkConfig, Microsoft.BizTalk.Component.Interop.IPropertyBag handlerPropertyBag, IBTTransportProxy transportProxy, string transportType, string propertyNamespace, ControlledTermination control)
        {
            try
            {
                this.properties = new AtomReceiveProperties(uri);
              
                XmlDocument locationConfigDom = ConfigProperties.ExtractConfigDom(config);
                this.properties.LocationConfiguration(locationConfigDom,false);

                //  this is our handle back to the EPM
                this.transportProxy = transportProxy;

                //  used to control whether the EPM can unload us
                this.control = control;

                this.uri = uri;
                this.transportType = transportType;
                this.propertyNamespace = propertyNamespace;
                this.messageFactory = this.transportProxy.GetMessageFactory();
                stateSerializer = new XmlSerializer(typeof(AtomState));


                Start();
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
            }
        }

        //  update and delete
        public override void Update (IPropertyBag config, IPropertyBag bizTalkConfig, IPropertyBag handlerPropertyBag)
        {
            try
            {
                Stop();

                this.properties = new AtomReceiveProperties(this.properties.Uri);

                XmlDocument locationConfigDom = ConfigProperties.ExtractConfigDom(config);
                this.properties.LocationConfiguration(locationConfigDom,true);

                Start();
            }
            finally
            {
            }
        }

        public override void Dispose ()
        {
            try
            {
                Stop();
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
            }
        }

    private IBaseMessage CreateMessage(Shared.Components.Entry message)
    {

        MemoryStream mem = new MemoryStream(UTF8Encoding.UTF8.GetBytes(message.Content));
            
        IBaseMessageFactory factory = this.transportProxy.GetMessageFactory();
        IBaseMessagePart part = factory.CreateMessagePart();

        part.Data = mem;

        IBaseMessage msg = factory.CreateMessage();
        msg.AddPart("body", part, true);

        //  We must add these context properties
        SystemMessageContext context = new SystemMessageContext(msg.Context);
        context.InboundTransportLocation = this.uri;
        context.InboundTransportType = this.transportType;
            
            //we could promote entity id and updated, msg.Context.Promote(ns, message.Id
            return msg;
    }

    public void SubmitBatch()
    {
        bool needToLeave = false;

        busy = true;


        try
        {
            //  used to block the Terminate from BizTalk 
            if (!this.control.Enter())
            {
                needToLeave = false;
                return;
            }

                needToLeave = true;

              
                AtomReader atom = new AtomReader(XmlReader.Create(this.properties.Uri), atomState.LastEntryId,atomState.LastUpdated);

                Stack<Queue<Entry>> stk = atom.Entries();


                bool leave = false;

                while (stk.Count > 0)
                {
                    Queue<Entry> queueEntry = stk.Pop();

                    while (queueEntry.Count > 0)
                    {
                        Entry entry = queueEntry.Dequeue();

                        if (SubmitMessage(entry) == false)
                            leave = true;
                    }

                    if (leave == true)
                        break;


                }

                atomState.LastUpdated = atom.LastUpdate;

                //  no exception in Done so we will be getting a BatchComplete which will do the necessary Leave
                needToLeave = false;


        }
        catch (Exception e)
        {
            this.transportProxy.SetErrorInfo(e);

        }
        finally
        {
            busy = false;
            //  if this is true there must have been some exception in or before Done
            if (needToLeave)
                this.control.Leave();

                
                
        }
}

       
    private bool SubmitMessage(Shared.Components.Entry message)
    {

        try
        {

            SyncReceiveSubmitBatch batch = new SyncReceiveSubmitBatch(this.transportProxy, this.control, 1);

            batch.SubmitMessage(CreateMessage(message));
                
            batch.Done();

            if (batch.Wait() == true)
               atomState.LastEntryId = message.Id;

            return batch.OverallSuccess;

        }
        catch (Exception e)
        {
            this.transportProxy.SetErrorInfo(e);
        }

        return false;
          


    }
   
        private void Start()
        {
            if(File.Exists(this.properties.StateFile))
            {
                
                atomState = (AtomState)stateSerializer.Deserialize(new FileStream(this.properties.StateFile, FileMode.Open));
            }

            if (atomState == null)
                atomState = new AtomState();

            this.timer = new Timer(new TimerCallback(TimerTask));
            this.timer.Change(0, this.properties.PollingInterval);
        }

        private void Stop()
        {
            stateSerializer.Serialize(new FileStream(this.properties.StateFile, FileMode.Create), atomState);
            this.timer.Dispose();
        }

        private void TimerTask(object state)
        {
            if(busy == false)
                SubmitBatch();//if busy wait for next timer

        }
        
       
    }
}
