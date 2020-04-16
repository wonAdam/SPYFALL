#pragma warning disable 0649
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using DG.Tweening;

public class MenuManager : MonoBehaviourPunCallbacks
{
    

    [Header("Panel")]
    [SerializeField] private GameObject _MainCanvas;
    [SerializeField] private GameObject _ConnectingPanel;
    [SerializeField] private GameObject _NameRoomPanel;
    [SerializeField] private GameObject _RoomPanel;

    [Header("Btn, InputField, Text")]
    [SerializeField] private Text ConnectingText = null;
    [SerializeField] private InputField _NameInputField= null;
    [SerializeField] private InputField _RoomInputField= null;
    [SerializeField] private Button _RoomEnterBtn= null;

    [Header("Audio")]
    [SerializeField] AudioClip ClickSound;

    private void Start() {

        Initialize_Panels();
        StartPanelEffect(_NameRoomPanel);
        StartPanelEffect(_ConnectingPanel);
        StartPanelEffect(_RoomPanel);
        PhotonNetwork.PhotonServerSettings.AppSettings.FixedRegion = "kr";
        PhotonNetwork.ConnectUsingSettings();
        PhotonNetwork.AutomaticallySyncScene = true;
    }

    private void StartPanelEffect(GameObject Panel){
        StartCoroutine(PanelEffectAnim(Panel, 3f));
    }
    private IEnumerator PanelEffectAnim(GameObject Panel, float speed){
        while(true){
            Panel.GetComponent<Image>().DOColor(new Color(0.3f, 0.3f, 0.3f, 1f), speed);
            yield return new WaitForSecondsRealtime(speed);
            Panel.GetComponent<Image>().DOColor(new Color(0f, 0f, 0f, 1f), speed);
            yield return new WaitForSecondsRealtime(speed);
        }
    }

    public void Initialize_Panels(){
        _MainCanvas.SetActive(true);
        _ConnectingPanel.SetActive(true);
        _NameRoomPanel.SetActive(false);
        _RoomPanel.SetActive(false);
    }

    public override void OnConnected()
    {
        ConnectingText.text = "Internet Connected";
        Debug.Log("Internet Connected");
    }

    public override void OnConnectedToMaster(){
        Debug.Log("Connected to Master");
        ConnectingText.text = "Server Connected";
        PhotonNetwork.JoinLobby();
    }
    
    public override void OnJoinedLobby()
    {
        ConnectingText.text = "Joined to Lobby";
        Debug.Log("Joined Lobby");


        _NameRoomPanel.SetActive(true);
        _ConnectingPanel.SetActive(false);
    }

    public void OnValueChange_NameNRoomInputField()
    {
        if (_NameInputField.text == null || _RoomInputField.text == null)
            _RoomEnterBtn.interactable = false;
        else
            _RoomEnterBtn.interactable = true;
    }

    public void OnClick_EnterButton()
    {
        GetComponent<AudioSource>().PlayOneShot(ClickSound);

        PhotonNetwork.NickName =  _NameInputField.text;
        Debug.Log(_RoomInputField.text + " 방에 입장을 시도합니다.");
        RoomOptions ro = new RoomOptions { MaxPlayers = 10 };
        PhotonNetwork.JoinRoom(_RoomInputField.text);
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.Log("입장에 실패하였습니다. 방을 만듭니다.");
        RoomOptions ro = new RoomOptions { MaxPlayers = 10, PublishUserId = true };
        PhotonNetwork.CreateRoom(_RoomInputField.text, ro, TypedLobby.Default);
    }

    public override void OnCreatedRoom()
    {
        Debug.Log(PhotonNetwork.CurrentRoom.Name + " 방을 만들었습니다.");
    }

    public override void OnJoinedRoom()
    {
        Debug.Log(PhotonNetwork.CurrentRoom.Name + " 방에 입장하였습니다.");
        Debug.Log(PhotonNetwork.MasterClient.NickName + "이 방장입니다.");

        PhotonNetwork.LoadLevel(1);

    }


}
