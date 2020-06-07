using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
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

namespace D5_Buddy
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        /// <summary>
        /// CarDB entry format, storing a cars ID, name, model etc
        /// </summary>
        public class CarDB
        {
            [JsonProperty("car_id")]
            public short CarID { get; set; }
            [JsonProperty("car_name")]
            public string CarName { get; set; }
            [JsonProperty("car_model")]
            public string CarModel { get; set; }

            [JsonProperty("colors")]
            public ObservableCollection<CarColor> CarColors { get; set; }

            public CarDB(short car_id, string car_name, string car_model, ObservableCollection<CarColor> colors)
            {
                CarID = car_id;
                CarName = car_name;
                CarModel = car_model;
                CarColors = colors;
            }
        }
        /// <summary>
        /// This handles parsing the Car Colors from the JSON database. 
        /// </summary>
        public class CarColor
        {
            [JsonProperty("color_id")]
            public short ColorID { get; set; }
            [JsonProperty("color_name")]
            public string ColorName { get; set; }
            public int[] ColorRGB { get; set; }

            public CarColor(short color_id, string color_name, int[] color_rgb)
            {
                ColorID = color_id;
                ColorName = color_name;
                ColorRGB = color_rgb;
            }
            public Color ToColor()
            {
                return Color.FromRgb(Convert.ToByte(ColorRGB[0]), Convert.ToByte(ColorRGB[1]), Convert.ToByte(ColorRGB[2]));
            }
        }
        /// <summary>
        /// A Car. More specifically, a copy of the players car, containing all the infos needed to make changes and or fill the UI.
        /// </summary>
        public class Car
        {
            public CarDB CarObject { get; set; }
            public short ID { get; set; }
            public string Model { get; set; }
            public string Name { get; set; }
            public short SelectedColor { get; set; }
            public ObservableCollection<CarColor> Colors { get; set; }
            public byte[] Tuning { get; set; }
            public byte[] NewTuning { get; set; }
            public Car(CarDB carobject, short id, string model, string name, short colorid, ObservableCollection<CarColor> colors, byte[] tuning, byte[] newTuning)
            {
                CarObject = carobject;
                ID = id;
                Model = model;
                Name = name;
                SelectedColor = colorid;
                Colors = colors;
                Tuning = tuning;
                NewTuning = newTuning;
            }

            public Car()
            {
                Model = "";
            }

        }

        readonly Encoding shiftJIS = Encoding.GetEncoding("shift-jis");
        readonly string CarJSONPath = "D5_Buddy.Resources.cars.json";
        string CarJSON = "";
        public ObservableCollection<CarDB> AllCars = new ObservableCollection<CarDB>();
        readonly string[] AllRanks = { "D3", "D2", "D1", "C3", "C2", "C1", "B3", "B2", "B1", "A3", "A2", "A1", "S3", "S2", "S1", "SS" };

        // Set up empty player variables here
        string LoadedName = "";
        int LoadedDPoints = 0;
        string LoadedGender = "Male";
        int LoadedRank = 0;

        #region Debug Variables
        int LoadedSelectedCar = 0;
        #endregion

        //List<String> CarsInGarage = new List<String>();
        ObservableCollection<string> CarsInGarage = new ObservableCollection<string>();
        // Set up all 3 empty car slots
        Car FirstCar = new Car();
        Car SecondCar = new Car();
        Car ThirdCar = new Car();

        // ALL TUNING OPTIONS
        readonly byte[] fullTuneMTBytes = new byte[] { 0xFF, 0xFF };
        readonly byte[] fullTuneATBytes = new byte[] { 0xFF, 0x7F };
        readonly byte[] lastTuneStepBytes = new byte[] { 0xFE, 0xFF };

        // Load original card in mem, for saving later
        public byte[] CardInMemory;
        public string CardPath = "";
        public bool RefreshBlocked = false;
        public bool SelectedCarRefreshBlocked = false;
        public bool CardLoading = false;


        public MainWindow()
        {
            InitializeComponent();
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(CarJSONPath))
            {
                TextReader tr = new StreamReader(stream);
                CarJSON = tr.ReadToEnd();
            }
            AllCars = JsonConvert.DeserializeObject<ObservableCollection<CarDB>>(CarJSON);
            Rank_ComboBox.ItemsSource = AllRanks;
        }

        private void LoadCard_Button_Click(object sender, RoutedEventArgs e)
        {
            CardLoading = true;
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
                using (FileStream fs = File.OpenRead(openFileDialog.FileName))
                using (BinaryReader binaryReader = new BinaryReader(fs))
                {
                    CardPath = openFileDialog.FileName;
                    // CAR AMOUNT, SELECTED CAR
                    fs.Seek(0x69, SeekOrigin.Begin);
                    int selectedCar = binaryReader.ReadByte();
                    int amountOfCars = binaryReader.ReadByte();

                    LoadedSelectedCar = selectedCar;
                    // DPOINTS
                    fs.Seek(0x84, SeekOrigin.Begin);
                    int dPoints = binaryReader.ReadInt32();
                    LoadedDPoints = dPoints;

                    // RANK
                    fs.Seek(0x67, SeekOrigin.Begin);
                    LoadedRank = binaryReader.ReadByte();

                    // NAME READING AND FILTERING
                    fs.Seek(0xB4, SeekOrigin.Begin);
                    byte[] nameBuf = binaryReader.ReadBytes(12);
                    List<byte> nameList = new List<byte>();
                    for (int i = 0; i <= nameBuf.Length - 1; i++)
                    {

                        if (nameBuf[i] != 0)
                        {

                            nameList.Add(nameBuf[i]);
                        }
                        else
                        {

                            break;
                        }
                    }
                    byte[] nameBufferFiltered = nameList.ToArray();
                    string nameString = shiftJIS.GetString(nameBufferFiltered);
                    LoadedName = nameString;

                    ////////////////////////
                    //// LOAD FIRST CAR
                    ////////////////////////
                    
                    // Seek to start of Car 1 data
                    fs.Seek(0xC4, SeekOrigin.Begin);

                    // Load ID and Color
                    FirstCar.ID = BitConverter.ToInt16(binaryReader.ReadBytes(2), 0);
                    FirstCar.SelectedColor = BitConverter.ToInt16(binaryReader.ReadBytes(2), 0);

                    // Load data from the CarDB via the CarID
                    LoadCarFromID(ref FirstCar);

                    // Seek forward to Car 1 Tuning
                    fs.Seek(0xC8, SeekOrigin.Begin);
                    FirstCar.Tuning = binaryReader.ReadBytes(2);
                    FirstCar.NewTuning = FirstCar.Tuning;
                    if (FirstCar.Tuning.SequenceEqual(fullTuneMTBytes))
                    {
                        FirstCarFullTuneMT.IsChecked = true;
                    }

                    if (FirstCar.Tuning.SequenceEqual(fullTuneATBytes))
                    {
                        FirstCarFullTuneAT.IsChecked = true;
                    }

                    if (FirstCar.Tuning.SequenceEqual(lastTuneStepBytes))
                    {
                        FirstCarLastStepTune.IsChecked = true;
                    }

                    CarsInGarage.Add(FirstCar.Name);

                    ////////////////////////
                    //// LOAD SECOND CAR
                    ////////////////////////
                    
                    if (amountOfCars > 1)
                    {
                        // Seek to start of Car 2 data
                        fs.Seek(0x124, SeekOrigin.Begin);

                        // Load ID and Color
                        SecondCar.ID = BitConverter.ToInt16(binaryReader.ReadBytes(2), 0);
                        SecondCar.SelectedColor = BitConverter.ToInt16(binaryReader.ReadBytes(2), 0);

                        // Load data from the CarDB via the CarID
                        LoadCarFromID(ref SecondCar);

                        // Seek forward to Car 2 Tuning
                        fs.Seek(0x128, SeekOrigin.Begin);
                        SecondCar.Tuning = binaryReader.ReadBytes(2);
                        SecondCar.NewTuning = SecondCar.Tuning;
                        if (SecondCar.Tuning.SequenceEqual(fullTuneMTBytes))
                        {
                            SecondCarFullTuneMT.IsChecked = true;
                        }

                        if (SecondCar.Tuning.SequenceEqual(fullTuneATBytes))
                        {
                            SecondCarFullTuneAT.IsChecked = true;
                        }

                        if (SecondCar.Tuning.SequenceEqual(lastTuneStepBytes))
                        {
                            SecondCarLastStepTune.IsChecked = true;
                        }

                        CarsInGarage.Add(SecondCar.Name);
                    }

                    ////////////////////////
                    //// LOAD THIRD CAR
                    ////////////////////////

                    if (amountOfCars > 2)
                    {
                        // Seek to start of Car 2 data
                        fs.Seek(0x184, SeekOrigin.Begin);

                        // Load ID and Color
                        ThirdCar.ID = BitConverter.ToInt16(binaryReader.ReadBytes(2), 0);
                        ThirdCar.SelectedColor = BitConverter.ToInt16(binaryReader.ReadBytes(2), 0);

                        // Load data from the CarDB via the CarID
                        LoadCarFromID(ref ThirdCar);

                        // Seek forward to Car 3 Tuning
                        fs.Seek(0x188, SeekOrigin.Begin);
                        ThirdCar.Tuning = binaryReader.ReadBytes(2);
                        ThirdCar.NewTuning = ThirdCar.Tuning;
                        if (ThirdCar.Tuning.SequenceEqual(fullTuneMTBytes))
                        {
                            ThirdCarFullTuneMT.IsChecked = true;
                        }

                        if (ThirdCar.Tuning.SequenceEqual(fullTuneATBytes))
                        {
                            ThirdCarFullTuneAT.IsChecked = true;
                        }

                        if (ThirdCar.Tuning.SequenceEqual(lastTuneStepBytes))
                        {
                            ThirdCarLastStepTune.IsChecked = true;
                        }

                        CarsInGarage.Add(ThirdCar.Name);
                    }

                    // LOAD EVERYTHING INTO THE UI
                    LoadUIValues();

                    fs.Seek(0x00, SeekOrigin.Begin);
                    CardInMemory = binaryReader.ReadBytes((int)binaryReader.BaseStream.Length);
                    SaveCard_Button.IsEnabled = true;
                }
        }

        private void LoadUIValues()
        {
            HideAllSlots();
            // Set up General Page
            Name_TextBox.Text = LoadedName;
            DPoint_TextBox.Text = LoadedDPoints.ToString();
            Rank_ComboBox.SelectedIndex = LoadedRank - 1;
            SelectedCar_ComboBox.ItemsSource = CarsInGarage;
            SelectedCarRefreshBlocked = true;
            SelectedCar_ComboBox.SelectedIndex = LoadedSelectedCar;
            SelectedCarRefreshBlocked = false;



            if (FirstCar.Model != "")
            {
                FirstCarModel_Combobox.ItemsSource = AllCars;
                FirstCarModel_Combobox.SelectedValue = FirstCar.ID;

                FirstCarColor_Combobox.ItemsSource = FirstCar.Colors;
                FirstCarColor_Combobox.SelectedValue = FirstCar.SelectedColor;
                UnhideSlot(1);
            }

            if (SecondCar.Model != "")
            {
                SecondCarModel_Combobox.ItemsSource = AllCars;
                SecondCarModel_Combobox.SelectedValue = SecondCar.ID;

                SecondCarColor_Combobox.ItemsSource = SecondCar.Colors;
                SecondCarColor_Combobox.SelectedValue = SecondCar.SelectedColor;
                UnhideSlot(2);
            }

            if (ThirdCar.Model != "")
            {
                ThirdCarModel_Combobox.ItemsSource = AllCars;
                ThirdCarModel_Combobox.SelectedValue = ThirdCar.ID;

                ThirdCarColor_Combobox.ItemsSource = ThirdCar.Colors;
                ThirdCarColor_Combobox.SelectedValue = ThirdCar.SelectedColor;
                UnhideSlot(3);
            }

            CardLoading = false;
        }

        private void SaveCard_Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (MemoryStream memoryStream = new MemoryStream(CardInMemory))
                {
                    using (BinaryWriter binaryWriter = new BinaryWriter(memoryStream))
                    {
                        Console.WriteLine("saving to binary card data");
                        // SAVE NEW NAME
                        string newName = Name_TextBox.Text;
                        byte[] newNameBytes = shiftJIS.GetBytes(newName);
                        byte[] emptyName = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
                        memoryStream.Seek(0xB4, SeekOrigin.Begin);
                        // Overwrite with empty first
                        binaryWriter.Write(emptyName);
                        memoryStream.Seek(0xB4, SeekOrigin.Begin);
                        // ADD IN CHECK FOR NAME LENGTH
                        binaryWriter.Write(newNameBytes);

                        // DPOINTS
                        memoryStream.Seek(0x84, SeekOrigin.Begin);
                        binaryWriter.Write(int.Parse(DPoint_TextBox.Text));

                        // RANK
                        memoryStream.Seek(0x67, SeekOrigin.Begin);
                        int newRank = Rank_ComboBox.SelectedIndex + 1;
                        binaryWriter.Write(Convert.ToByte(newRank));

                        // SELECTED CAR
                        memoryStream.Seek(0x69, SeekOrigin.Begin);
                        binaryWriter.Write(Convert.ToByte(LoadedSelectedCar));

                        ////////////////////////
                        //// WRITE FIRST CAR
                        ////////////////////////

                        // Seek to start of Car 1 data
                        memoryStream.Seek(0xC4, SeekOrigin.Begin);

                        // Save ID and Color
                        binaryWriter.Write(FirstCar.ID);
                        binaryWriter.Write(FirstCar.SelectedColor);

                        // Save Tuning
                        binaryWriter.Write(FirstCar.NewTuning);

                        if (SecondCar.Model != "")
                        {
                            ////////////////////////
                            //// WRITE SECOND CAR
                            ////////////////////////

                            // Seek to start of Car 2 data
                            memoryStream.Seek(0x124, SeekOrigin.Begin);

                            // Save ID and Color
                            binaryWriter.Write(SecondCar.ID);
                            binaryWriter.Write(SecondCar.SelectedColor);

                            // Save Tuning
                            binaryWriter.Write(SecondCar.NewTuning);
                        }

                        if (ThirdCar.Model != "")
                        {
                            ////////////////////////
                            //// WRITE THIRD CAR
                            ////////////////////////

                            // Seek to start of Car 2 data
                            memoryStream.Seek(0x184, SeekOrigin.Begin);

                            // Save ID and Color
                            binaryWriter.Write(ThirdCar.ID);
                            binaryWriter.Write(ThirdCar.SelectedColor);

                            // Save Tuning
                            binaryWriter.Write(ThirdCar.NewTuning);
                        }

                    }
                }
                //File.WriteAllBytes("saved.card", CardInMemory);
                Console.WriteLine("Card Saving Path: " + CardPath);
                File.WriteAllBytes(CardPath, CardInMemory);
                ShowPopupNotification("Card has been saved.");
            }
            catch (Exception error)
            {
                Console.WriteLine("{0} Exception caught.", e);
                ShowPopupNotification("Card could not be saved. \n Error: " + error);
            }
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        public async void ShowPopupNotification(string messageText)
        {
            var dialogContent = new TextBlock
            {
                Text = messageText,
                Margin = new Thickness(20)
            };
            await MaterialDesignThemes.Wpf.DialogHost.Show(dialogContent);
        }

        private void Car_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string whichCarSlot = ((ComboBox)sender).Tag.ToString();
            int CarIndex = int.Parse((sender as ComboBox).SelectedValue.ToString());
            CarDB Car = new ObservableCollection<CarDB>(AllCars.Where(x => x.CarID == CarIndex)).First();
            Uri car_icon_uri = new Uri("Resources/" + Car.CarModel + ".png", UriKind.Relative);
            ImageSource car_icon_src = new BitmapImage(car_icon_uri);
            switch (whichCarSlot)
            {
                case "first":
                    FirstCarIcon.Source = car_icon_src;
                    FirstCar.ID = Car.CarID;
                    LoadCarFromID(ref FirstCar);
                    RefreshBlocked = true;
                    FirstCarColor_Combobox.ItemsSource = null;
                    RefreshBlocked = false;
                    FirstCarColor_Combobox.ItemsSource = FirstCar.Colors;
                    if(!CardLoading)
                    {
                        FirstCarColor_Combobox.SelectedIndex = 0;
                    }
                    CarsInGarage[0] = FirstCar.Name;
                    SelectedCarRefreshBlocked = true;
                    SelectedCar_ComboBox.SelectedIndex = LoadedSelectedCar;
                    SelectedCarRefreshBlocked = false;
                    break;

                case "second":
                    SecondCarIcon.Source = car_icon_src;
                    SecondCar.ID = Car.CarID;
                    LoadCarFromID(ref SecondCar);
                    RefreshBlocked = true;
                    SecondCarColor_Combobox.ItemsSource = null;
                    RefreshBlocked = false;
                    SecondCarColor_Combobox.ItemsSource = SecondCar.Colors;
                    if (!CardLoading)
                    {
                        SecondCarColor_Combobox.SelectedIndex = 0;
                    }
                    CarsInGarage[1] = SecondCar.Name;
                    SelectedCarRefreshBlocked = true;
                    SelectedCar_ComboBox.SelectedIndex = LoadedSelectedCar;
                    SelectedCarRefreshBlocked = false;
                    break;

                case "third":
                    ThirdCarIcon.Source = car_icon_src;
                    ThirdCar.ID = Car.CarID;
                    LoadCarFromID(ref ThirdCar);
                    RefreshBlocked = true;
                    ThirdCarColor_Combobox.ItemsSource = null;
                    RefreshBlocked = false;
                    ThirdCarColor_Combobox.ItemsSource = ThirdCar.Colors;
                    if (!CardLoading)
                    {
                        ThirdCarColor_Combobox.SelectedIndex = 0;
                    }
                    CarsInGarage[2] = ThirdCar.Name;
                    SelectedCarRefreshBlocked = true;
                    SelectedCar_ComboBox.SelectedIndex = LoadedSelectedCar;
                    SelectedCarRefreshBlocked = false;
                    break;

                default:
                    Console.WriteLine("Illegal car selection. Eh?");
                    break;
            }
        }

        private void Color_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!RefreshBlocked)
            {
                string whichColorSlot = ((ComboBox)sender).Tag.ToString();
                int ColorIndex = int.Parse((sender as ComboBox).SelectedValue.ToString());
                
                switch (whichColorSlot)
                {
                    case "first":
                        FirstCar.SelectedColor = (short)ColorIndex;
                        FirstCarColorBox.Fill = new SolidColorBrush(FirstCar.Colors[FirstCar.SelectedColor].ToColor());
                        break;
                    case "second":
                        SecondCar.SelectedColor = (short)ColorIndex;
                        SecondCarColorBox.Fill = new SolidColorBrush(SecondCar.Colors[SecondCar.SelectedColor].ToColor());
                        break;
                    case "third":
                        ThirdCar.SelectedColor = (short)ColorIndex;
                        ThirdCarColorBox.Fill = new SolidColorBrush(ThirdCar.Colors[ThirdCar.SelectedColor].ToColor());
                        break;
                    default:
                        Console.WriteLine("Illegal car selection. Eh?");
                        break;
                }
            }
        }

        private void HideAllSlots()
        {
            Car1_Label.Visibility = Visibility.Hidden;
            FirstCarName_Label.Visibility = Visibility.Hidden;
            FirstCarModel_Combobox.Visibility = Visibility.Hidden;
            FirstCarColor_Label.Visibility = Visibility.Hidden;
            FirstCarColor_Combobox.Visibility = Visibility.Hidden;
            FirstCarIcon.Visibility = Visibility.Hidden;
            FirstCarColorBox.Visibility = Visibility.Hidden;
            FirstCarFullTuneMT.Visibility = Visibility.Hidden;
            FirstCarFullTuneAT.Visibility = Visibility.Hidden;
            FirstCarLastStepTune.Visibility = Visibility.Hidden;
            FirstCarKeepTune.Visibility = Visibility.Hidden;

            Car2_Label.Visibility = Visibility.Hidden;
            SecondCarName_Label.Visibility = Visibility.Hidden;
            SecondCarModel_Combobox.Visibility = Visibility.Hidden;
            SecondCarColor_Label.Visibility = Visibility.Hidden;
            SecondCarColor_Combobox.Visibility = Visibility.Hidden;
            SecondCarIcon.Visibility = Visibility.Hidden;
            SecondCarColorBox.Visibility = Visibility.Hidden;
            SecondCarFullTuneMT.Visibility = Visibility.Hidden;
            SecondCarFullTuneAT.Visibility = Visibility.Hidden;
            SecondCarLastStepTune.Visibility = Visibility.Hidden;
            SecondCarKeepTune.Visibility = Visibility.Hidden;

            Car3_Label.Visibility = Visibility.Hidden;
            ThirdCarName_Label.Visibility = Visibility.Hidden;
            ThirdCarModel_Combobox.Visibility = Visibility.Hidden;
            ThirdCarColor_Label.Visibility = Visibility.Hidden;
            ThirdCarColor_Combobox.Visibility = Visibility.Hidden;
            ThirdCarIcon.Visibility = Visibility.Hidden;
            ThirdCarColorBox.Visibility = Visibility.Hidden;
            ThirdCarFullTuneMT.Visibility = Visibility.Hidden;
            ThirdCarFullTuneAT.Visibility = Visibility.Hidden;
            ThirdCarLastStepTune.Visibility = Visibility.Hidden;
            ThirdCarKeepTune.Visibility = Visibility.Hidden;
        }

        private void UnhideSlot(int slot)
        {
            switch(slot)
            {
                case 1:
                    Car1_Label.Visibility = Visibility.Visible;
                    FirstCarName_Label.Visibility = Visibility.Visible;
                    FirstCarModel_Combobox.Visibility = Visibility.Visible;
                    FirstCarColor_Label.Visibility = Visibility.Visible;
                    FirstCarColor_Combobox.Visibility = Visibility.Visible;
                    FirstCarIcon.Visibility = Visibility.Visible;
                    FirstCarColorBox.Visibility = Visibility.Visible;
                    FirstCarFullTuneMT.Visibility = Visibility.Visible;
                    FirstCarFullTuneAT.Visibility = Visibility.Visible;
                    FirstCarLastStepTune.Visibility = Visibility.Visible;
                    FirstCarKeepTune.Visibility = Visibility.Visible;
                    break;

                case 2:
                    Car2_Label.Visibility = Visibility.Visible;
                    SecondCarName_Label.Visibility = Visibility.Visible;
                    SecondCarModel_Combobox.Visibility = Visibility.Visible;
                    SecondCarColor_Label.Visibility = Visibility.Visible;
                    SecondCarColor_Combobox.Visibility = Visibility.Visible;
                    SecondCarIcon.Visibility = Visibility.Visible;
                    SecondCarColorBox.Visibility = Visibility.Visible;
                    SecondCarFullTuneMT.Visibility = Visibility.Visible;
                    SecondCarFullTuneAT.Visibility = Visibility.Visible;
                    SecondCarLastStepTune.Visibility = Visibility.Visible;
                    SecondCarKeepTune.Visibility = Visibility.Visible;
                    break;

                case 3:
                    Car3_Label.Visibility = Visibility.Visible;
                    ThirdCarName_Label.Visibility = Visibility.Visible;
                    ThirdCarModel_Combobox.Visibility = Visibility.Visible;
                    ThirdCarColor_Label.Visibility = Visibility.Visible;
                    ThirdCarColor_Combobox.Visibility = Visibility.Visible;
                    ThirdCarIcon.Visibility = Visibility.Visible;
                    ThirdCarColorBox.Visibility = Visibility.Visible;
                    ThirdCarFullTuneMT.Visibility = Visibility.Visible;
                    ThirdCarFullTuneAT.Visibility = Visibility.Visible;
                    ThirdCarLastStepTune.Visibility = Visibility.Visible;
                    ThirdCarKeepTune.Visibility = Visibility.Visible;
                    break;
            }

        }

        private void LoadCarFromID(ref Car CarVariable)
        {
            int CarID = CarVariable.ID;
            CarVariable.CarObject = new ObservableCollection<CarDB>(AllCars.Where(x => x.CarID == CarID)).First();
            CarVariable.Model = CarVariable.CarObject.CarModel;
            CarVariable.Name = CarVariable.CarObject.CarName;
            CarVariable.Colors = CarVariable.CarObject.CarColors;
        }

        private void TuneChanged(object sender, RoutedEventArgs e)
        {
            //string whichCarSlot = ((ComboBox)sender).Tag.ToString();
            string SelectedTune = ((RadioButton)sender).Tag.ToString();
            string SelectedCar = ((RadioButton)sender).GroupName;
            Console.WriteLine("Selected Tune: " + SelectedTune + " From: " + SelectedCar);
            
            switch(SelectedCar)
            {
                case "Car1":
                    ChangeTune(ref FirstCar, SelectedTune);
                    break;
                case "Car2":
                    ChangeTune(ref SecondCar, SelectedTune);
                    break;
                case "Car3":
                    Console.WriteLine("Changing Tune of Car 3");
                    ChangeTune(ref ThirdCar, SelectedTune);
                    break;
            }

        }

        private void ChangeTune(ref Car CarVariable, string Tune)
        { 
            switch(Tune)
            {
                case "FullTuneMT":
                    Console.WriteLine("Change FullTuneMT");
                    CarVariable.NewTuning = fullTuneMTBytes;
                    break;
                case "FullTuneAT":
                    Console.WriteLine("Change FullTuneAT");
                    CarVariable.NewTuning = fullTuneATBytes;
                    break;
                case "LastStepTune":
                    Console.WriteLine("Change LastStepTune");
                    CarVariable.NewTuning = lastTuneStepBytes;
                    break;
                case "KeepTune":
                    Console.WriteLine("Change KeepTune");
                    CarVariable.NewTuning = CarVariable.Tuning;
                    break;
            }
        }

        private void SelectedCarChanged(object sender, SelectionChangedEventArgs e)
        {
            Console.WriteLine("Selected car index: " + ((ComboBox)sender).SelectedIndex);
            if (!SelectedCarRefreshBlocked)
            {
                if (((ComboBox)sender).SelectedIndex != -1)
                {
                    Console.WriteLine("Selected car index: " + ((ComboBox)sender).SelectedIndex);
                    LoadedSelectedCar = ((ComboBox)sender).SelectedIndex;
                }
                else
                {
                    Console.WriteLine("Empty car index what the fuck");
                }
            }
        }
    }
}
