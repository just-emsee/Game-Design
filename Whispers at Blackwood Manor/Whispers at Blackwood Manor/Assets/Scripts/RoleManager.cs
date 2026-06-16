using System;
using System.Collections.Generic;
using UnityEngine;

public class RoleManager : MonoBehaviour
{
    public enum VisibleRole
    {
        Detective,
        Witness,
        Butler,
        Scholar,
        Medium,
        Guest
    }

    public enum SecretState
    {
        Innocent,
        Killer,
        Henchman,
        Cursed,
        Shapeshifter,
        Victim
    }

    [Serializable]
    public class AssignedPlayerRole
    {
        public string clientId;
        public string playerName;
        public VisibleRole visibleRole;
        public SecretState secretState;
        public bool isActiveRolePlayer;
    }

    [Header("Hidden Role Settings")]
    [SerializeField] private bool includeHenchman = true;
    [SerializeField] private bool includeCursed = true;
    [SerializeField] private bool includeShapeshifter = true;

    [Header("Role Count Settings")]
    [SerializeField] private int activeRolePlayerCount = 13;
    [SerializeField] private int requiredGuestCount = 3;

    [Header("Debug Role Generation")]
    [SerializeField] private bool enableDebugRoleGeneration = true;
    [SerializeField] private KeyCode debugGenerateKey = KeyCode.R;

    [SerializeField]
    private string[] debugPlayerNames =
{
    "Arthur",
    "Tycho",
    "Emma",
    "Bram",
    "Lisa",
    "Noah",
    "Milan",
    "Sophie",
    "Daan",
    "Lars",
    "Nina",
    "Julia",
    "Sam",
    "Max",
    "Eva",
    "Lucas"
    };

    private readonly List<AssignedPlayerRole> assignedRoles = new List<AssignedPlayerRole>();

    public IReadOnlyList<AssignedPlayerRole> AssignedRoles => assignedRoles;

    public AssignedPlayerRole SelectedHenchman { get; private set; }

    public void CreateRolePool(ServerConnection.PlayerData[] onlinePlayers, ServerConnection.PlayerData selectedKiller)
    {
        assignedRoles.Clear();
        SelectedHenchman = null;

        if (onlinePlayers == null || onlinePlayers.Length == 0)
        {
            Debug.LogWarning("Cannot create roles: no online players.");
            return;
        }

        if (selectedKiller == null)
        {
            Debug.LogWarning("Cannot create roles: no selected killer.");
            return;
        }

        CreateBaseRoles(onlinePlayers);

        List<AssignedPlayerRole> activePlayers = PickActiveRolePlayers(selectedKiller.clientId);

        AssignKiller(selectedKiller);
        AssignSpecialHiddenStates(activePlayers);
        AssignNormalVisibleRolesToRemainingPlayers(activePlayers);

        DebugAssignedRoles();
    }

    private List<AssignedPlayerRole> PickActiveRolePlayers(string killerClientId)
    {
        List<AssignedPlayerRole> allPlayers = new List<AssignedPlayerRole>(assignedRoles);
        Shuffle(allPlayers);

        int targetCount = Mathf.Clamp(activeRolePlayerCount, 1, assignedRoles.Count);

        AssignedPlayerRole killerRole = GetAssignedRole(killerClientId);

        List<AssignedPlayerRole> activePlayers = new List<AssignedPlayerRole>();

        if (killerRole != null)
        {
            killerRole.isActiveRolePlayer = true;
            activePlayers.Add(killerRole);
        }

        foreach (AssignedPlayerRole role in allPlayers)
        {
            if (activePlayers.Count >= targetCount)
            {
                break;
            }

            if (role.clientId == killerClientId)
            {
                continue;
            }

            role.isActiveRolePlayer = true;
            activePlayers.Add(role);
        }

        return activePlayers;
    }

    private void Update()
    {
        if (!enableDebugRoleGeneration)
        {
            return;
        }

        if (Input.GetKeyDown(debugGenerateKey))
        {
            GenerateDebugRoles();
        }
    }

    private void GenerateDebugRoles()
    {
        if (debugPlayerNames == null || debugPlayerNames.Length < 2)
        {
            Debug.LogWarning("Need at least 2 debug players to generate roles.");
            return;
        }

        ServerConnection.PlayerData[] fakePlayers = new ServerConnection.PlayerData[debugPlayerNames.Length];

        for (int i = 0; i < debugPlayerNames.Length; i++)
        {
            fakePlayers[i] = new ServerConnection.PlayerData
            {
                clientId = "debug-player-" + i,
                name = debugPlayerNames[i],
                online = true
            };
        }

        ServerConnection.PlayerData fakeKiller =
            fakePlayers[UnityEngine.Random.Range(0, fakePlayers.Length)];

        Debug.Log("========== DEBUG ROLE GENERATION ==========");
        Debug.Log("Debug Killer selected: " + fakeKiller.name);

        CreateRolePool(fakePlayers, fakeKiller);
    }

    private void CreateBaseRoles(ServerConnection.PlayerData[] onlinePlayers)
    {
        foreach (ServerConnection.PlayerData player in onlinePlayers)
        {
            AssignedPlayerRole assignedRole = new AssignedPlayerRole
            {
                clientId = player.clientId,
                playerName = player.name,
                visibleRole = VisibleRole.Guest,
                secretState = SecretState.Innocent,
                isActiveRolePlayer = false
            };

            assignedRoles.Add(assignedRole);
        }
    }

    private void AssignKiller(ServerConnection.PlayerData selectedKiller)
    {
        AssignedPlayerRole killerRole = GetAssignedRole(selectedKiller.clientId);

        if (killerRole == null)
        {
            Debug.LogWarning("Could not find killer in assigned roles.");
            return;
        }

        killerRole.secretState = SecretState.Killer;
        killerRole.visibleRole = GetRandomCoverRole();
    }

