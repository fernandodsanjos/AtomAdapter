using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using System.Xml;
using System.IO;

namespace Shared.Components
{
    
    public class AtomReader
    {
        private Stack<Queue<Entry>> stackQueue = new Stack<Queue<Entry>>();
        private XmlReader reader = null;
        private string latestEntry = String.Empty;
        DateTime lastUpdated = DateTime.Now;
        DateTime latest = DateTime.Now;
        bool idFound = false;
        /// <summary>
        /// Reads archive type Atom+xml feeds
        /// </summary>
        /// <param name="stream">Recent Atom feed</param>
        /// <param name="entry">Previosly processed entry Id</param>
        /// <param name="updated">Latest processed feed updated time</param>
        public AtomReader(Stream stream,string id,DateTime updated)
        {
            reader = XmlReader.Create(stream);
            latestEntry =id;
            lastUpdated = updated;
        }

        public DateTime LastUpdate
        {
            get
            {
                return latest;
            }
        }

        public AtomReader(Stream stream, string id)
        {
            reader = XmlReader.Create(stream);
            latestEntry = id;
        }

        /// <summary>
        /// Reads archive type Atom+xml feeds
        /// </summary>
        /// <param name="reader">Recent Atom feed</param>
        /// <param name="entry">Previosly processed entry Id</param>
        /// <param name="updated">Latest processed feed updated time</param>
        public AtomReader(XmlReader reader, string id, DateTime updated)
        {
            this.reader = reader;
            latestEntry = id;
            lastUpdated = updated;
        }

        public AtomReader(XmlReader reader, string id)
        {
            this.reader = reader;
            latestEntry = id;
        }
        public Stack<Queue<Entry>> Entries()
        {
            stackQueue = new Stack<Queue<Entry>>();

            reader.MoveToContent();
            //read links
            if (reader.ReadToFollowing("updated") == false)
            {
                throw new Exception("Element updated could not be found in Atom feed");
            }

            latest = reader.ReadElementContentAsDateTime();

            if (latest == lastUpdated)
                return stackQueue;

            Entry();//calls Entry recursivelly


            //TODO save latest update date
            return stackQueue;

        }

        private bool IdFound
        {
            get
            {
                return idFound;
            }
            set
            {
                if (idFound == false)//once set to true it should not be reset
                    idFound = value;
            }
        }
        private void Entry()
        {
            
            Queue<Entry> entries = null;
            Dictionary<string, string> links = new Dictionary<string, string>();
            //if latest found empty entries in queue and continue, then return

            while(reader.Read())
            {

                if(reader.LocalName == "entry")
                    break;

                if (reader.LocalName == "link")
                {
                    AddToLinks(links);
                }

            }

            if (reader.LocalName == "entry")
            {
                entries = new Queue<Entry>();
            }
            else
            {
                throw new Exception("No feed entries found!");
            }
                

            do
            {
                ProcessEntry(entries);
            } while (reader.ReadToFollowing("entry"));

            if (entries.Count > 0)
                stackQueue.Push(entries);

            if(links.ContainsKey("prev-archive") && IdFound == false)
            {
                reader = XmlReader.Create(links["prev-archive"]);
                reader.MoveToContent();
                Entry();
            }

                

        }

        private void AddToLinks(Dictionary<string, string> links)
        {
            var rel = reader.GetAttribute("rel");
            var href = reader.GetAttribute("href");

            if (links.ContainsKey(rel) == false)
                links.Add(rel, href);
        }
        /// <summary>
        /// Processes an feed Entry
        /// </summary>
        /// <param name="entries">Queue with entries from an Atom feed</param>
        /// <returns>true/false if latest entry id has been found</returns>
        private void ProcessEntry(Queue<Entry> entries)
        {
           
            //latestEntry
            while (reader.Read())
            {
                if(reader.LocalName == "id")
                {
                    string id = reader.ReadElementContentAsString();

                    reader.ReadToFollowing("content");

                    string content = reader.ReadInnerXml();

                    if (id == latestEntry)
                    {
                        entries.Clear();
                        IdFound = true;
                    }
                    else
                    {
                        entries.Enqueue(new Entry { Content = content.Trim(), Id = id });
                    }

                }
            }

           

        }
    }
}
