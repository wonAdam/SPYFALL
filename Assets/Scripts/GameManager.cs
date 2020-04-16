#pragma warning disable 0649
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using LitJson;
using DG.Tweening;


public class GameManager : MonoBehaviourPunCallbacks
{
    const byte DIST_START_INFOS = 1;
    const byte SHOW_RESULT = 2;
    const byte RESTART = 3;
    const byte SPY_IS_GUESSING = 4;
    const byte SET_TIMER = 5;
    const byte ON_TIME_END = 6;

    public override void OnEnable()
    {
        base.OnEnable();
        PhotonNetwork.NetworkingClient.EventReceived += NetworkingClient_EventReceived;
    }

    public override void OnDisable()
    {
        base.OnDisable();
        PhotonNetwork.NetworkingClient.EventReceived -= NetworkingClient_EventReceived;
    }

    private void NetworkingClient_EventReceived(EventData obj)
    {
        Debug.Log(obj.Code.ToString() + "번째 RaiseEvent를 받았습니다.");

        if(obj.Code == DIST_START_INFOS)
        {
            object[] data = (object[])obj.CustomData;
            playerNicks = (string[]) data[0];
            shuffledCardIndex = (int[]) data[1];
            shuffledJobIndex = (int[]) data[2];
            mafiaIndex = (int)data[3];
            int start_time_min = (int)data[4];
            _Game_Round = (int)data[5];

            //Start Timer
            if(PhotonNetwork.IsMasterClient)
                StartTimer(start_time_min);

            //Set MyCardPanel & Open
            for(int i = 0; i < playerNicks.Length; i++){
                if(PhotonNetwork.NickName == playerNicks[i]){
                    SetMyCard(i);
                    break;
                }
            }
            _WaitingPanel.SetActive(false);
            _MyCardPanel.SetActive(true);
            _MyCardBtn.interactable = true;



            //Set ResultPanel & Hide
            SetResultPanel();




        }

        else if(obj.Code == SHOW_RESULT){
            _MyCardBtn.interactable = false;
            //Off Waiting Panel
            _WaitingPanel.SetActive(false);
            //Show Result Panel
            _ResultPanel.SetActive(true);
            if(PhotonNetwork.IsMasterClient){
                _RestartBtn.gameObject.SetActive(true);
            }
        }

        else if(obj.Code == RESTART){
            Initialize_Panels();
        }

        else if(obj.Code == SPY_IS_GUESSING){
            _TimerScript.notify = false;
            _TimerScript.sec = 0;
            GetComponent<AudioSource>().PlayOneShot(SPYComingSound);
            StartCoroutine(Start_Blinking(_WaitingPanel.GetComponent<Image>()));

            OnSpyGuess_Setting();

            _MyCardBtn.interactable = false;
            _MyCardPanel.SetActive(false);

        }

        else if(obj.Code == SET_TIMER){
            int sec = (int)obj.CustomData;

            _TimerScript.Update_Timer(sec);

        }
        
        else if(obj.Code == ON_TIME_END){
            OnEnd_Time();
            GetComponent<AudioSource>().PlayOneShot(OnTimerEndSound);

        }
    }


    [Header("Panel")]
    [SerializeField] private GameObject _GamePanel;
    [SerializeField] private GameObject _MyCardPanel;
    [SerializeField] private GameObject _WholeCardPanel;
    [SerializeField] private GameObject _TimerPanel;
    [SerializeField] private GameObject _MasterClientPanel;
    [SerializeField] private GameObject _WaitingPanel;
    [SerializeField] private GameObject _ResultPanel;


    [Header("other UIs")]
    [SerializeField] private Slider _TimeSetSlider;
    [SerializeField] private Button _MyCardBtn;
    [SerializeField] private Button _ShowResultBtn;
    [SerializeField] private Button _StartBtn;
    [SerializeField] private Text _TimeNumText;
    [SerializeField] private Timer _TimerScript;
    [SerializeField] private Text _JobText;
    [SerializeField] private Text _PlayerlistText;
    [SerializeField] private Text _WaitText;
    [SerializeField] private Button _RestartBtn;
    [SerializeField] private Text _ResultText1;
    [SerializeField] private Text _ResultText2;
    [SerializeField] private Button _SPYBtn;

