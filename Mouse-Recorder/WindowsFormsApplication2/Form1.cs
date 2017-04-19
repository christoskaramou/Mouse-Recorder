using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Reflection;
using Gma.UserActivityMonitor;
using System.Diagnostics;

namespace WindowsFormsApplication2
{
    public partial class Form1 : Form
    {
        #region Declarations

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern void mouse_event(int dwFlags, int dx, int dy, int cButtons, int dwExtraInfo);

        public const int MOUSEEVENTF_LEFTDOWN = 0x02;
        public const int MOUSEEVENTF_LEFTUP = 0x04;
        public const int MOUSEEVENTF_RIGHTDOWN = 0x08;
        public const int MOUSEEVENTF_RIGHTUP = 0x10;

        int timeInterval = 30;
        int tickNumber = 0;
        int speed = 1;
        int timer2Tick;
        int lineCounter;
        int getTick = 0;

        bool getLMouseDown;
        bool getRMouseDown;
        bool mouseLDown = false;
        bool mouseRDown = false;
        bool mouseLAlreadyDown = false;
        bool mouseLAlreadyUp = true;
        bool mouseRAlreadyDown = false;
        bool mouseRAlreadyUp = true;
        bool f7Pressed = false;
        bool f8Pressed = false;
        bool f9Pressed = false;        
        
        string elapsedTime;
        string txtForUSe;
        string[] allLines;
        string[] txtfiles;
        string[] lastLineSplit;
        string[] splitLine = new string[4]; // this array holds the varriables of each line (the tick number, left mouse button down, right mouse button down, X, Y)

        TimeSpan ts1 = new TimeSpan();
        TimeSpan ts2 = new TimeSpan();

        Point getMousePoint = new Point();
        Point mouseMoveLocation = new Point();
        Point mouseDownLocation = new Point();
        Point mouseUpLocation = new Point();

        Stopwatch timeCounter = new Stopwatch();
        
        #endregion

