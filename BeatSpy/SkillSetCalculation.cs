using BeatSpy.API.BeatSaver;
using ScoreSaberLib;
using ScoreSaberLib.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static BeatSpy.MainWindow;

namespace BeatSpy
{
    public class SkillSetCalculation
    {
        private string playerToCalculate;
        private List<MapData> mapDataList;

        public SkillSetCalculation(string playerScoresaberID)
        {
            playerToCalculate = playerScoresaberID;
        }

        public void AddMaps(List<MapData> mapDataList)
        {
            this.mapDataList = mapDataList;
        }

        public class PlayerMapScores
        {
            public string SongName { get; set; }
            public string Rank { get; set; }

            public string Acc { get; set; }
        }

        public async Task<Tuple<SkillSetData, List<PlayerMapScores>>> CalculateAndGetResults(MainWindow window = null)
        {
            var scoresaberClient = new ScoreSaberClient();

            //Get the last 5 recentsongs pages 
            var playerIdFixed = Convert.ToUInt64(playerToCalculate.Split("|")[playerToCalculate.Split("|").Length - 2]);

            List<Score> scores = new List<Score>();
            for (var x = 1; x <= 10; x++)
            {
                var lastRecentMaps = await scoresaberClient.Api.Players.GetRecentSongs(playerIdFixed, x);
                if (lastRecentMaps == null)
                {
                    await Task.Delay(5000);
                    lastRecentMaps = await scoresaberClient.Api.Players.GetRecentSongs(playerIdFixed, x);
                    if (lastRecentMaps == null)
                    {
                        if (window != null) window.ShowErrorMessage($"Could not get recentsongs page {x} from {playerIdFixed}");
                        continue;
                    }
                }
                foreach (var score in lastRecentMaps.Scores) scores.Add(score);
            }

            for (var x = 1; x <= 4; x++)
            {
                var lastTopMaps = await scoresaberClient.Api.Players.GetTopSongs(playerIdFixed, x);
                if (lastTopMaps == null)
                {
                    await Task.Delay(5000);
                    lastTopMaps = await scoresaberClient.Api.Players.GetTopSongs(playerIdFixed, x);
                    if (lastTopMaps == null)
                    {
                        if (window != null) window.ShowErrorMessage($"Could not get topsongs page {x} from {playerIdFixed}");
                        continue;
                    }
                }
                foreach (var score in lastTopMaps.Scores) scores.Add(score);
            }


            var playerData = await scoresaberClient.Api.Players.ByIDBasic(playerIdFixed);
            if (playerData == null) return null;
            var playerRank = playerData.PlayerInfo.Rank;

            var skillSetData = new SkillSetData();
            skillSetData.PlayerName = playerData.PlayerInfo.PlayerName;

            var playerMapsScores = new List<PlayerMapScores>();

            //Get the map in the maplist 
            foreach (var map in mapDataList)
            {
                var actualMap = scores.Where(x => x.SongHash.ToString().ToUpper() == map.HashCode.ToUpper() && x.DifficultyRaw.Replace("_", "").Replace("SoloStandard", "").Trim() == map.Difficulty);
                if (actualMap.Count() <= 0) continue;

                var beatSaverInfo = await BeatSaverApi.GetMapByHash(actualMap.First().SongHash.ToString());
                var metadataDynamic = beatSaverInfo.Metadata.Characteristics.First(x => x.Name == "Standard").Difficulties;
                var diff = actualMap.First().DifficultyRaw.Replace("SoloStandard", "").Replace("_", "").Trim();
                dynamic difficulty = metadataDynamic.GetType().GetProperty(diff).GetValue(metadataDynamic, null);
                var noteCount = (diff == "ExpertPlus" ? metadataDynamic.ExpertPlus.Notes : difficulty.notes);
                var maxScore = (Convert.ToInt32(noteCount) - 13) * 920 + 4715;
                if (maxScore < 0) maxScore = 0;

                double percentage = (Convert.ToDouble(actualMap.First().UnmodififiedScore) / maxScore) * 100;

                playerMapsScores.Add(new PlayerMapScores()
                {
                    SongName = actualMap.First().SongName,
                    Rank = $"#{actualMap.First().Rank}",
                    Acc = $"{ Math.Round(percentage, 2)}%"
                });

                skillSetData.TotalMapCountUsed += 1;

                var actualPlayer = playerToCalculate;
                var techPoints = (float)map.TechPoints;
                var accPoints = (float)map.AccPoints;
                var midSpeedPoints = (float)map.MidSpeedPoints;
                var highSpeedPoints = (float)map.HighSpeedPoints;

                skillSetData.TotalOriginalTechPoints += techPoints;
                skillSetData.TotalOriginalAccPoints += accPoints;
                skillSetData.TotalOriginalMidSpeedPoints += midSpeedPoints;
                skillSetData.TotalOriginalHighSpeedPoints += highSpeedPoints;

                var rankOnMap = actualMap.First().Rank;

                var rankDifference = playerRank - rankOnMap;

                //if (rankDifference > playerRank / 10) rankDifference = rankDifference / 10;
                //if (rankDifference > 0) rankDifference = 1;

                skillSetData.TotalTechPoints += techPoints * rankDifference;
                skillSetData.TotalAccPoints += accPoints * rankDifference;
                skillSetData.TotalMidSpeedPoints += midSpeedPoints * rankDifference;
                skillSetData.TotalHighSpeedPoints += highSpeedPoints * rankDifference;
            }

            //Corrections and making it percentual 
            var lowestNumber = skillSetData.TotalTechPoints;
            if (skillSetData.TotalAccPoints < lowestNumber) lowestNumber = skillSetData.TotalAccPoints;
            if (skillSetData.TotalMidSpeedPoints < lowestNumber) lowestNumber = skillSetData.TotalMidSpeedPoints;
            if (skillSetData.TotalHighSpeedPoints < lowestNumber) lowestNumber = skillSetData.TotalHighSpeedPoints;

            if (lowestNumber > 0)
            {
                //skillSetData.TotalTechPoints -= lowestNumber;
                //skillSetData.TotalAccPoints -= lowestNumber;
                //skillSetData.TotalMidSpeedPoints -= lowestNumber;
                //skillSetData.TotalHighSpeedPoints -= lowestNumber;
            }
            else
            {
                skillSetData.TotalTechPoints += Math.Abs(lowestNumber) * 2;
                skillSetData.TotalAccPoints += Math.Abs(lowestNumber) * 2;
                skillSetData.TotalMidSpeedPoints += Math.Abs(lowestNumber) * 2;
                skillSetData.TotalHighSpeedPoints += Math.Abs(lowestNumber) * 2;
            }

            var totalAmount = skillSetData.TotalTechPoints + skillSetData.TotalAccPoints + skillSetData.TotalMidSpeedPoints + skillSetData.TotalHighSpeedPoints;

            skillSetData.TotalTechPoints = (float)Math.Round((skillSetData.TotalTechPoints * 100 / totalAmount), 2);
            skillSetData.TotalAccPoints = (float)Math.Round((skillSetData.TotalAccPoints * 100 / totalAmount), 2);
            skillSetData.TotalMidSpeedPoints = (float)Math.Round((skillSetData.TotalMidSpeedPoints * 100 / totalAmount), 2);
            skillSetData.TotalHighSpeedPoints = (float)Math.Round((skillSetData.TotalHighSpeedPoints * 100 / totalAmount), 2);

            return new Tuple<SkillSetData, List<PlayerMapScores>>(skillSetData, playerMapsScores);
        }
    }

    public class SkillSetData
    {
        public string PlayerName;
        public int TotalMapCountUsed;

        public float TotalTechPoints;
        public float TotalAccPoints;
        public float TotalMidSpeedPoints;
        public float TotalHighSpeedPoints;

        public float TotalOriginalTechPoints;
        public float TotalOriginalAccPoints;
        public float TotalOriginalMidSpeedPoints;
        public float TotalOriginalHighSpeedPoints;
    }
}
