using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;

namespace testGetID
{
    public partial class xMCU : Form
    {
        /* you may read these from an *.ini file. */
        int serial_number;
        uint MCU_MAC_ADDRESS;
        int MCU_MAC_LENGTH = 3;
        uint MCU_ADDRESS_OFFSET = 0x08000000;
        int MCU_PROG_SPEED = 4000;
        uint MCU_FLASH_OBR_REGISTER_ADDRESS;
        string MCU_FAMILY;
        string MCU_DEVICE;
        string firmware_file_path;
        string execProg = System.Environment.CurrentDirectory + "\\JLink.exe";
        string DLL_VERSION_STRING = "DLL version V";
        string COMPILED_STRING = ", compiled";
        string JLINK_HARDWARE_NEW_STRING = "Hardware version: ";
        string FIRMWARE_STRING = "Firmware:";
        string JLINK_COMPILED_STRING = "compiled";
        DataTable myTable = new DataTable();
        Process pConsole = new Process();

        public xMCU()
        {
            InitializeComponent();
            combo_device.SelectedIndex = 0;
            combo_device.Enabled = true;
            check_Fuse.Enabled = false;
            textBox_serial.Enabled = true;
            serial_number = Convert.ToInt32(textBox_serial.Text);
            if (false == File.Exists(execProg))
            {
                MessageBox.Show("Jlink程序组件不存在!");
                return ;   //Jlink.exe dose not exists.
            }

            /* initialize needed information of JLink.exe process. */
            pConsole.StartInfo.FileName = execProg;
            pConsole.StartInfo.UseShellExecute = false;
            pConsole.StartInfo.RedirectStandardInput = true;
            pConsole.StartInfo.RedirectStandardOutput = true;
            pConsole.StartInfo.RedirectStandardError = true;
            pConsole.StartInfo.CreateNoWindow = true;

            /* initialize DataTable for myTable variable. */
            myTable.Columns.Add("编号");
            myTable.Columns.Add("UID");

            /* initialize DataGridView for dataGridUID variable. */
            dataGridUID.DataSource = myTable;
            dataGridUID.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            button_color_init();
        }

        private bool is_JLink_exe_existed() /* check if JLink.exe is existed. */
        {
            if (!File.Exists(execProg))
            {
                //MessageBox.Show("JLink.exe is not existed!", "error report", MessageBoxButtons.OK, MessageBoxIcon.Error);
                MessageBox.Show("JLink.exe不存在!", "错误报告", MessageBoxButtons.OK, MessageBoxIcon.Error);

                return false;
            }

            return true;
        }

        private bool is_firmware_file_existed() /* check if firmware file is existed. */
        {
            if ((String.Empty == firmware_file_path) || (!File.Exists(firmware_file_path)))
            {
                //MessageBox.Show("firmware file is not existed!", "error report", MessageBoxButtons.OK, MessageBoxIcon.Error);
                MessageBox.Show("固件文件不存在!", "错误报告", MessageBoxButtons.OK, MessageBoxIcon.Error);

                return false;
            }

            return true;
        }

        private bool JLink_DLL_version_get(string message) /* get DLL version of JLink.exe. */
        {
            int index;

            index = message.IndexOf(DLL_VERSION_STRING);
            if (0 > index)
            {
                //MessageBox.Show("JLink.exe DLL version get fail!", "error report", MessageBoxButtons.OK, MessageBoxIcon.Error);
                MessageBox.Show("JLink.exe的DLL版本获取失败!", "错误报告", MessageBoxButtons.OK, MessageBoxIcon.Error);

                return false;
            }
            string strDllVer = message.Substring(index + DLL_VERSION_STRING.Length, (message.IndexOf(COMPILED_STRING) - index - DLL_VERSION_STRING.Length));
            textDllVer.Text = strDllVer;
            strDllVer = System.Text.RegularExpressions.Regex.Replace(strDllVer, @"[^\d]*", "");
            int dll_ver = int.Parse(strDllVer);
            if (500 > dll_ver)
            {
                //MessageBox.Show("JLink.exe DLL version is too low!", "error report", MessageBoxButtons.OK, MessageBoxIcon.Error);
                MessageBox.Show("JLink.exe的DLL版本太低!", "错误报告", MessageBoxButtons.OK, MessageBoxIcon.Error);

                return false;
            }

            return true;
        }

