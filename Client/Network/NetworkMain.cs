﻿using Lidgren.Network;
using LunaClient.Base;
using LunaClient.Systems.SettingsSys;
using LunaCommon.Message;
using LunaCommon.Message.Interface;
using LunaCommon.Time;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace LunaClient.Network
{
    public class NetworkMain
    {
        public static ClientMessageFactory CliMsgFactory { get; } = new ClientMessageFactory();
        public static ServerMessageFactory SrvMsgFactory { get; } = new ServerMessageFactory();
        public static MasterServerMessageFactory MstSrvMsgFactory { get; } = new MasterServerMessageFactory();

        public static Task ReceiveThread { get; set; }
        public static Task SendThread { get; set; }

        public static NetPeerConfiguration Config { get; } = new NetPeerConfiguration("LMP")
        {
            UseMessageRecycling = true,
            ReceiveBufferSize = 500000, //500Kb
            SendBufferSize = 500000, //500Kb
            SuppressUnreliableUnorderedAcks = true, //We don't need ack for unreliable unordered!
            PingInterval = (float)TimeSpan.FromMilliseconds(SettingsSystem.CurrentSettings.HearbeatMsInterval).TotalSeconds,
            ConnectionTimeout = 15
        };

        public static NetClient ClientConnection { get; private set; }

        public static void DeleteAllTheControlLocksSoTheSpaceCentreBugGoesAway()
        {
            LunaLog.Log($"[LMP]: Clearing {InputLockManager.lockStack.Count} control locks");
            InputLockManager.ClearControlLocks();
        }

        public static void RandomizeBadConnectionValues()
        {
            var rnd = new Random();

            LunaTime.SimulatedMsTimeOffset = rnd.Next(-500, 500); //Between -500 and 500 ms
            Config.SimulatedMinimumLatency = (float)rnd.Next(50, 250)/1000; //Between 50 and 250 ms
            Config.SimulatedRandomLatency = (float)rnd.Next(10, 250) / 1000; //Between 10 and 250 ms
            Config.SimulatedDuplicatesChance = (float)rnd.Next(10, 50)/ 1000; //Between 1 and 5%
            Config.SimulatedLoss = (float)rnd.Next(10, 30) / 1000; //Between 1 and 3%
        }

        public static void ResetBadConnectionValues()
        {
            LunaTime.SimulatedMsTimeOffset = 0;
            Config.SimulatedMinimumLatency = 0;
            Config.SimulatedRandomLatency = 0;
            Config.SimulatedDuplicatesChance = 0;
            Config.SimulatedLoss = 0;
        }

        public static void ResetConnectionStaticsAndQueues()
        {
            NetworkSender.OutgoingMessages = new ConcurrentQueue<IMessageBase>();
            NetworkStatistics.LastReceiveTime = DateTime.MinValue;
            NetworkStatistics.LastSendTime = DateTime.MinValue;
        }

        public static void AwakeNetworkSystem()
        {
            Config.EnableMessageType(NetIncomingMessageType.ConnectionLatencyUpdated);
            Config.EnableMessageType(NetIncomingMessageType.NatIntroductionSuccess);
            Config.EnableMessageType(NetIncomingMessageType.UnconnectedData);

#if DEBUG
            Config.EnableMessageType(NetIncomingMessageType.DebugMessage);
            //Config.EnableMessageType(NetIncomingMessageType.VerboseDebugMessage);
#endif
            NetworkServerList.RequestServers();
        }

        public static void ResetNetworkSystem()
        {
            NetworkConnection.ResetRequested = true;

            if (ClientConnection?.Status > NetPeerStatus.NotRunning)
            {
                ClientConnection.Shutdown("Disconnected");
                Thread.Sleep(1000);
            }

            ClientConnection = new NetClient(Config);
            ClientConnection.Start();

            if (SendThread != null && !SendThread.IsCompleted)
                SendThread.Wait(1000);
            if (ReceiveThread != null && !ReceiveThread.IsCompleted)
                ReceiveThread.Wait(1000);

            NetworkConnection.ResetRequested = false;

            ReceiveThread = SystemBase.LongRunTaskFactory.StartNew(NetworkReceiver.ReceiveMain);
            SendThread = SystemBase.LongRunTaskFactory.StartNew(NetworkSender.SendMain);
        }

        public static void HandleDisconnectException(Exception e)
        {
            if (e.InnerException != null)
            {
                LunaLog.LogError($"[LMP]: Connection error: {e.Message}, {e.InnerException}");
                NetworkConnection.Disconnect($"Connection error: {e.Message}, {e.InnerException.Message}");
            }
            else
            {
                LunaLog.LogError($"[LMP]: Connection error: {e}");
                NetworkConnection.Disconnect($"Connection error: {e.Message}");
            }
        }
    }
}
