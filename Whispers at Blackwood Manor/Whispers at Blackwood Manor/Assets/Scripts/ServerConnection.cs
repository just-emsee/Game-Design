using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using NativeWebSocket;
using UnityEngine.UI;

public class ServerConnection : MonoBehaviour
{
    [Header("Server")]
    [SerializeField] private string serverAddress = "ws://localhost:3000";

    [Header("UI")]
    [SerializeField] private TMP_Text playerListText;
    [SerializeField] private TMP_Text hostInstructionText;

    [Header("Managers")]
    [SerializeField] private RoleManager roleManager;
    [SerializeField] private GameUIManager gameUIManager;

    [Header("Buttons")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button nextButton;


    private WebSocket websocket;
    private PlayerData[] currentPlayers = Array.Empty<PlayerData>();
    private PlayerData selectedKiller;
    private MurderData currentMurder;

    private readonly List<string> usedClues = new List<string>();

    private string[] currentWeaponOptions = Array.Empty<string>();
    private string[] currentLocationOptions = Array.Empty<string>();

    private readonly List<string> usedVictimWhispers = new List<string>();
    private readonly List<RoleManager.AssignedPlayerRole> guestsStillWaitingForWhisper = new List<RoleManager.AssignedPlayerRole>();

    private const float victimWhisperCooldownSeconds = 30f;

    private readonly string[] allWeapons =
    {
        "Dagger",
        "Poison Vial",
        "Candlestick",
        "Silk Rope",
        "Fireplace Poker",
        "Broken Mirror Shard"
    };

    private readonly string[] allLocations =
    {
        "Ballroom",
        "Foyer",
        "Conservatory",
        "Kitchen",
        "Dining Room",
        "Library"
    };

    private enum GamePhase
    {
        Lobby,
        KillerAwake,
        KillerChoosingMurder,
        MurderChosen,
        HenchmanAwake,
        RoleScreens
    }

    private GamePhase currentPhase = GamePhase.Lobby;

    [Serializable]
    public class ServerMessage
    {
        public string type;
        public PlayerData[] players;

        public string killerClientId;
        public string victimClientId;
        public string victimName;
        public string weapon;
        public string location;

        public string whisperText;
    }

    [Serializable]
    public class PlayerData
    {
        public string clientId;
        public string name;
        public bool online;
    }

    [Serializable]
    public class MurderData
    {
        public string killerClientId;
        public string killerName;
        public string victimClientId;
        public string victimName;
        public string weapon;
        public string location;
    }

    [Serializable]
    public class SendToClientMessage
    {
        public string type;
        public string targetClientId;
        public PhonePayload payload;
    }

    [Serializable]
    public class PhonePayload
    {
        public string type;
        public string title;
        public string body;

        public VictimOption[] victims;
        public string[] weapons;
        public string[] locations;

        public string killerName;
        public string victimName;
        public string weapon;
        public string location;

        public string visibleRole;
        public string clueText;
        public string playerName;

        public string[] whisperOptions;
        public string whisperText;
        public float cooldownSeconds;
    }

    [Serializable]
    public class VictimOption
    {
        public string clientId;
        public string name;
    }

    private async void Start()
    {
        currentPhase = GamePhase.Lobby;
        UpdateButtons();

        if (hostInstructionText != null)
        {
            hostInstructionText.gameObject.SetActive(false);
        }

        websocket = new WebSocket(serverAddress);

        websocket.OnOpen += () =>
        {
            Debug.Log("Unity connected to server.");

            SetHostInstruction("Lobby ready. Wait for players to join.");
        };

        websocket.OnError += (error) =>
        {
            Debug.LogError("WebSocket error: " + error);
        };

        websocket.OnClose += (closeCode) =>
        {
            Debug.Log("Unity disconnected from server.");
        };

        websocket.OnMessage += (bytes) =>
        {
            string message = Encoding.UTF8.GetString(bytes);
            Debug.Log("Message from server: " + message);
            HandleServerMessage(message);
        };

        await websocket.Connect();
    }

    private void Update()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        websocket?.DispatchMessageQueue();
#endif

        UpdateHostInstructionVisibility();
    }

    private void UpdateHostInstructionVisibility()
    {
        if (hostInstructionText == null)
        {
            return;
        }

        hostInstructionText.gameObject.SetActive(Input.GetKey(KeyCode.H));
    }

    private void HandleServerMessage(string message)
    {
        ServerMessage serverMessage = JsonUtility.FromJson<ServerMessage>(message);

        if (serverMessage.type == "playerList")
        {
            currentPlayers = serverMessage.players;
            UpdatePlayerList(currentPlayers);
        }

        if (serverMessage.type == "murderSubmitted")
        {
            HandleMurderSubmitted(serverMessage);
        }

        if (serverMessage.type == "submitVictimWhisper")
        {
            HandleVictimWhisperSubmitted(serverMessage);
        }

        if (serverMessage.type == "requestNextVictimWhisper")
        {
            SendWhisperChoicesToVictim();
        }
    }

