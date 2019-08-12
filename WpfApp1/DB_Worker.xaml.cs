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

namespace WpfApp1
{
    /// <summary>
    /// Логика взаимодействия для DB_Worker.xaml
    /// </summary>
    public partial class DB_Worker : Window
    {
        public DB_Worker()
        {
            InitializeComponent();
        }


        private void DBW_ok_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private void DBW_cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
