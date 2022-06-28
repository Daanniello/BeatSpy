using BeatSpy.API.BeatSaver;
using MahApps.Metro.Controls;
using Newtonsoft.Json;
using ScoreSaberLib;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
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
using System.Windows.Threading;
using static BeatSpy.SkillSetCalculation;

namespace BeatSpy
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            UpdateTeamAndPlayersListBoxes();
            UpdateMapDataGrid(DeSerializeAndGetMapData());
            UpdateDropDownTeamsButton();

            playerSkillsetGrid.Visibility = Visibility.Hidden;
        }
        #region TeamsList
        private void AddNewTeamButton_Click(object sender, RoutedEventArgs e)
        {
            var newTeam = AddNewTeamTextBox.Text;
            TeamsListBox.Items.Add(newTeam);
            UpdateDropDownTeamsButton();
        }

        private void DeleteNewTeamButton_Click(object sender, RoutedEventArgs e)
        {
            TeamsListBox.Items.Remove(TeamsListBox.SelectedItem);
            TeamsListBox.Items.Refresh();
        }

        private void TeamsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            PlayersListView.IsEnabled = true;
            AddPlayerButton.IsEnabled = true;
            DeletePlayerButton.IsEnabled = true;
            AddPlayerTextBox.IsEnabled = true;

            UpdateTeamAndPlayersListBoxes();
        }
        #endregion TeamsList

        #region PlayersList
        private async void AddPlayerButton_Click(object sender, RoutedEventArgs e)
        {
            var playerScoresaberID = AddPlayerTextBox.Text;
            if (!playerScoresaberID.All(x => char.IsDigit(x)))
            {
                ErrorMessage.Text = "Numbers only please.";
                return;
            }

            //Get player data from scoresaber
            var scoresaberClient = new ScoreSaberClient();
            var playerInfo = await scoresaberClient.Api.Players.GetPlayer(Convert.ToInt64(playerScoresaberID));
            if (playerInfo == null)
            {
                ErrorMessage.Text = "Could not find a player with this ID";
                return;
            }
            //Add user to the playerview 
            PlayersListView.Items.Add($"{playerInfo.Name} ({playerInfo.Id})");
            //Add data to a local storage

            SaveDataToJson($"{playerInfo.Name}|{playerInfo.Id}|{TeamsListBox.SelectedItem}", "Save\\Players.json");
            UpdateTeamAndPlayersListBoxes();
        }

        private void UpdateTeamAndPlayersListBoxes()
        {
            PlayersListView.Items.Clear();
            var lines = GetDataFromJson("Save\\Players.json");
            if (TeamsListBox.SelectedItem != null)
            {
                var playersTeamSelection = TeamsListBox.SelectedItem.ToString();
                var selectedTeamData = lines.Where(x => x.Contains(playersTeamSelection));
                foreach (var line in selectedTeamData)
                {
                    PlayersListView.Items.Add(line);
                }
            }

            if (TeamsListBox.SelectedIndex != -1) return;

            TeamsListBox.Items.Clear();


            foreach (var line in lines)
            {
                //if (!line.Contains("|"))
                //{
                //    TeamsListBox.Items.Add(line);
                //    continue;
                //}
                if (!TeamsListBox.Items.Contains(line.Split("|")[2])) TeamsListBox.Items.Add(line.Split("|")[2]);

            }
        }

        private void DeletePlayerButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedPlayer = PlayersListView.SelectedItem;
            RemoveDataFromJson(selectedPlayer.ToString(), "Save\\Players.json");
            PlayersListView.Items.Remove(PlayersListView.SelectedItem);
            PlayersListView.Items.Refresh();
        }

        #endregion PlayersList

        #region configureMapData
        public class MapData
        {
            public string Name { get; set; }
            public string Difficulty { get; set; }
            public double TechPoints { get; set; }
            public double AccPoints { get; set; }
            public double MidSpeedPoints { get; set; }
            public double HighSpeedPoints { get; set; }
            public string HashCode { get; set; }

            public string ImageUrl;
            public double Rating;
            public float Njs;
            public float Bpm;
            public float Nps;
            public float Duration;
        }

        private async void AddMapKeyButton_Click(object sender, RoutedEventArgs e)
        {
            var mapKey = MapKeyTextBox.Text.Trim();
            var mapInfo = await BeatSaverApi.GetMapByKey(mapKey);
            if (mapInfo == null)
            {
                ShowErrorMessage($"Could not get the map with key: {mapKey} from the beatsaver api");
                return;
            }

            var difficulty = (dynamic)difficultyDropDown.SelectedValue;
            if (difficulty == null)
            {
                ShowErrorMessage("You have not selected a difficulty for the map");
                return;
            }

            var mapDiffData = mapInfo.Versions.First().Diffs.First(x => x.Difficulty == difficulty.Content);

            var mapData = new MapData()
            {
                Name = mapInfo.Name,
                HashCode = mapInfo.Versions.First().Hash,
                Difficulty = difficulty.Content,
                TechPoints = 0,
                AccPoints = 0,
                MidSpeedPoints = 0,
                HighSpeedPoints = 0,
                ImageUrl = $"https://eu.cdn.beatsaver.com/{mapInfo.Versions.First().Hash}.jpg",
                Rating = mapInfo.Stats.Score,
                Bpm = mapInfo.Metadata.Bpm,
                Njs = mapDiffData.Njs,
                Nps = mapDiffData.Notes / mapInfo.Metadata.Duration,
                Duration = mapInfo.Metadata.Duration
            };

            //Save the map into 
            //SaveDataToJson(mapKey, "Save\\MapKeys.json");
            var mapDataList = SerializeAndSaveMapData(mapData);
            UpdateMapDataGrid(mapDataList);
            //UpdateMapKeyListBox();
        }

        private List<MapData> SerializeAndSaveMapData(MapData mapData)
        {
            var oldMapDataList = DeSerializeAndGetMapData();
            if (oldMapDataList == null) return null;
            oldMapDataList.Add(mapData);
            var json = JsonConvert.SerializeObject(oldMapDataList);
            File.WriteAllText("Save\\MapKeys.json", json);
            return oldMapDataList;
        }
        private List<MapData> SerializeAndSaveMapData(List<MapData> mapDataList)
        {
            var json = JsonConvert.SerializeObject(mapDataList);
            File.WriteAllText("Save\\MapKeys.json", json);
            return mapDataList;
        }

        private void RemoveMapData(MapData mapData)
        {
            var mapDataList = DeSerializeAndGetMapData();
            mapDataList.Remove(mapDataList.First(x => x.HashCode == mapData.HashCode));
            UpdateMapDataGrid(SerializeAndSaveMapData(mapDataList));
        }

        private List<MapData> DeSerializeAndGetMapData()
        {
            try
            {
                var json = File.ReadAllText("Save\\MapKeys.json");
                if (json != "")
                {
                    var mapDataList = JsonConvert.DeserializeObject<List<MapData>>(json);
                    return mapDataList;
                }
                else
                {
                    return new List<MapData>();
                }
            }
            catch
            {
                ShowErrorMessage("Could not collect the mapData json");
                return null;
            }
        }

        private void UpdateMapDataGrid(List<MapData> mapDataList)
        {
            MapDataGrid.ItemsSource = mapDataList;
        }

        private void SaveMapSkillsetButton_Click(object sender, RoutedEventArgs e)
        {
            var mapDataList = DeSerializeAndGetMapData();
            var selectedMapData = (MapData)MapDataGrid.SelectedItem;
            var mapData = mapDataList.First(x => x.HashCode == selectedMapData.HashCode);
            mapData.TechPoints = (double)TechNumberInput.Value;
            mapData.AccPoints = (double)AccNumberInput.Value;
            mapData.MidSpeedPoints = (double)MidSpeedNumberInput.Value;
            mapData.HighSpeedPoints = (double)HighSpeedNumberInput.Value;

            RemoveMapData(mapData);
            UpdateMapDataGrid(SerializeAndSaveMapData(mapData));
        }

        private void DeleteConfiguredMap_Click(object sender, RoutedEventArgs e)
        {
            MapData[] mapDataArray = new MapData[MapDataGrid.SelectedItems.Count];
            MapDataGrid.SelectedItems.CopyTo(mapDataArray, 0);
            foreach (var selectedItem in mapDataArray)
            {
                RemoveMapData((MapData)selectedItem);
            }

            //var mapData = GetDataFromJson("Save\\MapKeys.json");
            //var line = mapData.Where(x => x == MapKeyListBox.SelectedItem.ToString()).First();
            //RemoveDataFromJson(line, "Save\\MapKeys.json");
            //UpdateMapKeyListBox();
        }

        private void AddPredefinedMapsButton_Click(object sender, RoutedEventArgs e)
        {
            //var lines = new List<string>();
            //lines.Add("280301|2|3|2|10");
            //lines.Add("287979|5|3|2|8");
            //lines.Add("321189|2|4|4|7");
            //lines.Add("349164|9|3|2|2");
            //lines.Add("181568|5|4|5|3");
            //lines.Add("287407|8|3|4|4");
            //lines.Add("334693|2|5|7|5");
            //lines.Add("348076|1|3|4|10");
            //lines.Add("328907|2|3|4|10");

            //foreach (var line in lines)
            //{
            //    SaveDataToJson(line, "Save\\MapKeys.json");
            //}
            //UpdateMapKeyListBox();
        }
        #endregion configureMapData     


        private void UpdateDropDownTeamsButton()
        {
            TeamDropDown.Items.Clear();
            foreach (var item in TeamsListBox.Items)
            {
                TeamDropDown.Items.Add(item);
            }
        }

        private void UpdateDropDownPlayersButton()
        {
            PlayerDropDown.Items.Clear();
            var lines = GetDataFromJson("Save\\Players.json");
            foreach (var line in lines.Where(x => x.Split("|")[2] == TeamDropDown.SelectedItem.ToString()))
            {
                PlayerDropDown.Items.Add(line);
            }
        }

        private void SaveDataToJson(object data, string path)
        {
            var lines = GetDataFromJson(path);
            lines = lines.Append($"{data}").ToArray();

            File.WriteAllLines(path, lines);
        }

        private void RemoveDataFromJson(string data, string path)
        {
            var lines = GetDataFromJson(path);
            var linesList = lines.ToList();
            linesList.Remove(data);
            File.WriteAllLines(path, linesList.ToArray());
        }

        private string[] GetDataFromJson(string path)
        {
            string[] lines = File.ReadAllLines(path);
            return lines;
        }

        private void TeamDropDown_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TeamDropDown.SelectedItem.ToString().ToLower().Contains("nl") || TeamDropDown.SelectedItem.ToString().ToLower().Contains("netherland") || TeamDropDown.SelectedItem.ToString().ToLower().Contains("nederland"))
            {
                NoBeast.Visibility = Visibility.Hidden;
            }
            else
            {
                NoBeast.Visibility = Visibility.Visible;
            }
            UpdateDropDownPlayersButton();
        }

        private async void PlayerDropDown_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        public class PlayerInfo
        {
            public string Name { get; set; }

            public string MapCount { get; set; }
            public string AvgRank { get; set; }
            public string Tech { get; set; }
            public string Acc { get; set; }
            public string MidSpeed { get; set; }
            public string HighSpeed { get; set; }

            public List<PlayerMapScores> Maps { get; set; }

        }

        private async void CalculateTeamInfoButton_Click(object sender, RoutedEventArgs e)
        {
            var mapDataList = DeSerializeAndGetMapData();

            foreach (var mapData in mapDataList)
            {
                if (mapData.TechPoints == 0 && mapData.AccPoints == 0 && mapData.MidSpeedPoints == 0 && mapData.HighSpeedPoints == 0)
                {
                    ShowErrorMessage("Not all maps have skills added to them! Add them before doing the teams calculation.");
                    return;
                }
            }
            if (TeamDropDown.SelectedItem == null)
            {
                ShowErrorMessage("You have not selected a team in the dropdown list");
                return;
            }

            TeamSkillsetLoading.IsActive = true;
            CalculateTeamInfoButton.IsEnabled = false;
            TeamSkillsetLoadingText.Visibility = Visibility.Visible;

            var playerList = new List<PlayerInfo>();
            var lines = GetDataFromJson("Save\\Players.json");
            foreach (var line in lines.Where(x => x.Contains(TeamDropDown.SelectedItem.ToString())))
            {
                //Wait 5 sec for api call limit
                await Task.Delay(5000);
                //Add player into  the skillsetcalculation
                var skillSetCalculation = new SkillSetCalculation(line.ToString());
                //Add map into  the skillsetcalculation
                skillSetCalculation.AddMaps(mapDataList);
                var resultsTuple = await skillSetCalculation.CalculateAndGetResults(this);
                if (resultsTuple == null) continue;
                var results = resultsTuple.Item1;
                if (results.TotalMapCountUsed == 0) continue;

                playerList.Add(new PlayerInfo()
                {
                    Name = results.PlayerName,
                    Tech = $"{results.TotalTechPoints}%",
                    Acc = $"{results.TotalAccPoints}%",
                    MidSpeed = $"{results.TotalMidSpeedPoints}%",
                    HighSpeed = $"{results.TotalHighSpeedPoints}%",
                    MapCount = results.TotalMapCountUsed.ToString(),
                    AvgRank = "#" + Math.Round(resultsTuple.Item2.Average(x => Convert.ToInt32(x.Rank.Replace("#", ""))), 0).ToString(),
                    Maps = resultsTuple.Item2
                });
            }

            TeamDataGrid.IsReadOnly = true;
            TeamDataGrid.ItemsSource = playerList;

            TeamSkillsetLoading.IsActive = false;
            CalculateTeamInfoButton.IsEnabled = true;
            TeamSkillsetLoadingText.Visibility = Visibility.Hidden;
        }

        private async void CalculatePlayerInfoButton_Click(object sender, RoutedEventArgs e)
        {




            var mapDataList = DeSerializeAndGetMapData();

            foreach (var mapData in mapDataList)
            {
                if (mapData.TechPoints == 0 && mapData.AccPoints == 0 && mapData.MidSpeedPoints == 0 && mapData.HighSpeedPoints == 0)
                {
                    ShowErrorMessage("Not all maps have skills added to them! Add them before doing the teams calculation.");
                    return;
                }
            }

            if (PlayerDropDown.SelectedItem == null)
            {
                ShowErrorMessage("You have not selected a player in the drop down menu.");
                return;
            }

            playerSkillsetGrid.Visibility = Visibility.Hidden;
            playerSkillsetLoading.IsActive = true;
            var skillSetCalculation = new SkillSetCalculation(PlayerDropDown.SelectedItem.ToString());

            skillSetCalculation.AddMaps(mapDataList);
            var resultsTuple = await skillSetCalculation.CalculateAndGetResults(this);
            var results = resultsTuple.Item1;

            var techPoints = results.TotalTechPoints / results.TotalOriginalTechPoints;
            var accPoints = results.TotalAccPoints / results.TotalOriginalAccPoints;
            var midSpeedPoints = results.TotalMidSpeedPoints / results.TotalOriginalMidSpeedPoints;
            var highSpeedPoints = results.TotalHighSpeedPoints / results.TotalOriginalHighSpeedPoints;

            if (Double.IsNaN(techPoints)) techPoints = 0;
            if (Double.IsNaN(accPoints)) accPoints = 0;
            if (Double.IsNaN(midSpeedPoints)) midSpeedPoints = 0;
            if (Double.IsNaN(highSpeedPoints)) highSpeedPoints = 0;

            var skillsetRadarImage = SimpleRadar.createChart(highSpeedPoints, accPoints, midSpeedPoints, techPoints);

            using (var memory = new MemoryStream())
            {
                skillsetRadarImage.Save(memory, ImageFormat.Png);
                memory.Position = 0;

                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();

                RadarImage.Source = bitmapImage;
            }

            TechTextBlock.Text = Math.Round(techPoints, 2).ToString();
            AccTextBlock.Text = Math.Round(accPoints, 2).ToString();
            MidSpeedTextBlock.Text = Math.Round(midSpeedPoints, 2).ToString();
            HighSpeedTextBlock.Text = Math.Round(highSpeedPoints, 2).ToString();

            TotalMapsUsed.Text = results.TotalMapCountUsed.ToString();


            playerSkillsetLoading.IsActive = false;
            playerSkillsetGrid.Visibility = Visibility.Visible;

        }

        private void TeamDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TeamDataGrid.SelectedItem == null) return;
            dynamic maps = TeamDataGrid.SelectedItem;
            List<PlayerMapScores> mapScoreData = maps.Maps;
            TeamPlayerSpecificDataGrid.AutoGenerateColumns = true;
            TeamPlayerSpecificDataGrid.ItemsSource = mapScoreData;
        }

        private async void CopyMapToClipboardButton_Click(object sender, RoutedEventArgs e)
        {

        }

        public async void ShowErrorMessage(string message)
        {
            await Dispatcher.Invoke(async () =>
            {
                ErrorMessage.Text = message;
                ErrorMessage.Visibility = Visibility.Visible;
                await Task.Delay(5000);
                ErrorMessage.Visibility = Visibility.Hidden;
            });
        }

        private void MapDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (MapPreviewInfoGrid.Visibility == Visibility.Hidden) MapPreviewInfoGrid.Visibility = Visibility.Visible;

                var selectedMapData = (MapData)MapDataGrid.SelectedItem;
                if (selectedMapData == null) return;
                TechNumberInput.Value = selectedMapData.TechPoints;
                AccNumberInput.Value = selectedMapData.AccPoints;
                MidSpeedNumberInput.Value = selectedMapData.MidSpeedPoints;
                HighSpeedNumberInput.Value = selectedMapData.HighSpeedPoints;

                MapUrlTextBlock.Text = selectedMapData.Name;
                MapRatioTextBlock.Text = (Math.Round(selectedMapData.Rating, 2) * 100).ToString() + "% Upvotes";
                MapBpm.Text = $"BPM: {Math.Round(selectedMapData.Bpm, 2)}";
                MapNJS.Text = $"NJS: {Math.Round(selectedMapData.Njs, 2)}";
                MapNPS.Text = $"NPS: {Math.Round(selectedMapData.Nps, 2)}";
                MapDuration.Text = $"Duration: {Math.Round(selectedMapData.Duration, 2)}";

                mapImage.Source = new BitmapImage(new Uri(selectedMapData.ImageUrl));
            }
            catch
            {
                return;
            }
        }
    }
}
