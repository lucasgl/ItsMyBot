using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TwitchLib.Api;
using TwitchLib.Api.Helix.Models.Users;
using TwitchLib.Client;
using TwitchLib.Client.Enums;
using TwitchLib.Client.Events;

namespace ItsMyBot.Worker.Commands
{
    public class Commands
    {

        List<Follower> followers = new List<Follower>();
        public Dictionary<string, Action<OnMessageReceivedArgs, TwitchClient>> DrawActionCommands { get; set; }
        public TwitchClient Client { get; }
        public TwitchAPI Api { get; }
        public Dictionary<string, Follower> winners = new();

        public List<string> giveawayEntries = new();


        private string giveawayfile = @"giveawayentries.json";
        private string followersfile = @"followers.json";
        private string winnersFile = @"winners.json";


        public Commands(TwitchClient client, TwitchAPI api)
        {
            Client = client;
            Api = api;
            if (File.Exists(followersfile))
                using (StreamReader reader = new StreamReader(followersfile))
                    followers = JsonSerializer.Deserialize<List<Follower>>(reader.ReadToEnd());

            if (File.Exists(winnersFile))
                using (StreamReader reader = new StreamReader(winnersFile))
                    winners = JsonSerializer.Deserialize<Dictionary<string, Follower>>(reader.ReadToEnd());

            DrawActionCommands = new()
            {
                {
                    "!draw",
                    async (e, client) =>
                    {
                        if (e.ChatMessage.UserType == UserType.Moderator ||
                            e.ChatMessage.UserType == UserType.Broadcaster)
                        {
                            await ExecuteDraw(e, client, "Special");
                        }
                    }
                },
                {
                    "!resync",
                    async (e, client) =>
                    {
                        if (e.ChatMessage.UserType == UserType.Moderator ||
                            e.ChatMessage.UserType == UserType.Broadcaster)
                        {
                            try
                            {
                                await UpdateFollowersFile(e.ChatMessage.Channel);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("Exception!UpdateFollowres " + ex.Message);
                            }
                        }
                    }
                },
                {
                    "!giveaway",
                    (e, client) => client.SendMessage(e.ChatMessage.Channel,
                    "Follow me to join the giveaway!")
                },
                {
                    "!update",
                    async (e, client) =>
                    {
                        if (e.ChatMessage.UserType == UserType.Moderator ||
                            e.ChatMessage.UserType == UserType.Broadcaster)
                        {
                            try
                            {
                                var user = "";
                                if(e.ChatMessage.Message.StartsWith("Thank"))
                                {
                                    user = e.ChatMessage.Message.Trim().TrimEnd('!');
                                    user = user.Replace("@","");
                                    user = user.Substring(user.LastIndexOf(' ')).Trim();
                                }
                                await AddFollower(user, e.ChatMessage.Channel);
                                client.SendMessage(e.ChatMessage.Channel, $"@{user} can now enter in giveaway!");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("Exception!AddFollow " + ex.Message);
                            }
                        }
                    }
                },
                {
                    "!winners",
                    (e, client) =>
                    {
                        StringBuilder sb = new StringBuilder("Tonite winners are ");
                        foreach (var item in winners)
                        {
                            sb.Append($"{item.Key}: @{item.Value.FromUserName},");
                        }
                        sb.Append(" send a whisper to @ItsMyBot for know the details!");
                        client.SendMessage(e.ChatMessage.Channel,sb.ToString());
                    }
                }                
            };
        }


        Random random = new Random(DateTime.Now.Millisecond);
        private async Task ExecuteDraw(OnMessageReceivedArgs e, TwitchClient client, string drawType)
        {
            if (File.Exists(giveawayfile))
                using (StreamReader reader = new StreamReader(giveawayfile))
                    giveawayEntries = JsonSerializer.Deserialize<List<string>>(reader.ReadToEnd());

            var users = giveawayEntries;//followers;
            int winnerPosition = random.Next(0, users.Count);

            client.SendMessage(e.ChatMessage.Channel,
                    $"Beep Bop, computing the winner of the {drawType} giveaway!");

            await Task.Delay(1000);
            Follower winner = null;
            while(winner == null)
            {
                winnerPosition = await GetWinnerPosition(e, client, users, winnerPosition);
                string winnername = users.ElementAt(winnerPosition);
                winner = followers.FirstOrDefault(i => i.FromUserName.ToLower() == winnername.ToLower());
                if(winner is null)
                {
                    client.SendMessage(e.ChatMessage.Channel,
                    $"Wait, {winnername} is not a follower! Trying again!");
                    await Task.Delay(2000);
                }
            };
            
            if (winners.ContainsKey(drawType))
            {
                winners[drawType] = winner;
            }
            else
            {
                winners.Add(drawType, winner);
            }
            SerializeWinners();

            client.SendMessage(e.ChatMessage.Channel,
                $"Beep Boop!! And the winner is @{winner.FromUserName}! Please send me a whisper for details!");
        }