    private void HandleMurderSubmitted(ServerMessage serverMessage)
    {
        currentPhase = GamePhase.MurderChosen;

        if (roleManager != null && roleManager.GetHenchmanRole() != null)
        {
            SetNextButtonText("Wake Henchman");
        }
        else
        {
            SetNextButtonText("Continue");
        }

        UpdateButtons();

        currentMurder = new MurderData
        {
            killerClientId = serverMessage.killerClientId,
            killerName = selectedKiller != null ? selectedKiller.name : "Unknown",
            victimClientId = serverMessage.victimClientId,
            victimName = serverMessage.victimName,
            weapon = serverMessage.weapon,
            location = serverMessage.location
        };

        if (roleManager != null)
        {
            roleManager.SetVictim(currentMurder.victimClientId);
        }

        SetHostInstruction(
            "Murder chosen successfully.\n\n" +
            "Killer: " + currentMurder.killerName + "\n" +
            "Victim: " + currentMurder.victimName + "\n" +
            "Weapon: " + currentMurder.weapon + "\n" +
            "Location: " + currentMurder.location + "\n\n" +
            "Tell the killer to close their eyes again."
        );

        Debug.Log(
            "Murder chosen: " +
            currentMurder.killerName + " killed " +
            currentMurder.victimName + " with " +
            currentMurder.weapon + " in " +
            currentMurder.location
        );
    }

    private void UpdatePlayerList(PlayerData[] players)
    {
        if (playerListText == null)
        {
            Debug.LogWarning("PlayerListText is not assigned.");
            return;
        }

        string text = "Players joined:\n\n";

        foreach (PlayerData player in players)
        {
            if (player.online)
            {
                text += "- " + player.name + "\n";
            }
            else
            {
                text += "- " + player.name + " (offline)\n";
            }
        }

        playerListText.text = text;
    }

    private void UpdateButtons()
    {
        if (playButton != null)
        {
            playButton.gameObject.SetActive(currentPhase == GamePhase.Lobby);
        }

        if (nextButton != null)
        {
            bool showNext =
                currentPhase == GamePhase.KillerAwake ||
                currentPhase == GamePhase.MurderChosen ||
                currentPhase == GamePhase.HenchmanAwake;

            nextButton.gameObject.SetActive(showNext);
        }
    }

    private void SetNextButtonText(string text)
    {
        if (nextButton == null)
        {
            return;
        }

        TMP_Text buttonText = nextButton.GetComponentInChildren<TMP_Text>();

        if (buttonText != null)
        {
            buttonText.text = text;
        }
    }

    public void OnNextButtonPressed()
    {
        if (currentPhase == GamePhase.KillerAwake)
        {
            ShowMurderChoiceToKiller();
            return;
        }

        if (currentPhase == GamePhase.MurderChosen)
        {
            if (roleManager != null && roleManager.GetHenchmanRole() != null)
            {
                ShowHenchmanInfoIfNeeded();
            }
            else
            {
                SendRoleScreensToEveryone();
            }

            return;
        }

        if (currentPhase == GamePhase.HenchmanAwake)
        {
            SendRoleScreensToEveryone();
            return;
        }

        SetHostInstruction("Nothing to do in this phase: " + currentPhase);
    }

    public async void SelectKillerAndWake()
    {

        if (websocket == null || websocket.State != WebSocketState.Open)
        {
            Debug.LogWarning("WebSocket is not connected.");
            return;
        }

        PlayerData[] onlinePlayers = GetOnlinePlayers();

        if (onlinePlayers.Length < 2)
        {
            SetHostInstruction("Need at least 2 online players.");
            return;
        }

        currentPhase = GamePhase.KillerAwake;
        SetNextButtonText("Next");
        UpdateButtons();

        selectedKiller = onlinePlayers[UnityEngine.Random.Range(0, onlinePlayers.Length)];

        if (roleManager != null)
        {
            roleManager.CreateRolePool(onlinePlayers, selectedKiller);
        }
        else
        {
            Debug.LogWarning("RoleManager is not assigned.");
        }

        string henchmanText = "\nNo Henchman this round.";

        if (roleManager != null && roleManager.GetHenchmanRole() != null)
        {
            henchmanText = "\nHenchman this round: " + roleManager.GetHenchmanRole().playerName;
        }

        SetHostInstruction(
            "Everyone close your eyes.\n\n" +
            "Killer selected: " + selectedKiller.name +
            henchmanText +
            "\n\nTap the Killer's shoulder.\n\n" +
            "After they wake up, press Next."
        );

        SendToClientMessage message = new SendToClientMessage
        {
            type = "sendToClient",
            targetClientId = selectedKiller.clientId,
            payload = new PhonePayload
            {
                type = "showKillerWake",
                title = "You are the Killer",
                body = "Open your eyes quietly."
            }
        };

        string json = JsonUtility.ToJson(message);
        await websocket.SendText(json);

        Debug.Log("Killer selected: " + selectedKiller.name);
    }