    [Header("Audio")]
    [SerializeField] AudioClip ClickSound;
    [SerializeField] AudioClip TikTokSound;
    [SerializeField] AudioClip EndSound;
    [SerializeField] AudioClip SPYComingSound;
    [SerializeField] AudioClip MyCardOpenCloseSound;
    [SerializeField] AudioClip OnTimerEndSound;


    [Header("Variables")]
    [SerializeField] private GameObject[] CardsPrefabs;
    [SerializeField] private GameObject _SpyCard;
    [SerializeField] private CardAsset[] CardsDatas;
    private GameObject _MyCardInst;
    public string[] playerNicks;
    public int[] shuffledCardIndex;
    public int[] shuffledJobIndex;
    public int mafiaIndex;
    public bool isGameStarted = false;
    public int _Game_Round = 0;
    public int min_player = 4;

    private void Start()
    {
        Initialize_Panels();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        _PlayerlistText.text = "";
        Debug.Log(PhotonNetwork.MasterClient.NickName + "이 방장입니다.");
        Debug.Log("현재 방에는 ");
        for (int i = 0; i < PhotonNetwork.CurrentRoom.PlayerCount; i++)
        {
            Debug.Log((i + 1).ToString() + "번쩨 플레이어 " + PhotonNetwork.PlayerList[i].NickName + "님");
            _PlayerlistText.text += PhotonNetwork.PlayerList[i].NickName + "\n";
        }

        if(PhotonNetwork.IsMasterClient && PhotonNetwork.PlayerList.Length >= min_player)
        {
            _StartBtn.interactable = true;
        }
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        _PlayerlistText.text = "";
        Debug.Log(PhotonNetwork.MasterClient.NickName + "이 방장입니다.");

        Debug.Log("현재 방에는 ");
        for (int i = 0; i < PhotonNetwork.CurrentRoom.PlayerCount; i++)
        {
            Debug.Log((i + 1).ToString() + "번쩨 플레이어 " + PhotonNetwork.PlayerList[i].NickName + "님");
            _PlayerlistText.text += PhotonNetwork.PlayerList[i].NickName + "\n";
        }

        if (PhotonNetwork.IsMasterClient && PhotonNetwork.PlayerList.Length >= min_player)
        {
            _StartBtn.interactable = true;
        }
    }

    public void OnClick_StartGame()
    {
        GetComponent<AudioSource>().PlayOneShot(ClickSound);

        _MasterClientPanel.SetActive(false);

        //param set
        List<string> playerListNicks = new List<string>();

        foreach(Player player in PhotonNetwork.PlayerList){
            playerListNicks.Add(player.NickName);
        }

        List<int> playerJobIndex = new List<int>();
        for (int i = 1; i < 10; i++)
        {
            playerJobIndex.Add(i);
        }
        for (int i = 0; i < 9; i++)
        {
            int swap_target = UnityEngine.Random.Range(0, 9);
            int tmp = playerJobIndex[i];
            playerJobIndex[i] = playerJobIndex[swap_target];
            playerJobIndex[swap_target] = tmp;
        }

        int mafiaIndex = UnityEngine.Random.Range(0, PhotonNetwork.PlayerList.Length);

        int start_time_min = (int)_TimeSetSlider.value;





        if (_Game_Round == 0 || _Game_Round > 29)
        {
            List<int> playerPlaceIndex = new List<int>();
            for(int i = 0; i < CardsDatas.Length; i++){
                playerPlaceIndex.Add(i);
            }



            _Game_Round = 1;
            for (int i = 0; i < CardsDatas.Length; i++)
            {
                int swap_target = UnityEngine.Random.Range(0, CardsDatas.Length);
                int tmp = playerPlaceIndex[i];
                playerPlaceIndex[i] = playerPlaceIndex[swap_target];
                playerPlaceIndex[swap_target] = tmp;
            }
            DistributeStartInfos(playerListNicks.ToArray(), playerPlaceIndex.ToArray(), playerJobIndex.ToArray(), mafiaIndex, start_time_min, _Game_Round);
        }

        else{
            _Game_Round++;
            DistributeStartInfos(playerListNicks.ToArray(), shuffledCardIndex, playerJobIndex.ToArray(), mafiaIndex, start_time_min, _Game_Round);
        }


        


        

    }

