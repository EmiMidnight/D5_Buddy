using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime;
using System.Runtime.InteropServices;
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
            public string OriginalModel { get; set; }
            public string Name { get; set; }
            public short SelectedColor { get; set; }
            public ObservableCollection<CarColor> Colors { get; set; }
            public byte[] Tuning { get; set; }
            public byte[] NewTuning { get; set; }
            public Car(CarDB carobject, short id, string model, string originalModel, string name, short colorid, ObservableCollection<CarColor> colors, byte[] tuning, byte[] newTuning)
            {
                CarObject = carobject;
                ID = id;
                Model = model;
                OriginalModel = originalModel;
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

        public class AvatarPart
        {
            public int id { get; set; }
            public int categoryId { get; set; }
            public string name { get; set; }

            public AvatarPart(int ID, int Category, string Name)
            {
                id = ID;
                categoryId = Category;
                name = Name;
            }
        }

        public class Avatar
        {
            public int gender { get; set; } = 0;
            public int skin { get; set; } = 21;
            public int body { get; set; } = 143;
            public int eyes { get; set; } = 182;
            public int mouth { get; set; } = 216;
            public int acc { get; set; } = 0;
            public int shades { get; set; } = 0;
            public int hair { get; set; } = 370;
        }

        readonly string CarJSONPath = "D5_Buddy.Resources.cars.json";
        string CarJSON = "";
        public ObservableCollection<CarDB> AllCars = new ObservableCollection<CarDB>();
        readonly string[] AllRanks = { "D3", "D2", "D1", "C3", "C2", "C1", "B3", "B2", "B1", "A3", "A2", "A1", "S3", "S2", "S1", "SS" };

        // Set up empty player variables here
        string LoadedName = "";
        int LoadedDPoints = 0;
        int LoadedRank = 0;
        int GameVersion = 5;
        Avatar loadedAvatar = new();

        string MaleJSONPath = "D5_Buddy.Resources.D5.man_parts.json";
        string FemaleJSONPath = "D5_Buddy.Resources.D5.wmn_parts.json";
        string MaleJSON = "";
        string FemaleJSON = "";
        Int16[] CurrentPartsList = { 0, 0, 0, 0, 0, 0, 0 };
        public ObservableCollection<AvatarPart> AllPartsMale = new ObservableCollection<AvatarPart>();
        public ObservableCollection<AvatarPart> AllPartsFemale = new ObservableCollection<AvatarPart>();
        public ObservableCollection<AvatarPart> Parts_Female_Hair = new ObservableCollection<AvatarPart>();
        public ObservableCollection<AvatarPart> Parts_Female_Skin = new ObservableCollection<AvatarPart>();
        public ObservableCollection<AvatarPart> Parts_Female_Body = new ObservableCollection<AvatarPart>();
        public ObservableCollection<AvatarPart> Parts_Female_Shades = new ObservableCollection<AvatarPart>();
        public ObservableCollection<AvatarPart> Parts_Female_Eyes = new ObservableCollection<AvatarPart>();
        public ObservableCollection<AvatarPart> Parts_Female_Mouth = new ObservableCollection<AvatarPart>();
        public ObservableCollection<AvatarPart> Parts_Female_Acc = new ObservableCollection<AvatarPart>();
        // Category Lists Male
        public ObservableCollection<AvatarPart> Parts_Male_Hair = new ObservableCollection<AvatarPart>();
        public ObservableCollection<AvatarPart> Parts_Male_Skin = new ObservableCollection<AvatarPart>();
        public ObservableCollection<AvatarPart> Parts_Male_Body = new ObservableCollection<AvatarPart>();
        public ObservableCollection<AvatarPart> Parts_Male_Shades = new ObservableCollection<AvatarPart>();
        public ObservableCollection<AvatarPart> Parts_Male_Eyes = new ObservableCollection<AvatarPart>();
        public ObservableCollection<AvatarPart> Parts_Male_Mouth = new ObservableCollection<AvatarPart>();
        public ObservableCollection<AvatarPart> Parts_Male_Acc = new ObservableCollection<AvatarPart>();

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

        public bool AvatarLoaded = false;

        public bool KeepVisualTune = false;
        public byte[] EmptyVisualTuningBytes = {
            0x00, 0xFF, 0x00, 0xFF, 0x00, 0xFF, 0x00, 0xFF, 0x00, 0xFF, 0x00, 0xFF,
            0x00, 0xFF, 0x00, 0xFF, 0x00, 0xFF, 0x00, 0xFF, 0x00, 0xFF, 0x00, 0xFF,
            0x00, 0xFF, 0x00, 0xFF, 0x00, 0xFF, 0x00, 0xFF, 0x00, 0xFF, 0x00, 0xFF,
            0x00, 0xFF, 0x00, 0xFF, 0x00, 0xFF, 0x00, 0xFF, 0x00, 0xFF, 0x00, 0xFF,
            0x00, 0xFF, 0x00, 0xFF, 0x00, 0xFF, 0x00, 0xFF, 0x00, 0xFF, 0x00, 0xFF,
            0x00, 0xFF, 0x00, 0xFF
        };

        public MainWindow()
        {
            InitializeComponent();

            Debug.WriteLine(System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceNames());

            foreach (var item in System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceNames())
            {
                Debug.WriteLine(item.ToString());
            }
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(CarJSONPath))
            {
                TextReader tr = new StreamReader(stream);
                CarJSON = tr.ReadToEnd();
            }
            AllCars = JsonConvert.DeserializeObject<ObservableCollection<CarDB>>(CarJSON);
            Rank_ComboBox.ItemsSource = AllRanks;

            Debug.WriteLine("Loading Male Parts JSON");
            // Read Partslist (All Male) from json in resource folder
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(MaleJSONPath))
            {
                TextReader tr = new StreamReader(stream);
                MaleJSON = tr.ReadToEnd();
            }
            Debug.WriteLine("Loading Female Parts JSON");
            // Read Partslist (All Female) from json in resource folder
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(FemaleJSONPath))
            {
                TextReader tr = new StreamReader(stream);
                FemaleJSON = tr.ReadToEnd();
            }
            Debug.WriteLine("JSON READ");
            // Split Parts List into specific Lists (Female)
            AllPartsFemale = JsonConvert.DeserializeObject<ObservableCollection<AvatarPart>>(FemaleJSON);
            AllPartsMale = JsonConvert.DeserializeObject<ObservableCollection<AvatarPart>>(MaleJSON);
            Parts_Female_Skin = new ObservableCollection<AvatarPart>(AllPartsFemale.Where(x => x.categoryId == 1));
            Parts_Female_Body = new ObservableCollection<AvatarPart>(AllPartsFemale.Where(x => x.categoryId == 2));
            Parts_Female_Eyes = new ObservableCollection<AvatarPart>(AllPartsFemale.Where(x => x.categoryId == 3));
            Parts_Female_Mouth = new ObservableCollection<AvatarPart>(AllPartsFemale.Where(x => x.categoryId == 4));
            Parts_Female_Acc = new ObservableCollection<AvatarPart>(AllPartsFemale.Where(x => x.categoryId == 5));
            Parts_Female_Shades = new ObservableCollection<AvatarPart>(AllPartsFemale.Where(x => x.categoryId == 6));
            Parts_Female_Hair = new ObservableCollection<AvatarPart>(AllPartsFemale.Where(x => x.categoryId == 7));
            // Split Parts List into specific Lists (Male)
            Parts_Male_Skin = new ObservableCollection<AvatarPart>(AllPartsMale.Where(x => x.categoryId == 1));
            Parts_Male_Body = new ObservableCollection<AvatarPart>(AllPartsMale.Where(x => x.categoryId == 2));
            Parts_Male_Eyes = new ObservableCollection<AvatarPart>(AllPartsMale.Where(x => x.categoryId == 3));
            Parts_Male_Mouth = new ObservableCollection<AvatarPart>(AllPartsMale.Where(x => x.categoryId == 4));
            Parts_Male_Acc = new ObservableCollection<AvatarPart>(AllPartsMale.Where(x => x.categoryId == 5));
            Parts_Male_Shades = new ObservableCollection<AvatarPart>(AllPartsMale.Where(x => x.categoryId == 6));
            Parts_Male_Hair = new ObservableCollection<AvatarPart>(AllPartsMale.Where(x => x.categoryId == 7));

            // Add default NONE entry for shades and accessoires
            AvatarPart Shades_None = new AvatarPart(0, 6, "None");
            AvatarPart Acc_None = new AvatarPart(0, 5, "None");

            Parts_Female_Shades.Insert(0, Shades_None);
            Parts_Female_Acc.Insert(0, Acc_None);
            Parts_Male_Shades.Insert(0, Shades_None);
            Parts_Male_Acc.Insert(0, Acc_None);
        }

        public void LoadPartDropdowns()
        {
            if (loadedAvatar.gender == 1)
            {
                SkinPicker.ItemsSource = Parts_Female_Skin;
                BodyPicker.ItemsSource = Parts_Female_Body;
                EyesPicker.ItemsSource = Parts_Female_Eyes;
                MouthPicker.ItemsSource = Parts_Female_Mouth;
                AccPicker.ItemsSource = Parts_Female_Acc;
                ShadesPicker.ItemsSource = Parts_Female_Shades;
                HairPicker.ItemsSource = Parts_Female_Hair;
            }
            else
            {
                SkinPicker.ItemsSource = Parts_Male_Skin;
                BodyPicker.ItemsSource = Parts_Male_Body;
                EyesPicker.ItemsSource = Parts_Male_Eyes;
                MouthPicker.ItemsSource = Parts_Male_Mouth;
                AccPicker.ItemsSource = Parts_Male_Acc;
                ShadesPicker.ItemsSource = Parts_Male_Shades;
                HairPicker.ItemsSource = Parts_Male_Hair;
            }

        }

        private void UpdateParts()
        {
            // Set Shades and Acc to zero by default
            ShadesPicker.SelectedValue = 0;
            AccPicker.SelectedValue = 0;
            ChangeImage(0, 6);
            ChangeImage(0, 5);
            // Get every part field, run through FilterPart
            foreach (int part in CurrentPartsList)
            {
                FilterPart(part);
            }
        }

        private void FilterPart(int partID)
        {
            //var part = new ObservableCollection<IDZ_Avatar_Part>();
            var partCategory = 0;
            if (partID == 0)
            {
                return;
            }
            if (loadedAvatar.gender == 1)
            {
                // Get Part from List
                var part = new ObservableCollection<AvatarPart>(AllPartsFemale.Where(x => x.id == partID)).First();
                partCategory = part.categoryId;
            }
            else
            {
                // Get Part from List
                var part = new ObservableCollection<AvatarPart>(AllPartsMale.Where(x => x.id == partID)).First();
                partCategory = part.categoryId;
            }

            switch (partCategory)
            {
                case 1:
                    SkinPicker.SelectedValue = partID;
                    break;
                case 2:
                    BodyPicker.SelectedValue = partID;
                    break;
                case 3:
                    EyesPicker.SelectedValue = partID;
                    break;
                case 4:
                    MouthPicker.SelectedValue = partID;
                    break;
                case 5:
                    AccPicker.SelectedValue = partID;
                    break;
                case 6:
                    ShadesPicker.SelectedValue = partID;
                    break;
                case 7:
                    HairPicker.SelectedValue = partID;
                    break;
                default:
                    Console.WriteLine("No category? This is an error, we are doomed.");
                    break;
            }
            ChangeImage(partID, partCategory);
        }

        private void LoadCard_Button_Click(object sender, RoutedEventArgs e)
        {
            UnloadCard();


            CardLoading = true;
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "D5 Card Files (*.crd)|*.crd|All files (*.*)|*.*";
            if (openFileDialog.ShowDialog() == true)
                using (FileStream fs = File.OpenRead(openFileDialog.FileName))
                using (BinaryReader binaryReader = new BinaryReader(fs))
                {
                    CardPath = openFileDialog.FileName;

                    fs.Seek(0x14, SeekOrigin.Begin);
                    int gameVersion = BitConverter.ToInt16(binaryReader.ReadBytes(2), 0);
                    if (gameVersion == 21008)
                    {

                        // LOAD FROM CARD
                        fs.Seek(0x1E, SeekOrigin.Begin);
                        loadedAvatar.gender = binaryReader.ReadByte();
                        LoadPartDropdowns();

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
                        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                        Encoding shiftJIS = Encoding.GetEncoding("shift-jis");
                        string nameString = shiftJIS.GetString(nameBufferFiltered);
                        LoadedName = nameString;

                        ////////////////////////
                        //// AVATAR DATA
                        ////////////////////////

                        fs.Seek(0x88, SeekOrigin.Begin);
                        byte[] avatarBytes = binaryReader.ReadBytes(11);
                        Int16[] avatarParts = GetAvatarPartListFromCard(avatarBytes);
                        CurrentPartsList = avatarParts;
                        UpdateParts();
                        AvatarLoaded = true;


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
                        FirstCar.OriginalModel = FirstCar.Model;

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
                            SecondCar.OriginalModel = SecondCar.Model;

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
                            ThirdCar.OriginalModel = ThirdCar.Model;

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
                    else
                    {
                        UnloadCard();
                        ShowPopupNotification("Not a valid D5 Card. Loading cancelled.");
                        LoadUIValues();
                    }
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
                        KeepVisualTune = (bool)KeepVisuals_CheckBox.IsChecked;
                        // SAVE NEW NAME
                        string newName = ToFullWidth(Name_TextBox.Text);
                        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                        Encoding shiftJIS = Encoding.GetEncoding("shift-jis");
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
                        //// AVATAR DATA
                        //////////////////////// 

                        // Clear first in case the new list is shorter
                        byte[] emptyAvatar = new byte[11] { 0,0,0,0,0,0,0,0,0,0,0 };
                        memoryStream.Seek(0x88, SeekOrigin.Begin);
                        binaryWriter.Write(emptyAvatar);

                        memoryStream.Seek(0x88, SeekOrigin.Begin);
                        byte[] newAvatar = ConvertPartListToBytes(CurrentPartsList);
                        binaryWriter.Write(newAvatar);

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

                        // Clean Visual Tuning Parts
                        if (!KeepVisualTune && FirstCar.OriginalModel != FirstCar.Model)
                        {
                            memoryStream.Seek(0xE4, SeekOrigin.Begin);
                            binaryWriter.Write(EmptyVisualTuningBytes);
                        }

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

                            // Clean Visual Tuning Parts
                            if (!KeepVisualTune && SecondCar.OriginalModel != SecondCar.Model)
                            {
                                memoryStream.Seek(0x144, SeekOrigin.Begin);
                                binaryWriter.Write(EmptyVisualTuningBytes);
                            }

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

                            // Clean Visual Tuning Parts
                            if (!KeepVisualTune && ThirdCar.OriginalModel != ThirdCar.Model)
                            {
                                memoryStream.Seek(0x1A4, SeekOrigin.Begin);
                                binaryWriter.Write(EmptyVisualTuningBytes);
                            }
                        }

                    }
                }
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
                    if (!CardLoading)
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
            switch (slot)
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

            switch (SelectedCar)
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
            switch (Tune)
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

        private void UnloadCard()
        {
            FirstCar = new Car();
            SecondCar = new Car();
            ThirdCar = new Car();
            LoadedSelectedCar = 0;

            LoadedName = "";
            LoadedDPoints = 0;
            LoadedRank = 0;
            loadedAvatar = new();

            CarsInGarage.Clear();
            CardInMemory = new byte[2];
        }

        private void GetSelectedPartsFromUI()
        {
            if(SkinPicker.SelectedValue.ToString() != null)
            {
                CurrentPartsList[0] = (short)int.Parse(SkinPicker.SelectedValue.ToString());
            }

            CurrentPartsList[1] = (short)int.Parse(BodyPicker.SelectedValue.ToString());
            CurrentPartsList[2] = (short)int.Parse(EyesPicker.SelectedValue.ToString());
            CurrentPartsList[3] = (short)int.Parse(MouthPicker.SelectedValue.ToString());
            CurrentPartsList[4] = (short)int.Parse(AccPicker.SelectedValue.ToString());
            CurrentPartsList[5] = (short)int.Parse(ShadesPicker.SelectedValue.ToString());
            CurrentPartsList[6] = (short)int.Parse(HairPicker.SelectedValue.ToString());
        }

        public Int16[] GetAvatarPartListFromCard(byte[] avatarListBytes)
        {
            // jesus christ
            Int16[] partList = new Int16[7];
            partList[0] = (short)(((avatarListBytes[1] & 0xF) << 8) | avatarListBytes[0]);
            partList[1] = (short)((avatarListBytes[1] >> 4) | (16 * avatarListBytes[2]));
            partList[2] = (short)(((avatarListBytes[4] & 0xF) << 8) | avatarListBytes[3]);
            partList[3] = (short)((avatarListBytes[4] >> 4) | (16 * avatarListBytes[5]));
            partList[4] = (short)(((avatarListBytes[7] & 0xF) << 8) | avatarListBytes[6]);
            partList[5] = (short)((avatarListBytes[7] >> 4) | (16 * avatarListBytes[8]));
            partList[6] = (short)(((avatarListBytes[10] & 0xF) << 8) | avatarListBytes[9]);

            Debug.WriteLine($"Avatar parts: {partList[0].ToString()},{partList[1].ToString()},{partList[2].ToString()},{partList[3].ToString()},{partList[4].ToString()},{partList[5].ToString()},{partList[6].ToString()}");
            return partList;
        }

        public byte[] ConvertPartListToBytes(Int16[] partList)
        {
            // tears rolling down my face
            // for i have lost all hope
            byte[] byteList = new byte[11];
            byte[] part1 = BitConverter.GetBytes(partList[0]);
            byteList[0] = part1[0];
            byteList[1] = part1[1];
            byteList[1] = (byte)((part1[1] & 0xF) | (byteList[1] & 0xF0));
            Int16 part2Helper = ((short)((byteList[1] & 0xF) | (16 * partList[1])));
            byteList[1] = BitConverter.GetBytes(part2Helper)[0];
            byteList[2] = BitConverter.GetBytes(part2Helper)[1];

            byte[] part3 = BitConverter.GetBytes(partList[2]);
            byteList[3] = part3[0];
            byteList[4] = part3[1];
            byteList[4] = (byte)((part3[1] & 0xF) | (byteList[4] & 0xF0));
            Int16 part4Helper = ((short)((byteList[4] & 0xF) | (16 * partList[3])));
            byteList[4] = BitConverter.GetBytes(part4Helper)[0];
            byteList[5] = BitConverter.GetBytes(part4Helper)[1];

            byte[] part5 = BitConverter.GetBytes(partList[4]);
            byteList[6] = part5[0];
            byteList[7] = part5[1];
            byteList[7] = (byte)((part5[1] & 0xF) | (byteList[7] & 0xF0));
            Int16 part6Helper = ((short)((byteList[7] & 0xF) | (16 * partList[5])));
            byteList[7] = BitConverter.GetBytes(part6Helper)[0];
            byteList[8] = BitConverter.GetBytes(part6Helper)[1];

            byte[] part7 = BitConverter.GetBytes(partList[6]);
            byteList[9] = part7[0];
            byteList[10] = part7[1];
            byteList[10] = (byte)((part7[1] & 0xF) | (byteList[10] & 0xF0));
            Debug.WriteLine($"Avatar parts: {Convert.ToHexString(byteList)}");
            return byteList;
        }

        private void ChangeImage(int partID, int partCategory)
        {
            var gender_short = "MAN";
            if (loadedAvatar.gender == 1)
            {
                gender_short = "WMN";
            }

            if (partCategory == 7)
            {
                Uri hair_bg_uri = new Uri("Resources/D" + GameVersion.ToString() + "/" + gender_short + "/" + partID.ToString() + "_bg.png", UriKind.Relative);
                Console.WriteLine(hair_bg_uri);
                ImageSource hair_bg_Source = new BitmapImage(hair_bg_uri);
                HairBgImg.Source = hair_bg_Source;
                Uri hair_uri = new Uri("Resources/D" + GameVersion.ToString() + "/" + gender_short + "/" + partID.ToString() + "_fg.png", UriKind.Relative);
                ImageSource hair_Source = new BitmapImage(hair_uri);
                HairFgImg.Source = hair_Source;
            }
            else
            {
                Uri imageUri = new Uri("Resources/D" + GameVersion.ToString() + "/" + gender_short + "/" + partID.ToString() + ".png", UriKind.Relative);
                ImageSource imageSource = new BitmapImage(imageUri);
                switch (partCategory)
                {
                    case 1:

                        SkinImg.Source = imageSource;
                        break;
                    case 2:
                        BodyImg.Source = imageSource;
                        break;
                    case 3:
                        EyesImg.Source = imageSource;
                        break;
                    case 4:
                        MouthImg.Source = imageSource;
                        break;
                    case 5:
                        AccImg.Source = imageSource;
                        break;
                    case 6:
                        ShadesImg.Source = imageSource;
                        break;
                    default:
                        Console.WriteLine("No category? This is an error, we are doomed.");
                        break;
                }

            }
        }

        private void AvatarPartSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int partCategory = 0;
            int partID = 0;
            if (((ComboBox)sender).SelectedValue != null)
            {
                partID = int.Parse(((ComboBox)sender).SelectedValue.ToString());
            }

            switch (((ComboBox)sender).Name)
            {
                case "SkinPicker":
                    partCategory = 1;
                    break;
                case "BodyPicker":
                    partCategory = 2;
                    break;
                case "EyesPicker":
                    partCategory = 3;
                    break;
                case "MouthPicker":
                    partCategory = 4;
                    break;
                case "AccPicker":
                    partCategory = 5;
                    break;
                case "ShadesPicker":
                    partCategory = 6;
                    break;
                case "HairPicker":
                    partCategory = 7;
                    break;
            }
            ChangeImage(partID, partCategory);
            // Update avatar in memory
            if(AvatarLoaded)
            {
            GetSelectedPartsFromUI();
            }

        }

        private void VisualTuneWarning(object sender, RoutedEventArgs e)
        {
            if (((CheckBox)sender).IsChecked == true)
            {
                ShowPopupNotification("Warning: \n" +
                    "This will keep visual tuning parts \n" +
                    "when replacing a car with the editor. \n" +
                    "This could (maybe) lead to crashes. \n" +
                    "Don't complain when this breaks! \nYou were warned! :)");
            }
        }

        private const uint LCMAP_FULLWIDTH = 0x00800000;
        private const uint LOCALE_SYSTEM_DEFAULT = 0x0800;
        public static string ToFullWidth(string halfWidth)
        {
            StringBuilder sb = new StringBuilder(256);
            LCMapString(LOCALE_SYSTEM_DEFAULT, LCMAP_FULLWIDTH, halfWidth, -1, sb, sb.Capacity);
            return sb.ToString();
        }
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern int LCMapString(uint Locale, uint dwMapFlags, string lpSrcStr, int cchSrc, StringBuilder lpDestStr, int cchDest);
    }
}
