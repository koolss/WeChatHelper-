﻿
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using WebSocketSharp;
using WeChat.App.DataSource;
using WeChat.App.Handle;
using WeChat.App.ModelView;
using WeChat.App.Service;
using WeChat.Domain.Enum;
using WeChat.Domain.Models;
using WeChat.DTO.Message;
using WeChat.DTO.Socket;
using WeChat.Extend.Helper;
using WeChat.Extend.Helper.Date;
using WeChat.Service.Lrw;
using WeChat.Service.WeChat;

namespace WeChat.App
{
    public partial class WeChatHelper : Form
    {
        private WeChatService weChatService = new WeChatService();

        private WebSocket webSocket;
        public static WeChatHelper form;

        private UserService userService = new UserService();
        private UserFriendService friendService = new UserFriendService();
        private AutoGreetUserService autoGreetUserService = new AutoGreetUserService();

        

        #region 属性

        #region Socket是否连接中
        private bool wsConnectRunning;
        public bool WsConnectRunning 
        {
            get => wsConnectRunning;
            set 
            {
                if (value)
                {
                    RunUi(() =>
                    {
                        connectStateLabel.Text = "已连接";
                        connectStateLabel.ForeColor = Color.Green;
                    });
                    StartConnectBtn.Enabled = false;
                    DisConnectBtn.Enabled = true;
                }
                else 
                {
                    RunUi(() =>
                    {
                        connectStateLabel.Text = "未连接";
                        connectStateLabel.ForeColor = Color.Red;
                    });
                    StartConnectBtn.Enabled = true;
                    DisConnectBtn.Enabled = false;
                    
                }
                wsConnectRunning = value;
            }
        }
        #endregion

        #region 最后心跳时间
        private DateTime lastHeartTime;
        public DateTime LastHeartTime
        {
            get => lastHeartTime;
            set 
            {
                RunUi(() =>
                {
                    connectStateLabel.Text = DateHelper.FormatDateTime(value);
                    connectStateLabel.ForeColor = Color.Green;
                });
                lastHeartTime = value;
            }
        }
        #endregion

        private WxUser loginUser;
        public WxUser LoginUser 
        {
            get => loginUser;
            set 
            {
                loginUser = value;
            }
        }

        #endregion

        public WeChatHelper()
        {
            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false;
            form = this;
        }

        #region 窗体事件

        #region 刷新UI
        public void RunUi(Action action)
        {
            BeginInvoke(action);
        }
        #endregion

        #region 加载窗体
        private void WeChatHelper_Load(object sender, EventArgs e)
        {
            // 初始化自动问候视图
            this.InitAutoGreetView();
            // 初始化好友视图
            this.InitFriendView();
            this.InitGroupView();
            this.InitOpenAccountView();
            // 加载WsUrl
            WsUrlTxt.Text = Appsetting.SOCKET_URL;

            DisConnectBtn.Enabled = false;
        }
        #endregion

        #region 开始连接
        private void StartConnect_Click(object sender, EventArgs e)
        {
            // 启用Socket服务
            this.ConnectSocket();

            this.GetUserInfo();

            this.GetUserList();
        }
        #endregion

        #region 断开连接
        private void DisConnectBtn_Click(object sender, EventArgs e)
        {
            this.DisconnectSocket();
        }
        #endregion
        #region 启动微信
        private void OpenWeChatBtn_Click(object sender, EventArgs e)
        {
            // 启动微信
            var openWechat = weChatService.OpenWechat();
            if (openWechat)
            {
                ScrollingLogHandle.AppendTextToLog("启动微信成功");
            }

            // 注入DLL
            //var injectDll = weChatService.InjectDllToWeChat();
            //if (injectDll)
            //{
            //    ScrollingLogHandle.AppendTextToLog("成功注入DLL到微信");
            //}
        }
        #endregion

        private void CloseWeChatBtn_Click(object sender, EventArgs e)
        {
            weChatService.CloseWeChat();
        }

        private void ShowWeChatBtn_Click(object sender, EventArgs e)
        {
            weChatService.TopWindow();
        }

