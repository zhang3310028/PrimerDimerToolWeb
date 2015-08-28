using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Data;
using MSExcel = Microsoft.Office.Interop.Excel;
using System.Runtime.InteropServices;
using RDotNet;
using System.IO;
using System.Threading;
using System.Diagnostics;
using System.Xml;
using System.Collections;
using System.Web.Script.Serialization;


namespace PrimerDimerToolWeb
{
    public partial class WebForm2 : System.Web.UI.Page
    {
        private String key;
        JavaScriptSerializer js = new JavaScriptSerializer();
        public WebForm2()
        {

        }
        protected void Page_Load(object sender, EventArgs e)
        {
            string time = Request.QueryString["t"];
            key = Request.QueryString["guid"];
            if (String.IsNullOrEmpty(key)) key = Guid.NewGuid().ToString();
            string statusText = getStatus(key);
            if (time != null)
            {
                CustomTask customTask = (CustomTask)Cache.Get(key);
                if (statusText == "complete")
                {
                    Response.Clear();
                    Response.Write("<a href='" + customTask.url + "'>download result</a>");
                    Response.End();

                    Cache.Remove(key);
                }
                else
                {
                    string customJson =js.Serialize(customTask);
                    Response.Clear();
                    Response.Write(customJson);
                    Response.End();
                }
            }
            else
            {
                guid.Text = key;
                if (System.IO.File.Exists(Server.MapPath(key + ".txt")))
                {
                    System.IO.File.Delete(Server.MapPath(key + ".txt"));
                }
            }
        }

        private string getStatus(string key)
        {
            CustomTask customTask = (CustomTask)Cache.Get(key);
            if (customTask ==null || String.IsNullOrEmpty(customTask.status))
            {
                return "";
            }
            else
            {
                return customTask.status;
            }
        }
        private void WriteLog(string key, CustomTask customTask)
        {
            string basedir = AppDomain.CurrentDomain.BaseDirectory;
            string log_dir = basedir + "/logs";
            if (!Directory.Exists(log_dir))
            {
                Directory.CreateDirectory(log_dir);
            }
            System.IO.StreamWriter sw = new System.IO.StreamWriter(log_dir + "/" + key + ".txt", true);
            sw.WriteLine("Status = " + customTask.status + " " + System.DateTime.Now.ToString());
            sw.Close();
        }
        protected void Button1_Click(object sender, EventArgs e)
        {
            if (FileUpload1.HasFile)
            {

                CustomTask customTask = new CustomTask();
                customTask.key = key;
                customTask.status = "uploading ...";
                Queue task_queue = (Queue)Application["task_queue"];
                customTask.percent = 0;
                WriteLog(customTask.key, customTask);
                string fileName = this.FileUpload1.FileName;
                string fileExtensionApplication = System.IO.Path.GetExtension(fileName);
                string newFileName = Guid.NewGuid().ToString() + fileExtensionApplication;
                string mapPath = HttpContext.Current.Request.MapPath("~/tmp/");
                string newFilePath = mapPath + newFileName;
                this.FileUpload1.SaveAs(newFilePath);
                customTask.url = "tmp/" + newFileName;
                Application.Lock();
                customTask.status = "waiting ...";
                task_queue = (Queue)Application["task_queue"];
                Object running_task = Application["running_task"];
                if (running_task != null)
                {
                    customTask.waitQueue = 1;
                }
                customTask.waitQueue += task_queue.Count;
                customTask.percent = 0;
                task_queue.Enqueue(customTask);
                Cache.Insert(key, customTask);
                Application.UnLock();
                WriteLog(customTask.key, customTask);


            }
            
        }


    }
    public class CustomTask
    {
        public string key;

        public string status;
        public string url;
        public int waitQueue;
        public int percent;
        public CustomTask()
        {
        }
    }
}