    public async void ShowMurderChoiceToKiller()
    {
        currentPhase = GamePhase.KillerChoosingMurder;
        UpdateButtons();

        if (websocket == null || websocket.State != WebSocketState.Open)
        {
            Debug.LogWarning("WebSocket is not connected.");
            return;
        }

        if (selectedKiller == null)
        {
            SetHostInstruction("No killer selected yet. Press Play first.");
            return;
        }

        PlayerData[] onlinePlayers = GetOnlinePlayers();
        List<PlayerData> possibleVictims = new List<PlayerData>();

        string henchmanClientId = null;

        if (roleManager != null && roleManager.GetHenchmanRole() != null)
        {
            henchmanClientId = roleManager.GetHenchmanRole().clientId;
        }

        foreach (PlayerData player in onlinePlayers)
        {
            bool isKiller = player.clientId == selectedKiller.clientId;
            bool isHenchman = henchmanClientId != null && player.clientId == henchmanClientId;

            if (!isKiller && !isHenchman)
            {
                possibleVictims.Add(player);
            }
        }

        if (possibleVictims.Count == 0)
        {
            SetHostInstruction(
                "No valid victims available.\n\n" +
                "The Killer cannot murder themselves or the Henchman."
            );

            currentPhase = GamePhase.KillerAwake;
            SetNextButtonText("Next");
            UpdateButtons();
            return;
        }

        Shuffle(possibleVictims);

        int victimCount = Mathf.Min(3, possibleVictims.Count);
        VictimOption[] victimOptions = new VictimOption[victimCount];

        for (int i = 0; i < victimCount; i++)
        {
            victimOptions[i] = new VictimOption
            {
                clientId = possibleVictims[i].clientId,
                name = possibleVictims[i].name
            };
        }

        string[] weaponOptions = PickRandomStrings(allWeapons, 3);
        string[] locationOptions = PickRandomStrings(allLocations, 3);
        currentWeaponOptions = weaponOptions;
        currentLocationOptions = locationOptions;

        SendToClientMessage message = new SendToClientMessage
        {
            type = "sendToClient",
            targetClientId = selectedKiller.clientId,
            payload = new PhonePayload
            {
                type = "showMurderChoice",
                victims = victimOptions,
                weapons = weaponOptions,
                locations = locationOptions
            }
        };

        string json = JsonUtility.ToJson(message);
        await websocket.SendText(json);

        SetHostInstruction(
            "Killer is choosing the murder.\n\n" +
            "Wait until the phone says Murder Confirmed."
        );

        Debug.Log("Sent murder choice screen to killer.");
    }

    public async void ShowHenchmanInfoIfNeeded()
    {
        if (currentMurder == null)
        {
            SetHostInstruction("No murder has been chosen yet.");
            return;
        }

        if (roleManager == null || roleManager.GetHenchmanRole() == null)
        {
            SetHostInstruction(
                "No Henchman this round.\n\n" +
                "Next step: send role/disguise screens."
            );

            currentPhase = GamePhase.RoleScreens;
            UpdateButtons();

            return;
        }

        RoleManager.AssignedPlayerRole henchmanRole = roleManager.GetHenchmanRole();

        SendToClientMessage message = new SendToClientMessage
        {
            type = "sendToClient",
            targetClientId = henchmanRole.clientId,
            payload = new PhonePayload
            {
                type = "showHenchmanWake",
                title = "You are the Henchman",
                body = "Protect the Killer.",

                killerName = currentMurder.killerName,
                victimName = currentMurder.victimName,
                weapon = currentMurder.weapon,
                location = currentMurder.location
            }
        };

        string json = JsonUtility.ToJson(message);
        await websocket.SendText(json);

        currentPhase = GamePhase.HenchmanAwake;
        SetNextButtonText("Continue");
        UpdateButtons();

        SetHostInstruction(
            "Wake the Henchman.\n\n" +
            "Henchman: " + henchmanRole.playerName + "\n" +
            "Killer: " + currentMurder.killerName + "\n" +
            "Victim: " + currentMurder.victimName + "\n" +
            "Weapon: " + currentMurder.weapon + "\n" +
            "Location: " + currentMurder.location + "\n\n" +
            "After they read their screen, tell Killer and Henchman to close their eyes again."
        );
    }

