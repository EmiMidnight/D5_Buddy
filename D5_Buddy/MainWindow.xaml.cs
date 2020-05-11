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
        public class CarDB
        {
            [JsonProperty("car_id")]
            public int CarID { get; set; }
            [JsonProperty("car_name")]
            public string CarName { get; set; }
            [JsonProperty("car_model")]
            public string CarModel { get; set; }

            [JsonProperty("colors")]
            public CarColor[] CarColors { get; set; }

            public CarDB(int car_id, string car_name, string car_model, CarColor[] colors)
            {
                CarID = car_id;
                CarName = car_name;
                CarModel = car_model;
                CarColors = colors;
            }
        }

        public class CarColor
        {  
            [JsonProperty("color_id")]
            public int ColorID { get; set; }
            [JsonProperty("color_name")]
            public string ColorName { get; set; }

            public CarColor(int color_id, string color_name)
            {
                ColorID = color_id;
                ColorName = color_name;
            }
        }

        Encoding shiftJIS = Encoding.GetEncoding("shift-jis");
        string CarJSONPath = "D5_Buddy.Resources.cars.json";
        string CarJSON = "";
        public ObservableCollection<CarDB> AllCars = new ObservableCollection<CarDB>();
        string[] AllRanks = { "D3", "D2", "D1", "C3", "C2", "C1", "B3", "B2", "B1", "A3", "A2", "A1", "S3", "S2", "S1", "SS" };

        // Set up empty player variables here
        string LoadedName = "";
        int LoadedDPoints = 0;
        string LoadedGender = "Male";
        int LoadedRank = 0;
        string LoadedFirstCarModel = "";
        string LoadedFirstCarName = "";
        int LoadedFirstCarID = 0;
        int LoadedFirstCarColorID = 0;

        // Load original card in mem, for saving later
        public byte[] CardInMemory;

        public MainWindow()
        {
            InitializeComponent();
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(CarJSONPath))
            {
                TextReader tr = new StreamReader(stream);
                CarJSON = tr.ReadToEnd();
            }
            AllCars = JsonConvert.DeserializeObject<ObservableCollection<CarDB>>(CarJSON);
        }

        private void LoadCard_Button_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
                using (FileStream fs = File.OpenRead(openFileDialog.FileName))
                using (BinaryReader binaryReader = new BinaryReader(fs))
                {
                    // CAR AMOUNT, SELECTED CAR
                    fs.Seek(0x69, SeekOrigin.Begin);
                    int selectedCar = binaryReader.ReadByte();
                    int amountOfCars = binaryReader.ReadByte();

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

                    // Tune Example
                    byte[] fullTuneExample = new byte[] { 0xFF, 0xFF };

                    // FIRST CAR
                    fs.Seek(0xC4, SeekOrigin.Begin);
                    byte[] firstCarIDBytes = binaryReader.ReadBytes(2);
                    short firstCarID = BitConverter.ToInt16(firstCarIDBytes, 0);
                    byte[] firstCarColorBytes = binaryReader.ReadBytes(2);
                    short firstCarColorID = BitConverter.ToInt16(firstCarColorBytes, 0);

                    CarDB firstCar = new ObservableCollection<CarDB>(AllCars.Where(x => x.CarID == firstCarID)).First();
                    ObservableCollection<CarColor> firstCarColors = new ObservableCollection<CarColor>(firstCar.CarColors);
                    string firstCarName = firstCar.CarName;
                    string firstCarModel = firstCar.CarModel;
                    string firstCarColorName = new ObservableCollection<CarColor>(firstCarColors.Where(x => x.ColorID == firstCarColorID)).First().ColorName;

                    fs.Seek(0xC8, SeekOrigin.Begin);
                    byte[] firstCarTuning = binaryReader.ReadBytes(2);
                    bool firstCarFullTune = false;
                    if (firstCarTuning.SequenceEqual(fullTuneExample))
                    {
                        firstCarFullTune = true;
                    }

                    // SECOND CAR
                    bool secondCarFullTune = false;
                    byte[] secondCarTuning = new byte[2];

                    byte[] secondCarIDBytes = new byte[2];
                    short secondCarID = 0;
                    byte[] secondCarColorBytes = new byte[2];
                    short secondCarColorID = 0;
                    CarDB secondCar = new ObservableCollection<CarDB>(AllCars.Where(x => x.CarID == secondCarID)).First();
                    ObservableCollection<CarColor> secondCarColors = new ObservableCollection<CarColor>();
                    string secondCarName = "";
                    string secondCarColorName = "";

                    if (amountOfCars > 1)
                    {
                        fs.Seek(0x128, SeekOrigin.Begin);
                        secondCarTuning = binaryReader.ReadBytes(2);

                        if (secondCarTuning.SequenceEqual(fullTuneExample))
                        {
                            secondCarFullTune = true;
                        }

                        fs.Seek(0x124, SeekOrigin.Begin);
                        secondCarIDBytes = binaryReader.ReadBytes(2);
                        secondCarID = BitConverter.ToInt16(secondCarIDBytes, 0);
                        secondCarColorBytes = binaryReader.ReadBytes(2);
                        secondCarColorID = BitConverter.ToInt16(secondCarColorBytes, 0);

                        secondCar = new ObservableCollection<CarDB>(AllCars.Where(x => x.CarID == secondCarID)).First();
                        secondCarColors = new ObservableCollection<CarColor>(secondCar.CarColors);
                        secondCarName = secondCar.CarName;
                        secondCarColorName = new ObservableCollection<CarColor>(secondCarColors.Where(x => x.ColorID == secondCarColorID)).First().ColorName;
                    }

                    LoadedFirstCarName = firstCarName;
                    LoadedFirstCarModel = firstCarModel;

                    // DEBUG OUTPUT
                    Console.WriteLine("Player Name: " + nameString);
                    Console.WriteLine("D-Points: " + dPoints.ToString());
                    Console.WriteLine("Amount of cars: " + amountOfCars.ToString());
                    int selectedCarConverted = selectedCar += 1;
                    Console.WriteLine("Selected car: " + selectedCarConverted.ToString());
                    Console.WriteLine("First car ID: " + firstCarID.ToString() + " | Color ID: " + firstCarColorID.ToString());
                    Console.WriteLine("First car name: " + firstCarName.ToString() + " | Color Name: " + firstCarColorName.ToString());
                    Console.WriteLine("Second car ID: " + secondCarID.ToString() + " | Color ID: " + secondCarColorID.ToString());
                    Console.WriteLine("Second car name: " + secondCarName.ToString() + " | Color Name: " + secondCarColorName.ToString());
                    Console.WriteLine("First car has Full Tune: " + firstCarFullTune.ToString());
                    Console.WriteLine("Second car has Full Tune: " + secondCarFullTune.ToString());

                    // LOAD EVERYTHING INTO THE UI
                    LoadUIValues();

                    fs.Seek(0x00, SeekOrigin.Begin);
                    CardInMemory = binaryReader.ReadBytes((int)binaryReader.BaseStream.Length);
                    SaveCard_Button.IsEnabled = true;
                }
        }

        private void LoadUIValues()
        {
            Name_TextBox.Text = LoadedName;
            DPoint_TextBox.Text = LoadedDPoints.ToString();
            Rank_Debug_Label.Content = AllRanks[LoadedRank - 1];
            FirstCarName_Debug_Label.Content = LoadedFirstCarName;
            Uri car1_icon_uri = new Uri("Resources/" + LoadedFirstCarModel + ".png", UriKind.Relative);
            ImageSource car1_icon_src = new BitmapImage(car1_icon_uri);
            FirstCarIcon.Source = car1_icon_src;
        }

        private void SaveCard_Button_Click(object sender, RoutedEventArgs e)
        {
            using (MemoryStream memoryStream = new MemoryStream(CardInMemory))
            {
                using (BinaryWriter binaryWriter = new BinaryWriter(memoryStream))
                {
                    string newName = Name_TextBox.Text;
                    byte[] newNameBytes = shiftJIS.GetBytes(newName);
                    memoryStream.Seek(0xB4, SeekOrigin.Begin);
                    // ADD IN CHECK FOR NAME LENGTH
                    binaryWriter.Write(newNameBytes);

                    Console.WriteLine("saving to binary");
                }
            }

            File.WriteAllBytes("saved.card", CardInMemory);
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }
    }
}