    public void SetMyCard(int myIndex){


        _SPYBtn.gameObject.SetActive(true);
        _SPYBtn.interactable = false;
        if(myIndex == mafiaIndex){
            _JobText.text = "SPY";
            _SPYBtn.interactable = true;
            _MyCardInst = Instantiate(_SpyCard, _MyCardPanel.transform, false);
            _MyCardInst.transform.localScale = new Vector3(2f, 2f, 1f);
            return;
        }


        _MyCardInst = Instantiate(CardsPrefabs[shuffledCardIndex[_Game_Round]], _MyCardPanel.transform, false);
        _MyCardInst.transform.localScale = new Vector3(2f, 2f, 1f);

        switch(shuffledJobIndex[myIndex]){
            case 1:
                _JobText.text = (string)CardsDatas[shuffledCardIndex[_Game_Round]].player1;
                break;
            case 2:
                _JobText.text = (string)CardsDatas[shuffledCardIndex[_Game_Round]].player2;
                break;
            case 3:
                _JobText.text = (string)CardsDatas[shuffledCardIndex[_Game_Round]].player3;
                break;
            case 4:
                _JobText.text = (string)CardsDatas[shuffledCardIndex[_Game_Round]].player4;
                break;
            case 5:
                _JobText.text = (string)CardsDatas[shuffledCardIndex[_Game_Round]].player5;
                break;
            case 6:
                _JobText.text = (string)CardsDatas[shuffledCardIndex[_Game_Round]].player6;
                break;
            case 7:
                _JobText.text = (string)CardsDatas[shuffledCardIndex[_Game_Round]].player7;
                break;
            case 8:
                _JobText.text = (string)CardsDatas[shuffledCardIndex[_Game_Round]].player8;
                break;
            case 9:
                _JobText.text = (string)CardsDatas[shuffledCardIndex[_Game_Round]].player9;
                break;
            default:
                Debug.Log("job not selected");
                break;
        }

        
    }