    public async void SendRoleScreensToEveryone()
    {
        if (roleManager == null)
        {
            SetHostInstruction("RoleManager is not assigned.");
            return;
        }

        if (currentMurder == null)
        {
            SetHostInstruction("No murder has been chosen yet.");
            return;
        }

        usedClues.Clear();

        foreach (RoleManager.AssignedPlayerRole role in roleManager.AssignedRoles)
        {
            PhonePayload payload;

            if (role.secretState == RoleManager.SecretState.Victim)
            {
                payload = new PhonePayload
                {
                    type = "showVictimDead",
                    playerName = role.playerName,
                    title = "You were murdered",
                    body = "You may not speak."
                };
            }
            else
            {
                payload = new PhonePayload
                {
                    type = "showRoleScreen",
                    playerName = role.playerName,
                    visibleRole = role.visibleRole.ToString(),
                    clueText = MakeUniqueClue(() => GenerateClueForRole(role))
                };
            }

            SendToClientMessage message = new SendToClientMessage
            {
                type = "sendToClient",
                targetClientId = role.clientId,
                payload = payload
            };

            string json = JsonUtility.ToJson(message);
            await websocket.SendText(json);
        }

        currentPhase = GamePhase.RoleScreens;
        UpdateButtons();

        if (gameUIManager != null)
        {
            gameUIManager.ShowDiscussionScreen(
                currentMurder,
                roleManager != null ? roleManager.AssignedRoles : null
            );
        }
        else
        {
            Debug.LogWarning("GameUIManager is not assigned.");
        }

        SetHostInstruction(
            "Role screens sent.\n\n" +
            "Killer and Henchman now see their disguise roles.\n" +
            "Victim sees that they are dead.\n\n" +
            "Next step: everyone opens their eyes and discussion starts."
        );

        PrepareVictimWhisperSystem();
        SendWhisperChoicesToVictim();
    }

    private PlayerData[] GetOnlinePlayers()
    {
        int count = 0;

        foreach (PlayerData player in currentPlayers)
        {
            if (player.online)
            {
                count++;
            }
        }

        PlayerData[] onlinePlayers = new PlayerData[count];
        int index = 0;

        foreach (PlayerData player in currentPlayers)
        {
            if (player.online)
            {
                onlinePlayers[index] = player;
                index++;
            }
        }

        return onlinePlayers;
    }

    private string[] PickRandomStrings(string[] source, int amount)
    {
        List<string> list = new List<string>(source);
        Shuffle(list);

        int count = Mathf.Min(amount, list.Count);
        string[] result = new string[count];

        for (int i = 0; i < count; i++)
        {
            result[i] = list[i];
        }

        return result;
    }

