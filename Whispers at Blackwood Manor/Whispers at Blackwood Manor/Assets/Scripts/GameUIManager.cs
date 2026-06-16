using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GameUIManager : MonoBehaviour
{
    [Header("Screens")]
    [SerializeField] private GameObject lobbyScreen;
    [SerializeField] private GameObject discussionScreen;

    [Header("Discussion Screen Text")]
    [SerializeField] private TMP_Text victimInfoText;
    [SerializeField] private TMP_Text rolesInPlayText;

    private void Start()
    {
        ShowLobbyScreen();
    }

    public void ShowLobbyScreen()
    {
        if (lobbyScreen != null)
        {
            lobbyScreen.SetActive(true);
        }

        if (discussionScreen != null)
        {
            discussionScreen.SetActive(false);
        }
    }

    public void ShowDiscussionScreen(
        ServerConnection.MurderData murderData,
        IReadOnlyList<RoleManager.AssignedPlayerRole> assignedRoles
    )
    {
        if (murderData == null)
        {
            Debug.LogWarning("Cannot show discussion screen: murder data is null.");
            return;
        }

        if (lobbyScreen != null)
        {
            lobbyScreen.SetActive(false);
        }

        if (discussionScreen != null)
        {
            discussionScreen.SetActive(true);
        }

        UpdateVictimText(murderData);
        UpdateRolesInPlayText(assignedRoles);
    }

    private void UpdateVictimText(ServerConnection.MurderData murderData)
    {
        if (victimInfoText == null)
        {
            return;
        }

        victimInfoText.text =
            "Victim:\n" +
            murderData.victimName +
            "\n\n" +
            murderData.victimName + " may not speak.";
    }

    private void UpdateRolesInPlayText(IReadOnlyList<RoleManager.AssignedPlayerRole> assignedRoles)
    {
        if (rolesInPlayText == null)
        {
            return;
        }

        if (assignedRoles == null || assignedRoles.Count == 0)
        {
            rolesInPlayText.text = "Roles in Play:\nUnknown";
            return;
        }

        HashSet<string> rolesInPlay = new HashSet<string>();

        foreach (RoleManager.AssignedPlayerRole role in assignedRoles)
        {
            if (!role.isActiveRolePlayer)
            {
                continue;
            }

            if (role.secretState == RoleManager.SecretState.Killer)
            {
                rolesInPlay.Add("Killer");
                continue;
            }

            if (role.secretState == RoleManager.SecretState.Henchman)
            {
                rolesInPlay.Add("Henchman");
                continue;
            }

            if (role.secretState == RoleManager.SecretState.Cursed)
            {
                rolesInPlay.Add("Cursed");
                continue;
            }

            if (role.secretState == RoleManager.SecretState.Shapeshifter)
            {
                rolesInPlay.Add("Shapeshifter");
                continue;
            }

            if (role.secretState == RoleManager.SecretState.Victim)
            {
                rolesInPlay.Add("Victim");
                continue;
            }

            rolesInPlay.Add(role.visibleRole.ToString());
        }

        rolesInPlayText.text = "Roles in Play:\n";

        foreach (string roleName in rolesInPlay)
        {
            rolesInPlayText.text += roleName + "\n";
        }
    }
}