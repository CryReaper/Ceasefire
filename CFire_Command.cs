using Rocket.API;
using Rocket.Unturned;
using Rocket.Unturned.Commands;
using Rocket.Unturned.Player;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using System.IO;
using System.Threading;
using CFire_Plug;

namespace CFire_Command
{
    public class CommandCFire : IRocketCommand
    {
        #region Delcarations
		public bool AllowFromConsole
        {
            get
            {
                return false;
            }
        }
        public List<string> Permissions
        {
            get
            {
                return new List<string>() { 
                    "CommandCfire.cfire"
                };
            }
        }
        public bool RunFromConsole
        {
            get { return false; }
        }

        public string Name
        {
            get { return "cfire"; }
        }
        public string Syntax
        {
            get
            {
				return "cfire (player/accept/deny/list)";
            }
        }
        public string Help
        {
            get { return "Request a ceasefire with a player, accept or deny other requests, or list your active ceasefires."; }
        }
        public List<string> Aliases
        {
			get { return new List<string> { "cf" }; }
        }

        Dictionary<Steamworks.CSteamID, Steamworks.CSteamID> requests = new Dictionary<Steamworks.CSteamID, Steamworks.CSteamID>();
        Dictionary<Steamworks.CSteamID, DateTime> coolDown = new Dictionary<Steamworks.CSteamID, DateTime>();
        #endregion

        public AllowedCaller AllowedCaller
        {
            get { return AllowedCaller.Both; }
        }


		void Rocket_Unturned_Events_UnturnedPlayerEvents_OnPlayerDead (UnturnedPlayer player, Vector3 position)
		{
			
		}

