using Rocket.API;
using Rocket.API.Collections;
using Rocket.Core.Plugins;
using Rocket.Unturned.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;
using Rocket.Core;
using Rocket.Core.Serialization;
using Rocket.Unturned;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using Rocket.Unturned.Commands;
using SDG.Unturned;
using UnityEngine;
using SDG;
using Rocket.Core.Logging;
using Rocket.Unturned.Enumerations;
using Rocket.Unturned.Events;
using CFire_Command;

namespace CFire_Plug
{
    public class CFire_Plugin : RocketPlugin<CFireConfiguration>
    {
		//global variables
		public Vector3 lastPos;
		public Vector3 deathPos;
		public uint exp;
		public Dictionary<Steamworks.CSteamID, Vector3> tpDeaths = new Dictionary<Steamworks.CSteamID, Vector3>();
		public Dictionary<String, Steamworks.CSteamID> link = new Dictionary<String, Steamworks.CSteamID>();

		public static CFire_Plugin Instance;

        protected override void Load()
        {
            Instance = this;
			Logger.Log("CFire has been loaded!");
			U.Events.OnPlayerConnected += Events_OnPlayerConnected;
			U.Events.OnPlayerDisconnected += Events_OnPlayerDisconnected;
			Rocket.Unturned.Events.UnturnedPlayerEvents.OnPlayerDeath += UnturnedPlayerEvents_OnPlayerDeath;
			Rocket.Unturned.Events.UnturnedPlayerEvents.OnPlayerUpdatePosition += UnturnedPlayerEvents_OnPlayerUpdatePosition;
			Rocket.Unturned.Events.UnturnedPlayerEvents.OnPlayerUpdateExperience += UnturnedPlayerEvents_OnPlayerUpdateExperience;
			string currentPath = System.IO.Directory.GetCurrentDirectory();
            System.IO.Directory.CreateDirectory(currentPath + Path.DirectorySeparatorChar + "Plugins" + Path.DirectorySeparatorChar + "CFire");
            System.IO.Directory.CreateDirectory(currentPath + Path.DirectorySeparatorChar + "Plugins" + Path.DirectorySeparatorChar + "CFire" + Path.DirectorySeparatorChar + "Players");
            System.IO.Directory.CreateDirectory(currentPath + Path.DirectorySeparatorChar + "Plugins" + Path.DirectorySeparatorChar + "CFire" + Path.DirectorySeparatorChar + "Players" + Path.DirectorySeparatorChar + "Characters");
        }
        protected override void Unload()
		{
			U.Events.OnPlayerConnected -= Events_OnPlayerConnected;
			U.Events.OnPlayerDisconnected -= Events_OnPlayerDisconnected;
			base.Unload ();
		}
        public override TranslationList DefaultTranslations 
         { 
             get 
             { 
                 return new TranslationList 
                 { 
                     { "playerNotFound", "Player does not exist or is offline!" },
					 { "offlineRemove", "Offline ceasefire has been removed." },
                     { "nopermission_send", "You do not have permission to send ceasefire requests." }, 
                     { "nopermission_accept", "You do not have permission to accept ceasefire requests." }, 
                     { "nopermission_deny", "You do not have permission to deny ceasefire requests." }, 
					 { "error_tp", "Your last death was not a violated ceasefire or you've already used this command." }, 
                     { "error_cooldown", "You may only send requests every 10 seconds." }, 
                     { "request_accepted", "You've accepted the ceasefire request from: " }, 
                     { "request_denied", "You've denied the ceasefire request from: " }, 
                     { "request_accepted_1", "has accepted your ceasefire request!" }, 
                     { "request_denied_1", "has denied your ceasefire request!" },
                     { "request_sent", "You have sent a ceasefire request to: " }, 
                     { "request_sent_1", "has sent you a ceasefire request, you can use /cfire accept or /cfire deny." },
                     { "request_none", "You have no active requests." }, 
					 { "request_pending", "You already have a ceasefire request pending to: " },
					 { "request_dopple", "You cannot start a ceasefire with yourself." },
					 { "cancel_cf", "Your ceasefire with" }, 
					 { "cancel_cf_1", "will end in 10 seconds." },
					 { "cancel_cf_2", "has ended your ceasefire. It will go inactive in 10 seconds." },
					 { "cf_kill", "has violated your ceasefire! They have been executed. Type /cfire tp to go back." },
					 { "cf_kill_1", "You were executed for violating your ceasefire with" },
					 { "cf_ended", "Your ceasefire with " },
					 { "cf_ended_1", " has ended!" },
					 { "help_1", "If you kill someone you're in a ceasefire with, you will be executed." },  
					 { "help_2", "When a ceasefire is ended, there will be a text warning." }, 
					 { "help_3", "/cfire (playerName) - Sends a ceasefire request or terminates an active ceasefire." }, 
					 { "help_4", "/cfire accept or /cfire a - Accepts a ceasefire request." }, 
					 { "help_5", "/cfire deny or /cfire d - Denies a ceasefire request." },
					 { "help_6", "/cfire tp - Teleports you to location of last ceasefire death." },
                 }; 
             } 
         } 
		private void Events_OnPlayerConnected(UnturnedPlayer player)
		{
			string playerSID = player.CSteamID + ".txt";
			string playerName = player.CharacterName + ".txt";
			string currentPath = System.IO.Directory.GetCurrentDirectory();
            string filePathSID = currentPath + Path.DirectorySeparatorChar + "Plugins" + Path.DirectorySeparatorChar + "CFire" + Path.DirectorySeparatorChar + "Players" + Path.DirectorySeparatorChar + playerSID;
            string filePathName = currentPath + Path.DirectorySeparatorChar + "Plugins" + Path.DirectorySeparatorChar + "CFire" + Path.DirectorySeparatorChar + "Players" + Path.DirectorySeparatorChar + "Characters" + Path.DirectorySeparatorChar + playerName;

			if(!System.IO.File.Exists(filePathSID))
			{
				var file = System.IO.File.Create (filePathSID);
				file.Close();
			}
			if(!System.IO.File.Exists(filePathName))
			{
				var file = System.IO.File.Create (filePathName);
				file.Close();
				link.Add(player.CharacterName, player.CSteamID);
			}
			if (CFire_Plugin.Instance.Configuration.Instance.CFireDisplayActiveCeasefiresOnConnect == true) 
			{
				
				List<string> cfList = File.ReadAllLines (filePathName).ToList ();
				string cfActive = string.Join(",", cfList.ToArray());
				if (cfActive != "")
				{
					Rocket.Unturned.Chat.UnturnedChat.Say (player, "Active Ceasefires: " + cfActive, Color.red);
				}
				if (cfActive == "") 
				{
					Rocket.Unturned.Chat.UnturnedChat.Say (player, "Active Ceasefires: None", Color.red);
				}
			}
			//add update character name when SID logs in
		}

