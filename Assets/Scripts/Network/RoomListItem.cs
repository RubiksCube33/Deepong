using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Realtime;

public class RoomListItem : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI roomNameText;
    public TextMeshProUGUI playerCountText;
    public Button joinButton;

    private string roomName;
    private bool isOpen = true;

    private void Start()
    {
        if (joinButton != null)
        {
            joinButton.onClick.AddListener(OnJoinButtonClicked);
        }
    }

    public void SetRoomInfo(RoomInfo roomInfo)
    {
        roomName = roomInfo.Name;
        isOpen = roomInfo.IsOpen;

        if (roomNameText != null)
        {
            roomNameText.text = roomInfo.Name;
        }

        if (playerCountText != null)
        {
            playerCountText.text = $"{roomInfo.PlayerCount}/{roomInfo.MaxPlayers}";
        }

        if (joinButton != null)
        {
            joinButton.interactable = roomInfo.IsOpen;
        }
    }

    private void OnJoinButtonClicked()
    {
        if (isOpen && !string.IsNullOrEmpty(roomName))
        {
            MatchMakingManager matchMakingManager = FindObjectOfType<MatchMakingManager>();
            if (matchMakingManager != null)
            {
                matchMakingManager.JoinRoom(roomName);
            }
        }
    }
}