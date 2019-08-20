using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Data.SqlClient;

namespace WpfApp1
{
    /// <summary>
    /// Логика взаимодействия для DB_Worker.xaml
    /// </summary>
    public partial class DB_Worker : Window
    {
        //var DataBase
        string ConnectionString;
        string sqlExpression;
        SqlConnection connection;
        SqlCommand command;
        SqlDataReader reader;

        

        public DB_Worker(string ConnectionString, string target_path)
        {
            InitializeComponent();

            //сканирование папки
            string[] infolder = Directory.GetFiles(target_path);
            if (infolder.Length == 0)
            {
                this.Close();
            }
            else
            {
                this.Show();

                //подключение к БД
                this.ConnectionString = ConnectionString;
                connection = new SqlConnection(ConnectionString);
                try
                {
                    connection.Open();

                    /*MessageBox.Show("свойства подключениея к базе данных: " + "\n" +
                        "  * окно: ---------------- child\n" +
                        "  * база данных: --------- " + connection.Database + "\n" +
                        "  * id рабочей станции: -- " + connection.WorkstationId + "\n" +
                        "  * id клиента: ---------- " + connection.ClientConnectionId + "\n" +
                        "  * сервер: -------------- " + connection.DataSource + "\n" +
                        "  * версия сервера: ------ " + connection.ServerVersion + "\n" +
                        "  * состояние: ----------- " + connection.State);*/
                }
                catch (SqlException ex)
                {
                    MessageBox.Show(ex.Message);
                    this.Close();
                }

                //обработка содержимого папки
                for (int i = 0; i < infolder.Length; i++)
                {
                    infolder[i] = infolder[i].Substring(1 + target_path.Length);

                    f_listbox.Items.Add(infolder[i]);
                }

                //
            }
        }

        

       
        
        private void DBW_ok_Click(object sender, RoutedEventArgs e)
        {
            
        }
        private void DBW_cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        private void F_listbox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            object item = f_listbox.SelectedItem;
        }

        private void LastTrack_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