        public Form1()
        {
            InitializeComponent();
            this.MaximizeBox = false;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            button3.Visible = false;
            button2.Visible = true;
            timer1.Interval = timeInterval;
            timer2.Interval = timeInterval;
            timer3.Interval = 500;  //no need to be smaller
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            string[] args = Environment.GetCommandLineArgs();

            timer3.Start();
            radioButton1.Checked = true;

            //Loading hooks, they are not all needed though
            HookManager.MouseMove += HookManager_MouseMove;
            HookManager.MouseClick += HookManager_MouseClick;
            HookManager.MouseDown += HookManager_MouseDown;
            HookManager.MouseUp += HookManager_MouseUp;
            HookManager.KeyPress += HookManager_KeyPress;
            HookManager.KeyDown += HookManager_KeyDown;
            HookManager.KeyUp += HookManager_KeyUp;

            //Loading the saved txt files or not, into comboBox1 items list
            txtfiles = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.txt");
            foreach (string a in txtfiles)
            {
                string[] splitBuffer = a.Split('\\');
                comboBox1.Items.Add(splitBuffer.Last().Replace(".txt", ""));
            }

            if (args.Length == 2)
            {
                txtForUSe = args[1];
                txtForUSe = txtForUSe.Replace("\\", "/");
                txtForUSe = txtForUSe.Replace(@"\", "/");
                txtForUSe = txtForUSe.Split('/').Last();
                txtForUSe = txtForUSe.Replace(".txt", "");
                foreach (var s in comboBox1.Items)
                {
                    if (s.ToString() == txtForUSe)
                    {
                        comboBox1.SelectedItem = s;
                        break;
                    }
                    else
                        return;
                }
                button1_Click(sender, e);
            }
        }
        
        void HookManager_KeyUp(object sender, KeyEventArgs e)   // on KeyUp, pressed keys goes to false again
        {
            f7Pressed = false;
            f8Pressed = false;
            f9Pressed = false;
        }

        void HookManager_KeyDown(object sender, KeyEventArgs e) // some key handling for using shurtcuts
        {
            if (e.KeyData == Keys.Escape)   // This is the only way to interrupt the playback
            {
                if (timer2.Enabled)
                {
                    timer2.Stop();
                    mouse_event(MOUSEEVENTF_LEFTUP, Cursor.Position.X, Cursor.Position.Y, 0, 0);    // call this in case the mouse was left pushed in the end
                }
                if (timeCounter.IsRunning) // action proccess time elapsed counter
                    timeCounter.Stop();
                e.Handled = true;   // Let the key not being handled by others
            }
            if (e.KeyData == Keys.F7 && !f7Pressed)
            {
                f7Pressed =true;
                button2_Click(sender, e);
                e.Handled = true;
            }
            if (e.KeyData == Keys.F8 && !f8Pressed)
            {
                f8Pressed=true;
                button3_Click(sender, e);
                e.Handled = true;
            }
            if (e.KeyData == Keys.F9 && !f9Pressed)
            {
                f9Pressed = true;
                button1_Click(sender, e);
                e.Handled = true;
            }
            
        }

        void HookManager_KeyPress(object sender, KeyPressEventArgs e) { }
       
        void HookManager_MouseDown(object sender, MouseEventArgs e) // we have to know when the mouse is down or up with booleans
        {
            mouseDownLocation = e.Location;
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
                mouseLDown = true;
            if (e.Button == System.Windows.Forms.MouseButtons.Right)
                mouseRDown = true; ;
        }

        void HookManager_MouseUp(object sender, MouseEventArgs e) // on MouseUp we have to set the MouseDown values to false
        {
            mouseUpLocation = e.Location;
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
                mouseLDown = false;
            if (e.Button == System.Windows.Forms.MouseButtons.Right)
                mouseRDown = false;
        }

        void HookManager_MouseClick(object sender, MouseEventArgs e) { }
       
        void HookManager_MouseMove(object sender, MouseEventArgs e) // displaying the location of the mouse
        {
            mouseMoveLocation = e.Location;
            label1.Text = mouseMoveLocation.X.ToString();
            label2.Text = mouseMoveLocation.Y.ToString();
        }

        private void button1_Click(object sender, EventArgs e) // this is the reproduction button
        {
            if (comboBox1.Text == "") // if there is no text in combobox stop button1_Click
            {
                MessageBox.Show("Choose a file or type a new");
                return;
            }
            if (File.Exists(txtForUSe + ".txt")) // if there is text in combobox1 and it exists in our files
            {
                lastLineSplit = File.ReadLines(txtForUSe +  ".txt").Last().Split(); // read the last line and split the line into an array of strings, i ll get the number of ticks occured from this
                allLines = File.ReadAllLines(txtForUSe + ".txt"); // read all file and save each line in an array of strings
                lineCounter = 0; // this is used to point the values in allLines[]
                timer2Tick = int.Parse(lastLineSplit[0]); // we get the number of ticks occured
                timer2.Interval = timeInterval / speed; // setting the speed of reproduction, timeInterval is how much time the timer uses for next tick
                timer2.Start(); // we set the timer on
                button1.Enabled = false; // avoid using the buttons and other when program runs the reproduction
                button2.Enabled = false;
                comboBox1.Enabled = false;
                groupBox1.Enabled = false;
                timeCounter.Reset(); // this is just a clock for displaying the time on screen
                timeCounter.Start();
            }
            else // if file doesnt exist (ps. this is only for the reproduction fuction)
                MessageBox.Show(this, "No data in file!", "No Data", MessageBoxButtons.OK);
        }

        private void button2_Click(object sender, EventArgs e) // this is the record button
        {
            if (comboBox1.Text == "") // if there is no text in combobox stop button2_Click
            {
                MessageBox.Show("Type or choose a file name");
                return;
            }
            if (!File.Exists(txtForUSe + ".txt")) // if there is text in combobox1 and it is not exists in our files
            {
                if (DialogResult.OK == MessageBox.Show(this, "Recording will start in " + txtForUSe + ".txt", "Recording", MessageBoxButtons.OKCancel))
                {
                    File.Create(txtForUSe +  ".txt").Close(); // create the file
                    timer1.Start(); //  start the record, timer1 is used here to save vars in this file in a sequence
                    button1.Enabled = false;
                    button2.Enabled = false;
                    button3.Enabled = true;
                    button3.Visible = true;
                    groupBox1.Enabled = false;
                    timeCounter.Reset();
                    timeCounter.Start();
                    comboBox1.Items.Add(txtForUSe + ".txt"); // add the name of the file to combobox1 items
                }
            }
            else if (File.Exists(txtForUSe + ".txt")) // if there is text in combobox1 and it exists in our files make sure it is not a misstype mistake
            {
                if (DialogResult.Yes == MessageBox.Show(this, txtForUSe +".txt has data already! Erase and rewrite?", "Attention!", MessageBoxButtons.YesNo))
                {
                    File.WriteAllText(txtForUSe + ".txt", String.Empty); // empty the file the user wants to rewrite
                    timer1.Start();
                    button1.Enabled = false;
                    button2.Enabled = false;
                    button3.Enabled = true;
                    button3.Visible = true;
                    groupBox1.Enabled = false;
                    timeCounter.Reset();
                    timeCounter.Start();
                }
            }
        }

        private void button3_Click(object sender, EventArgs e) // this button stops the timer1 which is the recorder
        {
            timer1.Stop();
            tickNumber = 0; // set the tickNumber but to 0 for later uses again
            button1.Enabled = true; // enable the buttons again
            button2.Enabled = true;
            button3.Enabled = false;
            button3.Visible = false;
            groupBox1.Enabled = true; ;
        }

        private void timer1_Tick(object sender, EventArgs e) // the recorder-timer
        {
            TextWriter writer = new StreamWriter(txtForUSe + ".txt", true); // open the file, the "true" value is to append the file because we use it multiple times
            tickNumber++; // this is the number of tick occured, i use this to know when things are happening
            if (mouseRDown && mouseLDown) // this checks if in this particular time clicks are up or down, then save them in file
                writer.WriteLine(tickNumber + " " + true + " " + true + " " + mouseMoveLocation.X + " " + mouseMoveLocation.Y);
            else if (mouseLDown && !mouseRDown)
                writer.WriteLine(tickNumber + " " + true + " " + false + " " + mouseMoveLocation.X + " " + mouseMoveLocation.Y);
            else if (!mouseLDown && mouseRDown)
                writer.WriteLine(tickNumber + " " + false + " " + true + " " + mouseMoveLocation.X + " " + mouseMoveLocation.Y);
            else
                writer.WriteLine(tickNumber + " " + false + " " + false + " " + mouseMoveLocation.X + " " + mouseMoveLocation.Y);
            writer.Close();
            ts1 = timeCounter.Elapsed; // the display-timer format and diplay
            elapsedTime = String.Format("{0:00}:{1:00}.{2:0}", ts1.TotalMinutes, ts1.Seconds, ts1.Milliseconds/10);
            label3.Text = elapsedTime.ToString();
        }

        private void timer2_Tick(object sender, EventArgs e) // the playback-timer
        {            
            if (timer2Tick > 0)
            {
                splitLine = allLines[lineCounter].Split();
                getTick = int.Parse(splitLine[0]);
                getLMouseDown = bool.Parse(splitLine[1]);
                getRMouseDown = bool.Parse(splitLine[2]);
                getMousePoint.X = int.Parse(splitLine[3]);
                getMousePoint.Y = int.Parse(splitLine[4]);

                Cursor.Position = getMousePoint;

                if (getLMouseDown)
                {
                    if (!mouseLAlreadyDown)
                    {
                        mouse_event(MOUSEEVENTF_LEFTDOWN, Cursor.Position.X, Cursor.Position.Y, 0, 0);
                        mouseLAlreadyDown = true;
                        mouseLAlreadyUp = false;
                    }
                }
                else
                {
                    if (!mouseLAlreadyUp)
                    {
                        mouse_event(MOUSEEVENTF_LEFTUP, Cursor.Position.X, Cursor.Position.Y, 0, 0);
                        mouseLAlreadyDown = false;
                        mouseLAlreadyUp = true;
                    }
                }
                if (getRMouseDown)
                {
                    if (!mouseRAlreadyDown)
                    {
                        mouse_event(MOUSEEVENTF_RIGHTDOWN, Cursor.Position.X, Cursor.Position.Y, 0, 0);
                        mouseRAlreadyDown = true;
                        mouseRAlreadyUp = false;
                    }
                }
                else
                {
                    if (!mouseRAlreadyUp)
                    {
                        mouse_event(MOUSEEVENTF_RIGHTUP, Cursor.Position.X, Cursor.Position.Y, 0, 0);
                        mouseRAlreadyDown = false;
                        mouseRAlreadyUp = true;
                    }
                }
                lineCounter++;
            }
            else
            {
                mouse_event(MOUSEEVENTF_LEFTUP, Cursor.Position.X, Cursor.Position.Y, 0, 0);
                timer2.Stop();
                timeCounter.Stop();
            }
            timer2Tick--;

            ts2 = timeCounter.Elapsed;
            elapsedTime = String.Format("{0:00}:{1:00}.{2:00}", ts2.TotalMinutes, ts2.Seconds, ts2.Milliseconds/10);
            label3.Text = elapsedTime.ToString();

        }

        private void timer3_Tick(object sender, EventArgs e) // a timer with 500ms interval used for some needs
        {
            if (!timer2.Enabled && !timer1.Enabled)
            {
                button1.Enabled = true;
                button2.Enabled = true;
                groupBox1.Enabled = true;
                comboBox1.Enabled = true;
            }
        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e) // reproduction speed multiplier
        {
            if (radioButton1.Checked)
                speed = 1;
        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton2.Checked)
                speed = 10;
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e) // get the name of the file when it is chosen
        {
            txtForUSe = comboBox1.Text;
        }

        private void ComboBox1_TextChanged(object sender, EventArgs e) // a way to take the name of the file when someone type it
        {
            txtForUSe = comboBox1.Text;
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e) // unhook
        {
            HookManager.MouseMove -= HookManager_MouseMove;
            HookManager.MouseClick -= HookManager_MouseClick;
            HookManager.MouseDown -= HookManager_MouseDown;
            HookManager.MouseUp -= HookManager_MouseUp;
            HookManager.KeyPress -= HookManager_KeyPress;
            HookManager.KeyDown -= HookManager_KeyDown;
            HookManager.KeyUp -= HookManager_KeyUp;
        }

        #region No Need To Be Shown

        private void label2_Click(object sender, EventArgs e) { }

        private void label1_Click(object sender, EventArgs e) { }

        private void label2_Click_1(object sender, EventArgs e) { }

        private void label3_Click(object sender, EventArgs e) { }

        private void groupBox1_Enter(object sender, EventArgs e) { }

        #endregion
    }
}