        private void FriendView_CellMouseDown(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right) 
            {
                FriendView.ClearSelection();
                FriendView.Rows[e.RowIndex].Selected = true;
                FriendView.CurrentCell = FriendView.Rows[e.RowIndex].Cells[e.ColumnIndex];
                FriendViewMenu.Show(MousePosition.X, MousePosition.Y);
            }
        }
        #endregion




        public void HandleUserList(WxServerReceiveDTO<BindingList<WxFriendUserMV>> data)
        {
            FriendView.DataSource = null;
            GroupView.DataSource = null;
            OpenAccountView.DataSource = null;
            var users = data.Data;
            // 群聊
            var groupList = users.Where(p => p.WxId.EndsWith("@chatroom")).ToList();
            // 公众号
            var openAccountList = users.Where(p => p.WxId.StartsWith("gh_")).ToList();
            // 好友
            var friendList = users.Where(p => !p.WxId.EndsWith("@chatroom") && !p.WxId.StartsWith("gh_")).ToList();

            RunUi(() =>
            {
                FriendView.DataSource = friendList;
                GroupView.DataSource = groupList;
                OpenAccountView.DataSource = openAccountList;
            });



            //RunUi(() =>
            //{
            //    friendList.ForEach(item =>
            //    {
            //        var index = FriendView.Rows.Add();
            //        // 创建行
            //        DataGridViewRow row = new DataGridViewRow();
            //        // wxid
            //        FriendView.Rows[index].Cells[0].Value = item.WxId;
            //        // wxcode
            //        FriendView.Rows[index].Cells[1].Value = item.WxCode;
            //        // nickName
            //        FriendView.Rows[index].Cells[2].Value = item.NickName;
            //        FriendView.Rows[index].Cells[3].Value = item.HeadImg;
            //        FriendView.Rows[index].Cells[4].Value = item.Remark;
            //    });

            //    groupList.ForEach(item =>
            //    {
            //        var index = GroupView.Rows.Add();
            //        // 创建行
            //        DataGridViewRow row = new DataGridViewRow();
            //        // wxid
            //        GroupView.Rows[index].Cells[0].Value = item.WxId;
            //        // wxcode
            //        GroupView.Rows[index].Cells[1].Value = item.WxCode;
            //        // nickName
            //        GroupView.Rows[index].Cells[2].Value = item.NickName;
            //        GroupView.Rows[index].Cells[3].Value = item.HeadImg;
            //        GroupView.Rows[index].Cells[4].Value = item.Remark;
            //    });

            //    openAccountList.ForEach(item =>
            //    {
            //        var index = OpenAccountView.Rows.Add();
            //        // 创建行
            //        DataGridViewRow row = new DataGridViewRow();
            //        // wxid
            //        OpenAccountView.Rows[index].Cells[0].Value = item.WxId;
            //        // wxcode
            //        OpenAccountView.Rows[index].Cells[1].Value = item.WxCode;
            //        // nickName
            //        OpenAccountView.Rows[index].Cells[2].Value = item.NickName;
            //        OpenAccountView.Rows[index].Cells[3].Value = item.HeadImg;
            //        OpenAccountView.Rows[index].Cells[4].Value = item.Remark;
            //    });
            //});
            FriendView.AllowUserToAddRows = false;
            GroupView.AllowUserToAddRows = false;
            OpenAccountView.AllowUserToAddRows = false;
        }

        


        

        private void AutoGreetTask_Tick(object sender, EventArgs e)
        {
            // 当前系统时间  
            var curTime = DateTime.Now;
            var excTime = AutoGreetTime.Value;
            if (curTime.Hour == excTime.Hour && curTime.Minute == excTime.Minute && curTime.Second == excTime.Second)
            {
                //new AutoGreetService().ExcAutoGreetTask();
                ScrollingLogHandle.AppendTextToLog("执行自动问候");
            }

        }