        private bool JLink_hardware_version_get(string message) /* get hardware version of JLink.exe. */
        {
            int index;

            index = message.IndexOf(JLINK_HARDWARE_NEW_STRING);
            if (0 > index)
            {
                //MessageBox.Show("JLink.exe hardware version get fail,\n check if JLink is connected to PC!", "error report", MessageBoxButtons.OK, MessageBoxIcon.Error);
                MessageBox.Show("JLink.exe硬件版本获取失败,\n 检查JLink是否连接到了电脑!", "错误报告", MessageBoxButtons.OK, MessageBoxIcon.Error);

                return false;
            }
            string strJlinkHardware = message.Substring(index + JLINK_HARDWARE_NEW_STRING.Length, (message.Length - index - JLINK_HARDWARE_NEW_STRING.Length));
            index = strJlinkHardware.IndexOf("\r\n");
            strJlinkHardware = strJlinkHardware.Substring(0, index);
            textJlinkVer.Text = strJlinkHardware;
            index = message.IndexOf(FIRMWARE_STRING);
            strJlinkHardware = message.Substring(index, (message.Length - index));
            index = strJlinkHardware.IndexOf(JLINK_COMPILED_STRING);
            strJlinkHardware = strJlinkHardware.Substring(index + JLINK_COMPILED_STRING.Length, (strJlinkHardware.IndexOf("\r\n") - index - JLINK_COMPILED_STRING.Length));
            textJlinkVer.Text += strJlinkHardware;

            return true;
        }

        private bool MCU_family_get_by_JLink(string message) /* get family of the MCU which is connected to JLink. */
        {
            int index;

            index = message.IndexOf(MCU_FAMILY + " identified");
            if (0 > index)
            {
                //MessageBox.Show("no MCU / no right MCU\n is connected to JLink!", "error report", MessageBoxButtons.OK, MessageBoxIcon.Error);
                MessageBox.Show("JLink没有连接MCU!\n或者JLink没有连接正确的MCU型号!", "错误报告", MessageBoxButtons.OK, MessageBoxIcon.Error);

                return false;
            }

            return true;
        }

        private bool MCU_MAC_address_get_by_JLink(string message) /* get MAC address of the MCU which is connected to JLink. */
        {
            int index;

            string strMAC = String.Empty;
            index = message.IndexOf(MCU_MAC_ADDRESS.ToString("X8") + " = ");
            if (0 <= index)
            {
                strMAC = message.Substring(index + 11, 26);
                string[] temp = strMAC.Split(' ');
                strMAC = String.Empty;
                if (3 == temp.Length)
                {
                    foreach (string key in temp)
                    {
                        for (int i = 4; i > 0; i--)
                        {
                            strMAC += key.Substring((i - 1) * 2, 2);
                        }
                    }
                }
            }
            if (("FFFFFFFFFFFFFFFFFFFFFFFF" == strMAC) || ("000000000000000000000000" == strMAC) || (strMAC.Length != 2 * 4 * MCU_MAC_LENGTH))
            {
                strMAC = String.Empty;
            }
            textBox_uid.Text = strMAC;
            if (String.Empty == textBox_uid.Text)
            {
                //MessageBox.Show("MCU MAC address get fail!", "error report", MessageBoxButtons.OK, MessageBoxIcon.Error);
                MessageBox.Show("MCU的MAC地址获取失败!", "错误报告", MessageBoxButtons.OK, MessageBoxIcon.Error);

                return false;
            }

            return true;
        }

        private void button_color_init() /* initialize button color. */
        {
            button_burn.BackColor = System.Drawing.Color.Yellow;
            button_erase.BackColor = System.Drawing.Color.Yellow;
            button_read.BackColor = System.Drawing.Color.Yellow;
        }

