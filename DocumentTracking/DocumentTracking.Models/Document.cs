using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DocumentTracking.Models
{
    public class Document
    {
        public string Number { get; set; }
        public DateTime Date { get; set; }
        public string Format { get; set; }
        public string Type { get; set; }
        public string InterchangeNumber { get; set; }
        public string Sender { get; set; }
        public string Receiver { get; set; }
        public string DeliveryPoint { get; set; }
        public string DeliveryPointName { get; set; }
        public DateTime SystemDate { get; set; }
        public string CustomXML { get; set; }
        public List<Reference> References { get; set; }
    }
}
