using MahApps.Metro.Controls;
using Newtonsoft.Json;
using ScoreSaberLib;
using System;
using System.Collections.Generic;
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
            UpdateMapKeyListBox();
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
            var playerInfo = await scoresaberClient.Api.Players.ByIDFull(Convert.ToUInt64(playerScoresaberID));
            if(playerInfo == null)
            {
                ErrorMessage.Text = "Could not find a player with this ID";
                return;
            }
            //Add user to the playerview 
            PlayersListView.Items.Add($"{playerInfo.PlayerInfo.PlayerName} ({playerInfo.PlayerInfo.PlayerId})");
            //Add data to a local storage

            SaveDataToJson($"{playerInfo.PlayerInfo.PlayerName}|{playerInfo.PlayerInfo.PlayerId}|{TeamsListBox.SelectedItem}", "..\\..\\..\\Save\\Players.json");
            UpdateTeamAndPlayersListBoxes();
        }

        private void UpdateTeamAndPlayersListBoxes()
        {
            PlayersListView.Items.Clear();
            var lines = GetDataFromJson("..\\..\\..\\Save\\Players.json");
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

                       
            foreach(var line in lines)
            {
                //if (!line.Contains("|"))
                //{
                //    TeamsListBox.Items.Add(line);
                //    continue;
                //}
                if(!TeamsListBox.Items.Contains(line.Split("|")[2])) TeamsListBox.Items.Add(line.Split("|")[2]);

            }
          
                    
        }

        #endregion PlayersList

        #region configureMapData
        private void AddMapKeyButton_Click(object sender, RoutedEventArgs e)
        {
            var mapKey = MapKeyTextBox.Text;

            //Save the map into 
            SaveDataToJson(mapKey, "..\\..\\..\\Save\\MapKeys.json");
            UpdateMapKeyListBox();
        }

        private void UpdateMapKeyListBox()
        {
            MapKeyListBox.Items.Clear();
            var json = GetDataFromJson("..\\..\\..\\Save\\MapKeys.json");
            foreach (var item in json)
            {
                MapKeyListBox.Items.Add(item);
            }
        }

        private void SaveMapSkillsetButton_Click(object sender, RoutedEventArgs e)
        {
            var mapData = GetDataFromJson("..\\..\\..\\Save\\MapKeys.json");
            var line = mapData.Where(x => x == MapKeyListBox.SelectedItem.ToString()).First();
            RemoveDataFromJson(line, "..\\..\\..\\Save\\MapKeys.json");
            line += $"|{TechNumberInput.Value}|{AccNumberInput.Value}|{MidSpeedNumberInput.Value}|{HighSpeedNumberInput.Value}";
            SaveDataToJson(line, "..\\..\\..\\Save\\MapKeys.json");
            UpdateMapKeyListBox();
        }

        private void AddPredefinedMapsButton_Click(object sender, RoutedEventArgs e)
        {
            var lines = new List<string>();
            lines.Add("280301|2|3|2|10");
            lines.Add("287979|5|3|2|8");
            lines.Add("321189|2|4|4|7");
            lines.Add("349164|9|3|2|2");
            lines.Add("181568|5|4|5|3");
            lines.Add("287407|8|3|4|4");
            lines.Add("334693|2|5|7|5");
            lines.Add("348076|1|3|4|10");
            lines.Add("328907|2|3|4|10");

            foreach (var line in lines)
            {
                SaveDataToJson(line, "..\\..\\..\\Save\\MapKeys.json");
            }
            UpdateMapKeyListBox();
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
            var lines = GetDataFromJson("..\\..\\..\\Save\\Players.json");
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
            UpdateDropDownPlayersButton();
        }

        private async void PlayerDropDown_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            playerSkillsetGrid.Visibility = Visibility.Hidden;
            playerSkillsetLoading.IsActive = true;
            var skillSetCalculation = new SkillSetCalculation(PlayerDropDown.SelectedItem.ToString());

            var maps = GetDataFromJson("..\\..\\..\\Save\\MapKeys.json");
            skillSetCalculation.AddMaps(maps);
            var results = await skillSetCalculation.CalculateAndGetResults();

            TechTextBlock.Text = $"{(results.TotalTechPoints / results.TotalOriginalTechPoints)}";
            AccTextBlock.Text = $"{(results.TotalAccPoints / results.TotalOriginalAccPoints)}";
            MidSpeedTextBlock.Text = $"{(results.TotalMidSpeedPoints / results.TotalOriginalMidSpeedPoints)}";
            HighSpeedTextBlock.Text = $"{(results.TotalHighSpeedPoints / results.TotalOriginalHighSpeedPoints)}";

            TotalMapsUsed.Text = results.TotalMapCountUsed.ToString();

            TechOriginalPoints.Text = $"total maps Tech points {results.TotalOriginalTechPoints}";
            AccOriginalPoints.Text = $"total maps Acc points {results.TotalOriginalAccPoints}";
            MidSpeedOriginalPoints.Text = $"total maps Mid-Speed points {results.TotalOriginalMidSpeedPoints}";
            HighSpeedOriginalPoints.Text = $"total maps High-Speed points {results.TotalOriginalHighSpeedPoints}";

            playerSkillsetLoading.IsActive = false;
            playerSkillsetGrid.Visibility = Visibility.Visible;
        }
    }
}