    public void OnClick_SPYBtn(){
        GetComponent<AudioSource>().PlayOneShot(ClickSound);
        #region RE_SPY_Is_Guessing
        RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.All };
        SendOptions sendOptions = new SendOptions { Reliability = true };
        PhotonNetwork.RaiseEvent(SPY_IS_GUESSING, null, raiseEventOptions, sendOptions);
        #endregion
    }

    public void SetResultPanel(){
        _ResultText1.text = "";
        _ResultText2.text = "";

        for(int i = 0; i < PhotonNetwork.PlayerList.Length; i++){

            if(i < 5){
                if (i == mafiaIndex)
                {
                    _ResultText1.text += playerNicks[i] + " == " + "SPY \n";
                    continue;
                }

                _ResultText1.text += playerNicks[i] + " == " + CardsDatas[shuffledCardIndex[_Game_Round]].place + ", ";
                switch(shuffledJobIndex[i])
                {
                    case 1:
                                _ResultText1.text += (string)CardsDatas[shuffledCardIndex[_Game_Round]].player1;
                        break;
                    case 2:
                                _ResultText1.text += (string)CardsDatas[shuffledCardIndex[_Game_Round]].player2;
                        break;
                    case 3:
                                _ResultText1.text += (string)CardsDatas[shuffledCardIndex[_Game_Round]].player3;
                        break;
                    case 4:
                                _ResultText1.text += (string)CardsDatas[shuffledCardIndex[_Game_Round]].player4;
                        break;
                    case 5:
                                _ResultText1.text += (string)CardsDatas[shuffledCardIndex[_Game_Round]].player5;
                        break;
                    case 6:
                                _ResultText1.text += (string)CardsDatas[shuffledCardIndex[_Game_Round]].player6;
                        break;
                    case 7:
                                _ResultText1.text += (string)CardsDatas[shuffledCardIndex[_Game_Round]].player7;
                        break;
                    case 8:
                                _ResultText1.text += (string)CardsDatas[shuffledCardIndex[_Game_Round]].player8;
                        break;
                    case 9:
                                _ResultText1.text += (string)CardsDatas[shuffledCardIndex[_Game_Round]].player9;
                        break;
                    default:
                        Debug.Log("job not selected");
                        break;
                }
                _ResultText1.text += "\n";
            }
            else{
                if (i == mafiaIndex)
                {
                    _ResultText1.text += playerNicks[i] + " == " + "SPY \n";
                    continue;
                }

                _ResultText2.text += playerNicks[i] + " == " + CardsDatas[shuffledCardIndex[_Game_Round]].place + ", ";
                switch (shuffledJobIndex[i])
                {
                    case 1:
                        _ResultText2.text += (string)CardsDatas[shuffledCardIndex[_Game_Round]].player1;
                        break;
                    case 2:
                        _ResultText2.text += (string)CardsDatas[shuffledCardIndex[_Game_Round]].player2;
                        break;
                    case 3:
                        _ResultText2.text += (string)CardsDatas[shuffledCardIndex[_Game_Round]].player3;
                        break;
                    case 4:
                        _ResultText2.text += (string)CardsDatas[shuffledCardIndex[_Game_Round]].player4;
                        break;
                    case 5:
                        _ResultText2.text += (string)CardsDatas[shuffledCardIndex[_Game_Round]].player5;
                        break;
                    case 6:
                        _ResultText2.text += (string)CardsDatas[shuffledCardIndex[_Game_Round]].player6;
                        break;
                    case 7:
                        _ResultText2.text += (string)CardsDatas[shuffledCardIndex[_Game_Round]].player7;
                        break;
                    case 8:
                        _ResultText2.text += (string)CardsDatas[shuffledCardIndex[_Game_Round]].player8;
                        break;
                    case 9:
                        _ResultText2.text += (string)CardsDatas[shuffledCardIndex[_Game_Round]].player9;
                        break;
                    default:
                        Debug.Log("job not selected");
                        break;
                }
                _ResultText2.text += "\n";
            }
        }
    }

    public void Initialize_Panels(){
        _GamePanel.SetActive(true);
        _ShowResultBtn.gameObject.SetActive(false);
        _RestartBtn.gameObject.SetActive(false);
        _SPYBtn.gameObject.SetActive(false);
        _TimerPanel.SetActive(true);
        _WholeCardPanel.SetActive(true);
        _MyCardPanel.SetActive(false);
        _MyCardBtn.interactable = false;
        _StartBtn.interactable = false;
        _ResultPanel.SetActive(false);

        _WaitText.text = "방장이 게임 시간을 정하고 있습니다.";

        if (PhotonNetwork.IsMasterClient)
        {
            _WaitingPanel.SetActive(false);
            _MasterClientPanel.SetActive(true);

            for (int i = 0; i < PhotonNetwork.CurrentRoom.PlayerCount; i++)
            {
                Debug.Log((i + 1).ToString() + "번쩨 플레이어 " + PhotonNetwork.PlayerList[i].NickName + "님");
                _PlayerlistText.text += (i + 1).ToString() + ". " + PhotonNetwork.PlayerList[i].NickName + "\n";
            }
        }
        else if (!PhotonNetwork.IsMasterClient)
        {
            _MasterClientPanel.SetActive(false);
            _WaitingPanel.SetActive(true);
        }


        if (PhotonNetwork.IsMasterClient && PhotonNetwork.PlayerList.Length >= min_player)
        {
            _StartBtn.interactable = true;
        }
        else _StartBtn.interactable = false;

        _PlayerlistText.text = "";
        foreach(Player player in PhotonNetwork.PlayerList){
            _PlayerlistText.text += player.NickName + "\n";
        }
    }

    public void DistributeStartInfos(string[] playerNicks,int[] placeIndex, int[] jobindex, int mafiaIndex, int start_time_min, int Game_Round) // MasterCilent Only
    {
        #region Dist_All_Infos
        object[] data = { playerNicks,  placeIndex, jobindex, mafiaIndex, start_time_min, Game_Round };
        RaiseEventOptions raiseEventOptions = new RaiseEventOptions{ Receivers = ReceiverGroup.All };
        SendOptions sendOptions = new SendOptions{ Reliability = true};
        PhotonNetwork.RaiseEvent(DIST_START_INFOS, data, raiseEventOptions, sendOptions);
        #endregion
    }

    public void StartTimer(int startmin)
    {
        GetComponent<AudioSource>().PlayOneShot(TikTokSound);
        StartCoroutine(TikTok(startmin));
    }

    private IEnumerator TikTok(int startmin){
        int sec = startmin * 60;

        while(true){

            #region SET_TIMER
            object data = sec;
            RaiseEventOptions raiseEventOptions = new RaiseEventOptions{ Receivers = ReceiverGroup.All };
            SendOptions sendOptions = new SendOptions{ Reliability = true};
            PhotonNetwork.RaiseEvent(SET_TIMER, data, raiseEventOptions, sendOptions);
            #endregion

                if(sec== 0) {
                    #region ON_TIME_END
                    RaiseEventOptions raiseEventOptions1 = new RaiseEventOptions{ Receivers = ReceiverGroup.All };
                    SendOptions sendOptions1 = new SendOptions{ Reliability = true};
                    PhotonNetwork.RaiseEvent(ON_TIME_END, null, raiseEventOptions1, sendOptions1);
                    #endregion
                    break;
                }

            yield return new WaitForSecondsRealtime(1f);

            sec--;

        }

    }

    public void OnEnd_Time()
    {
        GetComponent<AudioSource>().PlayOneShot(EndSound);

        _SPYBtn.gameObject.SetActive(false);
        _MyCardPanel.SetActive(false);
        _MyCardBtn.interactable = false;
        _MyCardPanel.SetActive(false);

        
        Destroy(_MyCardInst);
        _WaitingPanel.SetActive(true);
        if(PhotonNetwork.IsMasterClient){
            _ShowResultBtn.gameObject.SetActive(true);
            _WaitText.text = "시간이 다 되었습니다. 투표하세요.";
        }
        else if(! PhotonNetwork.IsMasterClient){
            _WaitText.text = "시간이 다 되었습니다. 투표하세요.";
        }

        StartCoroutine(Start_Blinking(_WaitingPanel.GetComponent<Image>()));
    }

    public void OnSpyGuess_Setting(){
        _SPYBtn.gameObject.SetActive(false);
        _MyCardPanel.SetActive(false);

        if(PhotonNetwork.IsMasterClient)
            StopAllCoroutines();

        Destroy(_MyCardInst);
        _WaitingPanel.SetActive(true);
        if (PhotonNetwork.IsMasterClient)
        {
            _ShowResultBtn.gameObject.SetActive(true);
            _WaitText.text = "스파이가 스밍아웃을 합니다 ! ! !";
        }
        else if (!PhotonNetwork.IsMasterClient)
        {
            _WaitText.text = "스파이가 스밍아웃을 합니다 ! ! !";
        }
    }

    public void OnValueChange_TimeSetSlider(){
        _TimeNumText.text = _TimeSetSlider.value.ToString() + " min";
    }

    public void OnClick_MyCardBtn(){
        GetComponent<AudioSource>().PlayOneShot(MyCardOpenCloseSound);
        if(_MyCardPanel.activeInHierarchy == true){
            _MyCardPanel.SetActive(false);
        }
        else{
            _MyCardPanel.SetActive(true);
        }
    }

    public void OnClick_ShowResultBtn(){
        GetComponent<AudioSource>().PlayOneShot(ClickSound);
        #region RE_ShowResult
        RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.All };
        SendOptions sendOptions = new SendOptions { Reliability = true };
        PhotonNetwork.RaiseEvent(SHOW_RESULT, null, raiseEventOptions, sendOptions);
        #endregion
    }

    public void OnClick_RestartBtn(){
        GetComponent<AudioSource>().PlayOneShot(ClickSound);
        #region RE_Restart
        RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.All };
        SendOptions sendOptions = new SendOptions { Reliability = true };
        PhotonNetwork.RaiseEvent(RESTART, null, raiseEventOptions, sendOptions);
        #endregion
    }

    public IEnumerator Start_Blinking(Image image)
    {

        for (int i = 0; i < 3; i++)
        {
            _WaitingPanel.GetComponent<Image>().DOColor(new Color(0.5f, 0f, 0f, 0.7607843f), 0.15f);
            yield return new WaitForSecondsRealtime(0.15f);
            _WaitingPanel.GetComponent<Image>().DOColor(new Color(0f, 0f, 0f, 0.7607843f), 0.15f);
            yield return new WaitForSecondsRealtime(0.15f);
        }
    }
}
