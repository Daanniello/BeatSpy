using ScoreSaberLib;
using ScoreSaberLib.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeatSpy
{
    public class SkillSetCalculation
    {
        private string playerToCalculate;
        private List<string> mapInfoList = new List<string>();

        public SkillSetCalculation(string playerScoresaberID)
        {
            playerToCalculate = playerScoresaberID;
        }

        public void AddMaps(params string[] mapInfoParam)
        {
            foreach (var mapID in mapInfoParam) mapInfoList.Add(mapID);
        }

        public async Task<SkillSetData> CalculateAndGetResults()
        {
            var scoresaberClient = new ScoreSaberClient();

            //Get the last 5 recentsongs pages 
            var playerIdFixed = Convert.ToUInt64(playerToCalculate.Split("|")[1]);

            List<Score> scores = new List<Score>();
            for (var x = 0; x < 10; x++)
            {                
                var lastRecentMaps = await scoresaberClient.Api.Players.GetRecentSongs(playerIdFixed, 1);
                if (lastRecentMaps == null) continue;
                foreach (var score in lastRecentMaps.Scores) scores.Add(score);
            }

            for (var x = 0; x < 4; x++)
            {
                var lastTopMaps = await scoresaberClient.Api.Players.GetTopSongs(playerIdFixed, 1);
                if (lastTopMaps == null) continue;
                foreach (var score in lastTopMaps.Scores) scores.Add(score);
            }


            var playerData = await scoresaberClient.Api.Players.ByIDBasic(playerIdFixed);            
            var playerRank = playerData.PlayerInfo.Rank;

            var skillSetData = new SkillSetData();

            //Get the map in the maplist 
            foreach (var map in mapInfoList)
            {
                var actualMap = scores.Where(x => x.LeaderboardId.ToString() == map.Split("|")[0]);
                if (actualMap.Count() <= 0) continue;

                skillSetData.TotalMapCountUsed += 1;

                var actualPlayer = playerToCalculate;                
                var techPoints = Convert.ToInt64(map.Split("|")[1]);
                var accPoints = Convert.ToInt64(map.Split("|")[2]);
                var midSpeedPoints = Convert.ToInt64(map.Split("|")[3]);
                var highSpeedPoints = Convert.ToInt64(map.Split("|")[4]);

                skillSetData.TotalOriginalTechPoints += techPoints;
                skillSetData.TotalOriginalAccPoints += accPoints;
                skillSetData.TotalOriginalMidSpeedPoints += midSpeedPoints;
                skillSetData.TotalOriginalHighSpeedPoints += highSpeedPoints;

                var rankOnMap = actualMap.First().Rank;

                var rankDifference = playerRank - rankOnMap;

                //if (rankDifference > playerRank / 10) rankDifference = rankDifference / 10;
                if (rankDifference > 0) rankDifference = 1;

                skillSetData.TotalTechPoints += techPoints * rankDifference;
                skillSetData.TotalAccPoints += accPoints * rankDifference;
                skillSetData.TotalMidSpeedPoints += midSpeedPoints * rankDifference;
                skillSetData.TotalHighSpeedPoints += highSpeedPoints * rankDifference;
            }
            return skillSetData;            
        }
    }

    public class SkillSetData
    {
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
