using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Media;
using System.Collections;

namespace WavReplace
{
    public partial class M2WavReplace : Form
    {
        public byte[] head = new byte[4];
        public byte[] endAdd = new byte[4];
        public byte[] startAdd = new byte[4];
        public byte[] fileNum = new byte[4];
        public int numEndAdd;
        public int numStartAdd;
        public int numDataNum;
        public List<WavIndex> wavIndexData = new List<WavIndex>();
        public M2WavReplace()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            textBox1.Text = PickFile();
            if (textBox1.Text == "")
                return;
            listBox1.Items.Clear();
            wavIndexData.Clear();
            FileStream OriginefileStream = new FileStream(textBox1.Text, FileMode.Open);
            if (comboBox1.SelectedIndex <= 1)
                TypeADataInit(OriginefileStream);
            if (comboBox1.SelectedIndex >= 2)
                TypeBDataInit(OriginefileStream);

        }

        private void button2_Click(object sender, EventArgs e)
        {
            textBox2.Text = PickFile();
            if (textBox1.Text == "")
                return;
            listBox2.Items.Clear();
        }


        public void TypeADataInit(FileStream data)
        {
            StreamReadeTool(data , ref head, 0, 4);
            StreamReadeTool(data , ref endAdd, 4, 4);
            StreamReadeTool(data , ref startAdd, 8, 4);
            StreamReadeTool(data , ref fileNum, 12, 4);
            if (comboBox1.SelectedIndex == 0)
            {
                Array.Reverse(endAdd);
                Array.Reverse(startAdd);
                Array.Reverse(fileNum);
            }
            numEndAdd = BitConverter.ToInt32(endAdd, 0);
            numStartAdd = BitConverter.ToInt32(startAdd, 0);
            numDataNum = BitConverter.ToInt32(fileNum, 0);
            if (comboBox1.SelectedIndex == 0)
            {
                Array.Reverse(endAdd);
                Array.Reverse(startAdd);
                Array.Reverse(fileNum);
            }
            for (int i = 0; i < numDataNum; i++)
            {
                byte[] tempByteArr = new byte[4];
                WavIndex temp = new WavIndex();
                temp.dataPart1 = new byte[0x18];
                temp.fileLength = new byte[4];
                temp.dataPart2 = new byte[4];
                temp.fileStartAdd = new byte[4];
                temp.fileName = new byte[0xf0];
                int startAdd = 0x10 + 0x114 * i;
                StreamReadeTool(data , ref temp.dataPart1, startAdd, 0x18);
                StreamReadeTool(data , ref temp.fileLength, startAdd + 0x18, 4);
                StreamReadeTool(data , ref temp.dataPart2, startAdd + 0x1c, 4);
                StreamReadeTool(data , ref temp.fileStartAdd, startAdd + 0x20, 4);
                StreamReadeTool(data , ref temp.fileName, startAdd + 0x24, 0xF0);

                tempByteArr = temp.fileLength;
                if (comboBox1.SelectedIndex == 0)
                    Array.Reverse(tempByteArr);
                temp.length = BitConverter.ToInt32(tempByteArr, 0);

                tempByteArr = temp.fileStartAdd;
                if (comboBox1.SelectedIndex == 0)
                    Array.Reverse(tempByteArr);
                temp.stratAdd = BitConverter.ToInt32(tempByteArr, 0);

                temp.data = new byte[temp.length];
                try
                {
                    StreamReadeTool(data , ref temp.data, temp.stratAdd, temp.length);
                }
                catch
                {

                    data.Close();
                    MessageBox.Show("文件解析错误");
                    return;
                }

                listBox1.Items.Add(Encoding.ASCII.GetString(temp.fileName));

                wavIndexData.Add(temp);
            }
            data.Close();
        }