        private void UpdateResultList(object str)
        {
            if (dataGridUID.InvokeRequired)
            {
                // 当一个控件的InvokeRequired属性值为真时，说明有一个创建它以外的线程想访问它
                Action<string> actionDelegate = (x) =>
                {
                    DataRow[] myRows = myTable.Select("UID ='" + str.ToString() + "'");
                    if (myRows.Length != 0)
                    {
                        this.dataGridUID.ClearSelection();
                        this.dataGridUID.Rows[myTable.Rows.IndexOf(myRows[0])].Selected = true;
                        return;
                    }
                    string[] myStringArry = new string[2];
                    myStringArry[0] = serial_number.ToString();
                    myStringArry[1] = str.ToString();
                    myTable.Rows.Add(myStringArry);
                    serial_number++;
                    textBox_serial.Text = serial_number.ToString();
                    //UpdateBarCodeText(barCode);
                };
                // 或者
                // Action<string> actionDelegate = delegate(string txt) { this.label2.Text = txt; };
                this.dataGridUID.Invoke(actionDelegate, str);
            }
            else
            {
                /* try to search for current MCU in DataTable to check if it is existed already. */
                DataRow[] myRows = myTable.Select("UID ='" + str.ToString() + "'");
                /* the MCU is existed in DataTable already. */
                if (0 != myRows.Length)
                {
                    /* selecte the MCU in visual DataTable. */
                    dataGridUID.ClearSelection();
                    dataGridUID.Rows[myTable.Rows.IndexOf(myRows[0])].Selected = true;

                    return;
                }

                /* add the new MCU into DataTable. */
                string[] myStringArry = new string[2];
                myStringArry[0] = serial_number.ToString();
                myStringArry[1] = str.ToString();
                myTable.Rows.Add(myStringArry);

                /* update serial_number for a new different MCU. */
                serial_number++;
                textBox_serial.Text = serial_number.ToString();
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            /* if firmware file is not selected.... */
            if (DialogResult.OK != openFileDialog1.ShowDialog())
            {
                return;
            }

            /* show firmware file path in TextBox. */
            textBox_path.Text = openFileDialog1.FileName;

            /* get firmware file path for further use. */
            firmware_file_path = openFileDialog1.FileName;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            bool ret = false;
            string message;
            int index;

            button_color_init();

            if (!is_JLink_exe_existed())
            {
                goto exit;
            }

            if (!is_firmware_file_existed())
            {
                goto exit;
            }

            /* start operation by JLink.exe. */
            pConsole.Start();
            StreamWriter swi = pConsole.StandardInput;
            StreamReader sro = pConsole.StandardOutput;
            swi.WriteLine("connect");
            swi.WriteLine(MCU_DEVICE.ToString());
            swi.WriteLine("S");
            swi.WriteLine(MCU_PROG_SPEED.ToString("D"));
            swi.WriteLine("mem32 0x" + MCU_MAC_ADDRESS.ToString("X8") + " " + MCU_MAC_LENGTH.ToString("D"));
            swi.WriteLine("exec device = " + MCU_DEVICE);
            swi.WriteLine("loadfile " + firmware_file_path + " 0x" + MCU_ADDRESS_OFFSET.ToString("X8"));
            swi.WriteLine("r");
            swi.WriteLine("sleep 100");
            swi.WriteLine("g");
            if (check_Fuse.Checked)
            {
                swi.WriteLine("sleep 500");
                swi.WriteLine("r");
                swi.WriteLine("sleep 100");
                swi.WriteLine("g");
                swi.WriteLine("sleep 500");
                swi.WriteLine("mem32 0x" + MCU_FLASH_OBR_REGISTER_ADDRESS.ToString("X8") + " " + "1");
            }
            swi.WriteLine("qc");
            swi.Close();
            message = sro.ReadToEnd();
            sro.Close();
            pConsole.WaitForExit();

            if (!JLink_DLL_version_get(message))
            {
                goto exit;
            }

            if (!JLink_hardware_version_get(message))
            {
                goto exit;
            }

            if (!MCU_family_get_by_JLink(message))
            {
                goto exit;
            }

            if (!MCU_MAC_address_get_by_JLink(message))
            {
                goto exit;
            }

            /* try to update visual DataTable. */
            UpdateResultList(textBox_uid.Text);

            /* check FLASH download status. */
            index = message.IndexOf("Flash download: ");
            if (message.Substring(index).Contains("Flash contents already match") || message.Substring(index).Contains("O.K."))
            {
                //MessageBox.Show("MCU program OK！", "MCU program result", MessageBoxButtons.OK, MessageBoxIcon.Information);
                MessageBox.Show("MCU烧写成功！", "MCU烧写结果", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                //MessageBox.Show("MCU program FAIL！", "MCU program result", MessageBoxButtons.OK, MessageBoxIcon.Error);
                MessageBox.Show("MCU烧写失败！", "MCU烧写结果", MessageBoxButtons.OK, MessageBoxIcon.Error);

                goto exit;
            }

            /* check if firmware is encrypted by itself. */
            if (check_Fuse.Checked) {
                /* get content of the register of the MCU which is connected to JLink. */
                index = message.IndexOf(MCU_FLASH_OBR_REGISTER_ADDRESS.ToString("X8"));
                if (0 > index)
                {
                    //MessageBox.Show("MCU firmware lock query fail!", "MCU firmware lock query result", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    MessageBox.Show("MCU固件加密查询失败!", "MCU固件加密查询结果", MessageBoxButtons.OK, MessageBoxIcon.Error);

                    goto exit;
                }
                string register_content_str = message.Substring(index + 11, 8);
                uint register_content_uint = Convert.ToUInt32(register_content_str, 16);

                /* check if read protection is enabled. */
                switch (MCU_DEVICE)
                {
                    case "STM32F103C8":
                        if (0 == (0x00000002 & register_content_uint))
                        {
                            //MessageBox.Show("firmware is not encrypted by itself,\n it is needed if product is released!", "MCU firmware lock query result", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            MessageBox.Show("固件没有加密自己,\n 如果是发布产品则需要加密!", "MCU固件加密查询结果", MessageBoxButtons.OK, MessageBoxIcon.Error);

                            goto exit;
                        }

                        break;
                    case "STM32L151C8":
                        if (0xAA == (char)register_content_uint)
                        {
                            //MessageBox.Show("firmware is not encrypted by itself,\n it is needed if product is released!", "MCU firmware lock query result", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            MessageBox.Show("固件没有加密自己,\n 如果是发布产品则需要加密!", "MCU固件加密查询结果", MessageBoxButtons.OK, MessageBoxIcon.Error);

                            goto exit;
                        }

                        break;
                    default:
                        //MessageBox.Show("MCU firmware lock query fail!", "MCU firmware lock query result", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        MessageBox.Show("MCU固件加密查询失败!", "MCU固件加密查询结果", MessageBoxButtons.OK, MessageBoxIcon.Error);

                        goto exit;
                }

                //MessageBox.Show("firmware is encrypted by itself,\n repower is needed!", "MCU firmware lock query result", MessageBoxButtons.OK, MessageBoxIcon.Information);
                MessageBox.Show("固件加密了自己,\n 重新上电生效!", "MCU固件加密查询结果", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            ret = true;
        exit:
            if (ret)
            {
                button_burn.BackColor = System.Drawing.Color.Green;
            }
            else
            {
                button_burn.BackColor = System.Drawing.Color.Red;
            }
        }

        private void button_erase_Click(object sender, EventArgs e)
        {
            bool ret = false;
            string message;
            int index;

            button_color_init();

            if (!is_JLink_exe_existed())
            {
                goto exit;
            }

            /* start operation by JLink.exe. */
            pConsole.Start();
            StreamWriter swi = pConsole.StandardInput;
            StreamReader sro = pConsole.StandardOutput;
            swi.WriteLine("connect");
            swi.WriteLine(MCU_DEVICE.ToString());
            swi.WriteLine("S");
            swi.WriteLine(MCU_PROG_SPEED.ToString("D"));
            swi.WriteLine("mem32 0x" + MCU_MAC_ADDRESS.ToString("X8") + " " + MCU_MAC_LENGTH.ToString("D"));
            swi.WriteLine("exec device = " + MCU_DEVICE);
            swi.WriteLine("erase");
            swi.WriteLine("r");
            swi.WriteLine("sleep 100");
            swi.WriteLine("g");
            swi.WriteLine("qc");
            swi.Close();
            message = sro.ReadToEnd();
            sro.Close();
            pConsole.WaitForExit();

            if (!JLink_DLL_version_get(message))
            {
                goto exit;
            }

            if (!JLink_hardware_version_get(message))
            {
                goto exit;
            }

            if (!MCU_family_get_by_JLink(message))
            {
                goto exit;
            }

            if (!MCU_MAC_address_get_by_JLink(message))
            {
                goto exit;
            }

            /* try to update visual DataTable. */
            UpdateResultList(textBox_uid.Text);

            /* show MCU erase status. */
            index = message.IndexOf("Erasing done.");
            if (0 <= index)
            {
                //MessageBox.Show("MCU erase OK！", "MCU erase result", MessageBoxButtons.OK, MessageBoxIcon.Information);
                MessageBox.Show("MCU擦除成功！", "MCU擦除结果", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                //MessageBox.Show("MCU erase FAIL！", "MCU erase result", MessageBoxButtons.OK, MessageBoxIcon.Error);
                MessageBox.Show("MCU擦除失败！", "MCU擦除结果", MessageBoxButtons.OK, MessageBoxIcon.Error);

                goto exit;
            }

            ret = true;
        exit:
            if (ret)
            {
                button_erase.BackColor = System.Drawing.Color.Green;
            }
            else
            {
                button_erase.BackColor = System.Drawing.Color.Red;
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            bool ret = false;
            string message;

            button_color_init();

            if (!is_JLink_exe_existed())
            {
                goto exit;
            }

            /* start operation by JLink.exe. */
            pConsole.Start();
            StreamWriter swi = pConsole.StandardInput;
            StreamReader sro = pConsole.StandardOutput;
            swi.WriteLine("connect");
            swi.WriteLine(MCU_DEVICE.ToString());
            swi.WriteLine("S");
            swi.WriteLine(MCU_PROG_SPEED.ToString("D"));
            swi.WriteLine("mem32 0x" + MCU_MAC_ADDRESS.ToString("X8") + " " + MCU_MAC_LENGTH.ToString("D"));
            swi.WriteLine("qc");
            swi.Close();
            message = sro.ReadToEnd();
            sro.Close();
            pConsole.WaitForExit();

            if (!JLink_DLL_version_get(message))
            {
                goto exit;
            }

            if (!JLink_hardware_version_get(message))
            {
                goto exit;
            }

            if (!MCU_family_get_by_JLink(message))
            {
                goto exit;
            }

            if (!MCU_MAC_address_get_by_JLink(message))
            {
                goto exit;
            }

            /* try to update visual DataTable. */
            UpdateResultList(textBox_uid.Text);

            ret = true;
        exit:
            if (ret)
            {
                button_read.BackColor = System.Drawing.Color.Green;
            }
            else
            {
                button_read.BackColor = System.Drawing.Color.Red;
            }
        }

        private void combo_device_SelectedIndexChanged(object sender, EventArgs e)
        {
            /* get selected MCU expressed by string. */
            MCU_DEVICE = combo_device.SelectedItem.ToString();

            /* get detailed MCU_FAMILY and MCU_MAC_ADDRESS. */
            switch (MCU_DEVICE)
            {
                case "STM32F103C8":
                    MCU_FAMILY = "Cortex-M3";
                    MCU_MAC_ADDRESS = 0x1FFFF7E8;
                    MCU_FLASH_OBR_REGISTER_ADDRESS = 0x4002201C;

                    break;
                case "STM32L151C8":
                    MCU_FAMILY = "Cortex-M3";
                    MCU_MAC_ADDRESS = 0x1FF80050;
                    MCU_FLASH_OBR_REGISTER_ADDRESS = 0x40023C1C;

                    break;
                default:
                    //MessageBox.Show("unknown MCU_DEVICE: " + MCU_DEVICE, "error report", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    MessageBox.Show("未知的MCU_DEVICE: " + MCU_DEVICE, "错误报告", MessageBoxButtons.OK, MessageBoxIcon.Error);

                    break;
            }
        }

        private void textBox_serial_TextChanged(object sender, EventArgs e)
        {
            /* get new serial number. */
            serial_number = int.Parse(textBox_serial.Text);
        }

        private void but_clear_Click(object sender, EventArgs e)
        {
            /* clear TextBox. */
            textBox_uid.Clear();
        }
    }
}