    private void Shuffle<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int randomIndex = UnityEngine.Random.Range(i, list.Count);
            T temp = list[i];
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }

    private void SetHostInstruction(string message)
    {
        if (hostInstructionText != null)
        {
            hostInstructionText.text = message;
        }
    }

    private async void OnApplicationQuit()
    {
        if (websocket != null)
        {
            await websocket.Close();
        }
    }

    private string GenerateClueForRole(RoleManager.AssignedPlayerRole role)
    {
        bool truthfulInfo =
            role.secretState == RoleManager.SecretState.Innocent ||
            role.secretState == RoleManager.SecretState.Shapeshifter;

        switch (role.visibleRole)
        {
            case RoleManager.VisibleRole.Detective:
                return GenerateDetectiveClue(truthfulInfo);

            case RoleManager.VisibleRole.Witness:
                return GenerateWitnessClue(truthfulInfo);

            case RoleManager.VisibleRole.Butler:
                return GenerateButlerClue(truthfulInfo);

            case RoleManager.VisibleRole.Scholar:
                return GenerateScholarClue(truthfulInfo);

            case RoleManager.VisibleRole.Medium:
                return GenerateMediumClue(truthfulInfo);

            case RoleManager.VisibleRole.Guest:
                return "You have no special clue. Listen carefully, ask questions, and vote wisely.";

            default:
                return "No clue.";
        }
    }

    private string GenerateMediumClue(bool truthfulInfo)
    {
        int clueType = UnityEngine.Random.Range(0, 2);

        if (clueType == 0)
        {
            return GenerateNotLocationClue(truthfulInfo);
        }

        return GeneratePossibleLocationsClue(truthfulInfo);
    }

    private string GenerateDetectiveClue(bool truthfulInfo)
    {
        return GenerateDetectiveHiddenGroupClue(truthfulInfo);
    }

    private string GenerateDetectiveHiddenGroupClue(bool truthfulInfo)
    {
        List<RoleManager.AssignedPlayerRole> hiddenPlayers = new List<RoleManager.AssignedPlayerRole>();
        List<RoleManager.AssignedPlayerRole> normalPlayers = new List<RoleManager.AssignedPlayerRole>();
        List<RoleManager.AssignedPlayerRole> allValidPlayers = new List<RoleManager.AssignedPlayerRole>();

        foreach (RoleManager.AssignedPlayerRole role in roleManager.AssignedRoles)
        {
            if (role.secretState == RoleManager.SecretState.Victim)
            {
                continue;
            }

            allValidPlayers.Add(role);

            bool isHidingSomething =
                role.secretState == RoleManager.SecretState.Killer ||
                role.secretState == RoleManager.SecretState.Henchman ||
                role.secretState == RoleManager.SecretState.Cursed ||
                role.secretState == RoleManager.SecretState.Shapeshifter;

            if (isHidingSomething)
            {
                hiddenPlayers.Add(role);
            }
            else
            {
                normalPlayers.Add(role);
            }
        }

        Shuffle(hiddenPlayers);
        Shuffle(normalPlayers);
        Shuffle(allValidPlayers);

        List<RoleManager.AssignedPlayerRole> shownPlayers = new List<RoleManager.AssignedPlayerRole>();

        if (truthfulInfo && hiddenPlayers.Count > 0)
        {
            shownPlayers.Add(hiddenPlayers[0]);

            foreach (RoleManager.AssignedPlayerRole role in allValidPlayers)
            {
                if (shownPlayers.Count >= 3)
                {
                    break;
                }

                if (!shownPlayers.Contains(role))
                {
                    shownPlayers.Add(role);
                }
            }
        }
        else
        {
            // False clue: preferably show 3 normal players, then claim someone is hiding.
            foreach (RoleManager.AssignedPlayerRole role in normalPlayers)
            {
                if (shownPlayers.Count >= 3)
                {
                    break;
                }

                shownPlayers.Add(role);
            }

            foreach (RoleManager.AssignedPlayerRole role in allValidPlayers)
            {
                if (shownPlayers.Count >= 3)
                {
                    break;
                }

                if (!shownPlayers.Contains(role))
                {
                    shownPlayers.Add(role);
                }
            }
        }

        Shuffle(shownPlayers);

        string clue = "You investigated three people:\n\n";

        for (int i = 0; i < shownPlayers.Count && i < 3; i++)
        {
            RoleManager.AssignedPlayerRole target = shownPlayers[i];

            clue += target.playerName + " = " + target.visibleRole + "\n";
        }

        clue += "\nAt least one of them is hiding something.";

        return clue;
    }

    private string GenerateWitnessClue(bool truthfulInfo)
    {
        int clueType = UnityEngine.Random.Range(0, 4);

        switch (clueType)
        {
            case 0:
                return GenerateNotWeaponClue(truthfulInfo);

            case 1:
                return GeneratePossibleWeaponsClue(truthfulInfo);

            case 2:
                return GenerateNotLocationClue(truthfulInfo);

            case 3:
                return GeneratePossibleLocationsClue(truthfulInfo);

            default:
                return GenerateNotWeaponClue(truthfulInfo);
        }
    }

    private string GenerateButlerClue(bool truthfulInfo)
    {
        RoleManager.AssignedPlayerRole killer = roleManager.GetKillerRole();

        List<RoleManager.AssignedPlayerRole> safePlayers = new List<RoleManager.AssignedPlayerRole>();
        List<RoleManager.AssignedPlayerRole> allValidPlayers = new List<RoleManager.AssignedPlayerRole>();

        foreach (RoleManager.AssignedPlayerRole role in roleManager.AssignedRoles)
        {
            if (role.secretState == RoleManager.SecretState.Victim)
            {
                continue;
            }

            allValidPlayers.Add(role);

            if (killer != null && role.clientId != killer.clientId)
            {
                safePlayers.Add(role);
            }
        }

        List<RoleManager.AssignedPlayerRole> source = truthfulInfo ? safePlayers : allValidPlayers;
        Shuffle(source);

        List<RoleManager.AssignedPlayerRole> shownPlayers = new List<RoleManager.AssignedPlayerRole>();

        foreach (RoleManager.AssignedPlayerRole role in source)
        {
            if (shownPlayers.Count >= 3)
            {
                break;
            }

            shownPlayers.Add(role);
        }

        if (shownPlayers.Count < 3)
        {
            return "You could not confirm enough alibis.";
        }

        return "The Killer is not one of these people:\n\n" +
               shownPlayers[0].playerName + ", " +
               shownPlayers[1].playerName + ", " +
               shownPlayers[2].playerName;
    }

    private string GenerateScholarClue(bool truthfulInfo)
    {
        return GenerateKillerDisguiseOptionsClue(truthfulInfo);
    }

    private string GenerateKillerDisguiseOptionsClue(bool truthfulInfo)
    {
        RoleManager.AssignedPlayerRole killer = roleManager.GetKillerRole();

        if (killer == null)
        {
            return "The Killer's disguise could not be read.";
        }

        List<RoleManager.VisibleRole> allRoles = new List<RoleManager.VisibleRole>
    {
        RoleManager.VisibleRole.Detective,
        RoleManager.VisibleRole.Witness,
        RoleManager.VisibleRole.Butler,
        RoleManager.VisibleRole.Scholar,
        RoleManager.VisibleRole.Medium,
        RoleManager.VisibleRole.Guest
    };

        Shuffle(allRoles);

        List<RoleManager.VisibleRole> shownRoles = new List<RoleManager.VisibleRole>();

        if (truthfulInfo)
        {
            shownRoles.Add(killer.visibleRole);

            foreach (RoleManager.VisibleRole role in allRoles)
            {
                if (shownRoles.Count >= 3)
                {
                    break;
                }

                if (role != killer.visibleRole)
                {
                    shownRoles.Add(role);
                }
            }
        }
        else
        {
            foreach (RoleManager.VisibleRole role in allRoles)
            {
                if (shownRoles.Count >= 3)
                {
                    break;
                }

                if (role != killer.visibleRole)
                {
                    shownRoles.Add(role);
                }
            }
        }

        Shuffle(shownRoles);

        return "The Killer is disguised as one of these roles:\n\n" +
               shownRoles[0] + ", " + shownRoles[1] + ", " + shownRoles[2];
    }

    private string GetRandomWrongWeapon(string realWeapon)
    {
        List<string> options = new List<string>(allWeapons);
        options.Remove(realWeapon);
        return options[UnityEngine.Random.Range(0, options.Count)];
    }

    private string GetRandomWrongLocation(string realLocation)
    {
        List<string> options = new List<string>(allLocations);
        options.Remove(realLocation);
        return options[UnityEngine.Random.Range(0, options.Count)];
    }

    private string GetRandomRoleName()
    {
        string[] roleNames =
        {
        "Detective",
        "Witness",
        "Butler",
        "Scholar",
        "Medium",
        "Guest",
        "Shapeshifter"
    };

        return roleNames[UnityEngine.Random.Range(0, roleNames.Length)];
    }

    private string GenerateNotWeaponClue(bool truthfulInfo)
    {
        string weapon = truthfulInfo
            ? GetRandomWrongWeapon(currentMurder.weapon)
            : currentMurder.weapon;

        return "The weapon was not:\n\n" + weapon;
    }

    private string GeneratePossibleWeaponsClue(bool truthfulInfo)
    {
        List<string> weapons = new List<string>(allWeapons);
        Shuffle(weapons);

        List<string> shownWeapons = new List<string>();

        if (truthfulInfo)
        {
            shownWeapons.Add(currentMurder.weapon);

            foreach (string weapon in weapons)
            {
                if (shownWeapons.Count >= 3)
                {
                    break;
                }

                if (weapon != currentMurder.weapon)
                {
                    shownWeapons.Add(weapon);
                }
            }
        }
        else
        {
            foreach (string weapon in weapons)
            {
                if (shownWeapons.Count >= 3)
                {
                    break;
                }

                if (weapon != currentMurder.weapon)
                {
                    shownWeapons.Add(weapon);
                }
            }
        }

        Shuffle(shownWeapons);

        return "The weapon was one of these:\n\n" +
               shownWeapons[0] + ", " + shownWeapons[1] + ", " + shownWeapons[2];
    }

    private string GenerateNotLocationClue(bool truthfulInfo)
    {
        string location = truthfulInfo
            ? GetRandomWrongLocation(currentMurder.location)
            : currentMurder.location;

        return "The murder did not happen in:\n\n" + location;
    }

    private string GeneratePossibleLocationsClue(bool truthfulInfo)
    {
        List<string> locations = new List<string>(allLocations);
        Shuffle(locations);

        List<string> shownLocations = new List<string>();

        if (truthfulInfo)
        {
            shownLocations.Add(currentMurder.location);

            foreach (string location in locations)
            {
                if (shownLocations.Count >= 3)
                {
                    break;
                }

                if (location != currentMurder.location)
                {
                    shownLocations.Add(location);
                }
            }
        }
        else
        {
            foreach (string location in locations)
            {
                if (shownLocations.Count >= 3)
                {
                    break;
                }

                if (location != currentMurder.location)
                {
                    shownLocations.Add(location);
                }
            }
        }

        Shuffle(shownLocations);

        return "The murder happened in one of these rooms:\n\n" +
               shownLocations[0] + ", " + shownLocations[1] + ", " + shownLocations[2];
    }

    private string MakeUniqueClue(Func<string> clueGenerator, int maxAttempts = 20)
    {
        for (int i = 0; i < maxAttempts; i++)
        {
            string clue = clueGenerator();

            if (!usedClues.Contains(clue))
            {
                usedClues.Add(clue);
                return clue;
            }
        }

        string fallbackClue = clueGenerator();
        usedClues.Add(fallbackClue);
        return fallbackClue;
    }

    private void PrepareVictimWhisperSystem()
    {
        usedVictimWhispers.Clear();
        guestsStillWaitingForWhisper.Clear();

        if (roleManager == null)
        {
            return;
        }

        foreach (RoleManager.AssignedPlayerRole role in roleManager.AssignedRoles)
        {
            if (role.secretState == RoleManager.SecretState.Victim)
            {
                continue;
            }

            if (role.isActiveRolePlayer &&
                role.visibleRole == RoleManager.VisibleRole.Guest &&
                role.secretState == RoleManager.SecretState.Innocent)
            {
                guestsStillWaitingForWhisper.Add(role);
            }
        }

        Shuffle(guestsStillWaitingForWhisper);

        Debug.Log("Guests available for victim whispers: " + guestsStillWaitingForWhisper.Count);
    }

    private async void SendWhisperChoicesToVictim()
    {
        if (currentMurder == null)
        {
            return;
        }

        string[] whisperOptions = GenerateVictimWhisperOptions();

        if (guestsStillWaitingForWhisper.Count == 0)
        {
            await SendPhonePayload(
                currentMurder.victimClientId,
                new PhonePayload
                {
                    type = "showVictimWhisperChoice",
                    playerName = currentMurder.victimName,
                    title = "Send a Final Whisper",
                    body = "Choose one clue. A random Guest will receive it.",
                    whisperOptions = whisperOptions,
                    cooldownSeconds = victimWhisperCooldownSeconds
                }
            );

            return;
        }

        await SendPhonePayload(
            currentMurder.victimClientId,
            new PhonePayload
            {
                type = "showVictimWhisperChoice",
                title = "Send a Final Whisper",
                body = "Choose one clue. A random Guest will receive it.",
                whisperOptions = whisperOptions,
                cooldownSeconds = victimWhisperCooldownSeconds
            }
        );
    }

    private string[] GenerateVictimWhisperOptions()
    {
        List<string> options = new List<string>();

        AddUniqueWhisperOption(options, GenerateVictimSuspiciousGroupWhisper);
        AddUniqueWhisperOption(options, GenerateVictimKillerDisguiseWhisper);
        AddUniqueWhisperOption(options, GenerateVictimWeaponOrRoomWhisper);

        return options.ToArray();
    }

    private void AddUniqueWhisperOption(List<string> options, Func<string> generator)
    {
        int safety = 0;

        while (safety < 30)
        {
            safety++;

            string whisper = generator();

            if (usedVictimWhispers.Contains(whisper))
            {
                continue;
            }

            if (options.Contains(whisper))
            {
                continue;
            }

            options.Add(whisper);
            return;
        }

        options.Add("The victim's memory is unclear.");
    }

    private string GenerateVictimWeaponOrRoomWhisper()
    {
        int clueType = UnityEngine.Random.Range(0, 4);

        switch (clueType)
        {
            case 0:
                return "The weapon was not:\n\n" + GetRandomWrongWeapon(currentMurder.weapon);

            case 1:
                return GenerateVictimPossibleWeaponsWhisper();

            case 2:
                return "The murder did not happen in:\n\n" + GetRandomWrongLocation(currentMurder.location);

            case 3:
                return GenerateVictimPossibleLocationsWhisper();

            default:
                return "The victim's memory is unclear.";
        }
    }

    private string GenerateVictimPossibleWeaponsWhisper()
    {
        List<string> weapons = new List<string>(allWeapons);
        Shuffle(weapons);

        List<string> shownWeapons = new List<string>();
        shownWeapons.Add(currentMurder.weapon);

        foreach (string weapon in weapons)
        {
            if (shownWeapons.Count >= 3)
            {
                break;
            }

            if (weapon != currentMurder.weapon)
            {
                shownWeapons.Add(weapon);
            }
        }

        Shuffle(shownWeapons);

        return "The weapon was one of these:\n\n" +
               shownWeapons[0] + ", " + shownWeapons[1] + ", " + shownWeapons[2];
    }

    private string GenerateVictimPossibleLocationsWhisper()
    {
        List<string> locations = new List<string>(allLocations);
        Shuffle(locations);

        List<string> shownLocations = new List<string>();
        shownLocations.Add(currentMurder.location);

        foreach (string location in locations)
        {
            if (shownLocations.Count >= 3)
            {
                break;
            }

            if (location != currentMurder.location)
            {
                shownLocations.Add(location);
            }
        }

        Shuffle(shownLocations);

        return "The murder happened in one of these rooms:\n\n" +
               shownLocations[0] + ", " + shownLocations[1] + ", " + shownLocations[2];
    }

    private string GenerateSingleVictimWhisper()
    {
        int clueType = UnityEngine.Random.Range(0, 5);

        switch (clueType)
        {
            case 0:
                return "The weapon was not:\n\n" + GetRandomWrongWeapon(currentMurder.weapon);

            case 1:
                return "The murder did not happen in:\n\n" + GetRandomWrongLocation(currentMurder.location);

            case 2:
                return GenerateVictimKillerDisguiseWhisper();

            case 3:
                return GenerateVictimSafePlayerWhisper();

            case 4:
                return GenerateVictimSuspiciousGroupWhisper();

            default:
                return "The victim's memory is unclear.";
        }
    }

    private string GenerateVictimKillerDisguiseWhisper()
    {
        RoleManager.AssignedPlayerRole killer = roleManager.GetKillerRole();

        if (killer == null)
        {
            return "The Killer's disguise is unclear.";
        }

        List<RoleManager.VisibleRole> roles = new List<RoleManager.VisibleRole>
    {
        RoleManager.VisibleRole.Detective,
        RoleManager.VisibleRole.Witness,
        RoleManager.VisibleRole.Butler,
        RoleManager.VisibleRole.Scholar,
        RoleManager.VisibleRole.Medium
    };

        Shuffle(roles);

        List<RoleManager.VisibleRole> shownRoles = new List<RoleManager.VisibleRole>();
        shownRoles.Add(killer.visibleRole);

        foreach (RoleManager.VisibleRole role in roles)
        {
            if (shownRoles.Count >= 3)
            {
                break;
            }

            if (role != killer.visibleRole)
            {
                shownRoles.Add(role);
            }
        }

        Shuffle(shownRoles);

        return "The Killer is disguised as one of these roles:\n\n" +
               shownRoles[0] + ", " + shownRoles[1] + ", " + shownRoles[2];
    }

    private string GenerateVictimSafePlayerWhisper()
    {
        RoleManager.AssignedPlayerRole killer = roleManager.GetKillerRole();

        List<RoleManager.AssignedPlayerRole> safePlayers = new List<RoleManager.AssignedPlayerRole>();

        foreach (RoleManager.AssignedPlayerRole role in roleManager.AssignedRoles)
        {
            if (role.secretState == RoleManager.SecretState.Victim)
            {
                continue;
            }

            if (killer != null && role.clientId != killer.clientId)
            {
                safePlayers.Add(role);
            }
        }

        Shuffle(safePlayers);

        int count = Mathf.Min(2, safePlayers.Count);

        if (count < 2)
        {
            return "The victim could not identify safe people.";
        }

        return "These people are not the Killer:\n\n" +
               safePlayers[0].playerName + ", " + safePlayers[1].playerName;
    }

    private string GenerateVictimSuspiciousGroupWhisper()
    {
        RoleManager.AssignedPlayerRole killer = roleManager.GetKillerRole();

        if (killer == null)
        {
            return "The victim could not sense the Killer.";
        }

        List<RoleManager.AssignedPlayerRole> candidates = new List<RoleManager.AssignedPlayerRole>();

        foreach (RoleManager.AssignedPlayerRole role in roleManager.AssignedRoles)
        {
            if (role.secretState == RoleManager.SecretState.Victim)
            {
                continue;
            }

            candidates.Add(role);
        }

        Shuffle(candidates);

        List<RoleManager.AssignedPlayerRole> shown = new List<RoleManager.AssignedPlayerRole>();
        shown.Add(killer);

        foreach (RoleManager.AssignedPlayerRole role in candidates)
        {
            if (shown.Count >= 3)
            {
                break;
            }

            if (role.clientId != killer.clientId)
            {
                shown.Add(role);
            }
        }

        Shuffle(shown);

        return "One of these people is the Killer:\n\n" +
               shown[0].playerName + ", " +
               shown[1].playerName + ", " +
               shown[2].playerName;
    }

    private async void HandleVictimWhisperSubmitted(ServerMessage serverMessage)
    {
        if (currentMurder == null)
        {
            return;
        }

        if (guestsStillWaitingForWhisper.Count == 0)
        {
            SendWhisperChoicesToVictim();
            return;
        }

        string chosenWhisper = serverMessage.whisperText;

        if (string.IsNullOrWhiteSpace(chosenWhisper))
        {
            return;
        }

        usedVictimWhispers.Add(chosenWhisper);

        RoleManager.AssignedPlayerRole targetGuest = guestsStillWaitingForWhisper[0];
        guestsStillWaitingForWhisper.RemoveAt(0);

        await SendPhonePayload(
            targetGuest.clientId,
            new PhonePayload
            {
                type = "showGuestVictimWhisper",
                playerName = targetGuest.playerName,
                title = "Victim's Whisper",
                whisperText = chosenWhisper
            }
        );

        await SendPhonePayload(
            currentMurder.victimClientId,
            new PhonePayload
            {
                type = "showVictimWhisperCooldown",
                playerName = currentMurder.victimName,
                title = "Whisper Sent",
                body = "Your whisper was sent to a Guest.",
                cooldownSeconds = victimWhisperCooldownSeconds
            }
        );

        Debug.Log("Victim whisper sent to " + targetGuest.playerName + ": " + chosenWhisper);
    }

    private async System.Threading.Tasks.Task SendPhonePayload(string targetClientId, PhonePayload payload)
    {
        if (websocket == null || websocket.State != WebSocketState.Open)
        {
            Debug.LogWarning("WebSocket is not connected.");
            return;
        }

        SendToClientMessage message = new SendToClientMessage
        {
            type = "sendToClient",
            targetClientId = targetClientId,
            payload = payload
        };

        string json = JsonUtility.ToJson(message);
        await websocket.SendText(json);
    }
}