    private void AssignSpecialHiddenStates(List<AssignedPlayerRole> activePlayers)
    {
        List<AssignedPlayerRole> availablePlayers = new List<AssignedPlayerRole>();

        foreach (AssignedPlayerRole role in activePlayers)
        {
            if (role.secretState == SecretState.Innocent)
            {
                availablePlayers.Add(role);
            }
        }

        Shuffle(availablePlayers);

        if (includeHenchman && availablePlayers.Count > 0)
        {
            AssignedPlayerRole henchman = availablePlayers[0];
            availablePlayers.RemoveAt(0);

            henchman.secretState = SecretState.Henchman;
            henchman.visibleRole = GetRandomCoverRole();

            SelectedHenchman = henchman;
        }

        if (includeCursed && availablePlayers.Count > 0)
        {
            AssignedPlayerRole cursed = availablePlayers[0];
            availablePlayers.RemoveAt(0);

            cursed.secretState = SecretState.Cursed;
            cursed.visibleRole = GetRandomInformationRole();
        }

        if (includeShapeshifter && availablePlayers.Count > 0)
        {
            AssignedPlayerRole shapeshifter = availablePlayers[0];
            availablePlayers.RemoveAt(0);

            shapeshifter.secretState = SecretState.Shapeshifter;
            shapeshifter.visibleRole = GetRandomInformationRole();
        }
    }

    private void AssignNormalVisibleRolesToRemainingPlayers(List<AssignedPlayerRole> activePlayers)
    {
        List<AssignedPlayerRole> remainingActivePlayers = new List<AssignedPlayerRole>();

        foreach (AssignedPlayerRole role in activePlayers)
        {
            if (role.secretState == SecretState.Innocent)
            {
                remainingActivePlayers.Add(role);
            }
        }

        Shuffle(remainingActivePlayers);

        List<VisibleRole> requiredRoles = new List<VisibleRole>
    {
        VisibleRole.Detective,
        VisibleRole.Scholar,
        VisibleRole.Witness,
        VisibleRole.Medium,
        VisibleRole.Butler
    };

        Shuffle(requiredRoles);

        foreach (VisibleRole requiredRole in requiredRoles)
        {
            if (remainingActivePlayers.Count == 0)
            {
                Debug.LogWarning("Not enough innocent active players to assign all required roles.");
                break;
            }

            AssignedPlayerRole target = remainingActivePlayers[0];
            remainingActivePlayers.RemoveAt(0);

            target.visibleRole = requiredRole;
        }

        int guestsAssigned = 0;

        while (guestsAssigned < requiredGuestCount && remainingActivePlayers.Count > 0)
        {
            AssignedPlayerRole guest = remainingActivePlayers[0];
            remainingActivePlayers.RemoveAt(0);

            guest.visibleRole = VisibleRole.Guest;
            guestsAssigned++;
        }

        foreach (AssignedPlayerRole role in remainingActivePlayers)
        {
            role.visibleRole = GetRandomInformationRole();
        }
    }

    public void SetVictim(string victimClientId)
    {
        AssignedPlayerRole victimRole = GetAssignedRole(victimClientId);

        if (victimRole == null)
        {
            Debug.LogWarning("Could not set victim. Player not found: " + victimClientId);
            return;
        }

        victimRole.secretState = SecretState.Victim;
    }

    private VisibleRole GetRandomCoverRole()
    {
        VisibleRole[] coverRoles =
        {
            VisibleRole.Detective,
            VisibleRole.Witness,
            VisibleRole.Butler,
            VisibleRole.Scholar,
            VisibleRole.Medium
        };

        return coverRoles[UnityEngine.Random.Range(0, coverRoles.Length)];
    }

    private VisibleRole GetRandomInformationRole()
    {
        VisibleRole[] informationRoles =
        {
            VisibleRole.Detective,
            VisibleRole.Witness,
            VisibleRole.Butler,
            VisibleRole.Scholar,
            VisibleRole.Medium
        };

        return informationRoles[UnityEngine.Random.Range(0, informationRoles.Length)];
    }

    public AssignedPlayerRole GetAssignedRole(string clientId)
    {
        foreach (AssignedPlayerRole role in assignedRoles)
        {
            if (role.clientId == clientId)
            {
                return role;
            }
        }

        return null;
    }

    public AssignedPlayerRole GetKillerRole()
    {
        foreach (AssignedPlayerRole role in assignedRoles)
        {
            if (role.secretState == SecretState.Killer)
            {
                return role;
            }
        }

        return null;
    }

    public AssignedPlayerRole GetHenchmanRole()
    {
        foreach (AssignedPlayerRole role in assignedRoles)
        {
            if (role.secretState == SecretState.Henchman)
            {
                return role;
            }
        }

        return null;
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

    private void DebugAssignedRoles()
    {
        int activeCount = 0;

        string activeText = "ACTIVE ROLE PLAYERS:\n\n";
        string inactiveText = "\nINACTIVE PLAYERS:\n\n";

        foreach (AssignedPlayerRole role in assignedRoles)
        {
            string line =
                role.playerName +
                " | Visible: " + role.visibleRole +
                " | Secret: " + role.secretState +
                "\n";

            if (role.isActiveRolePlayer)
            {
                activeCount++;
                activeText += line;
            }
            else
            {
                inactiveText += line;
            }
        }

        string debugText =
            "Assigned roles:\n" +
            "Active role players: " + activeCount + " / " + assignedRoles.Count + "\n\n" +
            activeText +
            inactiveText;

        Debug.Log(debugText);
    }
}