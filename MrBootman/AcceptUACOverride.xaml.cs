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
using System.Windows.Shapes;

namespace MrBootman
{
    /// <summary>
    /// Interaction logic for About.xaml
    /// </summary>
    public partial class AcceptUACOverride
    {
        public AcceptUACOverride()
        {
            InitializeComponent();
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            // Close down the application
            Application.Current.Shutdown();
        }

        private void ckAcceptUAC_Checked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.UACOverrideAccepted = true;
            Properties.Settings.Default.Save();
        }
    }
}