        #region 保存自动问候配置
        private void SaveAutoGreetConfigBtn_Click(object sender, EventArgs e)
        {
            //var user = WxSocket.GetCurUser();
            //if (user == null)
            //{
            //    return;
            //}
            //using (WeChatHelperContext c = new WeChatHelperContext())
            //{
            //    //var config = c.WxAutoGreetConfigs.FirstOrDefault(p => p.UserId.Equals(user.UserId));
            //    //if (config == null)
            //    //{
            //    //    config = new WxAutoGreetConfig();
            //    //}
            //    ////config.UserId = user.UserId;
            //    //config.Status = AutoGreetStatus.Checked;
            //    //config.GreetTime = AutoGreetTime.Value.TimeOfDay;
            //    //config.EnableCiba = EnableCiBa.Checked;
            //    //config.EnableMotto = EnableMotto.Checked;
            //    //config.EnableWeather = EnableWeather.Checked;
            //    //new AutoGreetConfigService().SaveOrUpdate(config);

            //    if (AutoGreetStatus.Checked)
            //    {
            //        // 开启自动问候定时任务
            //        AutoGreetTask.Enabled = true;
            //        AutoGreetTask.Interval = 1000;
            //    }
            //    else
            //    {
            //        // 关闭自动问候任务
            //        AutoGreetTask.Enabled = false;
            //    }
            //    MessageBox.Show("保存成功", "提示", MessageBoxButtons.OK, MessageBoxIcon.None, MessageBoxDefaultButton.Button1);
            //}
        }

        #endregion



        



        #region 初始化组件
        public void InitAutoGreetView()
        {
            AutoGreetView.Columns.Add("WxId", "ID");
            AutoGreetView.Columns.Add("NickName", "昵称");
            AutoGreetView.Columns.Add("Mobile", "昵称");
            AutoGreetView.Columns.Add("Remarks", "备注");
            AutoGreetView.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            AutoGreetView.BackgroundColor = Color.White;
            AutoGreetView.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            AutoGreetView.BorderStyle = BorderStyle.Fixed3D;
        }

        public void InitFriendView()
        {
            DataGridViewTextBoxColumn TextBoxColumnX = new DataGridViewTextBoxColumn();
            // 获取或设置数据源属性的名称或与 DataGridViewColumn 绑定的数据库列的名称。
            TextBoxColumnX.DataPropertyName = "WxId";
            TextBoxColumnX.HeaderText = "微信ID";
            // 获取或设置列名
            TextBoxColumnX.Name = "WxId";
            FriendView.Columns.Add(TextBoxColumnX);

            TextBoxColumnX = new DataGridViewTextBoxColumn();
            TextBoxColumnX.DataPropertyName = "WxCode";
            TextBoxColumnX.HeaderText = "微信号";
            TextBoxColumnX.Name = "WxCode";
            FriendView.Columns.Add(TextBoxColumnX);

            TextBoxColumnX = new DataGridViewTextBoxColumn();
            TextBoxColumnX.DataPropertyName = "NickName";
            TextBoxColumnX.HeaderText = "昵称";
            TextBoxColumnX.Name = "NickName";
            FriendView.Columns.Add(TextBoxColumnX);

            TextBoxColumnX = new DataGridViewTextBoxColumn();
            TextBoxColumnX.DataPropertyName = "HeadImg";
            TextBoxColumnX.HeaderText = "头像";
            TextBoxColumnX.Name = "HeadImg";
            FriendView.Columns.Add(TextBoxColumnX);

            TextBoxColumnX = new DataGridViewTextBoxColumn();
            TextBoxColumnX.DataPropertyName = "Remark";
            TextBoxColumnX.HeaderText = "备注";
            TextBoxColumnX.Name = "Remark";
            FriendView.Columns.Add(TextBoxColumnX);


            FriendView.AutoGenerateColumns = false;
            FriendView.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            FriendView.BackgroundColor = Color.White;
            FriendView.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            FriendView.BorderStyle = BorderStyle.Fixed3D;
        }

        public void InitGroupView()
        {
            GroupView.Columns.Add("WxId", "微信ID");
            GroupView.Columns.Add("WxCode", "微信号");
            GroupView.Columns.Add("NickName", "昵称");
            GroupView.Columns.Add("HeadImg", "头像");
            GroupView.Columns.Add("Remark", "备注");
            GroupView.AutoGenerateColumns = false;
            GroupView.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            GroupView.BackgroundColor = Color.White;
            GroupView.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            GroupView.BorderStyle = BorderStyle.Fixed3D;
        }