		private void Events_OnPlayerDisconnected(UnturnedPlayer player)
		{
			
		}
		private void UnturnedPlayerEvents_OnPlayerDeath(Rocket.Unturned.Player.UnturnedPlayer player, SDG.Unturned.EDeathCause cause, SDG.Unturned.ELimb limb, Steamworks.CSteamID murderer)
		{
			UnturnedPlayer killer = UnturnedPlayer.FromCSteamID(murderer);
			string currentPath = System.IO.Directory.GetCurrentDirectory();
			string playerSID = player.CSteamID + ".txt";
			string murdererSID = Rocket.Unturned.Player.UnturnedPlayer.FromCSteamID(murderer) + "";
			string player1SID = player.CSteamID + "";
			string player2SID = killer.CSteamID + "";
			string player1Name = player.CharacterName + "";
			string player2Name = killer.CharacterName + "";
			string player1File = player.CSteamID + ".txt";
			string player2File = killer.CSteamID + ".txt";
			string player3File = player.CharacterName + ".txt";
			string player4File = killer.CharacterName + ".txt";
            string fileName1 = currentPath + +Path.DirectorySeparatorChar + "Plugins" + Path.DirectorySeparatorChar + "CFire" + Path.DirectorySeparatorChar + "Players" + Path.DirectorySeparatorChar + player1File;
            string fileName2 = currentPath + Path.DirectorySeparatorChar + "Plugins" + Path.DirectorySeparatorChar + "CFire" + Path.DirectorySeparatorChar + "Players" + Path.DirectorySeparatorChar + player2File;
            string fileName3 = currentPath + Path.DirectorySeparatorChar + "Plugins" + Path.DirectorySeparatorChar + "CFire" + Path.DirectorySeparatorChar + "Players" + Path.DirectorySeparatorChar + "Characters" + Path.DirectorySeparatorChar + player3File;
            string fileName4 = currentPath + Path.DirectorySeparatorChar + "Plugins" + Path.DirectorySeparatorChar + "CFire" + Path.DirectorySeparatorChar + "Players" + Path.DirectorySeparatorChar + "Characters" + Path.DirectorySeparatorChar + player4File;
			string line;

			System.IO.StreamReader file =
                new System.IO.StreamReader(currentPath + Path.DirectorySeparatorChar + "Plugins" + Path.DirectorySeparatorChar + "CFire" + Path.DirectorySeparatorChar + "Players" + Path.DirectorySeparatorChar + playerSID);
			while ((line = file.ReadLine ()) != null) 
			{
				if (line == murdererSID) 
				{
					file.Close ();
					Execute(player, killer);

					deathPos = lastPos;
					tpDeaths.Add(player.CSteamID, deathPos);

					//remove ceasefire from first player SID
					List<string> cfList1 = File.ReadAllLines(fileName1).ToList();
					cfList1.Remove(player2SID);
					File.WriteAllLines(fileName1, cfList1.ToArray());
					//remove ceasefire from second player SID
					List<string> cfList2 = File.ReadAllLines(fileName2).ToList();
					cfList2.Remove(player1SID);
					File.WriteAllLines(fileName2, cfList2.ToArray());
					//remove ceasefire from first player Name
					List<string> cfList3 = File.ReadAllLines(fileName3).ToList();
					cfList3.Remove(player2Name);
					File.WriteAllLines(fileName3, cfList3.ToArray());
					//remove ceasefire from second player Name
					List<string> cfList4 = File.ReadAllLines(fileName4).ToList();
					cfList4.Remove(player1Name);
					File.WriteAllLines(fileName4, cfList4.ToArray());

					Rocket.Unturned.Chat.UnturnedChat.Say(player, killer.CharacterName + " " + CFire_Plugin.Instance.Translate("cf_kill"), Color.red);
					Rocket.Unturned.Chat.UnturnedChat.Say(killer, CFire_Plugin.Instance.Translate("cf_kill_1") + " " + player.CharacterName, Color.red);
					break;
				}
			}
			file.Close ();
		}
		private void Execute(UnturnedPlayer player, UnturnedPlayer killer)
		{
			killer.Damage(255, player.Position, EDeathCause.PUNCH, ELimb.SKULL, killer.CSteamID);
		}
		private void UnturnedPlayerEvents_OnPlayerUpdatePosition (UnturnedPlayer player, Vector3 position)
		{
			lastPos = position;
		}
		private void UnturnedPlayerEvents_OnPlayerUpdateExperience (UnturnedPlayer player, uint experience)
		{
			exp = experience;
		}
    }

    public class CFireConfiguration : IRocketPluginConfiguration
    {
        public int CFireCoolDownSeconds;
		public int CFireCeaseFireEndSeconds;
		public bool CFireDisplayActiveCeasefiresOnConnect;
        public void LoadDefaults()
        {
            CFireCoolDownSeconds = 10;
			CFireCeaseFireEndSeconds = 10;
			CFireDisplayActiveCeasefiresOnConnect = true;
        }
    }
}
