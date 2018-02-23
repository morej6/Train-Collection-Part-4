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
using Assigment1; //imported .DLL file.
using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Runtime.Serialization;
using System.Data.SqlClient;
using System.Data.Sql;
using System.Data;

namespace Assigment4
{



    public partial class MainWindow : Window
    {
        //Global variable which contains the location of the local DB
        string connString = @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=TrainSchedule;Integrated Security=True;Pooling=False";
        
        //*********************************************
        // Function: MainWindow
        //
        // Purpose: To execute code that insert into
        //          DB and populate listview and listbox
        //
        //************************************************
        public MainWindow()
        {

            InitializeComponent();
            //Setting all TextBox to read only. User will not be able to erase date from the textbox
            textBox.IsReadOnly = true;
            textBox1.IsReadOnly = true;
            textBox2.IsReadOnly = true;
            textBox3.IsReadOnly = true;
            textBox4.IsReadOnly = true;
            textBox5.IsReadOnly = true;
            //If there is any data in the DB already it will be loaded upon starting the program.
            fillListBox();
     
        }


        //Open the Dialog window to look for the file
        OpenFileDialog openJson = new OpenFileDialog();
        //StationCollection variable which will hold the data.
        StationCollection Collection = new StationCollection();
        //BranchSchedule variable which will hold the data.
        BranchSchedule bre = new BranchSchedule();
        //TrainCollection variable which will hold the data.
        TrainCollection train = new TrainCollection();

        //*********************************************
        // Function: MenuItem_Click (Open Branch Schedule)
        //
        // Purpose: Deserialize json file and populates
        //         listbox with the data.
        //
        //************************************************

        #region Branch Schedule Deserialize and Populates listbox
        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            openJson.Filter = " JSON|*.json"; //Filster to only show .json file
            if (openJson.ShowDialog() == true) //When cancel is press it will work as intended. if cancel is press the following code will not run
            {
                listBox1.Items.Clear();
                listView.Items.Clear();

                #region  Deserialize json file
                FileStream reader = new FileStream(openJson.FileName, FileMode.Open, FileAccess.Read); // reads the path file

                DataContractJsonSerializer inputSerializer;
                inputSerializer = new DataContractJsonSerializer(typeof(BranchSchedule));

                bre = (BranchSchedule)inputSerializer.ReadObject(reader); //saves the file info into the Station Collection variable

                reader.Close();
                textBox4.Text = bre.branchId.ToString();
                #endregion

                // Deserialize Train data
                ReadTrain();
                // Populate listbox with the train ids.
                foreach(int n in bre.trainId)
                {
                    listBox1.Items.Add(n);
                }
            }
        }
        #endregion


        //**************************************************
        // Function: MenuItem_Click1 (Import)
        //
        // Purpose: Deserialize json file and populates
        //         listbox with the data and insert
        //         into the DB
        //
        //************************************************
        #region Inserts data into DB and Populates listbox
        private void MenuItem_Click_1(object sender, RoutedEventArgs e)
        {
            //if any data is inside the DB it is erased before inserting again
            Delete_All();

            #region Deserialize json file
            openJson.Filter = "JSON|*.json"; //Filster to only show .json file
            if (openJson.ShowDialog() == true) //When cancel is press it will work as intended. if cance; is press the following code will not run
            {
                // textBox.Text = openJson.FileName; //saves file name path into textbox


                FileStream reader = new FileStream(openJson.FileName, FileMode.Open, FileAccess.Read); // reads the path file

                DataContractJsonSerializer inputSerializer;
                inputSerializer = new DataContractJsonSerializer(typeof(StationCollection));

                Collection = (StationCollection)inputSerializer.ReadObject(reader); //saves the file info into the Station Collection variable

                reader.Close();
                #endregion

                #region Insert data into DB
                SqlConnection sqlConn;
                sqlConn = new SqlConnection(connString);
                sqlConn.Open();

                SqlCommand command = new SqlCommand("INSERT INTO Stations" +
                        "(StationId, Name, Location, FareZone, MileageToPenn, PicFilename)  Values" +
                        "(@StationId, @Name, @Location, @FareZone, @MileageToPenn, @PicFilename)", sqlConn);
                try
                {
                    command.Parameters.Add("@StationId", SqlDbType.Int);
                    command.Parameters.Add("@Name", SqlDbType.NVarChar);
                    command.Parameters.Add("@Location", SqlDbType.NVarChar);
                    command.Parameters.Add("@FareZone", SqlDbType.NVarChar);
                    command.Parameters.Add("@MileageToPenn", SqlDbType.Decimal);
                    command.Parameters.Add("@PicFilename", SqlDbType.NVarChar);

                    foreach (station n in Collection.stationList)
                    {
                        // command.Parameters.Add("@StationId", SqlDbType.Int).Value = n.Id;
                        command.Parameters[0].Value = n.Id;
                        command.Parameters[1].Value = n.Name;
                        command.Parameters[2].Value = n.location;
                        command.Parameters[3].Value = n.FareZone;
                        command.Parameters[4].Value = n.MilageToPenn;
                        command.Parameters[5].Value = n.PicFileName;
                        int rowsAffected = command.ExecuteNonQuery();
                    }
                    sqlConn.Close();

                }
                catch (SqlException) { }
                #endregion

                //clears listbox
                listBox.Items.Clear();
                //fills listbox with the names of the stations
                fillListBox();

            }

        } // import ends

