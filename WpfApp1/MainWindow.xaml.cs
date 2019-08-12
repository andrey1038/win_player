using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
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
using System.Diagnostics;
using TagLib;
using System.Data.SqlClient;



namespace WpfApp1
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    /// 

    struct Track
    {
        //обязательные поля
        public object Id;
        public string filename;
        public string title;
        public short bitrate;
        public TimeSpan duration;

        //необязательные поля
        public object artist;
        public DateTime year;
        //art
    }
    struct PList
    {
        public object Id;
        public string name;
    }

    public partial class MainWindow : Window
    {
        //var DataBase
        string ConnectionString = @"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=C:\Users\Andrey\source\repos\C#\WpfApp1\WpfApp1\DatabaseApp.mdf;Integrated Security=True";
        string sqlExpression;
        SqlConnection connection;
        SqlCommand command;
        SqlDataReader reader;
        List<Track> tracks = new List<Track>();
        List<PList> pLists = new List<PList>();

        //var main application
        const string allmusic_folder_name = "all your music";
        const string newmusic_folder_name = "here new music";
        const string music_folder_name = "wpf1";
        const string music_folder_path = "C:\\Users\\Andrey\\Music";
        const string music_folder_full = music_folder_path + "\\" + music_folder_name + "\\";
        bool b_sp = false;
        int m_index = 0;
        bool bool_repeat = false;
        bool bool_shuffle = false;
        int[] orderOfTheIndexes;
        bool crutch_1 = true;

        //var 2 window 
        DB_Worker dB_Worker = new DB_Worker();


        //------------my Functions----------//
        private string Func_shielding(string str)
        {
            if (str != null || str.Length != 0)
            {
                //экранирование должно работать но НЕТ оно не работает
                /*for (int i = 0; i < str.Length; i++)
                {
                    if (str[i] == '\'')
                    {
                        str = str.Insert(i, "\"");
                        i++;
                    }
                }*/

                //замена символа ТОЖЕ не работает
                //str = str.Replace("'","\"");

                //удаление неугодного сивола
                for (int i = 0; i < str.Length; i++)
                {
                    if (str[i] == '\'')
                    {
                        str = str.Remove(i, 1);
                        i++;
                    }
                }

                return str;
            }

            return null;
        }
        private void ScanningFolder()
        {
            //получение всех файлов в сканируемой папке
            string[] newMusic = Directory.GetFiles(music_folder_full + newmusic_folder_name + "\\");
            if (newMusic.Length != 0)
            {
                foreach (string str in newMusic)
                {
                    //пытаемся добавить файл в БД AddTrack(str)
                    //если успешно то перемещаем файл в папку со всеми треками
                    if (AddTrack(str))
                    {
                        FileInfo file = new FileInfo(str);
                        file.MoveTo(music_folder_full + allmusic_folder_name + "\\" + file.Name);
                    }
                }
            }
        }
        private bool AddPlayList(string namePL)
        {
            if (namePL.Length == 0)
            {
                MessageBox.Show("введите имя для нового листа");
                return false;
            }

            char[] forbidden_characters = { '/', '\\', ':', '*', '?', '!', '<', '>', '|', '+', '-', '.', '"', '%', '@', '[',']' ,'{' , '}' };

            //проверка на недопустимые символы
            foreach (char nPL in namePL)
            {
                foreach (char FChar in forbidden_characters)
                {
                    if (nPL == FChar)
                    {
                        MessageBox.Show("имя листа не должно содержать следующих знаков\n" +
                            "/ \\ : * ? ! < > | + - . \" % @ [ ] { } ", "внимание",MessageBoxButton.OK,MessageBoxImage.Information);
                        return false;
                    }
                }
            }

            //добавление данных в БД
            sqlExpression = "INSERT INTO PlayList (name) VALUES (N'" + NewNamePlayList.Text + "')";
            command = new SqlCommand(sqlExpression, connection);
            int bf = command.ExecuteNonQuery();
            if (bf <= 0)
            {
                MessageBox.Show("отмена операции !\n" +
                    "запрос на добавление нового листа не выполнен\n" +
                    "(но это не точно)\n" +
                    "ExecuteNonQuery return: " + bf);
                return false;
            }
            
            p_listbox.Items.Add(NewNamePlayList.Text);
            NewNamePlayList.Text = "";

            return true;
        }
        private bool AddTrack(string nameFile)
        {
            //библиотека taglib
            var taglib = TagLib.File.Create(nameFile);

            //заполнение обязательных полей
            Track track;
            track.filename = Func_shielding(nameFile.Substring(1 + music_folder_full.Length + newmusic_folder_name.Length));
            track.title = Func_shielding(taglib.Tag.Title);
            track.bitrate = Convert.ToInt16(taglib.Properties.AudioBitrate);
            track.duration = taglib.Properties.Duration;

            //проверка на недопустимые значения полей
            if (track.filename.Length > 100) { MessageBox.Show("имя фала привышает 100 знаков"); return false; }
            if (track.title.Length > 40) {MessageBox.Show("title. трека привышает 40 знаков"); return false; }

            //заполнение необязательных полей

            //не работает

            //track.year = DateTime.MinValue;
            //track.year.AddYears(Convert.ToInt32(taglib.Tag.Year));
            //MessageBox.Show(Convert.ToString(track.year.Year));

            //заполнение поля артист
            track.artist = null;
            string artist = taglib.Tag.FirstPerformer;
            if (artist == null || artist.Length == 0 || artist.Length > 30)
            {
                MessageBox.Show("невозможно добавить артиста тк. " +
                    "длина его имени привышает 30 символов");
                return false;
            }
            else
            {
                //защита
                artist = Func_shielding(artist);

                //проверка на существование артиста в БД
                sqlExpression = "SELECT * FROM Artist WHERE name=N'" + artist + "'";
                command = new SqlCommand(sqlExpression, connection);
                reader = command.ExecuteReader();
                if (reader.HasRows)
                {
                    //артист есть в БД
                    //привязка артиста к треку
                    reader.Read();
                    track.artist = reader.GetValue(0);
                    reader.Close();
                }
                else
                {
                    reader.Close();

                    //артиста нет в БД
                    //добавление нового артиста в БД
                    sqlExpression = "INSERT INTO Artist (name) VALUES (N'" + artist + "')";
                    command = new SqlCommand(sqlExpression, connection);
                    int i_artist = command.ExecuteNonQuery();
                    if (i_artist <= 0)
                    {
                        MessageBox.Show("отмена операции !\n" +
                            "запрос на добавление нового артиста не выполнен\n" +
                            "ExecuteNonQuery return: " + i_artist);
                        return false;
                    }

                    //получение id артиста
                    sqlExpression = "SELECT * FROM Artist WHERE name=N'" + artist + "'";
                    command = new SqlCommand(sqlExpression, connection);
                    reader = command.ExecuteReader();

                    //привязка трека к артисту
                    reader.Read();
                    track.artist = reader.GetValue(0);
                    reader.Close();
                }
            }

            //добавление нового трека в БД
            sqlExpression = "INSERT INTO Track (filename, title, artist, duration, bitrate) VALUES (";
            sqlExpression += "N'" + track.filename + "',";
            sqlExpression += "N'" + track.title + "',";
            sqlExpression += "'" + track.artist + "',";
            sqlExpression += "'" + track.duration + "',";
            sqlExpression += "'" + track.bitrate + "')";

            command = new SqlCommand(sqlExpression, connection);
            int i_track = command.ExecuteNonQuery();
            if (i_track <= 0)
            {
                MessageBox.Show("отмена операции !\n" +
                            "запрос на добавление нового трека не выполнен\n" +
                            "ExecuteNonQuery return: " + i_track);
                return false;
            }

            //привязка к альбому
            string alb = taglib.Tag.Album;
            if (alb == null || alb.Length == 0 || alb.Length > 40)
            {
                MessageBox.Show("не возможно добавить плей лист тк. " +
                    "его длина привышает 40 символов.");
                //return false;
            }
            else
            {
                //защита
                alb = Func_shielding(alb);

                //получение id трека
                sqlExpression = "SELECT Id FROM Track WHERE filename=N'" + track.filename + "'";
                command = new SqlCommand(sqlExpression, connection);
                reader = command.ExecuteReader();

                reader.Read();
                object id_track = reader.GetValue(0);
                reader.Close();

                //проверка на существование альбома в БД
                sqlExpression = "SELECT Id FROM PlayList WHERE name=N'" + alb + "'";
                command = new SqlCommand(sqlExpression, connection);
                reader = command.ExecuteReader();
                if (reader.HasRows)
                {
                    //альбом есть в БД
                    //связка трека и альбома
                    reader.Read();
                    sqlExpression = "INSERT INTO inPlayList (playlist,track) VALUES (";
                    sqlExpression += "'" + reader.GetValue(0) + "',";
                    sqlExpression += "'" + id_track + "')";

                    reader.Close();
                    command = new SqlCommand(sqlExpression, connection);
                    int i_inPL = command.ExecuteNonQuery();
                    if (i_inPL <= 0)
                    {
                        MessageBox.Show("отмена операции !\n" +
                            "запрос на добавление связи альбома и трека не выполнен\n" +
                            "ExecuteNonQuery return: " + i_inPL);
                        //return false;
                    }
                    reader.Close();
                }
                else
                {
                    reader.Close();

                    //альбома нет в БД
                    //добавление нового альбома в БД
                    sqlExpression = "INSERT INTO PlayList (name) VALUES (N'" + alb + "')";
                    command = new SqlCommand(sqlExpression, connection);
                    int i_PL = command.ExecuteNonQuery();
                    if (i_PL <= 0)
                    {
                        MessageBox.Show("отмена операции !\n" +
                           "запрос на добавление альбома не выполнен\n" +
                           "ExecuteNonQuery return: " + i_PL);
                        return false;
                    }
                    reader.Close();

                    //получение id плей листа
                    sqlExpression = "SELECT Id FROM PlayList WHERE name=N'" + alb + "'";
                    command = new SqlCommand(sqlExpression, connection);
                    reader = command.ExecuteReader();

                    //связка трека и альбома
                    reader.Read();
                    sqlExpression = "INSERT INTO inPlayList (playlist,track) VALUES (";
                    sqlExpression += "'" + reader.GetValue(0) + "',";
                    sqlExpression += "'" + id_track + "')";

                    reader.Close();
                    command = new SqlCommand(sqlExpression, connection);
                    int i_inPL = command.ExecuteNonQuery();
                    if (i_inPL <= 0)
                    {
                        MessageBox.Show("отмена операции !\n" +
                            "запрос на добавление связи альбома и трека не выполнен\n" +
                            "ExecuteNonQuery return: " + i_inPL);
                        return false;
                    }
                    reader.Close();
                }
            }

            return true;
        }
        private void loadedmediasourse(int index)
        {
            if (tracks.Count != 0)
            {
                mediaelement.Source = new Uri(music_folder_full + allmusic_folder_name + "\\" + tracks[index].filename);
                Track_art.Source = new BitmapImage(new Uri("imageapp/no_art.jpg", UriKind.Relative));
                //m_listbox.SelectedIndex = index; //вызывает ошибку в логике
                //mediaelement.Clock !!1!11!1!!!!

                L_title.Content = tracks[index].title;
                L_list.Content = "";
                L_artist.Content = tracks[index].artist;


                m_slider.Value = 0;
            }
        }

        

        public MainWindow()
        {
            InitializeComponent();

            //подключение к локальной БД
            connection = new SqlConnection(ConnectionString);
            try
            {
                connection.Open();

                /*MessageBox.Show("свойства подключениея к базе данных: " + "\n" +
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
                Environment.Exit(0);
            }

            //проверка на существование рабочей папки и подпапок
            if (!Directory.Exists(music_folder_path + "\\" + music_folder_name))
            {
                MessageBox.Show("Внимание!\nРабочая папка была перемещена, удалена или переименована");
                Environment.Exit(0); 
            }
            else if (!Directory.Exists(music_folder_full + allmusic_folder_name))
            {
                MessageBox.Show("Внимание!\nпод папка была перемещена, удалена или переименована");
                Environment.Exit(0);
            }
            else if (!Directory.Exists(music_folder_full + newmusic_folder_name))
            {
                MessageBox.Show("Внимание!\nпод папка была перемещена, удалена или переименована");
                Environment.Exit(0);
            }

            //сканирование папки на наличие новых треков и последующей обработке
            ScanningFolder();

            //вызов окна DB_worcker
            //dB_Worker.Show();
            
            //загрузка плей листов из БД и загрузка в listbox
            sqlExpression = "SELECT Id, name FROM PlayList";
            command = new SqlCommand(sqlExpression, connection);
            reader = command.ExecuteReader();
            if (reader.HasRows)
            {
                while (reader.Read())
                {
                    PList temp = new PList();
                    temp.Id = reader.GetValue(0);
                    temp.name = reader.GetString(1);

                    pLists.Add(temp);
                    p_listbox.Items.Add(temp.name);
                }
            }
            else
            {
                MessageBox.Show("плей листов нет!");
            }
            reader.Close();

            //загрузка треков из БД
            sqlExpression = "SELECT t1.Id, t1.filename, t1.title, t2.name FROM Track as t1 LEFT OUTER JOIN Artist as t2 ON t1.artist = t2.Id";
            command = new SqlCommand(sqlExpression, connection);
            reader = command.ExecuteReader();
            if (reader.HasRows)
            {
                while (reader.Read())
                {
                    Track temp = new Track();
                    temp.Id = reader.GetValue(0);
                    temp.filename = reader.GetString(1);
                    temp.title = reader.GetString(2);
                    temp.artist = reader.GetString(3);

                    tracks.Add(temp);
                }
            }
            reader.Close();

            //загрузка треков в listbox
            foreach (Track i in tracks)
            {
                m_listbox.Items.Add(i.title + " | " + i.artist);
            }

            //загрузка первого трека в mediaelement
            loadedmediasourse(0);

            //установка значения громкости
            m_volume.Value = 0.3;
        }



        //----------ControlElement----------//

            //---buttons---//

                //--page_1--//
        private void B_back_Click(object sender, RoutedEventArgs e)
        {
            if (m_index == 0)
                m_index = tracks.Count - 1;
            else
                m_index--;

            if (bool_shuffle)
            {
                loadedmediasourse(orderOfTheIndexes[m_index]);
            }
            else
            {
                loadedmediasourse(m_index);
            }
        }
        private void B_PausePlay_Click(object sender, RoutedEventArgs e)
        {
            if (b_sp)
            {
                mediaelement.Pause();
                b_sp = false;
            }
            else
            {
                mediaelement.Play();
                b_sp = true;
            }
        }
        private void B_next_Click(object sender, RoutedEventArgs e)
        {
            if (m_index == tracks.Count - 1)
                m_index = 0;
            else
                m_index++;

            if (bool_shuffle)
            {
                loadedmediasourse(orderOfTheIndexes[m_index]);
            }
            else
            {
                loadedmediasourse(m_index);
            }
        }
        private void B_repeat_Click(object sender, RoutedEventArgs e)
        {
            if (bool_repeat)
            {
                bool_repeat = false;
                MessageBox.Show("повторение композиций\nотключено");
            }
            else
            {
                bool_repeat = true;
                MessageBox.Show("повторение композиций\nвключено");
            }
                
        }
        private void B_shuffle_Click(object sender, RoutedEventArgs e)
        {
            if (bool_shuffle)
            {
                bool_shuffle = false;
                MessageBox.Show("повторение композиций\nотключено");

                //очистка списка
                crutch_1 = false;
                m_listbox.Items.Clear();
                crutch_1 = true;

                //загрузка элементов в лист бокс
                foreach (Track i in tracks)
                {
                    m_listbox.Items.Add(i.title + " | " + i.artist);
                }
            }
            else
            {
                bool_shuffle = true;
                MessageBox.Show("повторение композиций\nвключено");

                orderOfTheIndexes = new int[tracks.Count];
                Random rnd = new Random();
                int rndindex;
                int temp;

                //заполнение массива
                for (int i = 0; i < tracks.Count; i++)
                {
                    orderOfTheIndexes[i] = i;
                }

                //перемещение текущего трека в начало списка
                temp = orderOfTheIndexes[m_index];
                orderOfTheIndexes[m_index] = orderOfTheIndexes[0];
                orderOfTheIndexes[0] = temp;

                //перемешивание
                for (int i = 1; i < tracks.Count; i++)
                {
                    rndindex = rnd.Next(i, tracks.Count);
                    temp = orderOfTheIndexes[rndindex];
                    orderOfTheIndexes[rndindex] = orderOfTheIndexes[i];
                    orderOfTheIndexes[i] = temp;
                }

                //очистка списка
                crutch_1 = false;
                m_listbox.Items.Clear();
                crutch_1 = true;

                //повторная загрузка в ранее заданном порядке
                for (int i = 0; i < tracks.Count; i++)
                {
                    m_listbox.Items.Add(tracks[orderOfTheIndexes[i]].title + " | " + tracks[orderOfTheIndexes[i]].artist);
                }

                m_index = 0;

            }
        }

                //--page_2--//
        private void B_newPlayList_Click(object sender, RoutedEventArgs e)
        {
            AddPlayList(NewNamePlayList.Text);
        }

            //---sliders---//
        private void m_volume_MW(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 0)
                m_volume.Value += 0.03; //+3% громкости
            else
                m_volume.Value -= 0.03; //-3% громкости
        }
        private void m_slider_MW(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 0)
                m_slider.Value += 1; //+1сек
            else
                m_slider.Value -= 1; //-1сек
        }
        private void ExpanderVolume_MouseEnter(object sender, MouseEventArgs e)
        {
            ExpanderVolume.IsExpanded = true;
        }
        private void ExpanderVolume_MouseLeave(object sender, MouseEventArgs e)
        {
            ExpanderVolume.IsExpanded = false;
        }

        //--------------window--------------//
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //высвобождение ресурсов DB_Worcker
            if (dB_Worker.ShowActivated)
            {
                dB_Worker.Close();
            }
        }
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Space: B_PausePlay_Click(sender, e); break;
                case Key.Right: B_next_Click(sender, e); break;
                case Key.Left: B_back_Click(sender, e); break;
            }
        }

        //-----------mediaelement-----------//
        private void Mediaelement_MediaOpened(object sender, RoutedEventArgs e)
        {
            if (!mediaelement.NaturalDuration.HasTimeSpan)
            {
                MessageBox.Show("объект не предоставляет \"NaturalDuration.TimeSpan\"");
            }
            else
            {
                TimeSpan MNaturalDuration = mediaelement.NaturalDuration.TimeSpan;
                m_slider.Maximum = MNaturalDuration.TotalSeconds;
                m_naturalDuration_label.Content = MNaturalDuration.Minutes + ":" + MNaturalDuration.Seconds;

            }
        }
        private void Mediaelement_MediaEnded(object sender, RoutedEventArgs e)
        {
            if (bool_repeat)
            {
                m_slider.Value = 0;
            }
            else
            {
                if (m_index == (tracks.Count - 1))
                {
                    MessageBox.Show("конец списка");
                }
                else
                {
                    m_index++;

                    if (bool_shuffle)
                    {
                        loadedmediasourse(orderOfTheIndexes[m_index]);
                    }
                    else
                    {
                        loadedmediasourse(m_index);
                    }
                }
            }
        }
        private void Mediaelement_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            MessageBoxResult rezult = MessageBox.Show("Не удалось открыть файл.\nВозможно файл был удален или перемещен пользователем.\n" +
                "Удалить соответствующюю запись в базе данных ?", "ERROR", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (rezult == MessageBoxResult.Yes)
            {
                sqlExpression = "DELETE FROM inPlayList WHERE track='" + tracks[m_index].Id + "'";
                command = new SqlCommand(sqlExpression, connection);
                command.ExecuteNonQuery();

                sqlExpression = "DELETE FROM Track WHERE Id='" + tracks[m_index].Id + "'";
                command = new SqlCommand(sqlExpression, connection);
                command.ExecuteNonQuery();

                tracks.RemoveAt(m_index);
                m_listbox.Items.RemoveAt(m_index);
            }
        }

        //---------SelectionChanged---------//

            //----lists----//
        private void M_listbox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (crutch_1)
            {
                if (bool_shuffle)
                {
                    loadedmediasourse(orderOfTheIndexes[m_listbox.SelectedIndex]);
                    m_index = m_listbox.SelectedIndex;
                }
                else
                {
                    loadedmediasourse(m_listbox.SelectedIndex);
                    m_index = m_listbox.SelectedIndex;
                }
            }
                
        }
        private void M_listbox_combobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (m_listbox_combobox.SelectedIndex)
            {
                //все треки
                case 0:
                    //очистка списков
                    crutch_1 = false;
                    m_listbox.Items.Clear();
                    in_listbox.Items.Clear();
                    tracks.Clear();
                    crutch_1 = true;

                    //загрузка треков из БД
                    sqlExpression = "SELECT t1.Id, t1.filename, t1.title, t2.name FROM Track as t1 LEFT OUTER JOIN Artist as t2 ON t1.artist = t2.Id";
                    command = new SqlCommand(sqlExpression, connection);
                    reader = command.ExecuteReader();
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            Track temp = new Track();
                            temp.Id = reader.GetValue(0);
                            temp.filename = reader.GetString(1);
                            temp.title = reader.GetString(2);
                            temp.artist = reader.GetString(3);

                            tracks.Add(temp);
                        }
                    }
                    reader.Close();

                    //обновление литбокса
                    foreach (Track i in tracks)
                    {
                        m_listbox.Items.Add(i.title + " | " + i.artist);
                    }

                    //установка индекса в начало списка
                    m_index = 0;

                    break;
            }
        }
        private void P_listbox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //очитска списков
            crutch_1 = false;
            m_listbox.Items.Clear();
            in_listbox.Items.Clear();
            tracks.Clear();
            crutch_1 = true;

            //маленький костылик //одному богу известно за что он отвечает
            m_listbox_combobox.SelectedIndex = -1;

            //загрузка Id выбраного плей листа
            object tempid = pLists[p_listbox.SelectedIndex].Id;

            //запрос в БД на выборку треков текущего плей листа
            sqlExpression = "SELECT Track.Id, Track.filename, Track.title, Artist.name FROM " +
                "(Track LEFT OUTER JOIN Artist ON Track.artist = Artist.Id) " +
                "INNER JOIN inPlayList ON inPlayList.track = Track.Id " +
                "WHERE inPlayList.playlist = '" + tempid + "'";
            command = new SqlCommand(sqlExpression, connection);
            reader = command.ExecuteReader();
            if (!reader.HasRows)
            {
                MessageBox.Show("плей лист пуст");
            }
            else
            {
                //заполенение листа tracks
                while (reader.Read())
                {
                    Track temp = new Track();
                    temp.Id = reader.GetValue(0);
                    temp.filename = reader.GetString(1);
                    temp.title = reader.GetString(2);
                    temp.artist = reader.GetString(3);

                    tracks.Add(temp);
                }

                //обновление литбоксов
                foreach (Track i in tracks)
                {
                    m_listbox.Items.Add(i.title + " | " + i.artist);
                    in_listbox.Items.Add(i.title + " | " + i.artist);
                }

                //установка индекса в начало списка
                m_index = 0;
            }

            reader.Close();
        }
        private void In_listbox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (crutch_1)
            {
                loadedmediasourse(in_listbox.SelectedIndex);
                m_index = in_listbox.SelectedIndex;
            } 
        }

            //---sleders---//
        private void M_slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            TimeSpan timecode = new TimeSpan(0, 0, Convert.ToInt32(m_slider.Value));
            mediaelement.Position = timecode;
            m_position_label.Content = timecode.Minutes + ":" + timecode.Seconds;
        }
        private void M_volume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            mediaelement.Volume = m_volume.Value;
            m_vol_l.Content = Convert.ToInt16(m_volume.Value * 100);
        }
    }
}
