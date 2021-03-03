using System;
using TwitchLib.Client;
using TwitchLib.Client.Enums;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using TwitchLib.Api;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace ItsMyBot.Worker
{
    public class Bot : Hub
    {
        private readonly TwitchClient client;
        private readonly ILogger<BotWorker> _logger;
        private readonly TwitchAPI api;
        private bool giveawayAlert;
        private readonly IConfiguration configuration;
        
        public Commands.Commands Commands { get; set; }

        
        public void ExecuteGiveawayAlert()
        {
            if (!giveawayAlert &&
                DateTime.Now.Hour >= 22 &&
                DateTime.Now.Minute >= 30)
            {
                client.SendMessage(configuration["channelName"],
                "@itsjanacosplay it's giveaway time!!");
                giveawayAlert = true;
            }
        }

        public Bot(IConfiguration configuration, ILogger<BotWorker> logger)
        {
            this.configuration = configuration;
            _logger = logger;
            
            api = new TwitchAPI();
            api.Settings.ClientId = configuration["itsmybotappclientid"];
            api.Settings.Secret = configuration["itsmybotappsecret"]; // App Secret is not an Accesstoken
            
            ConnectionCredentials credentials = new ConnectionCredentials(configuration["botUserName"], configuration["botAccessToken"]);
            var clientOptions = new ClientOptions
            {
                MessagesAllowedInPeriod = 750,
                ThrottlingPeriod = TimeSpan.FromSeconds(30)
            };
            WebSocketClient customClient = new WebSocketClient(clientOptions);
            client = new TwitchClient(customClient);
            client.Initialize(credentials, configuration["channelName"]);
            client.OnLog += Client_OnLog;
            client.OnJoinedChannel += Client_OnJoinedChannel;
            client.OnMessageReceived += Client_OnMessageReceived;
            client.OnWhisperReceived += Client_OnWhisperReceived;
            client.OnNewSubscriber += Client_OnNewSubscriber;
            client.OnConnected += Client_OnConnected;

            client.Connect();

            Commands = new Commands.Commands(client, api);
        }

        private void Client_OnLog(object sender, OnLogArgs e)
        {
            this.WriteLine($"{e.DateTime}: {e.BotUsername} - {e.Data}");
        }

        private void Client_OnConnected(object sender, OnConnectedArgs e)
        {
            this.WriteLine($"Connected to {e.AutoJoinChannel}");
        }


        private void Client_OnJoinedChannel(object sender, OnJoinedChannelArgs e)
        {
            this.WriteLine("Hey guys! I am a bot connected via TwitchLib at " + e.Channel);
        }


        private void Client_OnMessageReceived(object sender, OnMessageReceivedArgs e)
        {
            var msg = e.ChatMessage.Message.ToLowerInvariant();
            
            if (msg.Contains("!hello"))
                client.SendMessage(e.ChatMessage.Channel, $"Hello {e.ChatMessage.Username}! Nice to meet you!");

            if (msg.Equals("!wddcommands"))
            {
                client.SendMessage(e.ChatMessage.Channel,
                    $"Glad {e.ChatMessage.Username} want to know about WDD!\nTry any of this commands - {String.Join(" ", Commands.WDD.Keys)}");
            }
            if (Commands.WDD.ContainsKey(msg))
            {
                client.SendMessage(e.ChatMessage.Channel, Commands.WDD[msg]);
            }
            if(msg.Contains("thank you for following"))
            {
                //Commands.DrawActionCommands["!update"](e, client);
            }
            if (Commands.DrawActionCommands.ContainsKey(msg))
            {
                Commands.DrawActionCommands[msg](e, client);
            }
            
        }

        private void Client_OnWhisperReceived(object sender, OnWhisperReceivedArgs e)
        {
            if (e.WhisperMessage.Username == "my_friend")
                client.SendWhisper(e.WhisperMessage.Username, "Hey! Whispers are so cool!!");
        }

        private void Client_OnNewSubscriber(object sender, OnNewSubscriberArgs e)
        {
            if (e.Subscriber.SubscriptionPlan == SubscriptionPlan.Prime)
                client.SendMessage(e.Channel, $"Welcome {e.Subscriber.DisplayName} to the substers! So kind of you to use your Twitch Prime on this channel!");
            else
                client.SendMessage(e.Channel, $"Welcome {e.Subscriber.DisplayName} to the substers!");
        }


        #region Talk to the SignalR Front end.
        public class SpeakResponse
        {
            public string Message { get; set; }
            public string WavUri { get; set; }
        }


        public async Task BotSpeak(SpeakResponse message)
        {
            if (Clients != null)
            {
                await Clients?.All.SendAsync("BotSpeak", message);
            }
        }
        #endregion

        private void WriteLine(string line)
        {
            _logger.LogInformation(line);
        }
    }
}