        public void InitOpenAccountView()
        {
            OpenAccountView.Columns.Add("WxId", "微信ID");
            OpenAccountView.Columns.Add("WxCode", "微信号");
            OpenAccountView.Columns.Add("NickName", "昵称");
            OpenAccountView.Columns.Add("HeadImg", "头像");
            OpenAccountView.Columns.Add("Remark", "备注");
            OpenAccountView.AutoGenerateColumns = false;
            OpenAccountView.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            OpenAccountView.BackgroundColor = Color.White;
            OpenAccountView.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            OpenAccountView.BorderStyle = BorderStyle.Fixed3D;
        }
        #endregion



        private void EnableHarvestCode_Click(object sender, EventArgs e)
        {
            //using (WeChatHelperContext c = new WeChatHelperContext())
            //{
            //    //var user = WxSocket.GetCurUser();
            //    //if (user == null)
            //    //{
            //    //    return;
            //    //}
            //    //var appConfig = c.WxAppConfigs.FirstOrDefault(p=>p.UserId.Equals(user.UserId));
            //    //if (appConfig == null)
            //    //{
            //    //    appConfig = new WxAppConfig
            //    //    {
            //    //        UserId = user.UserId,
            //    //        EnableHarvestCode = EnableHarvestCode.Checked
            //    //    };
            //    //}
            //    //else 
            //    //{
            //    //    appConfig.EnableHarvestCode = EnableHarvestCode.Checked;
            //    //}
            //    //new AppConfigService().SaveOrUpdate(appConfig);
            //}
        }

        private void ScrollingLog_TextChanged(object sender, EventArgs e)
        {
            ScrollingLog.SelectionStart = ScrollingLog.Text.Length;
            ScrollingLog.ScrollToCaret();
        }



        #region Socket服务
        /// <summary>
        /// 开启服务
        /// </summary>
        public void ConnectSocket()
        {
            DisconnectSocket();
            ScrollingLogHandle.AppendTextToLog($"[连接服务] 将连接到{Appsetting.SOCKET_URL}");
            webSocket = new WebSocket(Appsetting.SOCKET_URL);
            webSocket.Connect();

            webSocket.OnOpen += WsOnOpen;
            webSocket.OnClose += WsOnClose;
            webSocket.OnMessage += WsOnMessage;
            webSocket.OnError += WsOnError;
            ScrollingLogHandle.AppendTextToLog($"[连接服务] 连接操作成功");
            WsConnectRunning = true;
        }
        /// <summary>
        /// 关闭服务
        /// </summary>
        public void DisconnectSocket()
        {
            if (webSocket == null)
            {
                return;
            }
            ScrollingLogHandle.AppendTextToLog($"[断开服务] 尝试断开服务");
            webSocket.Close();
            webSocket.OnOpen -= WsOnOpen;
            webSocket.OnClose -= WsOnClose;
            webSocket.OnMessage -= WsOnMessage;
            webSocket.OnError -= WsOnError;
            ScrollingLogHandle.AppendTextToLog($"[断开服务] 服务已断开");
            WsConnectRunning = false;
        }
        #endregion

        #region Socket 事件
        private void WsOnOpen(object sender, EventArgs e)
        {
            ScrollingLogHandle.AppendTextToLog($"已连接到:{Appsetting.SOCKET_URL}");
        }
        private void WsOnClose(object sender, CloseEventArgs e)
        {
            ScrollingLogHandle.AppendTextToLog($"与{Appsetting.SOCKET_URL}的连接已关闭");
        }
        private void WsOnMessage(object sender, MessageEventArgs e)
        {
            if (e == null)
            {
                ScrollingLogHandle.AppendTextToLog($"[WS信息事件] 获取到空信息");
                return;
            }

            if (e.IsText)
            {
                HandleReceiveMessage(e.Data);
                return;
            }
        }
        private void WsOnError(object sender, ErrorEventArgs e)
        {
            ScrollingLogHandle.AppendTextToLog($"已连接到:{Appsetting.SOCKET_URL}");
        }
        #endregion