        #endregion

        //**************************************************
        // Function: MenuItem_Click2 (Help)
        //
        // Purpose: Messagebox with the information about the
        //           program
        //
        //************************************************
        #region Help subitem for menu
        private void MenuItem_Click_2(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Train Schedule\n" + "Version 3.0\n" + "Written by Juan Moreno" , "About Train Schedule");
        }
        #endregion

        //**************************************************
        // Function: listBox_SelectionChanged
        //
        // Purpose: Populates textbox with selected data
        //          from the DB
        //
        //************************************************

        #region Searches DB for information about the Station
        private void listBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {

                string curItem = listBox.SelectedItem.ToString();

                string Query = "SELECT StationId, Name, Location, FareZone,MileageToPenn, PicFilename  FROM dbo.Stations";
                SqlConnection sqlConn;
                sqlConn = new SqlConnection(connString);
                SqlCommand command = new SqlCommand(Query, sqlConn);
                SqlDataReader reader;

                sqlConn.Open();
                reader = command.ExecuteReader();
                   
                string name, id, location, fare, img, mi;
              
                while (reader.Read())
                {
                    id = reader["StationId"].ToString();
                    name = reader["Name"].ToString();
                    location = reader["Location"].ToString();
                    fare = reader["FareZone"].ToString();
                    mi = reader["MileageToPenn"].ToString();
                    img = reader["PicFilename"].ToString();
                 
;                    if (name == curItem)
                    {
                        
                        textBox.Text = name;
                        textBox1.Text = id;
                        textBox2.Text = location;
                        textBox3.Text = fare;
                        textBox5.Text = mi;
                        //Relative path does not work?
                        //image.Source = new BitmapImage(new Uri(@"C:\\bin\Debug\" + img, UriKind.Relative));

                        //I give the full path and insert the image name to find it.
                        image.Source = new BitmapImage(new Uri(AppDomain.CurrentDomain.BaseDirectory + img, UriKind.Absolute));

                    }
                }
            }
            catch (Exception) { }
            
        }

        #endregion

        //**************************************************
        // Function: Delete_All
        //
        // Purpose: Erases the data in the DB
        //
        //************************************************
        #region Delete_All deletes data from table
        private void Delete_All()
        {
            try
            {
                string sql = "DELETE  FROM dbo.Stations";

                SqlConnection sqlConn;
                sqlConn = new SqlConnection(connString);
                sqlConn.Open();

                SqlCommand command = new SqlCommand(sql, sqlConn);
                int rowsAffected = command.ExecuteNonQuery();
            }catch (SqlException) { }
        }
        #endregion

        //**************************************************
        // Function: fillListBox
        //
        // Purpose: Populates listbox witht he names of the stations
        //
        //************************************************
        #region fillListBox
        private void fillListBox()
        {

            string Query = "SELECT Name FROM dbo.Stations ;";
            SqlConnection sqlConn;
            sqlConn = new SqlConnection(connString);
            SqlCommand command = new SqlCommand(Query, sqlConn);
            SqlDataReader reader;

            sqlConn.Open();
            reader = command.ExecuteReader();

            int fieldCount = reader.FieldCount;

            string name;
            while (reader.Read())
            {
                for (int i = 0; i < fieldCount; i++)
                {
                     name = reader["Name"].ToString();
                    listBox.Items.Add(name);
                }
            }

        }
        #endregion


        //**************************************************
        // Function: ReadTrain
        //
        // Purpose: Deserialize json file 
        //         
        //
        //************************************************

        #region ReadTrain
        private void ReadTrain()
        {
            listView.Items.Clear();
            FileStream reader = new FileStream("TrainCollection2.json", FileMode.Open, FileAccess.Read); // reads the path file

            DataContractJsonSerializer inputSerializer;
            inputSerializer = new DataContractJsonSerializer(typeof(TrainCollection));

            train = (TrainCollection)inputSerializer.ReadObject(reader); //saves the file info into the Station Collection variable

            reader.Close();
        }
        #endregion


        //**************************************************
        // Function: listBox1_SelectionChanged
        //
        // Purpose: Matches the Train Id to the Branch Schedule 
        //          which will shows that trains arrival time
        //
        //************************************************

        #region Find arrival time for train
        private void listBox1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                listView.Items.Clear();
                string curItem = listBox1.SelectedItem.ToString();
                Train teemo = new Train();
                teemo = train.FindTrain(Convert.ToInt32(curItem));

                foreach (StationArrival n2 in teemo.stationArrivalList)
                {
                    listView.Items.Add(n2);
                }
            }
            catch (Exception) { }
        }

        #endregion

        //**************************************************
        // Function: MenuItem_Click_3 (Exit)
        //
        // Purpose: Terminates program
        //         
        //
        //************************************************
        #region Exits program
        private void MenuItem_Click_3(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        #endregion


    }
}
