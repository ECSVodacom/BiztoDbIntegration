using Spar.Client.ServiceReference1;
using System;
using System.Collections.Generic;
using System.Linq;
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

namespace Spar.Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            ServiceReference1.ImportDataClient client = new ServiceReference1.ImportDataClient();
            XElement x = XElement.Parse(txt.Text);
            Response res = client.ImportXml(ServiceReference1.XmlDataType.ReconImport, "Recon", x);
        }
    }
}
