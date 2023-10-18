using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace SoftwareReceiverWindowsService
{
    public partial class Service1 : ServiceBase
    {
        public Service1()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            TcpClientExample.Main();
            string Message = "finjaoif";

            string path = AppDomain.CurrentDomain.BaseDirectory  + "\\logs";
            if(!Directory.Exists(path)) 
            { 
                Directory.CreateDirectory(path);    
            }

            string filepath = AppDomain.CurrentDomain.BaseDirectory + "\\Logs\\ServiceLog_" + DateTime.Now.Date.ToString();
            if (!File.Exists(filepath))
            {
                using (StreamWriter writer = File.CreateText(filepath))
                {
                    writer.WriteLine(Message);
                }
            }
            else 
            {
                using (StreamWriter writer = File.AppendText(filepath))
                {
                    writer.WriteLine(Message);
                }
            }
        }

        protected override void OnStop()
        {
            string Message = "messages";

            string path = AppDomain.CurrentDomain.BaseDirectory + "\\logs";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            string filepath = AppDomain.CurrentDomain.BaseDirectory + "\\Logs\\ServiceLog_" + DateTime.Now.Date.ToString();
            if (!File.Exists(filepath))
            {
                using (StreamWriter writer = File.CreateText(filepath))
                {
                    writer.WriteLine(Message);
                }
            }
            else
            {
                using (StreamWriter writer = File.AppendText(filepath))
                {
                    writer.WriteLine(Message);
                }
            }
        }
    }
}
