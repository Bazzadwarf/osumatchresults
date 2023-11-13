using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace osumatchresults
{
    class Program
    {
        static async System.Threading.Tasks.Task Main(string[] args)
        {
            //Console.Write("API KEY: ");
            //var apikey = Console.ReadLine();

            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri("https://osu.ppy.sh/api/");

            ////////////////////////////////////////////////////////

            int matchid = 0;



            Console.Write("MATCH ID: ");

            var input = Console.ReadLine();
            var inputs = input.Split(" ");

            Console.Write("RUN: ");
            var run = Console.ReadLine();
            
            foreach (var varname in inputs)
            {
                bool success = int.TryParse(varname, out matchid);
                if (!success)
                {
                    Console.WriteLine("Please enter a valid id");
                    continue;
                }

                String responseString = await client.GetStringAsync(client.BaseAddress + "get_match?k=" + apikey + "&mp=" + matchid);
                var json = JObject.Parse(responseString);
                var lobbyname = Regex.Replace(json["match"]["name"].ToString(), "[^A-Za-z0-9 -]", "");
                Console.WriteLine(lobbyname);
                List<Game> games = new List<Game>();

                foreach (var game in json["games"])
                {
                    Game newGame = new Game();
                    newGame.scores = new List<Score>();
                    newGame.beatmapid = int.Parse(game["beatmap_id"].ToString());

                    if (newGame.beatmapid == 3860999)
                    {
                        newGame.beatmapid = 3861000;
                    }

                    String beatmapResponse = await client.GetStringAsync(client.BaseAddress + "get_beatmaps?k=" + apikey + "&b=" + newGame.beatmapid);
                    beatmapResponse = beatmapResponse.Substring(1, beatmapResponse.Length - 2);
                    var beatmapjson = JObject.Parse(beatmapResponse);
                    newGame.beatmap = beatmapjson["artist"] + " " + beatmapjson["title"] + " [" + beatmapjson["version"] + "]";
                    newGame.beatmapsetid = int.Parse(beatmapjson["beatmapset_id"].ToString());

                    Console.WriteLine(newGame.beatmapid + " " + newGame.beatmap);

                    foreach (var score in game["scores"])
                    {
                        Score newScore = new Score();

                        newScore.matchid = matchid;

                        var userid = int.Parse(score["user_id"].ToString());

                        String userResponse = await client.GetStringAsync(client.BaseAddress + "get_user?k=" + apikey + "&u=" + userid);
                        userResponse = userResponse.Substring(1, userResponse.Length - 2);

                        if (userResponse == "")
                        {
                            newScore.user = "Fia";
                        }
                        else
                        {
                            var userJson = JObject.Parse(userResponse);
                            newScore.user = userJson["username"].ToString();
                        }

                        newScore.score = int.Parse(score["score"].ToString());

                        Mods mods;

                        if (int.Parse(game["mods"].ToString()) == 0)
                        {
                            mods = (Mods)int.Parse(score["enabled_mods"].ToString());
                        }
                        else
                        {
                            mods = (Mods)int.Parse(game["mods"].ToString());
                        }

                        foreach (Mods flagToCheck in Mods.GetValues(typeof(Mods)))
                        {
                            if (mods.HasFlag(flagToCheck))
                            {
                                newScore.mods = newScore.mods + flagToCheck.ToString();
                            }
                        }

                        newScore.acc = ((int.Parse(score["count300"].ToString()) * 300.0f) + (int.Parse(score["count100"].ToString()) * 100.0f) + (int.Parse(score["count50"].ToString()) * 50.0f)) / ((int.Parse(score["count300"].ToString()) + int.Parse(score["count100"].ToString()) + int.Parse(score["count50"].ToString()) + int.Parse(score["countmiss"].ToString())) * 300.0f);

                        newScore.count300 = int.Parse(score["count300"].ToString());
                        newScore.count100 = int.Parse(score["count100"].ToString());
                        newScore.count50 = int.Parse(score["count50"].ToString());
                        newScore.countmiss = int.Parse(score["countmiss"].ToString());
                        newScore.run = "";

                        newGame.scores.Add(newScore);
                    }

                    games.Add(newGame);
                }

                StreamWriter file = new(lobbyname + ".xls", append: true);

                foreach (var game in games)
                {
                    //await file.WriteLineAsync(game.beatmapid + "," + game.beatmap);

                    foreach (var score in game.scores)
                    {
                        await file.WriteLineAsync(game.beatmapsetid + "," + score.user + "," + score.score + "," + score.acc + "," + run + "," + score.mods + "," + score.count300 + "," + score.count100 + "," + score.count50 + "," + score.countmiss);
                    }
                }

                await file.FlushAsync();
            }
        }
    }

    public struct Game
    {
        public List<Score> scores;
        public int beatmapid;
        public int beatmapsetid;
        public string beatmap;
    }

    public struct Score
    {
        public int matchid;
        public string user;
        public int score;
        public float acc;
        public string run;
        public string mods;
        public int count300;
        public int count100;
        public int count50;
        public int countmiss;
    }

    [Flags]
    enum Mods
    {
        Easy = 2,
        TouchDevice = 4,
        HD = 8,
        HR = 16,
        SuddenDeath = 32,
        DT = 64,
        Relax = 128,
        HalfTime = 256,
        Nightcore = 512, // Only set along with DoubleTime. i.e: NC only gives 576
        Flashlight = 1024,
        Autoplay = 2048,
        SpunOut = 4096,
        Relax2 = 8192,    // Autopilot
        Perfect = 16384, // Only set along with SuddenDeath. i.e: PF only gives 16416  
        Key4 = 32768,
        Key5 = 65536,
        Key6 = 131072,
        Key7 = 262144,
        Key8 = 524288,
        FadeIn = 1048576,
        Random = 2097152,
        Cinema = 4194304,
        Target = 8388608,
        Key9 = 16777216,
        KeyCoop = 33554432,
        Key1 = 67108864,
        Key3 = 134217728,
        Key2 = 268435456,
        ScoreV2 = 536870912,
        Mirror = 1073741824,
    }
}
