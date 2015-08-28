using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.SessionState;
using System.Collections;
using System.Data;
using MSExcel = Microsoft.Office.Interop.Excel;
using System.Runtime.InteropServices;
using RDotNet;
using System.IO;
using System.Threading;
using System.Diagnostics;

namespace PrimerDimerToolWeb
{
    public class Global : System.Web.HttpApplication
    {
        Queue task_queue = null;
        Thread thread = null;
        public class ThreadTransfer
        {
            public string cmd;
            public AutoResetEvent evt;
            public ThreadTransfer(string cmd, AutoResetEvent evt)
            {
                this.cmd = cmd;
                this.evt = evt;
            }
        }


        protected void Application_Start(object sender, EventArgs e)
        {
            task_queue = new Queue();
            Application["task_queue"] = task_queue;
            Application["running_task"] = null;
            thread = new Thread(new ThreadStart(runTask));
            thread.Start();
        }

        private void runTask()
        {
            string basedir = AppDomain.CurrentDomain.BaseDirectory;
            string mapPath = basedir + "/tmp/";
//            string mapPath = HttpContext.Current.Server.MapPath("~/tmp/");
            REngine engine = Utils.getREngineInstance();
            string primer3path = System.Web.Configuration.WebConfigurationManager.AppSettings["primer3Home"];
            string processNum = System.Web.Configuration.WebConfigurationManager.AppSettings["processNum"];
            string isDeleteTempDir = System.Web.Configuration.WebConfigurationManager.AppSettings["deleteTempDir"];
            while (true)
            {
                if (task_queue.Count != 0)
                {
                    Application.Lock();
                    CustomTask customTask = (CustomTask)task_queue.Dequeue();
                    Application.UnLock();
                    customTask.waitQueue = 0;
                    Object[] task_queue_array = task_queue.ToArray();
                    for (int i = 0; i < task_queue_array.Length; i++)
                    {
                        CustomTask tmpTask = (CustomTask)task_queue_array[i];
                        tmpTask.waitQueue = i + 1;
                    }
                        Application["running_task"] = customTask;
                        string fileName = customTask.url;
                        fileName = basedir + "/" + customTask.url;
                        DataTable dt = read_primer_sequence(fileName);
                        string[,] primerMat = getPrimerMat(dt);
                        customTask.status = "preparing ...";
                        WriteLog(customTask.key,customTask);
                        CharacterMatrix primer = engine.CreateCharacterMatrix(primerMat);
                        string rand_file = System.IO.Path.GetRandomFileName();
                        string tmp_path = mapPath + rand_file;
                        //string primer3path = "D:/Install/primer3-win-bin-2.3.6";

                        if (Directory.Exists(tmp_path))
                        {
                            DirectoryInfo di = new DirectoryInfo(tmp_path);
                            di.Delete(true);
                        }
                        else if (File.Exists(tmp_path))
                        {
                            FileInfo fi = new FileInfo(tmp_path);
                            fi.Delete();
                        }
                        Directory.CreateDirectory(tmp_path);
                        engine.Evaluate("library(xlsx)");
                        customTask.percent = 8;
                        string script_path = basedir + "/primer_dimer_check.R";
                        script_path=script_path.Replace(@"\", @"/");
                        engine.Evaluate("source(\"" + script_path + "\")");
                        customTask.percent = 10;
                        engine.SetSymbol("primer", primer);
                        engine.SetSymbol("tmp_dir", engine.CreateCharacter(tmp_path));
                        engine.SetSymbol("primer", primer);
                        engine.SetSymbol("primer3dir", engine.CreateCharacter(primer3path));
                        int? nProcess = Convert.ToInt32(processNum);
                        if (nProcess != null)
                        {
                            engine.SetSymbol("nprocess", engine.CreateInteger(Convert.ToInt32(nProcess)));
                        }
                        else
                        {
                            engine.SetSymbol("nprocess", engine.CreateInteger(4));
                        }
                        engine.SetSymbol("outputfile", engine.CreateCharacter(fileName));
                        string[] bat_cmds = engine.Evaluate("prepare_bat(tmp_dir,primer,primer3dir,nprocess)").AsCharacter().ToArray();
                        customTask.status = "dimer calculating ...";
                        customTask.percent = 20;
                        WriteLog(customTask.key, customTask);
                        AutoResetEvent[] resets = new AutoResetEvent[bat_cmds.Length];

                        for (int i = 0; i < bat_cmds.Length; i++)
                        {
                            resets[i] = new AutoResetEvent(false);
                            Global.ThreadTransfer transfer = new Global.ThreadTransfer(bat_cmds[i], resets[i]);
                            Thread thread = new Thread(new ParameterizedThreadStart(run_cmd));
                            thread.Start(transfer);
                        }
                        foreach (var v in resets)
                        {
                            v.WaitOne();
                            customTask.percent += 60 / resets.Length;
                        }
                        customTask.status = "result generating ...";
                        customTask.percent = 80;
                        WriteLog(customTask.key, customTask);
                        engine.Evaluate("output_result(tmp_dir,primer,outputfile)");
                        if (isDeleteTempDir == "true")
                        {
                            DirectoryInfo di = new DirectoryInfo(tmp_path);
                            di.Delete(true);
                        }
                        customTask.status = "complete";
                        customTask.percent = 100;
                        WriteLog(customTask.key, customTask);


                        Application["running_task"] = null;

                   
                    
                }
            }
        }
        private void SaveData(CustomTask customTask)
        {
            string key = customTask.key;
            WriteLog(key,customTask);
        }
        private void WriteLog(string key,CustomTask customTask)
        {
            string basedir = AppDomain.CurrentDomain.BaseDirectory;
            string log_dir = basedir + "/logs";
            if (!Directory.Exists(log_dir))
            {
                Directory.CreateDirectory(log_dir);
            }
            System.IO.StreamWriter sw = new System.IO.StreamWriter(log_dir + "/" + key + ".txt", true);
            sw.WriteLine("Status = " + customTask.status +" "+ System.DateTime.Now.ToString());
            sw.Close();
        }
        static void run_cmd(object obj)
        {
            Global.ThreadTransfer transfer = (Global.ThreadTransfer)obj;
            Execute(transfer.cmd, 10);
            transfer.evt.Set();
        }
        public static string Execute(string command, int seconds)
        {
            string output = "";
            if (command != null && !command.Equals(""))
            {
                Process process = new Process();
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.FileName = "cmd.exe";
                startInfo.Arguments = "/C " + command;
                startInfo.UseShellExecute = false;
                startInfo.RedirectStandardInput = false;
                startInfo.RedirectStandardOutput = true;
                startInfo.CreateNoWindow = true;
                process.StartInfo = startInfo;
                try
                {
                    if (process.Start())
                    {
                        if (seconds == 0)
                        {
                            process.WaitForExit();
                        }
                        else
                        {
                            process.WaitForExit(seconds);
                        }
                        output = process.StandardOutput.ReadToEnd();
                    }
                }
                catch
                {
                }
                finally
                {
                    if (process != null)
                        process.Close();
                }
            }
            return output;
        }
        protected void Session_Start(object sender, EventArgs e)
        {

        }

        protected void Application_BeginRequest(object sender, EventArgs e)
        {

        }

        protected void Application_AuthenticateRequest(object sender, EventArgs e)
        {

        }

        protected void Application_Error(object sender, EventArgs e)
        {

        }

        protected void Session_End(object sender, EventArgs e)
        {

        }

        protected void Application_End(object sender, EventArgs e)
        {
            thread.Abort();
        }
         private static string[,] getPrimerMat(DataTable dt)
        {
            string[,] primerMat = new string[dt.Rows.Count, 3];
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                string seqID = dt.Rows[i]["seqID"].ToString();
                string f_seq = dt.Rows[i]["f_sequence"].ToString();
                string r_seq = dt.Rows[i]["r_sequence"].ToString();
                primerMat[i, 0] = seqID;
                primerMat[i, 1] = f_seq;
                primerMat[i, 2] = r_seq;
            }
            return primerMat;
        }
        private DataTable read_primer_sequence(string filename)
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("seqID");
            dt.Columns.Add("f_sequence");
            dt.Columns.Add("r_sequence");