        public void TypeBDataInit(FileStream data)
        {
            if (comboBox1.SelectedIndex == 2)
            {
                StreamReadeTool(data , ref head, 0, 4); //从文件头获取第一个文件的起始地址，从而知晓索引结束位置
            }
            else if (comboBox1.SelectedIndex == 3)
            {
                StreamReadeTool(data , ref fileNum, 0, 4);
                StreamReadeTool(data , ref head, 4, 4);//从文件头获取第一个文件的起始地址，从而知晓索引结束位置
            }
            numEndAdd = BitConverter.ToInt32(head, 0);      //索引结束位置
            int i = 0;
            if (comboBox1.SelectedIndex == 3)
                i = 4;
            for (; i+4 < numEndAdd; i += 8)    //每次读取八字节
            {
                byte[] tempByteArr = new byte[4];
                WavIndex temp = new WavIndex();
                temp.fileStartAdd = new byte[4];
                temp.fileLength = new byte[4];
                int startAdd = i;
                StreamReadeTool(data , ref temp.fileStartAdd, startAdd, 4);
                StreamReadeTool(data , ref temp.fileLength, startAdd + 4, 4);
                temp.stratAdd = BitConverter.ToInt32(temp.fileStartAdd, 0);
                temp.length = BitConverter.ToInt32(temp.fileLength, 0);
                //if (temp.stratAdd == 0 || temp.length == 0)     //如果读到空则退出循环
                //    break;
                temp.data = new byte[temp.length];
                try
                {
                    StreamReadeTool(data , ref temp.data, temp.stratAdd, temp.length);
                }
                catch
                {

                    data.Close();
                    MessageBox.Show("文件解析错误");
                    return;
                }
                temp.fileName = Encoding.ASCII.GetBytes(Path.GetFileNameWithoutExtension(textBox1.Text)+"_"+(i/8).ToString());

                listBox1.Items.Add(Encoding.ASCII.GetString(temp.fileName));

                wavIndexData.Add(temp);
            }

            data.Close();
        }

        private void StreamReadeTool(FileStream data,ref byte[] readData ,long start,int num)
        {
            long temp = data.Position;
            data.Seek(start, SeekOrigin.Begin);
            data.Read(readData, 0, num);
            data.Seek(temp, SeekOrigin.Begin);
        }

