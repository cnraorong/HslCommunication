﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using HslCommunication.Profinet;
using HslCommunication;
using HslCommunication.ModBus;
using System.Threading;

namespace HslCommunicationDemo
{
    public partial class FormModbus : HslFormContent
    {
        public FormModbus()
        {
            InitializeComponent( );
        }


        private ModbusTcpNet busTcpClient = null;

        private void FormSiemens_Load( object sender, EventArgs e )
        {
            panel2.Enabled = false;

            comboBox1.SelectedIndex = 0;

            comboBox1.SelectedIndexChanged += ComboBox1_SelectedIndexChanged;
            checkBox3.CheckedChanged += CheckBox3_CheckedChanged;

            Language( Program.Language );
        }


        private void Language( int language )
        {
            if (language == 2)
            {
                Text = "Modbus Tcp Read Demo";

                label1.Text = "Ip:";
                label3.Text = "Port:";
                label21.Text = "station";
                checkBox1.Text = "address from 0";
                checkBox3.Text = "string reverse";
                button1.Text = "Connect";
                button2.Text = "Disconnect";
                
                label11.Text = "Address:";
                label12.Text = "length:";
                button25.Text = "Bulk Read";
                label13.Text = "Results:";
                label16.Text = "Message:";
                label14.Text = "Results:";
                button26.Text = "Read";

                groupBox3.Text = "Bulk Read test";
                groupBox4.Text = "Message reading test, hex string needs to be filled in";
                groupBox5.Text = "Special function test";

                button3.Text = "Pressure test, r/w 3,000s";

                label4.Text = "Account";
                label2.Text = "Pwd";
                label5.Text = "When the server is a server built by hsl, login with account name and password is supported.";

            }
        }

        private void ComboBox1_SelectedIndexChanged( object sender, EventArgs e )
        {
            if (busTcpClient != null)
            {
                switch (comboBox1.SelectedIndex)
                {
                    case 0: busTcpClient.DataFormat = HslCommunication.Core.DataFormat.ABCD;break;
                    case 1: busTcpClient.DataFormat = HslCommunication.Core.DataFormat.BADC; break;
                    case 2: busTcpClient.DataFormat = HslCommunication.Core.DataFormat.CDAB; break;
                    case 3: busTcpClient.DataFormat = HslCommunication.Core.DataFormat.DCBA; break;
                    default:break;
                }
            }
        }

        private void CheckBox3_CheckedChanged( object sender, EventArgs e )
        {
            if (busTcpClient != null)
            {
                busTcpClient.IsStringReverse = checkBox3.Checked;
            }
        }
        

        private void FormSiemens_FormClosing( object sender, FormClosingEventArgs e )
        {

        }
        

        #region Connect And Close



        private void button1_Click( object sender, EventArgs e )
        {
            // 连接
            if (!System.Net.IPAddress.TryParse( textBox1.Text, out System.Net.IPAddress address ))
            {
                MessageBox.Show( DemoUtils.IpAddressInputWrong );
                return;
            }


            if(!int.TryParse(textBox2.Text,out int port))
            {
                MessageBox.Show( DemoUtils.PortInputWrong );
                return;
            }


            if(!byte.TryParse(textBox15.Text,out byte station))
            {
                MessageBox.Show( "Station input is wrong！" );
                return;
            }

            busTcpClient?.ConnectClose( );
            busTcpClient = new ModbusTcpNet( textBox1.Text, port, station );
            busTcpClient.AddressStartWithZero = checkBox1.Checked;

            busTcpClient.SetLoginAccount( textBox14.Text, textBox12.Text );

            ComboBox1_SelectedIndexChanged( null, new EventArgs( ) );  // 设置数据服务
            busTcpClient.IsStringReverse = checkBox3.Checked;

            try
            {
                OperateResult connect = busTcpClient.ConnectServer( );
                if (connect.IsSuccess)
                {
                    MessageBox.Show( HslCommunication.StringResources.Language.ConnectedSuccess );
                    button2.Enabled = true;
                    button1.Enabled = false;
                    panel2.Enabled = true;

                    userControlReadWriteOp1.SetReadWriteNet( busTcpClient, "100", true );
                }
                else
                {
                    MessageBox.Show( HslCommunication.StringResources.Language.ConnectedFailed + connect.Message );
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show( ex.Message );
            }
        }

        private void button2_Click( object sender, EventArgs e )
        {
            // 断开连接
            busTcpClient.ConnectClose( );
            button2.Enabled = false;
            button1.Enabled = true;
            panel2.Enabled = false;
        }
        
        #endregion

        #region 批量读取测试

        private void button25_Click( object sender, EventArgs e )
        {
            DemoUtils.BulkReadRenderResult( busTcpClient, textBox6, textBox9, textBox10 );
        }



        #endregion

        #region 报文读取测试


        private void button26_Click( object sender, EventArgs e )
        {
            OperateResult<byte[]> read = busTcpClient.ReadFromCoreServer( HslCommunication.BasicFramework.SoftBasic.HexStringToBytes( textBox13.Text ) );
            if (read.IsSuccess)
            {
                textBox11.Text = "Result：" + HslCommunication.BasicFramework.SoftBasic.ByteToHexString( read.Content );
            }
            else
            {
                MessageBox.Show( "Read Failed：" + read.ToMessageShowString( ) );
            }
        }


        #endregion

        #region 压力测试

        private void button4_Click( object sender, EventArgs e )
        {
            PressureTest2( );
        }

        private int thread_status = 0;
        private int failed = 0;
        private DateTime thread_time_start = DateTime.Now;
        // 压力测试，开3个线程，每个线程进行读写操作，看使用时间
        private void PressureTest2( )
        {
            thread_status = 3;
            failed = 0;
            thread_time_start = DateTime.Now;
            new Thread( new ThreadStart( thread_test2 ) ) { IsBackground = true, }.Start( );
            new Thread( new ThreadStart( thread_test2 ) ) { IsBackground = true, }.Start( );
            new Thread( new ThreadStart( thread_test2 ) ) { IsBackground = true, }.Start( );
            button3.Enabled = false;
        }

        private void thread_test2( )
        {
            int count = 500;
            while (count > 0)
            {
                if (!busTcpClient.Write( "100", (short)1234 ).IsSuccess) failed++;
                if (!busTcpClient.ReadInt16( "100" ).IsSuccess) failed++;
                count--;
            }
            thread_end( );
        }

        private void thread_end( )
        {
            if (Interlocked.Decrement( ref thread_status ) == 0)
            {
                // 执行完成
                Invoke( new Action( ( ) =>
                {
                    button3.Enabled = true;
                    MessageBox.Show( "Spend：" + (DateTime.Now - thread_time_start).TotalSeconds + Environment.NewLine + " Read Failed：" + failed );
                } ) );
            }
        }
        
        #endregion
        
    }
}