            MSExcel.Application excelApp = new MSExcel.Application();
            excelApp.Workbooks.Open(filename);
            //string tabName = excelApp.Workbooks[1].Worksheets[1].Name;
            excelApp.Workbooks[1].Worksheets[1].Activate();

            int i = 2;
            while (true)
            {
                DataRow dr = dt.NewRow();

                string id = Convert.ToString(excelApp.Cells[i, 1].Value);
                string f_sequence = excelApp.Cells[i, 2].Value;
                string r_sequence = excelApp.Cells[i, 3].Value;
                if (id == null || id.Length < 1 || id.Length < 1) break;
                dr[0] = id;
                dr[1] = f_sequence;
                dr[2] = r_sequence;
                dt.Rows.Add(dr);
                i++;
            }

            release_excel_app(ref excelApp);
            return dt;
        }
        
        [DllImport(@"C:\Windows\System32\User32.dll", CharSet = CharSet.Auto)]
        public static extern int GetWindowThreadProcessId(IntPtr hwnd, out int ID);
        private void release_excel_app(ref MSExcel.Application excelApp){
            excelApp.Workbooks[1].Close();
            excelApp.Quit();
            if (excelApp != null){
                int lpdwProcessID;
                GetWindowThreadProcessId(new IntPtr(excelApp.Hwnd), out lpdwProcessID); 
                System.Diagnostics.Process.GetProcessById(lpdwProcessID).Kill();
            }
        }
    }
}