        #region 接收处理Socket消息
        /// <summary>
        /// 处理Socket消息
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="base64Str"></param>
        public void HandleReceiveMessage(string data)
        {
            var model = JsonHelper.FromJson<WxServerReceiveDTO<object>>(data);

            switch (model.Code)
            {
                case SocketDataEnum.HEART_BEAT:
                    HandleHeartBeat(JsonHelper.FromJson<WxServerReceiveDTO<string>>(data));
                    break;
                case SocketDataEnum.USER_LIST:
                    this.HandleUserList(JsonHelper.FromJson<WxServerReceiveDTO<BindingList<WxFriendUserMV>>>(data));
                    break;
                case SocketDataEnum.GET_USER_LIST_SUCCSESS:
                    this.HandleUserList(JsonHelper.FromJson<WxServerReceiveDTO<BindingList<WxFriendUserMV>>>(data));
                    break;
                case SocketDataEnum.GET_USER_INFO:
                    this.HandleUserInfo(JsonHelper.FromJson<WxServerReceiveDTO<string>>(data));
                    break;
                default:
                    HandleRecTxtMsg(JsonHelper.FromJson<WxServerReceiveDTO<object>>(data));
                    break;
            }

        }
        #endregion

        #region 发送处理Socket消息
        public void SendSocket(SocketDTO data)
        {
            if (data == null)
            {
                LogHelper.Error("发送数据不能为空");
                return;
            }
            webSocket.Send(JsonHelper.ToJson(data));
        }
        #endregion

        #region 处理消息
        public void HandleHeartBeat(WxServerReceiveDTO<string> data)
        {
            LastHeartTime = Convert.ToDateTime(data.time);
            var contentJson = JsonHelper.ToJson(data);
            //ScrollingLogHandle.AppendTextToLog($"[处理服务器心跳] 已获取信息：{contentJson}");
        }
        public void HandleRecTxtMsg(WxServerReceiveDTO<object> data)
        {
            if (data.receiver == "self")
            {
                // 个人
            }
            else
            {
                // 群聊
            }
            var contentJson = data.Data;
            //ScrollingLogHandle.AppendTextToLog($"[处理接收文字消息] 已获取信息:{contentJson}");
        }

        public void HandleUserInfo(WxServerReceiveDTO<string> data)
        {
            var contentJson = data.Data;

            var wxUserInfo = JsonHelper.FromJson<WxUserMV>(contentJson);

            // 查询登录用户信息
            loginUser = userService.SelectByWxId(wxUserInfo.WxId);
        }
        #endregion

        #region 获取通讯录列表
        public void GetUserList()
        {
            var data = new SocketDTO
            {
                type = SocketDataEnum.USER_LIST
            };
            SendSocket(data);
        }
        #endregion

        #region 获取个人信息
        public void GetUserInfo() 
        {
            var data = new SocketDTO
            {
                type = SocketDataEnum.GET_USER_INFO
            };
            SendSocket(data);
        }



        #endregion

        private void FriendToAutoGreetToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // 获取当前选中行
            var curRow = FriendView.CurrentRow;
            var wxFriendUserMV = (WxFriendUserMV)curRow.DataBoundItem;
            // 保存或更新用户基础信息
            var friendUser = new WxUser()
            {
                WxId = wxFriendUserMV.WxId,
                WxCode = wxFriendUserMV.WxCode,
                Remark = wxFriendUserMV.Remark,
                NickName = wxFriendUserMV.NickName,
            };
            userService.SaveOrUpdate(friendUser);

            // 保存朋友关系
            WxUserFriend wxUserFriend = new WxUserFriend()
            {
                UserId = loginUser.Id,
                FriendUserId = friendUser.Id,
                Remark = friendUser.Remark,
            };
            friendService.AddFriend(wxUserFriend);

            // 保存问候关系
            WxAutoGreetUser wxAutoGreetUser = new WxAutoGreetUser()
            {
                UserId=loginUser.Id,
                FriendUserId=friendUser.Id,
                CreateTime = DateTime.Now,
            };
            autoGreetUserService.Save(wxAutoGreetUser);
        }
    }
}
