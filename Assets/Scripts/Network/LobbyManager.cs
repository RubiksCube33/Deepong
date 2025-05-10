using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class LobbyManager : MonoBehaviourPunCallbacks
{
    [Header("로비 UI 참조")]
    public GameObject lobbyUI; // 로비 UI 패널
    public GameObject roomListContent; // 방 목록이 표시될 컨텐츠 영역
    public GameObject roomListItemPrefab; // 방 목록 아이템 프리팹

    // 이벤트 델리게이트 정의
    public delegate void LobbyEvent();
    public static event LobbyEvent OnLobbyJoined;
    public static event LobbyEvent OnLobbyLeft;
    public static event LobbyEvent OnRoomListChanged;

    private Dictionary<string, RoomInfo> cachedRoomList = new Dictionary<string, RoomInfo>();
    private Dictionary<string, GameObject> roomListItems = new Dictionary<string, GameObject>();
    
    private void Awake()
    {
        // 씬 전환 시에도 이 매니저 유지
        DontDestroyOnLoad(this.gameObject);
    }

    private void Start()
    {
        // 로비 UI가 있다면 시작 시 비활성화
        if (lobbyUI != null) 
            lobbyUI.SetActive(false);
    }

    // 로비에 입장했을 때 호출
    public override void OnJoinedLobby()
    {
        Debug.Log("로비에 입장했습니다.");
        cachedRoomList.Clear();
        roomListItems.Clear();
        
        // 로비 UI 활성화
        if (lobbyUI != null)
            lobbyUI.SetActive(true);
        
        // 이벤트 발생
        if (OnLobbyJoined != null) OnLobbyJoined();
    }

    // 로비에서 나갔을 때 호출
    public override void OnLeftLobby()
    {
        Debug.Log("로비에서 나갔습니다.");
        cachedRoomList.Clear();
        ClearRoomListView();
        
        // 로비 UI 비활성화
        if (lobbyUI != null)
            lobbyUI.SetActive(false);
        
        // 이벤트 발생
        if (OnLobbyLeft != null) OnLobbyLeft();
    }

    // 방 목록이 업데이트될 때 호출
    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        Debug.Log($"방 목록 업데이트: {roomList.Count}개의 방 정보 수신");
        
        // 방 목록 업데이트
        UpdateCachedRoomList(roomList);
        UpdateRoomListView();
        
        // 이벤트 발생
        if (OnRoomListChanged != null) OnRoomListChanged();
    }

    // 캐시된 방 목록 업데이트
    private void UpdateCachedRoomList(List<RoomInfo> roomList)
    {
        foreach (RoomInfo info in roomList)
        {
            // 삭제된 방은 목록에서 제거
            if (info.RemovedFromList)
            {
                cachedRoomList.Remove(info.Name);
            }
            else
            {
                // 존재하는 방 정보 업데이트 또는 새 방 추가
                cachedRoomList[info.Name] = info;
            }
        }
    }

    // UI에 방 목록 표시 업데이트
    private void UpdateRoomListView()
    {
        // UI 컴포넌트가 없으면 리턴
        if (roomListContent == null || roomListItemPrefab == null)
        {
            Debug.LogWarning("방 목록 UI 컴포넌트가 할당되지 않았습니다.");
            return;
        }

        // 기존 목록에 없는 방들 제거
        foreach (var item in new Dictionary<string, GameObject>(roomListItems))
        {
            if (!cachedRoomList.ContainsKey(item.Key))
            {
                Destroy(item.Value);
                roomListItems.Remove(item.Key);
            }
        }

        // 새 방 추가 및 기존 방 정보 업데이트
        foreach (var room in cachedRoomList)
        {
            if (roomListItems.TryGetValue(room.Key, out GameObject roomItem))
            {
                // 기존 방 정보 업데이트
                // 실제 UI 업데이트 로직은 RoomListItem 스크립트 참조
                RoomListItem roomListItemScript = roomItem.GetComponent<RoomListItem>();
                if (roomListItemScript != null)
                {
                    roomListItemScript.SetRoomInfo(room.Value);
                }
            }
            else
            {
                // 새 방 추가
                GameObject newRoomItem = Instantiate(roomListItemPrefab, roomListContent.transform);
                RoomListItem roomListItemScript = newRoomItem.GetComponent<RoomListItem>();
                if (roomListItemScript != null)
                {
                    roomListItemScript.SetRoomInfo(room.Value);
                }
                roomListItems[room.Key] = newRoomItem;
            }
        }
    }

    // 방 목록 뷰 초기화
    private void ClearRoomListView()
    {
        foreach (var item in roomListItems.Values)
        {
            Destroy(item);
        }
        roomListItems.Clear();
    }

    // 로비 입장
    public void JoinLobby()
    {
        if (!PhotonNetwork.InLobby)
        {
            Debug.Log("로비 입장 시도...");
            PhotonNetwork.JoinLobby();
        }
    }

    // 로비 나가기
    public void LeaveLobby()
    {
        if (PhotonNetwork.InLobby)
        {
            Debug.Log("로비 퇴장 시도...");
            PhotonNetwork.LeaveLobby();
        }
    }

    // 방 생성 버튼 이벤트 - MatchMakingManager로 전달
    public void OnCreateRoomButtonClicked()
    {
        // UI 요소가 나중에 완성되면 여기서 데이터를 수집하고 MatchMakingManager를 호출
        MatchMakingManager matchMaking = FindObjectOfType<MatchMakingManager>();
        if (matchMaking != null)
        {
            matchMaking.CreateRoom();
        }
    }

    // 랜덤 매칭 버튼 이벤트 - MatchMakingManager로 전달
    public void OnRandomMatchButtonClicked()
    {
        MatchMakingManager matchMaking = FindObjectOfType<MatchMakingManager>();
        if (matchMaking != null)
        {
            matchMaking.JoinRandomRoom();
        }
    }
}