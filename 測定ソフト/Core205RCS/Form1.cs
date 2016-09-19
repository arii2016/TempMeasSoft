using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO.Ports;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Threading;
using System.Runtime.InteropServices;

namespace Core205RCS
{
    public partial class Form1 : Form
    {
        private SerialPort m_clsPort;
        private StreamWriter m_clsSW;
        /// <summary>スレッドクラス</summary>
        Thread m_clsThread;
        /// <summary>キャンセルFlg</summary>
        bool m_bCanncelFlg;
        delegate void deleEnableControl(bool bEnable);
        delegate void deleShowTemp(string strBuf);

        /// <summary>キャンセルFlg</summary>
        bool m_bTimerCanncelFlg;
        /// <summary>周波数</summary>
        long m_lpFrequency;
        long m_lStartTime;

        [DllImport("winmm.dll")]
        static extern int timeBeginPeriod(int uPeriod);
        [DllImport("winmm.dll")]
        static extern int timeEndPeriod(int uPeriod);

        [DllImport("kernel32.dll")]
        static extern bool QueryPerformanceFrequency(out long lpFrequency);
        [DllImport("kernel32.dll")]
        static extern bool QueryPerformanceCounter(out long lpPerformanceCount);

        public Form1()
        {
            InitializeComponent();

        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // 分解能を1ミリ秒に設定
            timeBeginPeriod(1);
            QueryPerformanceFrequency(out m_lpFrequency);

            string strComName = "";

            // COMポートを見つける
            ManagementClass sp = new ManagementClass("Win32_SerialPort");
            foreach (ManagementObject p in sp.GetInstances())
            {
                string strDeviceID = (string)p.GetPropertyValue("DeviceID");
                string strCaption = (string)p.GetPropertyValue("Caption");
                if (strCaption == "Arduino Mega 2560" + " (" + strDeviceID + ")")
                {
                    strComName = strDeviceID;
                }
            }
            if (strComName == "")
            {
                MessageBox.Show("COMの初期化に失敗しました。");
                return;
            }

            // COMポート初期化
            try
            {
                m_clsPort = new SerialPort(strComName, 115200, Parity.None, 8, StopBits.One);
                m_clsPort.Open();
                m_clsPort.ReadTimeout = 2500;
                m_clsPort.NewLine = "\n";
            }
            catch
            {
                MessageBox.Show("COMの初期化に失敗しました。");
                return;
            }
            // ダミーデーター送信
            m_clsPort.WriteLine("");
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            m_clsPort.Close();
            // 分解能をクリア
            timeEndPeriod(1);

        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (button1.Text == "開始")
            {
                // スレッド開始
                m_clsThread = new Thread(new ThreadStart(this.Execute));
                m_clsThread.Start();
            }
            else
            {
                // スレッド停止
                m_bTimerCanncelFlg = true;
                m_bCanncelFlg = true;
                while (m_clsThread.Join(1) == false)
                {
                    Application.DoEvents();
                }
            }
        }
        //--------------------------------------------------------
        public void Execute()
        {
            // ファイルを開く
            try
            {
                m_clsSW = new StreamWriter(Path.GetDirectoryName(Environment.GetCommandLineArgs()[0]) + "\\" + DateTime.Now.ToString("MMddHHmmss") + ".csv", false);
            }
            catch
            {
                MessageBox.Show("ファイル作成失敗");
                return;
            }

            // キャンセルFlg OFF
            m_bCanncelFlg = false;
            // ボタン変更
            Invoke(new deleEnableControl(EnableControl), new object[] { false });
            // レーザーON
            m_clsPort.WriteLine("LASER_ON");

            const int iMeasCnt = 30;

            for (int i = 0; i < iMeasCnt * 60; i++)
            {
                // キャンセルされた場合
                if (m_bCanncelFlg == true)
                {
                    goto RETURN_NG;
                }

                string strBuf;
                try
                {
                    // 受信データ破棄
                    m_clsPort.DiscardInBuffer();
                    // データ送信
                    m_clsPort.WriteLine("GET_THMC");
                    // データ受信
                    strBuf = m_clsPort.ReadLine();

                    Invoke(new deleShowTemp(ShowTemp), new object[] { strBuf });
                }
                catch
                {
                    goto RETURN_NG;
                }

                if (i % 60 == 0)
                {
                    // 1分ごとにファイルに書き込み
                    m_clsSW.Write("{0},{1}", DateTime.Now.ToLongTimeString(), strBuf);
                }

                // 1秒スリープ
                WaitTime(1000*1000);
            }
            // 終了音を鳴らす
            Console.Beep(2000, 3000);

        RETURN_NG:
            // レーザーON
            m_clsPort.WriteLine("LASER_OFF");
            // ファイルを閉じる
            m_clsSW.Flush();
            m_clsSW.Close();
            // ボタン変更
            Invoke(new deleEnableControl(EnableControl), new object[] { true });
        }
        public bool WaitTime(long Microseconds)
        {
            m_bTimerCanncelFlg = false;
            QueryPerformanceCounter(out m_lStartTime);
            while (true)
            {
                if (m_bTimerCanncelFlg == true)
                {
                    return false;
                }
                if (LapTime() > Microseconds)
                {
                    break;
                }
                Application.DoEvents();
            }

            return true;
        }
        public long LapTime()
        {
            long lEndTime;

            QueryPerformanceCounter(out lEndTime);

            return (lEndTime - m_lStartTime) * 1000000 / m_lpFrequency;
        }
        //--------------------------------------------------------
        public void EnableControl(bool bEnable)
        {
            if (bEnable == false)
            {
                button1.Text = "停止";
            }
            else
            {
                button1.Text = "開始";
            }
        }
        //--------------------------------------------------------
        public void ShowTemp(string strBuf)
        {
            string[] strTemp = strBuf.Replace("\r", "").Replace("\n", "").Split(',');
            label1.Text = "温度1：" + strTemp[0] + "°";
            label2.Text = "温度2：" + strTemp[1] + "°";

        }
        //--------------------------------------------------------
    }
}
