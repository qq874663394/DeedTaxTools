//email:874663394@qq.com
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ToolsHelper;
using DeedTax.Properties;

namespace WindowsFormsApp1
{
    public partial class frmMain : Form
    {
        private static bool flag_While = false;
        private static int scrollBarValue = 1;
        private static string utoken;
        private static long qishuiNo;
        private static string name;
        private static string idcard;
        private static int uniacid = 263;
        private static int eventsId = 0;
        private static int WeekInMaxSid = 0;
        private static DateTime YuYueTime;
        private static Data sourceByDid = null;
        private static List<Time_range> time_Range = null;
        public frmMain()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            LoadUserSettingInfo();
            Task.Factory.StartNew(() =>
            {
                SetLabel("正在获取基础信息,请勿操作");
                EnableAllButton();
                if (Init())
                {
                    EnableAllButton(true);
                }
            });
        }


        private void EnableAllButton(bool flag = false)
        {
            foreach (var item in Controls)
            {
                Button button = item as Button;
                if (button != null)
                {
                    button.Invoke(new Action(() => { button.Enabled = flag; }));
                }
            }
        }
        private void LoadUserSettingInfo()
        {
            textBox1.Text = Settings.Default.token;
            textBox2.Text = Settings.Default.qishuihao;
            textBox3.Text = Settings.Default.name;
            textBox4.Text = Settings.Default.identity;
        }
        Thread[] thread = null;
        private void button1_Click(object sender, EventArgs e)
        {
            flag_While = true;
            int threadCount = trackBar1.Value;

            ThreadPool.SetMaxThreads(threadCount, threadCount);
            ThreadPool.SetMinThreads(1, 1);
            Task.Factory.StartNew(new Action(() =>
            {
                for (int i = 0; i < threadCount; i++)
                {
                    ThreadPool.QueueUserWorkItem(GetYuYueDengJi, null);
                }
            }));

            //Task.Factory.StartNew(new Action(() =>
            //{
            //    for (int i = 0; i < threadCount; i++)
            //    {
            //        thread = new Thread[threadCount];
            //        thread[i] = new Thread(new ParameterizedThreadStart(GetYuYueDengJi));
            //        thread[i].IsBackground = true;
            //        thread[i].Start();//逐个开启线程
            //    }
            //}));
        }
        /// <summary>
        /// 设置状态栏
        /// </summary>
        /// <param name="msg"></param>
        private void SetLabel(string msg)
        {
            label2.Invoke(new Action(() => { label2.Text = msg; LogHelper.WriteLog(msg); }));
        }
        /// <summary>
        /// 初始化，请求需要的数据
        /// </summary>
        /// <returns></returns>
        public bool Init()
        {
            utoken = textBox1.Text.Trim();
            name = textBox3.Text.Trim();
            idcard = textBox4.Text.Trim();
            YuYueTime = dateTimePicker1.Value;
            bool flag = true;
            while (flag)
            {
                Thread.Sleep(3000);
                if (!long.TryParse(textBox2.Text.Trim(), out qishuiNo))
                {
                    SetLabel("获取契税号失败,重试中...");
                    continue;
                }
                sourceByDid = GetSourceByDid(1, utoken, uniacid);
                if (sourceByDid == null || sourceByDid.time_range.Count <= 0)
                {
                    SetLabel("获取基础数据失败,重试中...");
                    continue;
                }
                if (sourceByDid.events == null || sourceByDid.events.Count <= 0)
                {
                    SetLabel("获取events数据失败,重试中...");
                    continue;
                }
                if (sourceByDid.week == null || sourceByDid.week.Count <= 0)
                {
                    SetLabel("获取week数据失败,重试中...");
                    continue;
                }
                WeekInMaxSid = sourceByDid.week.Max(p => p.id);
                if (WeekInMaxSid <= 0)
                {
                    SetLabel("获取基础数据WeekID失败,重试中...");
                    continue;
                }
                flag = false;
            }
            this.BeginInvoke(new Action(() =>
            {
                this.Text = sourceByDid.events[0].name;
            }));
            eventsId = sourceByDid.events[0].id;
            uniacid = sourceByDid.events[0].uniacid;
            while (time_Range == null || sourceByDid.events.Count <= 0)
            {
                time_Range = getDetailSourceBySid(WeekInMaxSid, utoken, uniacid);
                if (sourceByDid == null)
                {
                    SetLabel("获取明细数据失败,重试中...");
                }
            }
            SetLabel("获取所有数据成功");
            return true;
        }
        private void GetYuYueDengJi(object state = null)
        {
            int threadManagerId = Thread.CurrentThread.ManagedThreadId;
            if (time_Range == null)
            {
                LogHelper.WriteLog($"ThreadID={threadManagerId},time_Range==null");
                Init();
            }
            do
            {
                if (!getTimeSpan(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")))
                {
                    LogHelper.WriteLog($"ThreadID={threadManagerId},不在指定时间范围内");
                    SetLabel($"{DateTime.Now}—ThreadID={threadManagerId},不在指定时间范围内");
                    Thread.Sleep(1000);
                    continue;
                }
                foreach (var p in time_Range)
                {
                    System.Diagnostics.Stopwatch watch = new Stopwatch();
                    watch.Start();
                    if (p == null)
                    {
                        LogHelper.WriteLog($"ThreadID={threadManagerId},ForEach=>time_Range=>Model==null");
                        continue;
                    }

                    ResultMessage resultMsg = SetSubscribe(qishuiNo, name, idcard, p.s_r_id, "2ea889e581914defb02abe74ea88a111", p.id, 1, eventsId, utoken, uniacid);
                    if (resultMsg == null)
                    {
                        LogHelper.WriteLog($"ThreadID={threadManagerId},ForEach=>time_Range=>SetSubscribe=>resultMsg==null");
                        continue;
                    }
                    string msg = resultMsg.msg;
                    watch.Stop();
                    var mSeconds = watch.ElapsedMilliseconds;

                    object[] obj = new object[] { DateTime.Now.ToString("MM-dd HH:mm:ss fff"), sourceByDid.week.Find(c => c.id == WeekInMaxSid).date_time, p.time_range, msg + "—" + p.id + "—" + threadManagerId, mSeconds };
                    string objStr = string.Join(",", obj.ToList());
                    LogHelper.WriteLog($"ThreadID={threadManagerId}，obj={objStr}");
                    dataGridView1.BeginInvoke(new Action(() =>
                    {
                        dataGridView1.Rows.Insert(0, obj);
                    }));

                }
            }
            while (flag_While);
        }

        private void button6_Click(object sender, EventArgs e)
        {
            Task.Factory.StartNew(new Action(() =>
            {
                int threadManagerId = Thread.CurrentThread.ManagedThreadId;
                for (int i = WeekInMaxSid; i <= WeekInMaxSid + 20; i++)
                {
                    List<Time_range> time_Range_Next = getDetailSourceBySid(i, utoken, uniacid);

                    if (time_Range_Next == null)
                    {
                        LogHelper.WriteLog($"ThreadID={threadManagerId},time_Range==null");
                        Init();
                    }
                    if (time_Range_Next != null && time_Range_Next.Count > 0)
                    {
                        foreach (Time_range p in time_Range_Next)
                        {
                            System.Diagnostics.Stopwatch watch = new Stopwatch();
                            watch.Start();
                            if (p == null)
                            {
                                LogHelper.WriteLog($"ThreadID={threadManagerId},ForEach=>time_Range=>Model==null");
                                continue;
                            }
                            ResultMessage resultMsg = SetSubscribe(qishuiNo, name, idcard, p.s_r_id, "2ea889e581914defb02abe74ea88a111", p.id, 1, eventsId, utoken, uniacid);
                            if (resultMsg == null)
                            {
                                LogHelper.WriteLog($"ThreadID={threadManagerId},ForEach=>time_Range=>SetSubscribe=>resultMsg==null");
                                continue;
                            }
                            watch.Stop();
                            var mSeconds = watch.ElapsedMilliseconds;
                            object[] obj = new object[] { DateTime.Now.ToString("MM-dd HH:mm:ss fff"), sourceByDid.week.Find(c => c.id == WeekInMaxSid).date_time, p.time_range, $"尝试提前预约{i}结果：{resultMsg.msg}", mSeconds };
                            string objStr = string.Join(",", obj.ToList());
                            LogHelper.WriteLog($"ThreadID={threadManagerId}，obj={objStr}");
                            //AddRow(obj);
                        }
                    }
                    else
                    {
                        //AddRow(new object[] { DateTime.Now.ToString("MM-dd HH:mm:ss fff"), "", "", $"最新周数{WeekInMaxSid},最大周数{i}", "" });
                        break;
                    }
                }
            }));
        }

        private void button3_Click(object sender, EventArgs e)
        {
            flag_While = false;
            GetYuYueDengJi();
        }
        /// <summary>
        /// 主数据did=1&utoken=oodke5IvxWr4J41ZZ1H0xyaz2YEg&uniacid=263
        /// </summary>
        /// <param name="did"></param>
        /// <param name="utoken"></param>
        /// <param name="uniacid"></param>
        /// <returns></returns>
        public Data GetSourceByDid(int did, string utoken, int uniacid)
        {
            WebHeaderCollection header = new WebHeaderCollection();
            header.Add(HttpRequestHeader.AcceptEncoding, "gzip, deflate");
            header.Add("client", "XCX");
            HttpHelper http = new HttpHelper();
            HttpItem item = new HttpItem()
            {
                ContentType = "application/x-www-form-urlencoded",
                Encoding = System.Text.Encoding.UTF8,
                PostEncoding = System.Text.Encoding.UTF8,
                Host = "yuyue.csdfa.cn",
                KeepAlive = true,
                Header = header
            };
            item.URL = $"https://yuyue.csdfa.cn/addons/yb_yuyue/index.php?s=api/user/getSourceByDid";
            item.Method = "post";
            item.Postdata = $"did={did}&utoken={utoken}&uniacid={uniacid}";
            HttpResult result = http.GetHtml(item);
            if (result == null || result.StatusCode != System.Net.HttpStatusCode.OK)
            {
                LogHelper.WriteLog($"SetSubscribe==null,Param={item.Postdata},Html={item}");
                return null;
            }
            string context = Regex.Unescape(result.Html);
            context = context.Replace("\"rule\":\"", "\"rule\":").Replace("}]\",\"week\":", "}],\"week\":");
            ResultMessage rm = JsonHelper.DeserializeJsonToObject<ResultMessage>(context);
            if (rm == null)
            {
                LogHelper.WriteLog($"rm==null,Param={item.Postdata},context={context}");
                return null;
            }
            rm.data = JsonHelper.DeserializeJsonToObject<Data>(rm.data.ToString());
            return rm.data as Data;
        }
        /// <summary>
        /// 获取用户选择日期的详细信息
        /// </summary>
        /// <param name="sid">传入上一次获取的week的id值，对应界面选择日期</param>
        /// <param name="utoken"></param>
        /// <param name="uniacid"></param>
        /// <returns></returns>
        public List<Time_range> getDetailSourceBySid(int sid, string utoken, int uniacid)
        {

            WebHeaderCollection header = new WebHeaderCollection();
            header.Add(HttpRequestHeader.AcceptEncoding, "gzip, deflate");
            header.Add("client", "XCX");
            HttpHelper http = new HttpHelper();
            HttpItem item = new HttpItem()
            {
                ContentType = "application/x-www-form-urlencoded",
                Encoding = System.Text.Encoding.UTF8,
                PostEncoding = System.Text.Encoding.UTF8,
                Host = "yuyue.csdfa.cn",
                KeepAlive = true,
                Header = header
            };
            item.URL = $"https://yuyue.csdfa.cn/addons/yb_yuyue/index.php?s=api/user/getDetailSourceBySid";
            item.Method = "post";
            item.Postdata = $"sid={sid}&utoken={utoken}&uniacid={uniacid}";
            HttpResult result = http.GetHtml(item);
            if (result == null || result.StatusCode != System.Net.HttpStatusCode.OK)
            {
                LogHelper.WriteLog($"SetSubscribe==null,Param={item.Postdata},Html={item}");
                return null;
            }
            //string context = Regex.Unescape(result.Html);
            //string context = result.Html.Replace("\"data\":[{", "\"data\":{").Replace("}]}", "}}");
            ResultMessage rm = JsonHelper.DeserializeJsonToObject<ResultMessage>(result.Html);
            if (rm == null)
            {
                LogHelper.WriteLog($"rm==null,Param={item.Postdata},context={result.Html}");
                return null;
            }
            rm.data = JsonHelper.DeserializeJsonToObject<List<Time_range>>(rm.data.ToString());
            return rm.data as List<Time_range>;

        }
        /// <summary>
        /// 预约登记
        /// </summary>
        /// <param name="sid"></param>
        /// <param name="utoken"></param>
        /// <param name="uniacid"></param>
        /// <returns></returns>
        public ResultMessage SetSubscribe(long laifang, string name, string idcard, int dateId, string formId, int timerangeId, int departmentId, int eventsId, string utoken, int uniacid)
        {
            ResultMessage rm = null;

            WebHeaderCollection header = new WebHeaderCollection();
            header.Add(HttpRequestHeader.AcceptEncoding, "gzip, deflate");
            header.Add("client", "XCX");
            HttpHelper http = new HttpHelper();
            HttpItem item = new HttpItem()
            {
                ContentType = "application/x-www-form-urlencoded",
                Encoding = System.Text.Encoding.UTF8,
                PostEncoding = System.Text.Encoding.UTF8,
                Host = "yuyue.csdfa.cn",
                KeepAlive = true,
                Header = header
            };
            item.URL = $"https://yuyue.csdfa.cn/addons/yb_yuyue/index.php?s=api/user/subscribe";
            item.Method = "post";
            item.Postdata = $"laifang={laifang}&name={name}&idcard={idcard}&date={dateId}&formId={formId}&timerange={timerangeId}&department={departmentId}&events={eventsId}&utoken={utoken}&uniacid={uniacid}";
            HttpResult result = http.GetHtml(item);
            if (result == null)
            {
                LogHelper.WriteLog($"SetSubscribe==null,Param={item.Postdata},Html={item}");
                return rm;
            }
            rm = JsonHelper.DeserializeJsonToObject<ResultMessage>(result.Html);
            return rm;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            flag_While = false;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            dataGridView1.Rows.Clear();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            Settings.Default.token = textBox1.Text.Trim();
            Settings.Default.qishuihao = textBox2.Text.Trim();
            Settings.Default.name = textBox3.Text.Trim();
            Settings.Default.identity = textBox4.Text.Trim();
            YuYueTime = dateTimePicker1.Value;
            DeedTax.Properties.Settings.Default.Save();//使用Save方法保存更改
            LoadUserSettingInfo();
        }

        private void dataGridView1_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            if (dataGridView1 == null || dataGridView1.Rows.Count <= 0)
            {
                return;
            }
            if (dataGridView1.Rows.Count >= 5000)
            {
                dataGridView1.Rows.Clear();
            }
        }

        private void trackBar1_ValueChanged(object sender, EventArgs e)
        {
            if (flag_While)
            {
                SetLabel("在任务中改变线程数,需要手动重新运行任务");
                flag_While = false;
            }
            scrollBarValue = trackBar1.Value;
            label8.Text = scrollBarValue.ToString();
        }
        protected bool getTimeSpan(string timeStr)
        {
            //判断当前时间是否在工作时间段内
            string _strWorkingDayAM = "20:40";//工作时间上午08:30
            string _strWorkingDayPM = "21:10";
            TimeSpan dspWorkingDayAM = DateTime.Parse(_strWorkingDayAM).TimeOfDay;
            TimeSpan dspWorkingDayPM = DateTime.Parse(_strWorkingDayPM).TimeOfDay;

            //string time1 = "2017-2-17 8:10:00";
            DateTime t1 = Convert.ToDateTime(timeStr);

            TimeSpan dspNow = t1.TimeOfDay;
            if (dspNow > dspWorkingDayAM && dspNow < dspWorkingDayPM)
            {
                return true;
            }
            return false;
        }
    }
    public class ResultMessage
    {
        public int code { get; set; }
        public string msg { get; set; }
        public object data { get; set; }
    }
    public class Data
    {
        public List<Week> week { get; set; }
        public List<Events> events { get; set; }
        public List<Time_range> time_range { get; set; }
        public int all { get; set; }
    }
    public class Week : Data
    {
        public int id { get; set; }
        public int d_id { get; set; }
        public string date_time { get; set; }
        public List<Rule> rule { get; set; }
        public int week { get; set; }
        public long create_time { get; set; }
        public int status { get; set; }
        public int uniacid { get; set; }
        public string week_status { get; set; }
    }
    public class Rule : Week
    {
        public string t_s { get; set; }
        public string t_e { get; set; }
        public string num { get; set; }
        public string num2 { get; set; }
        public string num3 { get; set; }
    }
    public class Events : Data
    {
        public int id { get; set; }
        public string name { get; set; }
        public int uniacid { get; set; }
        public int status { get; set; }
        public int sort { get; set; }
        public int d_id { get; set; }
    }
    public class Time_range : Data
    {
        public int id { get; set; }
        public int s_r_id { get; set; }
        public string time_range { get; set; }
        public int num { get; set; }
        public int num2 { get; set; }
        public int num3 { get; set; }
    }
}