        private static string PickFile()
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Title = "选择文件";
                openFileDialog.Filter = "所有文件 (*.*)|*.*";
                openFileDialog.FilterIndex = 1;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    // 用户选择了文件
                    return openFileDialog.FileName;
                }
            }
            // 用户没有选择文件
            return null;
        }


        private void button3_Click(object sender, EventArgs e)
        {
            if (listBox1.SelectedIndex == -1)
            {
                MessageBox.Show("未选择");
                return;

            }
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Filter = "wav files(*.wav)|*.wav|All files(*.*)|*.* ";
            dialog.FilterIndex = 1;
            dialog.FileName = listBox1.SelectedItem.ToString().Replace(".\\","");  //设置默认文件名
            dialog.RestoreDirectory = true;//保存对话框是否记忆上次打开的目录 
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                string save_filename = dialog.FileName.ToString(); //获得文件路径 
                string fileNameExt = save_filename.Substring(save_filename.LastIndexOf(@"\") + 1); //获取文件名，不带路径
                File.WriteAllBytes(save_filename, wavIndexData[listBox1.SelectedIndex].data);
            }

        }

        private void button4_Click(object sender, EventArgs e)
        {
            byte[] OriginData = File.ReadAllBytes(textBox2.Text);
            if (listBox1.SelectedIndex == -1)
            {
                MessageBox.Show("未选择");
                return;
            }
            wavIndexData[listBox1.SelectedIndex].data = OriginData;
            wavIndexData[listBox1.SelectedIndex].length = OriginData.Count();
            wavIndexData[listBox1.SelectedIndex].fileLength = BitConverter.GetBytes(OriginData.Count());
            MessageBox.Show("替换宛成");
        }

        private void button5_Click(object sender, EventArgs e)
        {
            StartAddManage();
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.FileName = Path.GetFileName(textBox1.Text);  //设置默认文件名
            dialog.RestoreDirectory = true;//保存对话框是否记忆上次打开的目录 
            string save_filename = "";
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                save_filename = dialog.FileName.ToString(); //获得文件路径 
                string fileNameExt = save_filename.Substring(save_filename.LastIndexOf(@"\") + 1); //获取文件名，不带路径
            }
            if (save_filename == "" || save_filename == null)
                return;
            FileStream fileStream = new FileStream(save_filename, FileMode.OpenOrCreate, FileAccess.ReadWrite);
            fileStream.Seek(0, SeekOrigin.Begin);
            fileStream.SetLength(0);
            byte[] toSaveData = new byte[0];
            if (comboBox1.SelectedIndex <= 1)
            {
                WriteDataInEnd(fileStream, head);
                WriteDataInEnd(fileStream, endAdd);
                WriteDataInEnd(fileStream, startAdd);
                WriteDataInEnd(fileStream, fileNum);
                for (int i = 0; i < wavIndexData.Count; i++)
                {
                    WriteDataInEnd(fileStream, wavIndexData[i].dataPart1);
                    if (comboBox1.SelectedIndex == 0)
                        Array.Reverse(wavIndexData[i].fileLength);
                    WriteDataInEnd(fileStream, wavIndexData[i].fileLength);
                    WriteDataInEnd(fileStream, wavIndexData[i].dataPart2);
                    WriteDataInEnd(fileStream, wavIndexData[i].fileStartAdd);
                    WriteDataInEnd(fileStream, wavIndexData[i].fileName);
                }
                byte[] emptyByteArr = new byte[0x14] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
                WriteDataInEnd(fileStream, emptyByteArr);
            }
            if (comboBox1.SelectedIndex >= 2)
            {
                if (comboBox1.SelectedIndex == 3)
                    WriteDataInEnd(fileStream, fileNum);
                for (int i = 0; i < wavIndexData.Count; i++)
                {
                    WriteDataInEnd(fileStream, wavIndexData[i].fileStartAdd);
                    WriteDataInEnd(fileStream, wavIndexData[i].fileLength);
                }
                if (comboBox1.SelectedIndex == 3)
                {
                    byte[] emptyByteArr = new byte[0x4] { 0, 0, 0, 0 };
                    WriteDataInEnd(fileStream, emptyByteArr);
                }
            }
            for (int i = 0; i < wavIndexData.Count; i++)
            {
                Console.WriteLine(i);
                WriteDataInEnd(fileStream, wavIndexData[i].data);
            }
            fileStream.Close();
            MessageBox.Show("保存成功库啵");
        }

        public int countDataPtr = 0;
        private void WriteDataInEnd(FileStream fileStream , byte[] BData)
        {
            fileStream.Seek(0, SeekOrigin.End);
            fileStream.Write(BData,0,BData.Length);
        }

        private void StartAddManage()
        {
            int start = 0;
            if (comboBox1.SelectedIndex <=1)
                start = 0x10 + 0x114 * wavIndexData.Count + 0x14;
            if (comboBox1.SelectedIndex >= 2)
                start = numEndAdd;
            for (int i = 0; i < wavIndexData.Count; i++)
            {
                byte[] startAdd = BitConverter.GetBytes(start);
                if (comboBox1.SelectedIndex == 0)
                    Array.Reverse(startAdd);
                if (BitConverter.ToInt32(wavIndexData[i].fileStartAdd,0)!=0)
                    wavIndexData[i].fileStartAdd = startAdd;
                start += wavIndexData[i].length;
            }
        }

        private byte[] JoinArr(byte[] a, byte[] b)
        {
            byte[] byteFinal;
            byteFinal = new byte[a.Length + b.Length];
            Array.Copy(a, 0, byteFinal, 0, a.Length);
            Array.Copy(b, 0, byteFinal, a.Length, b.Length);
            return byteFinal;
        }

        public byte[] intToBytes(int value)
        {
            byte[] src = new byte[4];
            src[3] = (byte)((value >> 24) & 0xFF);
            src[2] = (byte)((value >> 16) & 0xFF);
            src[1] = (byte)((value >> 8) & 0xFF);//高8位
            src[0] = (byte)(value & 0xFF);//低位
            return src;
        }

        private void button6_Click(object sender, EventArgs e)
        {
            string save_filename = "";
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            dialog.Description = "选择文件夹";
            dialog.ShowNewFolderButton = true;
            if (textBox1.Text != null && textBox1.Text != "")
                dialog.SelectedPath = Path.GetDirectoryName(textBox1.Text);
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                save_filename = dialog.SelectedPath; //获得文件路径 
                Console.WriteLine(save_filename);
                for(int i =0;i<listBox1.Items.Count;i++)
                {
                    File.WriteAllBytes(Path.Combine(save_filename, listBox1.Items[i].ToString()+".wav"), wavIndexData[i].data);
                }
            }
        }
        private SoundPlayer player = new SoundPlayer();
        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

            // 需要重写StreamToSoundPlayer方法，以便它从byte数组创建一个流
            try
            {
                player.Stream = new System.IO.MemoryStream(wavIndexData[listBox1.SelectedIndex].data);
                player.Play(); // 播放音乐
            }
            catch
            {
                MessageBox.Show("播放出现了问题库啵");
            }
        }

        private void button7_Click(object sender, EventArgs e)
        {
            try
            {
                Console.WriteLine();//printdata
                player.Stream = new System.IO.MemoryStream(wavIndexData[listBox1.SelectedIndex].data);
                player.Play(); // 播放音乐
            }
            catch
            {
                MessageBox.Show("播放出现了问题库啵");
            }
        }

        private void button8_Click(object sender, EventArgs e)
        {
            try
            {
                player.Stop(); // 播放音乐
            }
            catch
            {
                MessageBox.Show("播放出现了问题库啵");
            }
        }
    }

    public class WavIndex
    {
        public byte[] dataPart1;
        public byte[] fileStartAdd;
        public byte[] dataPart2;
        public byte[] fileLength;
        public byte[] fileName;
        public byte[] data;

        public int length;
        public int stratAdd;
    }
}
