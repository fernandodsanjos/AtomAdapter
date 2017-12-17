using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Shared.Components;
using System.Xml.Serialization;
namespace BizTalk.Adapter
{
    class Program
    {
        static void Main(string[] args)
        {
            string root = @"C:\atom\";
            //string filename = "recent.xml";
            string filename = "state.xml";
            FileStream stm = new FileStream(root + filename, FileMode.CreateNew);
            /*
            AtomReader atom = new AtomReader(stm, "4f3a1034-d67d-11e6-82da-9820e33d4bee");
            
            Stack<Queue<Entry>> stk = atom.Entries();
            */
            BizTalk.Adapter.Atom.AtomState atom = new BizTalk.Adapter.Atom.AtomState();
            atom.LastEntryId = "1234";
            atom.LastUpdated = DateTime.Now;

            XmlSerializer atomstate = new XmlSerializer(typeof(BizTalk.Adapter.Atom.AtomState));

            atomstate.Serialize(stm, atom);

        }
    }
}