        private async Task<int> GetWinnerPosition(OnMessageReceivedArgs e, TwitchClient client, List<string> users, int winnerPosition)
        {
            for (int i = 6 - 1; i >= 1; i--)
            {
                winnerPosition = random.Next(0, users.Count());
                client.SendMessage(e.ChatMessage.Channel,
                $"{i} ({winnerPosition})!");
                await Task.Delay(2000);
            }

            return winnerPosition;
        }

        public class Follower
        {
            public string FromUserId { get; set; }
            public string FromUserName { get; set; }
            public string ToUserId { get; set; }
            public string ToUserName { get; set; }
            public DateTime FollowedAt { get; set; }
            public Follower()
            {

            }
            public Follower(Follow follow)
            {

                FollowedAt = follow.FollowedAt;
                FromUserId = follow.FromUserId;
                FromUserName = follow.FromUserName;
                ToUserId = follow.ToUserId;
                ToUserName = follow.ToUserName;
            }
        }

        private int GetIndex(string userid)
        {
            int currentIndex = followers.FindIndex(i => i.FromUserId == userid);
            return currentIndex >=0 ? followers.Count - currentIndex : currentIndex;
        }

        public async Task UpdateFollowersFile(string channel)
        {
            var users = await Api.Helix.Users.GetUsersAsync(logins: new() { channel });

            foreach (var item in users.Users)
            {
                Console.WriteLine(item.DisplayName);
                followers = new List<Follower>();
                var end = false;
                string cursor = null;
                do
                {
                    var followsResponse = await Api.Helix.Users.GetUsersFollowsAsync(toId: item.Id, first: 100, after: cursor);
                    foreach (var follower in followsResponse.Follows)
                    {
                        followers.Add(new Follower(follower));
                    }
                    cursor = followsResponse.Pagination.Cursor;
                    end = followsResponse.Follows.Length < 100;
                } while (!end);

                using (FileStream writer = new FileStream("followers.json", FileMode.Create))
                    await JsonSerializer.SerializeAsync(writer, followers);

            }
        }

        public async Task AddFollower(string userLogin, string channel)
        {
            var users = await Api.Helix.Users.GetUsersAsync(logins: new() { channel });

            foreach (var item in users.Users)
            {
                var newusers = await Api.Helix.Users.GetUsersAsync(logins: new() { userLogin });
                var followsResponse = await Api.Helix.Users.GetUsersFollowsAsync(fromId:newusers.Users.First().Id, toId: item.Id);
                followers.Insert(0, new Follower(followsResponse.Follows.First()));

                using (FileStream writer = new FileStream("followers.json", FileMode.Create))
                    await JsonSerializer.SerializeAsync(writer, followers);

            }
        }

        public Dictionary<string, string> WDD { get; set; } =
        new Dictionary<string, string>()
        {
            {"!t1d","Type 1 diabetes (T1D), previously known as juvenile diabetes, is a form of diabetes in which very little or no insulin is produced by the pancreas.\n Type !t1ds to learn about common symptoms" },
            {"!t1ds","The T1D classic symptoms are frequent urination, increased thirst, increased hunger, and weight loss. Additional symptoms may include blurry vision, tiredness, and poor wound healing. Symptoms typically develop over a short period of time, often a matter of weeks." },
            {"!t2d","Type 2 diabetes (T2D), formerly known as adult-onset diabetes, is a form of diabetes that is characterized by high blood sugar, insulin resistance, and relative lack of insulin.\n Type !t2ds to learn about common symptoms." },
            {"!t2ds","The T2D common symptoms include increased thirst, frequent urination, and unexplained weight loss. Symptoms may also include increased hunger, feeling tired, and sores that do not heal. Often symptoms come on slowly." },
            {"!di", "Diabetes insipidus (DI) is a condition characterized by large amounts of dilute urine and increased thirst. The amount of urine produced can be nearly 20 liters per day. Reduction of fluid has little effect on the concentration of the urine. Complications may include dehydration or seizures." },
            {"!gd", "Gestational diabetes is a condition in which a woman without diabetes develops high blood sugar levels during pregnancy.\n Type !gds to learn about common symptoms." },
            {"!gds", "Gestational diabetes generally results in few symptoms; however, it does increase the risk of pre-eclampsia, depression, and requiring a Caesarean section." },
            {"!wdd", "World Diabetes Day is every year on November 14, first created in 1991 by the International Diabetes Foundation and the World Health Organization. Type !diabetes to know more!" },
            {"!diabetes","Diabetes mellitus refers to a group of diseases that affect how your body uses blood sugar (glucose).\n Type !t1d, !t2d, !gd or !di to know more." }
        };
        
        public void SerializeWinners()
        {
            using (StreamWriter writer = new StreamWriter(winnersFile))
                writer.Write(JsonSerializer.Serialize(winners));
        }
    }
}
