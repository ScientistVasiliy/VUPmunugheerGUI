using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace VUPmunugheerGUI
{
    public partial class Form1 : Form
    {
        OpenFileDialog load_file = new OpenFileDialog();
        SaveFileDialog out_file = new SaveFileDialog();

        string root;
        uint[,] filelist = new uint[65536,2];
        ushort filecount = 0, enc_type;

        string[] cyphers = new string[3] { "Отсутствует", "Виженера", "XOR" };

        public void file_create(int num)
        {
            var file = new FileStream(root + "file" + Convert.ToChar(num + 48) + ".vup", FileMode.Create);
            byte[] name = new byte[33]{ 86, 85, 80, Convert.ToByte(num % 256), Convert.ToByte(num / 256),0,0,0,0,0,0,16,0,0,0,255,0,0,0,0,0,0,0,0,240,255,255,255,16,0,0,0,1};
            file.Write(name, 0, 33);
            file.Close();
        }

        public string get_file_name(uint start_file, uint pointer, short start_info_point = 0, uint start_i = 0)
        {
            using (var file = new FileStream(root + "file" + Convert.ToChar(start_file + 48) + ".vup", FileMode.Open, FileAccess.Read))
            using (var reader = new BinaryReader(file))
            {
                file.Seek(pointer, SeekOrigin.Begin);
                ushort info_point = (ushort)start_info_point, next_file, place_point = (ushort)(6 - start_info_point);
                uint i, next_place, place_size;
                char symbol;
                StringBuilder name = new StringBuilder();
                next_place = reader.ReadUInt32();
                next_file = reader.ReadUInt16();
                file.Seek(6, SeekOrigin.Current);
                place_size = reader.ReadUInt32();
                if (info_point != 6 && place_size < 7 - info_point)
                {
                    return get_file_name(next_file, next_place, (short)(place_size + info_point));
                }
                file.Seek(pointer + 22 - start_info_point, SeekOrigin.Begin);
                for (i = start_i; i != 256 && place_size > place_point; i++)
                {
                    place_point++;
                    symbol = reader.ReadChar();
                    if (symbol == 0) break;
                    name.Append(symbol);
                }

                if (place_size <= place_point && i != 256)
                {
                    name.Append(get_file_name(next_file, next_place, 6, i));
                }

                return name.ToString();
            }
        }

        public ulong get_file_weight(uint next_file, uint next_place)
        {
            ushort byte2, info_point = 0;
            ulong byte8, file_size = 0;
            uint byte4, i, place_size = 0;
            byte byte1;
            bool first_file = true;

            while (true)
            {
                using (var file = new FileStream(root + "file" + Convert.ToChar(next_file + 48) + ".vup", FileMode.Open, FileAccess.ReadWrite))
                using (var reader = new BinaryReader(file))
                {
                    file.Seek(next_place, SeekOrigin.Begin);
                    next_place = reader.ReadUInt32();
                    next_file = reader.ReadUInt16();
                    file.Seek(6, SeekOrigin.Current);
                    place_size = reader.ReadUInt32();
                    i = 0;
                    for (i = i; i < place_size && info_point != 5; i++)
                    {
                        if (info_point < 1)
                        {
                            enc_type = reader.ReadByte();
                            info_point = 1;
                        }
                        else if (info_point < 5)
                        {
                            byte1 = reader.ReadByte();
                            file_size += (ulong)byte1 * (ulong)Math.Pow(256, info_point - 1);
                            info_point++;
                        }
                    }
                    if (first_file)
                    {
                        first_file = false;
                        next_place = reader.ReadUInt32();
                        next_file = reader.ReadUInt16();
                    }
                }
                if (info_point == 5)
                {
                    break;
                }
            }
            return file_size;
        }

        public ushort folder_view(ushort start_file, uint pointer, ushort files = 0)
        {
            uint byte4;
            ushort byte2;

            if (!File.Exists(root + "file" + (char)(start_file + 48) + ".vup"))
            {
                file_create(start_file);
            }

            using (var file = new FileStream(root + "file" + (char)(start_file + 48) + ".vup", FileMode.Open, FileAccess.Read))
            {
                while (true)
                {
                    file.Seek(pointer, SeekOrigin.Begin);
                    byte[] byte4Array = new byte[4];
                    file.Read(byte4Array, 0, 4);
                    byte4 = BitConverter.ToUInt32(byte4Array, 0);
                    byte[] byte2Array = new byte[2];
                    file.Read(byte2Array, 0, 2);
                    byte2 = BitConverter.ToUInt16(byte2Array, 0);
                    if (byte2 == start_file)
                    {
                        if (byte4 == 0)
                        {
                            break;
                        }

                        filelist[files,0] = start_file;
                        filelist[files,1] = byte4;
                        pointer = byte4;
                        files++;
                    }
                    else
                    {
                        files = folder_view(byte2, byte4, files);
                        break;
                    }
                }
            }
            return files;
        }

        public Form1()
        {
            InitializeComponent();

            load_file.Filter = "Все файлы(*.*)|*.*";
            out_file.Filter = "Все файлы(*.*)|*.*";

            if (!File.Exists("VUPmunugheer.exe"))
            {
                MessageBox.Show("Файл VUPmunugheer.exe отсутствует!" + Environment.NewLine +  "Работа программы невозможна!", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog())
            {
                DialogResult result = folderBrowserDialog.ShowDialog();

                if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(folderBrowserDialog.SelectedPath))
                {
                    string selectedPath = folderBrowserDialog.SelectedPath;
                    root = selectedPath + "\\";
                    label1.Text = selectedPath;
                    button2.Enabled = true;
                    filecount = folder_view(0, 5);
                    treeView1.Nodes.Add(new TreeNode("root"));
                    for (int i = 0; i != filecount; i++)
                    {
                        treeView1.Nodes[0].Nodes.Add(get_file_name(filelist[i,0], filelist[i,1]));
                    }
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            uint encry_type = 0;
            string password="0000";
            if (radioButton2.Checked == true || radioButton3.Checked == true)
            {
                if (textBox1.Text == "" || textBox1.Text == "Введите пароль!")
                {
                    textBox1.Text = "Введите пароль!";
                }
                if (textBox2.Text == "" || textBox2.Text == "Введите пароль!")
                {
                    textBox2.Text = "Введите пароль!";
                }
                if (textBox1.Text != "" && textBox1.Text != "Введите пароль!" && textBox2.Text != "" && textBox2.Text != "Введите пароль!")
                {
                    if (textBox1.Text == textBox2.Text)
                    {
                        password = textBox1.Text;
                        if(radioButton2.Checked == true)
                        {
                            encry_type = 1;
                        }
                        else if (radioButton3.Checked == true)
                        {
                            encry_type = 2;
                        }
                    }
                    else
                    {
                        textBox2.Text = "Пароли не одинаковы!";
                        return;
                    }
                }
                else
                {
                    return;
                }
            }

            if (!File.Exists("VUPmunugheer.exe"))
            {
                MessageBox.Show("Файл VUPmunugheer.exe отсутствует!" + Environment.NewLine + "Работа программы невозможна!", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            DialogResult result = load_file.ShowDialog();
            if (result == DialogResult.OK && File.Exists(load_file.FileName))
            {
                Process process = new Process();
                process.StartInfo.FileName = "VUPmunugheer.exe";
                process.StartInfo.Arguments = root + " 0 \"" + load_file.FileName + "\" " + Convert.ToString(encry_type) + " " + password;
                process.Start();
                process.WaitForExit();
                int exitCode = process.ExitCode;
                process.Close();
                filecount = folder_view(0, 5);
                treeView1.Nodes.RemoveAt(0);
                treeView1.Nodes.Add(new TreeNode("root"));
                for (int i = 0; i != filecount; i++)
                {
                    treeView1.Nodes[0].Nodes.Add(get_file_name(filelist[i, 0], filelist[i, 1]));
                }
                label2.Text = "Вес: ";
                label3.Text = "Шифрование: ";
                button3.Enabled = false;
                button4.Enabled = false;
            }
        }

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (treeView1.SelectedNode != treeView1.Nodes[0])
            {
                button3.Enabled = true;
                button4.Enabled= true;
                ulong size = get_file_weight(filelist[treeView1.SelectedNode.Index, 0], filelist[treeView1.SelectedNode.Index, 1]);
                if (size < 1024)
                {
                    label2.Text = "Вес: " + Convert.ToString(size) +" байт";
                }else if (size < 1048576)
                {
                    label2.Text = "Вес: " + Convert.ToString(Convert.ToDouble(size)/1024) + " Кбайт";
                }
                else if (size < 1073741824)
                {
                    label2.Text = "Вес: " + Convert.ToString(Convert.ToDouble(size) / 1048576) + " Мбайт";
                }
                else
                {
                    label2.Text = "Вес: " + Convert.ToString(Convert.ToDouble(size) / 1073741824) + " Гбайт";
                }
                label3.Text = "Шифрование: " + cyphers[enc_type];
            }
            else
            {
                label2.Text = "Вес: ";
                label3.Text = "Шифрование: ";
                button3.Enabled = false;
                button4.Enabled= false;
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            string password = "0000";
            if (enc_type != 0)
            {
                if (textBox3.Text == "" || textBox3.Text == "Введите пароль для дешифрования!!!")
                {
                    textBox3.Text = "Введите пароль для дешифрования!!!";
                    return;
                }
                else
                {
                    password = textBox3.Text;
                }
            }

            if (!File.Exists("VUPmunugheer.exe"))
            {
                MessageBox.Show("Файл VUPmunugheer.exe отсутствует!" + Environment.NewLine + "Работа программы невозможна!", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            out_file.FileName = treeView1.SelectedNode.Text;
            DialogResult result = out_file.ShowDialog();
            if (result == DialogResult.OK)
            {
                Process process = new Process();
                process.StartInfo.FileName = "VUPmunugheer.exe";
                process.StartInfo.Arguments = root + " 1 " + Convert.ToString(treeView1.SelectedNode.Index) +" \"" + out_file.FileName + "\" " + password;
                process.Start();
                process.WaitForExit();
                int exitCode = process.ExitCode;
                process.Close();
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            if (textBox1.Text != "Введите пароль!")
            {
                textBox1.Text = textBox1.Text.Replace(" ", "");
                for (int i = 0; i < textBox1.Text.Length; i++)
                {
                    if (!((textBox1.Text[i] > 47 && textBox1.Text[i] < 58) || (textBox1.Text[i] > 96 && textBox1.Text[i] < 103) || (textBox1.Text[i] > 64 && textBox1.Text[i] < 71)))
                    {
                        textBox1.Text = textBox1.Text.Replace(Convert.ToString(Convert.ToChar(textBox1.Text[i])), "");
                    }
                }
            }
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            if (textBox2.Text != "Введите пароль!" && textBox2.Text != "Пароли не одинаковы!")
            {
                textBox2.Text = textBox2.Text.Replace(" ", "");
                for (int i = 0; i < textBox2.Text.Length; i++)
                {
                    if (!((textBox2.Text[i] > 47 && textBox2.Text[i] < 58) || (textBox2.Text[i] > 96 && textBox2.Text[i] < 103) || (textBox2.Text[i] > 64 && textBox2.Text[i] < 71)))
                    {
                        textBox2.Text = textBox2.Text.Replace(Convert.ToString(Convert.ToChar(textBox2.Text[i])), "");
                    }
                }
            }
        }

        private void textBox3_TextChanged(object sender, EventArgs e)
        {
            if (textBox3.Text != "Введите пароль для дешифрования!!!")
            {
                textBox3.Text = textBox3.Text.Replace(" ", "");
                for (int i = 0; i < textBox3.Text.Length; i++)
                {
                    if (!((textBox3.Text[i] > 47 && textBox3.Text[i] < 58) || (textBox3.Text[i] > 96 && textBox3.Text[i] < 103) || (textBox3.Text[i] > 64 && textBox3.Text[i] < 71)))
                    {
                        textBox3.Text = textBox3.Text.Replace(Convert.ToString(Convert.ToChar(textBox3.Text[i])), "");
                    }
                }
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (!File.Exists("VUPmunugheer.exe"))
            {
                MessageBox.Show("Файл VUPmunugheer.exe отсутствует!" + Environment.NewLine + "Работа программы невозможна!", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            DialogResult result = MessageBox.Show("Вы уверены?", "Предупреждение", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result == DialogResult.Yes)
            {
                Process process = new Process();
                process.StartInfo.FileName = "VUPmunugheer.exe";
                process.StartInfo.Arguments = root + " 2 " + Convert.ToString(treeView1.SelectedNode.Index);
                process.Start();
                process.WaitForExit();
                int exitCode = process.ExitCode;
                process.Close();
                filecount = folder_view(0, 5);
                treeView1.Nodes.RemoveAt(0);
                treeView1.Nodes.Add(new TreeNode("root"));
                for (int i = 0; i != filecount; i++)
                {
                    treeView1.Nodes[0].Nodes.Add(get_file_name(filelist[i, 0], filelist[i, 1]));
                }
                label2.Text = "Вес: ";
                label3.Text = "Шифрование: ";
                button3.Enabled = false;
                button4.Enabled = false;
            }
        }
    }
}
