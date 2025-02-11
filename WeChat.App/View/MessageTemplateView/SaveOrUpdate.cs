﻿using Masuit.Tools.Systems;
using Sunny.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WeChat.App.Quartz;
using WeChat.Domain;
using WeChat.Domain.Constant;
using WeChat.Domain.Models;
using WeChat.Extend.Helper.Date;
using WeChat.Service.Robot;

namespace WeChat.App.View.MessageTemplateView
{
    public partial class SaveOrUpdate : UIForm
    {
        private MessageTemplate messageTemplate;

        private WxMessageTemplate wxMessageTemplate;

        public WxMessageTemplate WxMessageTemplate 
        {
            get { return wxMessageTemplate; }
            set 
            { 
                wxMessageTemplate = value;
                mtContent.Text = wxMessageTemplate.Content;
                mtCron.Text = wxMessageTemplate.Cron;
                mtName.Text = wxMessageTemplate.Name;
                mtEnable.Active = wxMessageTemplate.Enable;
                mtRemark.Text = wxMessageTemplate.Remark;
            }
        }

        public SaveOrUpdate(MessageTemplate messageTemplate)
        {
            InitializeComponent();
            this.messageTemplate = messageTemplate;
        }

        private void mtSubmit_Click(object sender, EventArgs e)
        {
            if (wxMessageTemplate == null)
            {
                wxMessageTemplate = new WxMessageTemplate()
                {
                    Name = mtName.Text,
                    Content = mtContent.Text,
                    Enable = mtEnable.Active,
                    Cron = mtCron.Text,
                    Remark = mtRemark.Text,
                    CreateTime = DateTime.Now,
                    CreateBy = AppData.GetUserId(),
                    TaskCode = SnowFlake.GetInstance().GetUniqueId(),
                };
            }
            else 
            {
                wxMessageTemplate.Name = mtName.Text;
                wxMessageTemplate.Content = mtContent.Text;
                wxMessageTemplate.Enable = mtEnable.Active;
                wxMessageTemplate.Cron = mtCron.Text;
                wxMessageTemplate.Remark = mtRemark.Text;
                wxMessageTemplate.UpdateTime = DateTime.Now;
                wxMessageTemplate.UpdateBy = AppData.GetUserId();
            }
            

            var result = new MessageTemplateService().SaveOrUpdate(wxMessageTemplate);
            if (result)
            {
                if (wxMessageTemplate.Enable)
                {
                    //QuartzManage.StartOrModifyJob<MessageTemplateQuartz>(wxMessageTemplate.TaskCode, QuartzConstant.MESSAGE_TEMPLATE, wxMessageTemplate.Cron);
                }
                else 
                {
                    //QuartzManage.DeleteJob(wxMessageTemplate.TaskCode, QuartzConstant.MESSAGE_TEMPLATE);
                }
                
                messageTemplate.LoadData();
                this.Hide();
            }
        }
    }
}
