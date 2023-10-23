using DocumentTracking.Client.ServiceReference1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Linq;

namespace DocumentTracking.Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            XElement xe = XElement.Load("c:/test/INTTAXOUT.xml");
            BasicHttpBinding binding = new BasicHttpBinding();
            binding.MaxReceivedMessageSize = 2147483647;
            binding.MaxBufferSize = 2147483647;
            binding.OpenTimeout = new TimeSpan(0, 10, 0);
            binding.CloseTimeout = new TimeSpan(0, 10, 0);
            binding.SendTimeout = new TimeSpan(0, 10, 0);
            binding.ReceiveTimeout = new TimeSpan(0, 10, 0);
            EndpointAddress endpoint = new EndpointAddress(new Uri("http://localhost:54095/DocumentTrackingDataImportService.svc"));
            ServiceReference1.ImportDataClient client = new ServiceReference1.ImportDataClient();
            Response res =  client.ImportXmlString(ServiceReference1.XmlDataType.Invoice, "Document", xe.ToString());
        }
    }
}