		public void Execute(IRocketPlayer caller, string[] command)
        {
            UnturnedPlayer player = (UnturnedPlayer)caller;
			if (command.Length < 1)
			{
				return;
			}
			if (command[0].ToString().ToLower() == "help")
            {
				Rocket.Unturned.Chat.UnturnedChat.Say(caller, CFire_Plugin.Instance.Translate("help_1"), Color.yellow);
				Rocket.Unturned.Chat.UnturnedChat.Say(caller, CFire_Plugin.Instance.Translate("help_2"), Color.yellow);
                Rocket.Unturned.Chat.UnturnedChat.Say(caller, CFire_Plugin.Instance.Translate("help_3"), Color.yellow);
				Rocket.Unturned.Chat.UnturnedChat.Say(caller, CFire_Plugin.Instance.Translate("help_4"), Color.yellow);
				Rocket.Unturned.Chat.UnturnedChat.Say(caller, CFire_Plugin.Instance.Translate("help_5"), Color.yellow);
				Rocket.Unturned.Chat.UnturnedChat.Say(caller, CFire_Plugin.Instance.Translate("help_6"), Color.yellow);
                return;
            }
			if (command [0].ToString ().ToLower () == "list") 
			{
				string playerName = player.CharacterName + ".txt";
				string currentPath = System.IO.Directory.GetCurrentDirectory();
                string filePathName = currentPath + Path.DirectorySeparatorChar + "Plugins" + Path.DirectorySeparatorChar + "CFire" + Path.DirectorySeparatorChar + "Players" + Path.DirectorySeparatorChar + "Characters"+Path.DirectorySeparatorChar + playerName;

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
			if (command[0].ToString().ToLower() == "tp")
			{
				float flo = 0.0F;
				Vector3 pos;

				if (CFire_Plug.CFire_Plugin.Instance.tpDeaths.ContainsKey (player.CSteamID)) 
				{
					CFire_Plug.CFire_Plugin.Instance.tpDeaths.TryGetValue(player.CSteamID, out pos);
					player.Teleport (pos, flo);
					CFire_Plug.CFire_Plugin.Instance.tpDeaths.Remove(player.CSteamID);
				}
				else 
				{
					Rocket.Unturned.Chat.UnturnedChat.Say (player, CFire_Plugin.Instance.Translate ("error_tp"), Color.red);
					return;
				}
			}
			if (command[0].ToString().ToLower() == "accept" || command[0].ToString().ToLower() == "a")
            {

                if (!player.HasPermission("cfire.accept"))
                {
                    Rocket.Unturned.Chat.UnturnedChat.Say(player, CFire_Plugin.Instance.Translate("nopermission_accept"), Color.red);
                    return;
                }

                if (requests.ContainsKey(player.CSteamID))
                {
                    UnturnedPlayer tpP = UnturnedPlayer.FromCSteamID(requests[player.CSteamID]);

                    requests.Remove(player.CSteamID);
                    Rocket.Unturned.Chat.UnturnedChat.Say(caller, CFire_Plugin.Instance.Translate("request_accepted") + " " + tpP.CharacterName, Color.yellow);
                    Rocket.Unturned.Chat.UnturnedChat.Say(tpP, player.CharacterName + " " + CFire_Plugin.Instance.Translate("request_accepted_1"), Color.yellow);

					string player1filename = player.CSteamID + ".txt";
					string player2filename = tpP.CSteamID + ".txt";
					string player1SID = player.CSteamID + "";
					string player2SID = tpP.CSteamID + "";

					string player1filename2 = player.CharacterName + ".txt";
					string player2filename2 = tpP.CharacterName + ".txt";
					string player1Name = player.CharacterName + "";
					string player2Name = tpP.CharacterName + "";

					string currentPath = System.IO.Directory.GetCurrentDirectory();

					//write SID files
					using (System.IO.StreamWriter file =
                        new System.IO.StreamWriter(currentPath + Path.DirectorySeparatorChar + "Plugins" + Path.DirectorySeparatorChar + "CFire" + Path.DirectorySeparatorChar + "Players" + Path.DirectorySeparatorChar  + player2filename, true))
					{
						file.WriteLine(player1SID);
						file.Close ();
					}
					using (System.IO.StreamWriter file = 
						new System.IO.StreamWriter(currentPath +Path.DirectorySeparatorChar+"Plugins"+Path.DirectorySeparatorChar+"CFire"+Path.DirectorySeparatorChar+"Players"+Path.DirectorySeparatorChar+ player1filename, true))
					{
						file.WriteLine(player2SID);
						file.Close ();
					}
					//write character name files
					using (System.IO.StreamWriter file = 
						new System.IO.StreamWriter(currentPath +Path.DirectorySeparatorChar+"Plugins"+Path.DirectorySeparatorChar+"CFire"+Path.DirectorySeparatorChar+"Players"+Path.DirectorySeparatorChar+"Characters"+Path.DirectorySeparatorChar+ player2filename2, true))
					{
						file.WriteLine(player1Name);
						file.Close ();
					}
					using (System.IO.StreamWriter file =
                        new System.IO.StreamWriter(currentPath + Path.DirectorySeparatorChar + "Plugins" + Path.DirectorySeparatorChar + "CFire" + Path.DirectorySeparatorChar + "Players" + Path.DirectorySeparatorChar + "Characters"+Path.DirectorySeparatorChar+ player1filename2, true))
					{
						file.WriteLine(player2Name);
						file.Close ();
					}
                }
                else
                {
                    Rocket.Unturned.Chat.UnturnedChat.Say(caller, CFire_Plugin.Instance.Translate("request_none"), Color.red);
                }
               
            }
			else if (command[0].ToString() == "deny"|| command[0].ToString().ToLower() == "d")
            {
                if (!player.HasPermission("cfire.deny"))
                {
                    Rocket.Unturned.Chat.UnturnedChat.Say(player, CFire_Plugin.Instance.Translate("nopermission_deny"), Color.red);
                    return;
                }

                if (requests.ContainsKey(player.CSteamID))
                {
                    UnturnedPlayer tpP = UnturnedPlayer.FromCSteamID(requests[player.CSteamID]);
                    requests.Remove(player.CSteamID);
                    Rocket.Unturned.Chat.UnturnedChat.Say(caller, CFire_Plugin.Instance.Translate("request_denied") + " " + tpP.CharacterName, Color.yellow);
                    Rocket.Unturned.Chat.UnturnedChat.Say(tpP, player.CharacterName + " " + CFire_Plugin.Instance.Translate("request_denied_1"), Color.red);
                }
                else
                {
                    Rocket.Unturned.Chat.UnturnedChat.Say(caller, CFire_Plugin.Instance.Translate("request_none"), Color.red);
                }
            }
            else //Try sending a ceasefire request to a player.
            {
				if (!player.HasPermission("cfire.send"))
                {
                    Rocket.Unturned.Chat.UnturnedChat.Say(player, CFire_Plugin.Instance.Translate("nopermission_send"), Color.red);
                    return;
                }
                UnturnedPlayer rTo = UnturnedPlayer.FromName(command[0].ToString());
                #region Error Checking
				if (rTo == null && command[0].ToString() != "tp" && command[0].ToString() != "list")
                {
	            	Rocket.Unturned.Chat.UnturnedChat.Say(caller, CFire_Plugin.Instance.Translate("playerNotFound"), Color.red);
					Steamworks.CSteamID id;
					CFire_Plug.CFire_Plugin.Instance.link.TryGetValue(command[0].ToString(), out id);
					string currentPathz = System.IO.Directory.GetCurrentDirectory();
					string player5SID = player.CSteamID + "";
					string player5Name = player.CharacterName + "";
					string player5SIDFile = player.CSteamID + ".txt";
					string player5File = player.CharacterName + ".txt";
                    string fileName5SID = currentPathz + Path.DirectorySeparatorChar + "Plugins" + Path.DirectorySeparatorChar + "CFire" + Path.DirectorySeparatorChar + "Players" + Path.DirectorySeparatorChar + player5SIDFile;
                    string fileName5 = currentPathz + Path.DirectorySeparatorChar + "Plugins" + Path.DirectorySeparatorChar + "CFire" + Path.DirectorySeparatorChar + "Players" + Path.DirectorySeparatorChar + "Characters" + Path.DirectorySeparatorChar + player5File;
					string player6SID = id + "";
					string player6Name = command[0].ToString() + "";
					string player6SIDFile = id + ".txt";
					string player6File = command[0].ToString() + ".txt";
                    string fileName6SID = currentPathz + Path.DirectorySeparatorChar + "Plugins" + Path.DirectorySeparatorChar + "CFire" + Path.DirectorySeparatorChar + "Players" + Path.DirectorySeparatorChar + player6SIDFile;
                    string fileName6 = currentPathz + Path.DirectorySeparatorChar + "Plugins" + Path.DirectorySeparatorChar + "CFire" + Path.DirectorySeparatorChar + "Players" + Path.DirectorySeparatorChar + "Characters" + Path.DirectorySeparatorChar + player6File;
					string linez;
					System.IO.StreamReader filez =
						new System.IO.StreamReader (fileName5);
					while ((linez = filez.ReadLine ()) != null) 
					{
						if (linez == player6Name)
						{
							filez.Close ();
							List<string> cfListz = File.ReadAllLines(fileName5).ToList();
							cfListz.Remove(player6Name);
							File.WriteAllLines(fileName5, cfListz.ToArray());

							List<string> cfListx = File.ReadAllLines(fileName5SID).ToList();
							cfListx.Remove(player6SID);
							File.WriteAllLines(fileName5SID, cfListx.ToArray());

							List<string> cfListy = File.ReadAllLines(fileName6).ToList();
							cfListy.Remove(player5Name);
							File.WriteAllLines(fileName6, cfListy.ToArray());

							List<string> cfListw = File.ReadAllLines(fileName6SID).ToList();
							cfListw.Remove(player5SID);
							File.WriteAllLines(fileName6SID, cfListw.ToArray());

							Rocket.Unturned.Chat.UnturnedChat.Say(caller, CFire_Plugin.Instance.Translate("offlineRemove"), Color.red);
							break;
						}
					}
				}
	                
				if (rTo != null && player != null && caller != null) //prevents null exceptions
				{
					//Need to prevent spam requests.
	                if (requests.ContainsKey(rTo.CSteamID))
	                {
	                    if (requests[rTo.CSteamID] == player.CSteamID)
	                    {
							Rocket.Unturned.Chat.UnturnedChat.Say(caller, CFire_Plugin.Instance.Translate("request_pending") + " " + rTo.CharacterName, Color.red);
	                        return;
	                    }
	                }
	                #endregion

	                if (coolDown.ContainsKey(player.CSteamID))
	                {
	                    //Rocket.Unturned.Chat.UnturnedChat.Say(caller, "Debug: " + (DateTime.Now - coolDown[player.CSteamID]).TotalSeconds);
	                    if ((DateTime.Now - coolDown[player.CSteamID]).TotalSeconds < CFire_Plugin.Instance.Configuration.Instance.CFireCoolDownSeconds)
	                    {
	                        Rocket.Unturned.Chat.UnturnedChat.Say(caller, CFire_Plugin.Instance.Translate("error_cooldown"), Color.red);
	                        return;
	                    }
	                    coolDown.Remove(player.CSteamID);
	                }

	                if (coolDown.ContainsKey(player.CSteamID))
	                {
	                    coolDown[player.CSteamID] = DateTime.Now;
	                }
	                else
	                {
	                    coolDown.Add(player.CSteamID, DateTime.Now);
	                }
					UnturnedPlayer tgt = UnturnedPlayer.FromName(command[0].ToString());
					if (player.CharacterName == tgt.CharacterName) 
					{
						Rocket.Unturned.Chat.UnturnedChat.Say(caller, CFire_Plugin.Instance.Translate("request_dopple"), Color.yellow);
						return;
					}
					bool cf = false;
					string currentPath = System.IO.Directory.GetCurrentDirectory();
					string player1SID = player.CSteamID + "";
					string player2SID = tgt.CSteamID + "";
					string player1Name = player.CharacterName + "";
					string player2Name = tgt.CharacterName + "";
					string player1File = player.CSteamID + ".txt";
					string player2File = tgt.CSteamID + ".txt";
					string player3File = player.CharacterName + ".txt";
					string player4File = tgt.CharacterName + ".txt";
                    string fileName1 = currentPath + Path.DirectorySeparatorChar + "Plugins" + Path.DirectorySeparatorChar + "CFire" + Path.DirectorySeparatorChar + "Players" + Path.DirectorySeparatorChar + player1File;
                    string fileName2 = currentPath + Path.DirectorySeparatorChar + "Plugins" + Path.DirectorySeparatorChar + "CFire" + Path.DirectorySeparatorChar + "Players" + Path.DirectorySeparatorChar + player2File;
                    string fileName3 = currentPath + Path.DirectorySeparatorChar + "Plugins" + Path.DirectorySeparatorChar + "CFire" + Path.DirectorySeparatorChar + "Players" + Path.DirectorySeparatorChar + "Characters" + Path.DirectorySeparatorChar + player3File;
                    string fileName4 = currentPath + Path.DirectorySeparatorChar + "Plugins" + Path.DirectorySeparatorChar + "CFire" + Path.DirectorySeparatorChar + "Players" + Path.DirectorySeparatorChar + "Characters" + Path.DirectorySeparatorChar + player4File;
					string line;
					System.IO.StreamReader file =
						new System.IO.StreamReader (fileName1);
					while ((line = file.ReadLine ()) != null) 
					{
						if (line == player2SID) 
						{
							Rocket.Unturned.Chat.UnturnedChat.Say(player, CFire_Plugin.Instance.Translate("cancel_cf") + " " + rTo.CharacterName + " " + CFire_Plugin.Instance.Translate("cancel_cf_1"), Color.yellow);
							Rocket.Unturned.Chat.UnturnedChat.Say(rTo, player.CharacterName + " " + CFire_Plugin.Instance.Translate("cancel_cf_2"), Color.yellow);
							new Thread(() => 
								{
									Thread.CurrentThread.IsBackground = true; 
									while ((DateTime.Now - coolDown[player.CSteamID]).TotalSeconds < CFire_Plugin.Instance.Configuration.Instance.CFireCeaseFireEndSeconds)
									{
										//do nothing for delay
									}
									file.Close ();
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

									Rocket.Unturned.Chat.UnturnedChat.Say(player, CFire_Plugin.Instance.Translate("cf_ended") + rTo.CharacterName + CFire_Plugin.Instance.Translate("cf_ended_1"), Color.red);
									Rocket.Unturned.Chat.UnturnedChat.Say(rTo, CFire_Plugin.Instance.Translate("cf_ended") + player.CharacterName + CFire_Plugin.Instance.Translate("cf_ended_1"), Color.red);
								}).Start();
							cf = true;
							break;
						}
					}
					file.Close ();

					if (player.CharacterName != tgt.CharacterName) 
					{
						if (cf == false)
						{
							if (requests.ContainsKey (rTo.CSteamID)) 
							{
								requests [rTo.CSteamID] = player.CSteamID;
								Rocket.Unturned.Chat.UnturnedChat.Say (caller, CFire_Plugin.Instance.Translate ("request_sent") + " " + rTo.CharacterName, Color.yellow);
								Rocket.Unturned.Chat.UnturnedChat.Say (rTo, player.CharacterName + " " + CFire_Plugin.Instance.Translate ("request_sent_1"), Color.yellow);
							} 
							else 
							{

								requests.Add (rTo.CSteamID, player.CSteamID);
								Rocket.Unturned.Chat.UnturnedChat.Say (caller, CFire_Plugin.Instance.Translate ("request_sent") + " " + rTo.CharacterName, Color.yellow);
								Rocket.Unturned.Chat.UnturnedChat.Say (rTo, player.CharacterName + " " + CFire_Plugin.Instance.Translate ("request_sent_1"), Color.yellow);
							}
						 }
					 }
				}
            }

        }
    }
}
